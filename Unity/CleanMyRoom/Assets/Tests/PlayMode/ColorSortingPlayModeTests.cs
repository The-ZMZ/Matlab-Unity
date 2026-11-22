using System.Collections;
using System.Collections.Generic;
using System.Text;
using CleanMyRoom.ColorSorting;
using CleanMyRoom.Core;
using CleanMyRoom.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CleanMyRoom.Tests.PlayMode
{
    /// <summary>
    /// PlayMode verification for the playable Issue #022 scene.
    ///
    /// The tests load the real ColorSorting scene and drive the controller with
    /// a deterministic gateway plus protocol-shaped MATLAB messages. That keeps
    /// the network test separate while proving that Unity only sends requests
    /// and displays answers that MATLAB supplied.
    /// </summary>
    public sealed class ColorSortingPlayModeTests
    {
        private ColorSortingController controller;
        private FakeSortingGateway gateway;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.ColorSorting);
            yield return null;

            controller = Object.FindAnyObjectByType<ColorSortingController>();
            Assert.IsNotNull(controller, "ColorSorting scene has no ColorSortingController.");

            gateway = new FakeSortingGateway();
            controller.Gateway = gateway;

            if (GameManager.Instance == null)
            {
                GameObject root = new GameObject("Game Manager (Issue022 PlayMode test)");
                root.AddComponent<GameManager>();
                yield return null;
            }
            GameManager.Instance.ResetProgress();
        }

        [UnityTest]
        public IEnumerator LoadedScene_HasExpectedObjectsAndControllerWiring()
        {
            Assert.AreEqual(SceneNames.ColorSorting, SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Camera.main, "The activity camera is missing.");

            SortableObject[] toys = Object.FindObjectsByType<SortableObject>(
                FindObjectsSortMode.None);
            ColorBasket[] baskets = Object.FindObjectsByType<ColorBasket>(
                FindObjectsSortMode.None);

            Assert.AreEqual(ColorSortingCatalog.ObjectCount, toys.Length);
            Assert.AreEqual(ColorSortingCatalog.BasketCount, baskets.Length);
            Assert.AreEqual(ColorSortingCatalog.ObjectCount, controller.SortableObjects.Count);
            Assert.AreEqual(ColorSortingCatalog.BasketCount, controller.Baskets.Count);

            Assert.AreEqual(ColorSortingCatalog.ObjectCount, DistinctObjectIds(toys));
            Assert.AreEqual(ColorSortingCatalog.BasketCount, DistinctBasketIds(baskets));
            Assert.IsNotNull(Object.FindAnyObjectByType<ColorSortingFeedback>());
            Assert.IsNotNull(Object.FindAnyObjectByType<ActivityPointerCursor>());
            Assert.IsNotNull(Object.FindAnyObjectByType<ActivityAudioManager>());

            yield break;
        }

        [UnityTest]
        public IEnumerator CorrectMatlabReply_PlacesToyAndMirrorsReportedScore()
        {
            Assert.IsTrue(controller.TrySubmitPlacement("object_01", "basket_red", 0.4f, 0.7f));
            Assert.AreEqual(1, gateway.Sent.Count);

            FakeRequest request = gateway.Sent[0];
            Assert.AreEqual("object_01", request.ObjectId);
            Assert.AreEqual("basket_red", request.TargetId);

            controller.HandleIncomingMessage(PlaceResponse(request.Id, true, true, 1));
            SortableObject toy = FindToy("object_01");
            Assert.IsTrue(toy.IsPlaced);
            Assert.AreEqual("basket_red", toy.PlacedInBasket);
            Assert.AreEqual(1, controller.Progress.CorrectlyPlacedCount,
                "PLACE_RES.score is MATLAB's reported running count.");

            controller.HandleIncomingMessage(ActivityResult(request.Id, 1, 0, false));
            Assert.AreEqual(1, controller.Progress.CorrectlyPlacedCount);

            yield break;
        }

        [UnityTest]
        public IEnumerator IncorrectMatlabReply_ReturnsToyWithoutScoring()
        {
            Assert.IsTrue(controller.TrySubmitPlacement("object_03", "basket_red", 0.5f, 0.5f));
            FakeRequest request = gateway.Sent[0];

            SortingEvent result = controller.HandleIncomingMessage(
                PlaceResponse(request.Id, true, false, 0));

            Assert.AreEqual(SortingOutcome.Rejected, result.Outcome);
            Assert.IsFalse(FindToy("object_03").IsPlaced);
            Assert.AreEqual(0, controller.Progress.CorrectlyPlacedCount);

            yield break;
        }

        [UnityTest]
        public IEnumerator DisconnectedGateway_DoesNotSendOrValidateLocally()
        {
            gateway.ActivitiesEnabled = false;

            bool submitted = controller.TrySubmitPlacement(
                "object_05", "basket_green", 0.5f, 0.5f);

            Assert.IsFalse(submitted);
            Assert.AreEqual(0, gateway.Sent.Count);
            Assert.AreEqual(0, controller.Progress.PendingCount);
            Assert.IsFalse(FindToy("object_05").IsPlaced);

            yield break;
        }

        [UnityTest]
        public IEnumerator MatlabCompletion_FiresOnceAndMirrorsStarIntoGameManager()
        {
            string finalRequestId = string.Empty;
            for (int i = 0; i < ColorSortingCatalog.AllObjects.Length; i++)
            {
                string objectId = ColorSortingCatalog.AllObjects[i].Id;
                Assert.IsTrue(controller.TrySubmitPlacement(
                    objectId, "basket_red", 0.5f, 0.5f), "Could not submit " + objectId);

                FakeRequest request = gateway.Sent[gateway.Sent.Count - 1];
                finalRequestId = request.Id;
                SortingEvent placement = controller.HandleIncomingMessage(
                    PlaceResponse(request.Id, true, true, i + 1));
                Assert.AreEqual(SortingOutcome.Accepted, placement.Outcome);
            }

            Assert.IsFalse(controller.Progress.IsComplete,
                "Twelve PLACE_RES messages alone must not complete the activity.");

            SortingEvent completion = controller.HandleIncomingMessage(ActivityResult(
                finalRequestId, ColorSortingCatalog.ObjectCount, 1, true));

            Assert.AreEqual(SortingOutcome.Completed, completion.Outcome);
            Assert.IsTrue(controller.Progress.IsComplete);
            Assert.AreEqual(ColorSortingCatalog.ObjectCount,
                controller.Progress.CorrectlyPlacedCount);
            Assert.AreEqual(1, controller.Progress.TotalStars);
            Assert.IsTrue(GameManager.Instance.IsActivityComplete(ColorSortingCatalog.ActivityId));
            Assert.AreEqual(1, GameManager.Instance.TotalStars);

            GameObject banner = GameObject.Find("Completion_Banner");
            Assert.IsNotNull(banner);
            Assert.IsTrue(banner.activeSelf, "Completion feedback was not displayed.");

            SortingEvent repeated = controller.HandleIncomingMessage(ActivityResult(
                finalRequestId, ColorSortingCatalog.ObjectCount, 1, true));
            Assert.IsFalse(repeated.Handled);

            yield break;
        }

        [UnityTest]
        public IEnumerator EscapeOrReturnCallsExistingBedroomNavigation()
        {
            Assert.IsTrue(controller.ReturnToBedroom());
            yield return null;
            Assert.AreEqual(SceneNames.Bedroom, SceneManager.GetActiveScene().name);
        }

        private static int DistinctObjectIds(SortableObject[] toys)
        {
            HashSet<string> ids = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < toys.Length; i++)
            {
                ids.Add(toys[i].ObjectId);
            }
            return ids.Count;
        }

        private static int DistinctBasketIds(ColorBasket[] baskets)
        {
            HashSet<string> ids = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < baskets.Length; i++)
            {
                ids.Add(baskets[i].BasketId);
            }
            return ids.Count;
        }

        private static SortableObject FindToy(string objectId)
        {
            SortableObject[] toys = Object.FindObjectsByType<SortableObject>(
                FindObjectsSortMode.None);
            for (int i = 0; i < toys.Length; i++)
            {
                if (string.Equals(toys[i].ObjectId, objectId, System.StringComparison.Ordinal))
                {
                    return toys[i];
                }
            }
            return null;
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
                score = score
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

        private sealed class FakeSortingGateway : ISortingGateway
        {
            private bool activitiesEnabled = true;
            private int nextRequestId;

            public readonly List<FakeRequest> Sent = new List<FakeRequest>();

            public bool ActivitiesEnabled
            {
                get { return activitiesEnabled; }
                set { activitiesEnabled = value; }
            }

            public string SendPlacement(string objectId, string targetId, float x, float y)
            {
                if (!activitiesEnabled)
                {
                    return null;
                }

                nextRequestId++;
                StringBuilder builder = new StringBuilder("issue022_req_");
                builder.Append(nextRequestId.ToString("D3"));
                string id = builder.ToString();
                Sent.Add(new FakeRequest
                {
                    Id = id,
                    ObjectId = objectId,
                    TargetId = targetId,
                    X = x,
                    Y = y
                });
                return id;
            }
        }

        private sealed class FakeRequest
        {
            public string Id;
            public string ObjectId;
            public string TargetId;
            public float X;
            public float Y;
        }
    }
}
