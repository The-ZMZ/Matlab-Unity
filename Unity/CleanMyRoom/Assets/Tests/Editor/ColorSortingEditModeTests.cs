using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CleanMyRoom.ColorSorting;
using CleanMyRoom.Core;
using CleanMyRoom.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CleanMyRoom.Tests.Issue022
{
    /// <summary>
    /// EditMode coverage for Issue #022.
    ///
    /// These tests inspect the presentation catalogue, the state machine that
    /// mirrors MATLAB replies, the MATLAB-owned answer-key file and the generated
    /// scene assets. They never decide whether an object belongs in a basket:
    /// every correct/incorrect flag in this file is injected as a MATLAB message.
    /// </summary>
    public sealed class ColorSortingEditModeTests
    {
        [Test]
        public void Catalog_HasExpectedCountsAndUniqueStableIds()
        {
            Assert.AreEqual(ColorSortingCatalog.BasketCount, ColorSortingCatalog.AllBaskets.Length);
            Assert.AreEqual(ColorSortingCatalog.ObjectCount, ColorSortingCatalog.AllObjects.Length);

            HashSet<string> basketIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ColorSortingCatalog.AllBaskets.Length; i++)
            {
                ColorSortingCatalog.BasketInfo basket = ColorSortingCatalog.AllBaskets[i];
                Assert.IsFalse(basketIds.Contains(basket.Id), "Duplicate basket id: " + basket.Id);
                basketIds.Add(basket.Id);
                Assert.IsFalse(string.IsNullOrEmpty(basket.Label), "Basket has no label: " + basket.Id);
            }

            HashSet<string> objectIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ColorSortingCatalog.AllObjects.Length; i++)
            {
                ColorSortingCatalog.ObjectInfo item = ColorSortingCatalog.AllObjects[i];
                Assert.IsFalse(objectIds.Contains(item.Id), "Duplicate object id: " + item.Id);
                objectIds.Add(item.Id);
                Assert.IsFalse(string.IsNullOrEmpty(item.Label), "Toy has no label: " + item.Id);
                Assert.IsNotNull(ColorSortingCatalog.GetBasket(item.ColorId),
                    "Toy references an unknown presentation basket: " + item.Id);
            }
        }

        [Test]
        public void MatlabAnswerKeyFile_DefinesEverySceneObject()
        {
            string path = Path.Combine(RepositoryRoot(), "Shared_Data", "ActivityData",
                "color_sorting.json");
            Assert.IsTrue(File.Exists(path), "MATLAB activity definition is missing: " + path);

            MatlabDefinition definition = JsonUtility.FromJson<MatlabDefinition>(
                File.ReadAllText(path));
            Assert.IsNotNull(definition, "Could not parse MATLAB activity definition.");
            Assert.AreEqual(ColorSortingCatalog.ActivityId, definition.id);
            Assert.IsNotNull(definition.objects);
            Assert.IsNotNull(definition.targets);
            Assert.AreEqual(ColorSortingCatalog.ObjectCount, definition.objects.Length);
            Assert.AreEqual(ColorSortingCatalog.BasketCount, definition.targets.Length);

            Dictionary<string, string> targetsById = new Dictionary<string, string>(
                StringComparer.Ordinal);
            for (int i = 0; i < definition.objects.Length; i++)
            {
                targetsById[definition.objects[i].id] = definition.objects[i].target;
            }

            for (int i = 0; i < ColorSortingCatalog.AllObjects.Length; i++)
            {
                ColorSortingCatalog.ObjectInfo item = ColorSortingCatalog.AllObjects[i];
                Assert.IsTrue(targetsById.ContainsKey(item.Id),
                    "MATLAB answer key has no entry for " + item.Id);
                Assert.AreEqual(item.ColorId, targetsById[item.Id],
                    "MATLAB answer key and scene presentation disagree for " + item.Id);
            }
        }

        [Test]
        public void CorrectPlaceResponse_ConfirmsObjectAndMirrorsMatlabScore()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            Assert.IsTrue(progress.RegisterPending("req_ok", "object_01", "basket_red", 0f));

            SortingEvent result = progress.HandlePlaceResponse(
                PlaceResponse("req_ok", true, true, 1));

            Assert.AreEqual(SortingOutcome.Accepted, result.Outcome);
            Assert.IsTrue(progress.IsObjectConfirmed("object_01"));
            Assert.AreEqual(1, progress.CorrectlyPlacedCount,
                "PLACE_RES.score is MATLAB's reported running count, not a local judgement.");
            Assert.AreEqual(0, progress.PendingCount);
        }

        [Test]
        public void IncorrectPlaceResponse_RejectsObjectWithoutConfirmingIt()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_no", "object_03", "basket_red", 0f);

            SortingEvent result = progress.HandlePlaceResponse(
                PlaceResponse("req_no", true, false, 0));

            Assert.AreEqual(SortingOutcome.Rejected, result.Outcome);
            Assert.IsFalse(progress.IsObjectConfirmed("object_03"));
            Assert.AreEqual(0, progress.CorrectlyPlacedCount);
            Assert.IsTrue(progress.CanStartPlacement("object_03"));
        }

        [Test]
        public void InvalidMatlabResponse_DoesNotConfirmObject()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_bad", "object_05", "basket_green", 0f);

            SortingEvent result = progress.HandlePlaceResponse(
                PlaceResponse("req_bad", false, true, 1));

            Assert.AreEqual(SortingOutcome.InvalidResponse, result.Outcome);
            Assert.IsFalse(progress.IsObjectConfirmed("object_05"));
            Assert.AreEqual(0, progress.CorrectlyPlacedCount,
                "An invalid MATLAB reply must not supply a score.");
            Assert.AreEqual(0, progress.PendingCount);
        }

        [Test]
        public void DuplicatePlaceResponse_IsIgnored()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_dup", "object_07", "basket_yellow", 0f);
            WireMessage response = PlaceResponse("req_dup", true, true, 1);

            Assert.AreEqual(SortingOutcome.Accepted,
                progress.HandlePlaceResponse(response).Outcome);
            SortingEvent duplicate = progress.HandlePlaceResponse(response);

            Assert.IsFalse(duplicate.Handled);
            Assert.AreEqual(SortingOutcome.None, duplicate.Outcome);
            Assert.AreEqual(1, CountConfirmedObjects(progress));
        }

        [Test]
        public void Completion_RequiresMatlabScoreConfirmedObjectsAndCompletedFlag()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            int completionCount = 0;
            int completedStars = -1;
            progress.CompletionAchieved += stars =>
            {
                completionCount++;
                completedStars = stars;
            };

            ConfirmAllObjects(progress);
            Assert.AreEqual(ColorSortingCatalog.ObjectCount, CountConfirmedObjects(progress));

            string finalRequest = "req_object_12";
            SortingEvent partial = progress.HandleActivityResult(
                ActivityResult(finalRequest, 11, 0, false));
            Assert.AreEqual(SortingOutcome.ScoreUpdated, partial.Outcome);
            Assert.IsFalse(progress.IsComplete);

            SortingEvent complete = progress.HandleActivityResult(
                ActivityResult(finalRequest, ColorSortingCatalog.ObjectCount, 1, true));
            Assert.AreEqual(SortingOutcome.Completed, complete.Outcome);
            Assert.IsTrue(progress.IsComplete);
            Assert.AreEqual(1, completionCount);
            Assert.AreEqual(1, completedStars);

            SortingEvent repeated = progress.HandleActivityResult(
                ActivityResult(finalRequest, ColorSortingCatalog.ObjectCount, 1, true));
            Assert.IsFalse(repeated.Handled);
            Assert.AreEqual(1, completionCount);
        }

        [Test]
        public void GameProgress_OnlyMirrorsMatlabStars()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            WireMessage message = new WireMessage
            {
                type = ProtocolConstants.TypeGameProgress,
                stars = 2,
                completedActivities = new[] { "COLOR" },
                gameCompleted = false
            };

            SortingEvent result = progress.HandleGameProgress(message);

            Assert.AreEqual(SortingOutcome.ScoreUpdated, result.Outcome);
            Assert.AreEqual(2, progress.TotalStars);
            Assert.IsFalse(progress.IsComplete,
                "GAME_PROGRESS alone must not complete the Color Sorting activity.");
        }

        [Test]
        public void TimeoutAndDisconnect_ClearPendingPlacementsWithoutConfirming()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_slow", "object_09", "basket_orange", 0f);

            SortingEvent[] timedOut = progress.CheckTimeouts(8.1f, 8f);
            Assert.AreEqual(1, timedOut.Length);
            Assert.AreEqual(SortingOutcome.Timeout, timedOut[0].Outcome);
            Assert.IsFalse(progress.IsObjectConfirmed("object_09"));
            Assert.AreEqual(0, progress.PendingCount);

            progress.RegisterPending("req_drop", "object_10", "basket_orange", 20f);
            SortingEvent[] cancelled = progress.CancelAllPending(
                SortingOutcome.Disconnected, "MATLAB disconnected.");
            Assert.AreEqual(1, cancelled.Length);
            Assert.AreEqual(SortingOutcome.Disconnected, cancelled[0].Outcome);
            Assert.IsFalse(progress.IsObjectConfirmed("object_10"));
            Assert.AreEqual(0, progress.PendingCount);
        }

        [Test]
        public void MatlabError_ClearsPendingPlacementWithoutConfirming()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_error", "object_11", "basket_purple", 0f);

            SortingEvent result = progress.HandleError(new WireMessage
            {
                type = ProtocolConstants.TypeError,
                requestId = "req_error",
                code = "INVALID_REQUEST",
                message = "MATLAB could not check this placement."
            });

            Assert.AreEqual(SortingOutcome.ProtocolError, result.Outcome);
            Assert.IsFalse(progress.IsObjectConfirmed("object_11"));
            Assert.AreEqual(0, progress.PendingCount);
        }

        [Test]
        public void GameManager_MirrorsOnlyMatlabReportedProgress()
        {
            GameManager manager = GameManager.Instance;
            bool created = false;
            if (manager == null)
            {
                GameObject root = new GameObject("Game Manager (Issue022 EditMode test)");
                manager = root.AddComponent<GameManager>();
                created = true;
            }

            manager.ResetProgress();
            const string activityId = "COLOR_EDITMODE_MIRROR";
            int eventCount = 0;
            int eventStars = -1;
            Action<string, int> handler = (id, stars) =>
            {
                if (string.Equals(id, activityId, StringComparison.Ordinal))
                {
                    eventCount++;
                    eventStars = stars;
                }
            };
            manager.ActivityCompletedFromMatlab += handler;

            try
            {
                manager.ApplyMatlabActivityResult(activityId, false, 2);
                Assert.AreEqual(2, manager.TotalStars);
                Assert.IsFalse(manager.IsActivityComplete(activityId));

                manager.ApplyMatlabActivityResult(activityId, true, 3);
                Assert.IsTrue(manager.IsActivityComplete(activityId));
                Assert.AreEqual(3, manager.TotalStars);
                Assert.AreEqual(1, eventCount);
                Assert.AreEqual(3, eventStars);

                manager.ApplyMatlabActivityResult(activityId, true, 4);
                Assert.AreEqual(1, eventCount, "A repeated MATLAB completion must not fire twice.");

                manager.ApplyMatlabGameProgress(5, new[] { "COLOR" }, true);
                Assert.AreEqual(5, manager.TotalStars);
                Assert.IsTrue(manager.GameCompleted);
                Assert.IsTrue(manager.IsActivityComplete("COLOR"));
            }
            finally
            {
                manager.ActivityCompletedFromMatlab -= handler;
                manager.ResetProgress();
                if (created)
                {
                    MethodInfo unregister = typeof(GameManager).GetMethod(
                        "Unregister", BindingFlags.Static | BindingFlags.NonPublic);
                    if (unregister != null)
                    {
                        unregister.Invoke(null, new object[] { manager });
                    }
                    UnityEngine.Object.DestroyImmediate(manager.gameObject);
                }
            }
        }

        [Test]
        public void GeneratedSceneAndAssets_Exist()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    "Assets/Scenes/ColorSorting.unity"),
                "Run Clean My Room/Issue 022/Build Color Sorting Scene.");

            string[] prefabs =
            {
                "Assets/Prefabs/ColorSorting/PF_ColorBasket.prefab",
                "Assets/Prefabs/ColorSorting/PF_SortableToy.prefab",
                "Assets/Prefabs/ColorSorting/PF_ActivityPointer.prefab"
            };
            for (int i = 0; i < prefabs.Length; i++)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]),
                    "Missing generated prefab: " + prefabs[i]);
            }

            string[] materials =
            {
                "Assets/Materials/ColorSorting/M_ColorSorting_Board.mat",
                "Assets/Materials/ColorSorting/M_ColorSorting_Red.mat",
                "Assets/Materials/ColorSorting/M_ColorSorting_Blue.mat",
                "Assets/Materials/ColorSorting/M_ColorSorting_Green.mat",
                "Assets/Materials/ColorSorting/M_ColorSorting_Yellow.mat",
                "Assets/Materials/ColorSorting/M_ColorSorting_Orange.mat",
                "Assets/Materials/ColorSorting/M_ColorSorting_Purple.mat"
            };
            for (int i = 0; i < materials.Length; i++)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(materials[i]),
                    "Missing generated material: " + materials[i]);
            }
        }

        private static void ConfirmAllObjects(ColorSortingProgress progress)
        {
            for (int i = 1; i <= ColorSortingCatalog.ObjectCount; i++)
            {
                string objectId = "object_" + i.ToString("D2");
                string requestId = "req_" + objectId;
                Assert.IsTrue(progress.RegisterPending(requestId, objectId, "basket_red", 0f));
                Assert.AreEqual(SortingOutcome.Accepted,
                    progress.HandlePlaceResponse(PlaceResponse(requestId, true, true, i)).Outcome);
            }
        }

        private static int CountConfirmedObjects(ColorSortingProgress progress)
        {
            int count = 0;
            for (int i = 1; i <= ColorSortingCatalog.ObjectCount; i++)
            {
                if (progress.IsObjectConfirmed("object_" + i.ToString("D2")))
                {
                    count++;
                }
            }
            return count;
        }

        private static WireMessage PlaceResponse(
            string requestId, bool valid, bool correct, int score)
        {
            return new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                requestId = requestId,
                activity = ColorSortingCatalog.ActivityId,
                valid = valid,
                correct = correct,
                score = score,
                message = correct ? "Correct placement" : "Not quite - try another basket"
            };
        }

        private static WireMessage ActivityResult(
            string requestId, int score, int stars, bool completed)
        {
            return new WireMessage
            {
                type = ProtocolConstants.TypeActivityResult,
                requestId = requestId,
                activity = ColorSortingCatalog.ActivityId,
                score = score,
                stars = stars,
                completed = completed
            };
        }

        private static string RepositoryRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
        }

        [Serializable]
        private sealed class MatlabDefinition
        {
            public string id;
            public MatlabObject[] objects;
            public string[] targets;
        }

        [Serializable]
        private sealed class MatlabObject
        {
            public string id;
            public string target;
        }
    }
}
