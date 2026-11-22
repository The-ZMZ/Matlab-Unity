using System;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Net;
using CleanMyRoom.UI.Taste;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Tests
{
    /// <summary>
    /// Task #24 EditMode coverage: mouse-driven TASTE interaction, protocol
    /// payload mapping and authoritative feedback handling. Unity never
    /// validates an answer in these tests.
    /// </summary>
    [Category("Task24")]
    public sealed class TasteActivityTests
    {
        private const string TasteScenePath = "Assets/Scenes/TasteSorting.unity";

        [SetUp]
        public void ResetState()
        {
            GameProgress.ClearListenersForTests();
            GameProgress.ResetSessionForTests();
        }

        [TearDown]
        public void ClearState()
        {
            GameProgress.ResetSessionForTests();
            GameProgress.ClearListenersForTests();
        }

        [Test]
        public void NormalizedCoordinates_UseTopLeftOriginAndClamp()
        {
            Rect canvas = new Rect(-100f, -50f, 200f, 100f);

            Vector2 centre = TasteActivityController.ToNormalized(Vector2.zero, canvas);
            Vector2 topLeft = TasteActivityController.ToNormalized(new Vector2(-100f, 50f), canvas);
            Vector2 bottomRight = TasteActivityController.ToNormalized(new Vector2(100f, -50f), canvas);
            Vector2 outside = TasteActivityController.ToNormalized(new Vector2(500f, -500f), canvas);

            Assert.AreEqual(0.5f, centre.x, 0.0001f);
            Assert.AreEqual(0.5f, centre.y, 0.0001f);
            Assert.AreEqual(0f, topLeft.x, 0.0001f);
            Assert.AreEqual(0f, topLeft.y, 0.0001f);
            Assert.AreEqual(1f, bottomRight.x, 0.0001f);
            Assert.AreEqual(1f, bottomRight.y, 0.0001f);
            Assert.AreEqual(1f, outside.x, 0.0001f);
            Assert.AreEqual(1f, outside.y, 0.0001f);
        }

        [Test]
        public void TasteScene_HasFourZonesAndEightFoodItems()
        {
            string originalScene = SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(TasteScenePath, OpenSceneMode.Single);
            try
            {
                TasteActivityController controller =
                    UnityEngine.Object.FindFirstObjectByType<TasteActivityController>();
                Assert.IsNotNull(controller, "TasteSorting scene needs a TasteActivityController.");
                Assert.AreEqual(4, controller.DropZoneCount, "Four taste areas are required.");
                Assert.AreEqual(8, controller.FoodItemCount, "Eight food objects are required.");

                TasteDropZone[] zones = UnityEngine.Object.FindObjectsByType<TasteDropZone>(
                    FindObjectsSortMode.None);
                Assert.AreEqual(4, zones.Length);
            }
            finally
            {
                RestoreScene(originalScene);
            }
        }

        [Test]
        public void DragSelection_IsSingleAndZoneDetectionUsesLocalRectangles()
        {
            TasteActivityController controller = CreateController();

            Vector2 foodCentre = new Vector2(-270f, 100f);
            Vector2 sweetCentre = new Vector2(-270f, -180f);

            Assert.IsTrue(controller.BeginDragAt(foodCentre));
            Assert.IsNotNull(controller.DraggedItem);
            Assert.IsFalse(controller.BeginDragAt(new Vector2(100f, 100f)),
                "A second food item must not be selected during an active drag.");

            TasteDropZone zone = controller.FindZone(sweetCentre);
            Assert.IsNotNull(zone);
            Assert.AreEqual("sweet", zone.TargetId);
            Assert.IsNotNull(controller.FindZone(new Vector2(-90f, -180f)));
            Assert.IsNotNull(controller.FindZone(new Vector2(90f, -180f)));
            Assert.IsNotNull(controller.FindZone(new Vector2(270f, -180f)));
        }

        [Test]
        public void DropOutsideAnyZone_ReturnsItemHome()
        {
            TasteActivityController controller = CreateController();
            Assert.IsTrue(controller.BeginDragAt(new Vector2(-270f, 100f)));
            TasteFoodItem item = controller.DraggedItem;

            bool sent = controller.DropAt(new Vector2(0f, 250f));

            Assert.IsFalse(sent);
            Assert.IsNull(controller.DraggedItem);
            Assert.AreEqual(item.HomePosition, item.RectTransform.anchoredPosition);
        }

        [Test]
        public void CorrectResponse_MarksItemOnceAndNeverCompletesLocally()
        {
            TasteActivityController controller = CreateController();
            Assert.IsTrue(controller.BeginDragAt(new Vector2(-270f, 100f)));
            TasteFoodItem item = controller.DraggedItem;
            controller.RegisterPending("req_000001", item);

            WireMessage message = new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                activity = ActivityIds.Taste,
                requestId = "req_000001",
                valid = true,
                correct = true,
                message = "Correct placement"
            };

            controller.HandleResponse(message);

            Assert.AreEqual(1, controller.DisplayedCompletedCount);
            Assert.IsTrue(item.IsCompleted);
            Assert.IsFalse(GameProgress.IsCompleted(ActivityIds.Taste),
                "Unity must not complete an activity locally.");
        }

        [Test]
        public void IncorrectResponse_ReturnsItemAndDoesNotMarkIt()
        {
            TasteActivityController controller = CreateController();
            Assert.IsTrue(controller.BeginDragAt(new Vector2(-270f, 100f)));
            TasteFoodItem item = controller.DraggedItem;
            controller.RegisterPending("req_000002", item);

            controller.HandleResponse(new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                activity = ActivityIds.Taste,
                requestId = "req_000002",
                valid = true,
                correct = false,
                message = "Try again"
            });

            Assert.AreEqual(0, controller.DisplayedCompletedCount);
            Assert.IsFalse(item.IsCompleted);
            Assert.AreEqual(item.HomePosition, item.RectTransform.anchoredPosition);
        }

        [Test]
        public void RepeatedResponse_IsIgnored()
        {
            TasteActivityController controller = CreateController();
            Assert.IsTrue(controller.BeginDragAt(new Vector2(-270f, 100f)));
            TasteFoodItem item = controller.DraggedItem;
            string requestId = "req_000003";
            controller.RegisterPending(requestId, item);

            WireMessage message = new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                activity = ActivityIds.Taste,
                requestId = requestId,
                valid = true,
                correct = true
            };

            controller.HandleResponse(message);
            controller.HandleResponse(message);
            controller.HandleResponse(message);

            Assert.AreEqual(1, controller.DisplayedCompletedCount);
            Assert.AreEqual(0, controller.PendingCount);
        }

        private static TasteActivityController CreateController()
        {
            GameObject canvasObject = new GameObject("TestCanvas",
                typeof(RectTransform), typeof(Canvas));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(800f, 600f);

            string[] zoneIds = { "sweet", "sour", "bitter", "salty" };
            float[] zoneX = { -270f, -90f, 90f, 270f };
            TasteDropZone[] zones = new TasteDropZone[zoneIds.Length];
            for (int i = 0; i < zoneIds.Length; i++)
            {
                GameObject zoneObject = new GameObject("Zone_" + zoneIds[i],
                    typeof(RectTransform), typeof(TasteDropZone));
                zoneObject.transform.SetParent(canvasObject.transform, false);
                RectTransform rect = zoneObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(150f, 120f);
                rect.anchoredPosition = new Vector2(zoneX[i], -180f);
                zones[i] = zoneObject.GetComponent<TasteDropZone>();
                zones[i].Configure(zoneIds[i], null);
            }

            string[] foodIds =
            {
                "food_honey", "food_banana", "food_lemon", "food_green_apple",
                "food_coffee", "food_cocoa", "food_olives", "food_salt"
            };
            TasteFoodItem[] items = new TasteFoodItem[foodIds.Length];
            for (int i = 0; i < foodIds.Length; i++)
            {
                GameObject itemObject = new GameObject("Food_" + foodIds[i],
                    typeof(RectTransform), typeof(TasteFoodItem));
                itemObject.transform.SetParent(canvasObject.transform, false);
                RectTransform rect = itemObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(120f, 60f);
                float x = -270f + (i % 4) * 180f;
                float y = i < 4 ? 100f : 20f;
                rect.anchoredPosition = new Vector2(x, y);
                items[i] = itemObject.GetComponent<TasteFoodItem>();
                items[i].Configure(foodIds[i], foodIds[i], null, null);
            }

            GameObject controllerObject = new GameObject("TasteController",
                typeof(TasteActivityController));
            TasteActivityController controller =
                controllerObject.GetComponent<TasteActivityController>();
            controller.Configure(canvasRect, items, zones, null, null);
            return controller;
        }

        private static void RestoreScene(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            else
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }
    }
}
