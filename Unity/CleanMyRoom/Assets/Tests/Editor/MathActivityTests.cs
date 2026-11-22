using System;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Net;
using CleanMyRoom.UI.Math;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Tests
{
    /// <summary>
    /// Task #25 EditMode coverage: mouse-driven MATH interaction, protocol
    /// payload mapping, dot rendering data and authoritative feedback handling.
    /// Unity never validates an answer in these tests.
    /// </summary>
    [Category("Task25")]
    public sealed class MathActivityTests
    {
        private const string MathScenePath = "Assets/Scenes/MathActivity.unity";

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

            Vector2 centre = MathActivityController.ToNormalized(Vector2.zero, canvas);
            Vector2 topLeft = MathActivityController.ToNormalized(new Vector2(-100f, 50f), canvas);
            Vector2 bottomRight = MathActivityController.ToNormalized(new Vector2(100f, -50f), canvas);
            Vector2 outside = MathActivityController.ToNormalized(new Vector2(500f, -500f), canvas);

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
        public void MathScene_HasTenZonesAndTenNumberCards()
        {
            string originalScene = SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(MathScenePath, OpenSceneMode.Single);
            try
            {
                MathActivityController controller =
                    UnityEngine.Object.FindFirstObjectByType<MathActivityController>();
                Assert.IsNotNull(controller, "MathActivity scene needs a MathActivityController.");
                Assert.AreEqual(10, controller.DotZoneCount, "Ten dot groups are required.");
                Assert.AreEqual(10, controller.NumberCardCount, "Ten number cards are required.");

                DotGroupZone[] zones = UnityEngine.Object.FindObjectsByType<DotGroupZone>(
                    FindObjectsSortMode.None);
                Assert.AreEqual(10, zones.Length);

                bool[] seen = new bool[11];
                for (int i = 0; i < zones.Length; i++)
                {
                    int count = zones[i].DotCount;
                    Assert.GreaterOrEqual(count, 1, "Dot count must be parsed from the target id.");
                    Assert.LessOrEqual(count, 10);
                    Assert.IsFalse(seen[count], "Each dot group must be unique.");
                    seen[count] = true;
                }
                for (int n = 1; n <= 10; n++)
                {
                    Assert.IsTrue(seen[n], "Dot group " + n + " is missing.");
                }
            }
            finally
            {
                RestoreScene(originalScene);
            }
        }

        [Test]
        public void DotGroupZone_ParsesCountFromTargetId()
        {
            GameObject zoneObject = new GameObject("Zone", typeof(RectTransform), typeof(DotGroupZone));
            DotGroupZone zone = zoneObject.GetComponent<DotGroupZone>();

            zone.Configure("dots_7", null);
            Assert.AreEqual(7, zone.DotCount);
            Assert.AreEqual(0, zone.RenderedDotCount, "No container means no rendering.");

            zone.Configure("dots_1", null);
            Assert.AreEqual(1, zone.DotCount);

            zone.Configure("dots_11", null);
            Assert.AreEqual(0, zone.DotCount, "Only counts 1-10 are supported.");

            zone.Configure("invalid", null);
            Assert.AreEqual(0, zone.DotCount);
        }

        [Test]
        public void DragSelection_IsSingleAndZoneDetectionUsesLocalRectangles()
        {
            MathActivityController controller = CreateController();

            Vector2 cardCentre = new Vector2(-320f, 240f);
            Assert.IsTrue(controller.BeginDragAt(cardCentre));
            Assert.IsNotNull(controller.DraggedCard);
            Assert.IsFalse(controller.BeginDragAt(new Vector2(100f, 240f)),
                "A second card must not be selected during an active drag.");

            Assert.AreEqual("dots_1", controller.FindZone(new Vector2(-320f, -20f)).TargetId);
            Assert.AreEqual("dots_5", controller.FindZone(new Vector2(320f, -20f)).TargetId);
            Assert.AreEqual("dots_6", controller.FindZone(new Vector2(-320f, -180f)).TargetId);
            Assert.AreEqual("dots_10", controller.FindZone(new Vector2(320f, -180f)).TargetId);
        }

        [Test]
        public void DropOutsideAnyZone_ReturnsCardHome()
        {
            MathActivityController controller = CreateController();
            Assert.IsTrue(controller.BeginDragAt(new Vector2(-320f, 240f)));
            NumberCard card = controller.DraggedCard;

            bool sent = controller.DropAt(new Vector2(0f, 320f));

            Assert.IsFalse(sent);
            Assert.IsNull(controller.DraggedCard);
            Assert.AreEqual(card.HomePosition, card.RectTransform.anchoredPosition);
        }

        [Test]
        public void CorrectResponse_MarksCardOnceAndNeverCompletesLocally()
        {
            MathActivityController controller = CreateController();
            Assert.IsTrue(controller.BeginDragAt(new Vector2(-320f, 240f)));
            NumberCard card = controller.DraggedCard;
            controller.RegisterPending("req_000001", card);

            WireMessage message = new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                activity = ActivityIds.Math,
                requestId = "req_000001",
                valid = true,
                correct = true,
                message = "Correct placement"
            };

            controller.HandleResponse(message);

            Assert.AreEqual(1, controller.DisplayedCompletedCount);
            Assert.IsTrue(card.IsCompleted);
            Assert.IsFalse(GameProgress.IsCompleted(ActivityIds.Math),
                "Unity must not complete an activity locally.");
        }

        [Test]
        public void IncorrectResponse_ReturnsCardAndDoesNotMarkIt()
        {
            MathActivityController controller = CreateController();
            Assert.IsTrue(controller.BeginDragAt(new Vector2(-320f, 240f)));
            NumberCard card = controller.DraggedCard;
            controller.RegisterPending("req_000002", card);

            controller.HandleResponse(new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                activity = ActivityIds.Math,
                requestId = "req_000002",
                valid = true,
                correct = false,
                message = "Try again"
            });

            Assert.AreEqual(0, controller.DisplayedCompletedCount);
            Assert.IsFalse(card.IsCompleted);
            Assert.AreEqual(card.HomePosition, card.RectTransform.anchoredPosition);
        }

        [Test]
        public void RepeatedResponse_IsIgnored()
        {
            MathActivityController controller = CreateController();
            Assert.IsTrue(controller.BeginDragAt(new Vector2(-320f, 240f)));
            NumberCard card = controller.DraggedCard;
            string requestId = "req_000003";
            controller.RegisterPending(requestId, card);

            WireMessage message = new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                activity = ActivityIds.Math,
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

        private static MathActivityController CreateController()
        {
            GameObject canvasObject = new GameObject("TestCanvas",
                typeof(RectTransform), typeof(Canvas));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1000f, 700f);

            float[] columnX = { -320f, -160f, 0f, 160f, 320f };
            float[] rowY = { -20f, -180f };
            DotGroupZone[] zones = new DotGroupZone[10];
            for (int i = 0; i < zones.Length; i++)
            {
                int row = i / 5;
                int column = i % 5;
                GameObject zoneObject = new GameObject("Zone_dots_" + (i + 1),
                    typeof(RectTransform), typeof(DotGroupZone));
                zoneObject.transform.SetParent(canvasObject.transform, false);
                RectTransform rect = zoneObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(120f, 100f);
                rect.anchoredPosition = new Vector2(columnX[column], rowY[row]);
                zones[i] = zoneObject.GetComponent<DotGroupZone>();
                zones[i].Configure("dots_" + (i + 1), null);
            }

            NumberCard[] cards = new NumberCard[10];
            for (int i = 0; i < cards.Length; i++)
            {
                int row = i / 5;
                int column = i % 5;
                GameObject cardObject = new GameObject("Number_" + (i + 1),
                    typeof(RectTransform), typeof(NumberCard));
                cardObject.transform.SetParent(canvasObject.transform, false);
                RectTransform rect = cardObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(100f, 50f);
                rect.anchoredPosition = new Vector2(columnX[column], 240f - row * 80f);
                cards[i] = cardObject.GetComponent<NumberCard>();
                cards[i].Configure("number_" + (i + 1), (i + 1).ToString(), null, null);
            }

            GameObject controllerObject = new GameObject("MathController",
                typeof(MathActivityController));
            MathActivityController controller =
                controllerObject.GetComponent<MathActivityController>();
            controller.Configure(canvasRect, cards, zones, null);
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
