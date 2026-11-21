using System;
using System.Collections;
using CleanMyRoom.ColorSorting;
using CleanMyRoom.Core;
using CleanMyRoom.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CleanMyRoom.Tests.ColorSorting
{
    /// <summary>
    /// Issue #022 PlayMode verification: the real scene loads through the
    /// existing SceneLoader, MATLAB protocol replies drive the activity and
    /// the existing GameManager receives the completed activity and star.
    ///
    /// A test gateway replaces the TCP client so these tests are deterministic
    /// and do not pretend that MATLAB is running. It can only send requests;
    /// every answer below is injected as a real protocol-shaped MATLAB message.
    /// </summary>
    public sealed class ColorSortingPlayModeTests
    {
        private sealed class RecordingGateway : ISortingGateway
        {
            public bool ActivitiesEnabled { get { return true; } }
            public int SentCount { get; private set; }
            public string LastObjectId { get; private set; }
            public string LastTargetId { get; private set; }

            public string SendPlacement(string objectId, string targetId, float x, float y)
            {
                SentCount++;
                LastObjectId = objectId;
                LastTargetId = targetId;
                return "test_req_" + SentCount.ToString("D3");
            }
        }

        [UnityTest]
        public IEnumerator ColorSortingLoadsWithTwelveObjectsAndSixBaskets()
        {
            yield return LoadColorSorting();

            Assert.AreEqual(SceneNames.ColorSorting, SceneManager.GetActiveScene().name);
            ColorSortingController controller = FindController();
            Assert.IsNotNull(controller);
            Assert.AreEqual(ColorSortingCatalog.ObjectCount, controller.SortableObjects.Count);
            Assert.AreEqual(ColorSortingCatalog.BasketCount, controller.Baskets.Count);
            Assert.IsNotNull(FindInActiveScene("Activity Pointer"));
            Assert.IsNotNull(FindInActiveScene("Instruction_Text"));

            foreach (SortableObject toy in controller.SortableObjects)
            {
                Assert.IsTrue(toy.GetComponent<Collider>() != null,
                    toy.ObjectId + " has no collider at runtime.");
            }
            foreach (ColorBasket basket in controller.Baskets)
            {
                Assert.IsTrue(basket.GetComponent<Collider>() != null,
                    basket.BasketId + " has no collider at runtime.");
            }
        }

        [UnityTest]
        public IEnumerator IncorrectPlacementIsRejectedThenCanBeRetried()
        {
            yield return LoadColorSorting();

            ColorSortingController controller = FindController();
            RecordingGateway gateway = new RecordingGateway();
            controller.Gateway = gateway;

            Assert.IsTrue(controller.TrySubmitPlacement("object_01", "basket_blue", 0.2f, 0.5f));
            Assert.AreEqual(1, gateway.SentCount);
            Assert.IsFalse(controller.Progress.CanStartPlacement("object_01"),
                "The object stays locked until MATLAB replies.");

            SortingEvent wrong = controller.HandleIncomingMessage(PlaceResponse(
                "test_req_001", true, false, 0, "Incorrect placement. Try again."));
            Assert.AreEqual(SortingOutcome.Rejected, wrong.Outcome);
            Assert.AreEqual(0, controller.Progress.CorrectlyPlacedCount);
            Assert.IsTrue(controller.Progress.CanStartPlacement("object_01"));

            Assert.IsTrue(controller.TrySubmitPlacement("object_01", "basket_red", 0.2f, 0.5f));
            SortingEvent right = controller.HandleIncomingMessage(PlaceResponse(
                "test_req_002", true, true, 1, "Correct placement"));
            Assert.AreEqual(SortingOutcome.Accepted, right.Outcome);
            Assert.AreEqual(1, controller.Progress.CorrectlyPlacedCount);
            Assert.AreEqual("basket_red", FindToy(controller, "object_01").PlacedInBasket);
        }

        [UnityTest]
        public IEnumerator CompletionAfterTwelveValidatedPlacementsAwardsOneStar()
        {
            yield return LoadColorSorting();

            GameManager manager = GameManager.Instance;
            Assert.IsNotNull(manager);
            manager.ResetProgress();

            ColorSortingController controller = FindController();
            RecordingGateway gateway = new RecordingGateway();
            controller.Gateway = gateway;

            string lastRequestId = string.Empty;
            for (int i = 0; i < ColorSortingCatalog.ObjectCount; i++)
            {
                ColorSortingCatalog.ObjectInfo info = ColorSortingCatalog.AllObjects[i];
                Assert.IsTrue(controller.TrySubmitPlacement(info.Id, info.ColorId, 0.5f, 0.5f),
                    "Placement " + i + " was not sent to the gateway.");
                lastRequestId = "test_req_" + (i + 1).ToString("D3");

                SortingEvent placement = controller.HandleIncomingMessage(PlaceResponse(
                    lastRequestId, true, true, i + 1, "Correct placement"));
                Assert.AreEqual(SortingOutcome.Accepted, placement.Outcome);
            }

            Assert.AreEqual(12, gateway.SentCount);
            Assert.AreEqual(12, controller.Progress.CorrectlyPlacedCount);
            Assert.IsFalse(controller.Progress.IsComplete);

            SortingEvent completion = controller.HandleIncomingMessage(ActivityResult(
                lastRequestId, true, 1));
            Assert.AreEqual(SortingOutcome.Completed, completion.Outcome);
            Assert.IsTrue(controller.Progress.IsComplete);
            Assert.IsTrue(manager.IsActivityComplete(ColorSortingCatalog.ActivityId),
                "GameManager must receive the completed activity from MATLAB.");
            Assert.AreEqual(1, manager.TotalStars, "Only one star may be awarded.");

            GameObject banner = FindInActiveScene("Completion_Banner");
            Assert.IsNotNull(banner);
            Assert.IsTrue(banner.activeSelf, "Completion banner must be visible.");

            // A duplicate result must not award a second star.
            controller.HandleIncomingMessage(ActivityResult(lastRequestId, true, 2));
            Assert.AreEqual(1, manager.TotalStars);
        }

        [UnityTest]
        public IEnumerator ReturnToBedroomUsesExistingSceneNavigation()
        {
            yield return LoadColorSorting();

            ColorSortingController controller = FindController();
            Assert.IsNotNull(controller);
            Assert.IsTrue(controller.ReturnToBedroom());
            yield return null;
            yield return null;

            Assert.AreEqual(SceneNames.Bedroom, SceneManager.GetActiveScene().name,
                "The activity must return through SceneLoader to Bedroom.");
        }

        private static IEnumerator LoadColorSorting()
        {
            // Each test must start from a fresh activity controller. SceneLoader
            // deliberately ignores a request for the already active scene, so
            // leave ColorSorting first when a previous test left it loaded.
            if (SceneManager.GetActiveScene().name == SceneNames.ColorSorting)
            {
                Assert.IsTrue(SceneLoader.LoadBedroom(),
                    "SceneLoader could not unload ColorSorting via Bedroom.");
                yield return null;
                yield return null;
            }

            Assert.IsTrue(SceneLoader.Load(SceneNames.ColorSorting),
                "SceneLoader rejected ColorSorting.");
            yield return null;
            yield return null;
        }

        private static ColorSortingController FindController()
        {
            ColorSortingController[] found =
                UnityEngine.Object.FindObjectsByType<ColorSortingController>(FindObjectsSortMode.None);
            return found.Length > 0 ? found[0] : null;
        }

        private static SortableObject FindToy(ColorSortingController controller, string objectId)
        {
            foreach (SortableObject toy in controller.SortableObjects)
            {
                if (toy.ObjectId == objectId)
                {
                    return toy;
                }
            }
            return null;
        }

        private static WireMessage PlaceResponse(
            string requestId, bool valid, bool correct, int score, string message)
        {
            return new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                requestId = requestId,
                activity = ColorSortingCatalog.ActivityId,
                valid = valid,
                correct = correct,
                score = score,
                message = message
            };
        }

        private static WireMessage ActivityResult(string requestId, bool completed, int stars)
        {
            return new WireMessage
            {
                type = ProtocolConstants.TypeActivityResult,
                requestId = requestId,
                activity = ColorSortingCatalog.ActivityId,
                completed = completed,
                stars = stars,
                score = 0,
                message = "Correct placement"
            };
        }

        private static GameObject FindInActiveScene(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                GameObject found = FindInHierarchy(root.transform, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent.gameObject;
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject found = FindInHierarchy(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
    }
}
