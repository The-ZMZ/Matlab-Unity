using System;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Net;
using CleanMyRoom.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Tests
{
    /// <summary>
    /// Issue #14 EditMode coverage for furniture interaction, deterministic
    /// prompt selection, E-key navigation, authoritative progress, stars and
    /// the guarded return-to-bedroom flow.
    /// </summary>
    [Category("Issue14")]
    public sealed class Issue14Tests : InputTestFixture
    {
        private const string BedroomScenePath = "Assets/Scenes/Bedroom.unity";
        private const string StarPrefabPath = "Assets/Prefabs/FloatingStar.prefab";

        [SetUp]
        public void ResetProgressState()
        {
            GameProgress.ClearListenersForTests();
            ActivityNavigation.ClearListenersForTests();
            GameProgress.ResetSessionForTests();
            SceneTransitionController.ResetInstanceForTests();
        }

        [TearDown]
        public void ClearProgressState()
        {
            GameProgress.ResetSessionForTests();
            GameProgress.ClearListenersForTests();
            ActivityNavigation.ClearListenersForTests();
        }

        [Test]
        public void BedroomScene_HasThreeFurnitureWithSolidAndTriggerColliders()
        {
            string originalScene = SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(BedroomScenePath, OpenSceneMode.Single);
            try
            {
                AssertFurniture("Bed", ActivityIds.Color, "Bed");
                AssertFurniture("Dining Table", ActivityIds.Taste, "Dining Table");
                AssertFurniture("Study Desk", ActivityIds.Math, "Study Desk");
            }
            finally
            {
                RestoreScene(originalScene);
            }
        }

        [Test]
        public void BedroomScene_HasReusableStarsAboveEachFurniture()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarPrefabPath);
            Assert.IsNotNull(prefab, "FloatingStar prefab is missing.");
            Assert.IsNotNull(prefab.GetComponent<FloatingStar>());
            Transform visual = prefab.transform.Find("Visual");
            Assert.IsNotNull(visual, "Star prefab needs a Visual child.");
            Assert.IsNotNull(visual.GetComponent<MeshFilter>().sharedMesh);
            Assert.IsNotNull(visual.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.IsNotNull(prefab.transform.Find("GlowLight").GetComponent<Light>());

            string originalScene = SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(BedroomScenePath, OpenSceneMode.Single);
            try
            {
                AssertStar("Bed", "Bed_Star", ActivityIds.Color);
                AssertStar("Dining Table", "DiningTable_Star", ActivityIds.Taste);
                AssertStar("Study Desk", "StudyDesk_Star", ActivityIds.Math);
            }
            finally
            {
                RestoreScene(originalScene);
            }
        }

        [Test]
        public void Prompt_AppearsForNearestTargetAndHidesOutsideRange()
        {
            GameObject bed = CreateFurniture(
                "Bed", ActivityIds.Color, "Bed", string.Empty, new Vector3(2f, 0f, 0f));
            GameObject desk = CreateFurniture(
                "Study Desk", ActivityIds.Math, "Study Desk", string.Empty, new Vector3(0f, 0f, 3f));
            InteractionController controller = CreatePlayerInteraction(Vector3.zero);

            try
            {
                Physics.SyncTransforms();
                controller.Refresh();

                Assert.AreEqual(bed.GetComponent<InteractableFurniture>(), controller.CurrentTarget);
                Assert.IsTrue(controller.IsPromptVisible);
                StringAssert.Contains("Press E", controller.PromptText);
                StringAssert.Contains("Bed", controller.PromptText);

                controller.transform.position = new Vector3(0f, 0f, 3f);
                Physics.SyncTransforms();
                controller.Refresh();

                Assert.AreEqual(desk.GetComponent<InteractableFurniture>(), controller.CurrentTarget);
                StringAssert.Contains("Study Desk", controller.PromptText);

                controller.transform.position = new Vector3(20f, 0f, 20f);
                Physics.SyncTransforms();
                controller.Refresh();

                Assert.IsFalse(controller.IsPromptVisible, "Prompt stayed visible outside range.");
                Assert.AreEqual(string.Empty, controller.PromptText);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(controller.gameObject);
                UnityEngine.Object.DestroyImmediate(desk);
                UnityEngine.Object.DestroyImmediate(bed);
            }
        }

        [Test]
        public void SelectNearest_TiesAreBrokenByActivityId()
        {
            GameObject math = CreateFurniture(
                "Study Desk", ActivityIds.Math, "Study Desk", string.Empty, new Vector3(0f, 0f, 2f));
            GameObject color = CreateFurniture(
                "Bed", ActivityIds.Color, "Bed", string.Empty, new Vector3(2f, 0f, 0f));

            try
            {
                Physics.SyncTransforms();
                InteractableFurniture mathTarget = math.GetComponent<InteractableFurniture>();
                InteractableFurniture colorTarget = color.GetComponent<InteractableFurniture>();

                Assert.AreEqual(
                    colorTarget,
                    InteractionController.SelectNearest(
                        new[] { mathTarget, colorTarget }, Vector3.zero));
                Assert.AreEqual(
                    colorTarget,
                    InteractionController.SelectNearest(
                        new[] { colorTarget, mathTarget }, Vector3.zero));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(color);
                UnityEngine.Object.DestroyImmediate(math);
            }
        }

        [Test]
        public void EKey_OpensOnlyTheSelectedFurnitureActivity()
        {
            const string colorPath = "Assets/Scenes/Activities/ColorActivity.unity";
            const string mathPath = "Assets/Scenes/Activities/MathActivity.unity";

            GameObject bed = CreateFurniture(
                "Bed", ActivityIds.Color, "Bed", colorPath, new Vector3(1f, 0f, 0f));
            GameObject desk = CreateFurniture(
                "Study Desk", ActivityIds.Math, "Study Desk", mathPath, new Vector3(0f, 0f, 6f));
            InteractionController controller = CreatePlayerInteraction(Vector3.zero);

            int calls = 0;
            string openedActivity = null;
            string openedPath = null;
            Action<string, string> handler = (activityId, scenePath) =>
            {
                calls++;
                openedActivity = activityId;
                openedPath = scenePath;
            };
            ActivityNavigation.OpenRequested += handler;

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Physics.SyncTransforms();
                controller.Refresh();

                Press(keyboard.eKey);
                Assert.IsTrue(controller.HandleInteractionInput());
                Release(keyboard.eKey);

                Assert.AreEqual(1, calls, "E must open exactly one activity.");
                Assert.AreEqual(ActivityIds.Color, openedActivity);
                Assert.AreEqual(colorPath, openedPath);
                Assert.AreNotEqual(mathPath, openedPath, "The wrong activity scene was opened.");
            }
            finally
            {
                ActivityNavigation.OpenRequested -= handler;
                UnityEngine.Object.DestroyImmediate(controller.gameObject);
                UnityEngine.Object.DestroyImmediate(desk);
                UnityEngine.Object.DestroyImmediate(bed);
            }
        }

        [Test]
        public void UnconfiguredFurniture_DoesNotRequestAnyScene()
        {
            GameObject bed = CreateFurniture(
                "Bed", ActivityIds.Color, "Bed", string.Empty, new Vector3(1f, 0f, 0f));
            InteractionController controller = CreatePlayerInteraction(Vector3.zero);

            int calls = 0;
            Action<string, string> handler = (activityId, scenePath) => calls++;
            ActivityNavigation.OpenRequested += handler;

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Physics.SyncTransforms();
                controller.Refresh();

                Press(keyboard.eKey);
                Assert.IsFalse(controller.HandleInteractionInput());
                Release(keyboard.eKey);

                Assert.AreEqual(0, calls, "An unconfigured activity must not load any scene.");
            }
            finally
            {
                ActivityNavigation.OpenRequested -= handler;
                UnityEngine.Object.DestroyImmediate(controller.gameObject);
                UnityEngine.Object.DestroyImmediate(bed);
            }
        }

        [Test]
        public void GameProgress_AcceptsOnlyAuthoritativeCompletion()
        {
            GameObject progressObject = new GameObject("TestProgress");
            GameProgress progress = progressObject.AddComponent<GameProgress>();

            int completionEvents = 0;
            Action<string> handler = activityId => completionEvents++;
            GameProgress.ActivityCompleted += handler;

            try
            {
                Assert.IsFalse(progress.ProcessMessage(new WireMessage
                {
                    type = ProtocolConstants.TypePlaceResponse,
                    activity = ActivityIds.Color,
                    completed = true
                }));

                Assert.IsFalse(progress.ProcessMessage(new WireMessage
                {
                    type = ProtocolConstants.TypeActivityResult,
                    activity = ActivityIds.Color,
                    completed = false
                }));
                Assert.IsFalse(GameProgress.IsCompleted(ActivityIds.Color));

                WireMessage successful = new WireMessage
                {
                    type = ProtocolConstants.TypeActivityResult,
                    activity = ActivityIds.Color,
                    completed = true
                };
                Assert.IsTrue(progress.ProcessMessage(successful));
                Assert.IsTrue(GameProgress.IsCompleted(ActivityIds.Color));
                Assert.AreEqual(1, completionEvents);

                Assert.IsFalse(progress.ProcessMessage(successful),
                    "Duplicate result must not award the star twice.");
                Assert.AreEqual(1, completionEvents);

                Assert.IsTrue(progress.ProcessMessage(new WireMessage
                {
                    type = ProtocolConstants.TypeGameProgress,
                    completedActivities = new[] { ActivityIds.Math }
                }));
                Assert.IsTrue(GameProgress.IsCompleted(ActivityIds.Math));
                Assert.IsFalse(GameProgress.IsCompleted(ActivityIds.Taste));
            }
            finally
            {
                GameProgress.ActivityCompleted -= handler;
                UnityEngine.Object.DestroyImmediate(progressObject);
            }
        }

        [Test]
        public void ProgressState_SurvivesSceneTransitionStyleRecreation()
        {
            GameObject first = new GameObject("FirstProgress");
            first.AddComponent<GameProgress>();
            GameProgress.TryComplete(ActivityIds.Taste);
            UnityEngine.Object.DestroyImmediate(first);

            GameObject second = new GameObject("SecondProgress");
            second.AddComponent<GameProgress>();
            try
            {
                Assert.IsTrue(GameProgress.IsCompleted(ActivityIds.Taste),
                    "Completion state was lost when the scene-owned object was recreated.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void FloatingStar_HidesOnlyForItsOwnCompletedActivity()
        {
            GameObject starObject = new GameObject("TestStar");
            FloatingStar star = starObject.AddComponent<FloatingStar>();
            star.Configure(ActivityIds.Color, starObject.transform, null);
            star.EnsureSubscribed();

            try
            {
                Assert.IsTrue(starObject.activeSelf);
                Assert.IsFalse(star.IsCompleted);

                GameProgress.TryComplete(ActivityIds.Taste);
                Assert.IsTrue(starObject.activeSelf,
                    "Completing a different activity must not hide this star.");

                GameProgress.TryComplete(ActivityIds.Color);
                Assert.IsFalse(starObject.activeSelf);
                Assert.IsTrue(star.IsCompleted);

                GameProgress.ResetSessionForTests();
                star.RefreshFromProgress();
                Assert.IsTrue(starObject.activeSelf,
                    "Incomplete stars must be visible again after a session reset.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(starObject);
            }
        }

        [Test]
        public void Completion_RequestsReturnOnlyForTheActiveActivity()
        {
            GameObject progressObject = new GameObject("TestProgress");
            progressObject.AddComponent<GameProgress>();
            GameObject transitionObject = new GameObject("TestTransitions");
            transitionObject.AddComponent<SceneTransitionController>().EnsureSubscribed();

            int returnRequests = 0;
            Action handler = () => returnRequests++;
            ActivityNavigation.ReturnToBedroomRequested += handler;

            try
            {
                GameProgress.SetActiveActivity(ActivityIds.Color);

                GameProgress.TryComplete(ActivityIds.Taste);
                Assert.AreEqual(0, returnRequests,
                    "A different activity must not trigger the return transition.");

                GameProgress.TryComplete(ActivityIds.Color);
                Assert.AreEqual(1, returnRequests,
                    "Successful completion must request a return to the bedroom.");
            }
            finally
            {
                ActivityNavigation.ReturnToBedroomRequested -= handler;
                UnityEngine.Object.DestroyImmediate(transitionObject);
                UnityEngine.Object.DestroyImmediate(progressObject);
            }
        }

        [Test]
        public void Issue13_PlayerMovementRemainsFunctional()
        {
            Vector2 diagonal = PlayerController.NormalizeForDiagonalMovement(new Vector2(1f, 1f));
            Assert.AreEqual(1f, diagonal.magnitude, 0.0001f,
                "Issue #13 diagonal normalisation regressed.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/ChildCharacter.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<PlayerController>());
            Assert.IsNotNull(prefab.GetComponent<InteractionController>(),
                "InteractionController should live on the Issue #13 player.");
        }

        private static void AssertFurniture(string objectName, string expectedActivityId, string expectedDisplay)
        {
            GameObject furnitureObject = GameObject.Find(objectName);
            Assert.IsNotNull(furnitureObject, objectName + " is missing from the bedroom.");

            InteractableFurniture furniture = furnitureObject.GetComponent<InteractableFurniture>();
            Assert.IsNotNull(furniture, objectName + " has no InteractableFurniture.");
            Assert.AreEqual(expectedActivityId, furniture.ActivityId);
            Assert.AreEqual(expectedDisplay, furniture.DisplayName);
            Assert.IsFalse(furniture.HasConfiguredActivityScene,
                "Activity scene paths must stay empty until real scenes are supplied.");
            Assert.IsNotNull(furniture.InteractionAnchor);

            Assert.IsTrue(HasCollider(furnitureObject, false),
                objectName + " needs a non-trigger collider that blocks movement.");
            Assert.IsTrue(HasCollider(furnitureObject, true),
                objectName + " needs a trigger collider for interaction discovery.");
        }

        private static bool HasCollider(GameObject target, bool trigger)
        {
            Collider[] colliders = target.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].isTrigger == trigger)
                {
                    return true;
                }
            }
            return false;
        }

        private static void AssertStar(string furnitureName, string starName, string expectedActivityId)
        {
            GameObject furniture = GameObject.Find(furnitureName);
            Assert.IsNotNull(furniture);
            GameObject star = GameObject.Find(starName);
            Assert.IsNotNull(star, starName + " is missing.");

            FloatingStar floating = star.GetComponent<FloatingStar>();
            Assert.IsNotNull(floating);
            Assert.AreEqual(expectedActivityId, floating.ActivityId);
            Assert.IsTrue(star.activeSelf, "Incomplete stars must be visible.");
            Assert.AreEqual(furniture.transform, star.transform.parent);
            Assert.Greater(star.transform.position.y, furniture.transform.position.y + 1.5f,
                "Star must float above its furniture.");
        }

        private static GameObject CreateFurniture(
            string name,
            string activityId,
            string displayName,
            string scenePath,
            Vector3 position)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            BoxCollider solid = root.AddComponent<BoxCollider>();
            solid.isTrigger = false;
            solid.center = Vector3.up * 0.5f;
            solid.size = new Vector3(1f, 1f, 1f);

            InteractableFurniture furniture = root.AddComponent<InteractableFurniture>();
            furniture.Configure(activityId, displayName, scenePath, 2.8f);
            return root;
        }

        private static InteractionController CreatePlayerInteraction(Vector3 position)
        {
            GameObject player = new GameObject("TestPlayer");
            player.transform.position = position;
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerController>();
            return player.AddComponent<InteractionController>();
        }

        private static void RestoreScene(string originalScene)
        {
            if (!string.IsNullOrEmpty(originalScene) && originalScene != BedroomScenePath)
            {
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
                return;
            }

            // The test runner can start with no scene loaded. Return to an empty
            // scene so scene-owned controllers/stars cannot leak into later tests.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
