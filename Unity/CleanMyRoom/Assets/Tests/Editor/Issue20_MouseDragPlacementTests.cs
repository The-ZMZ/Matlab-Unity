using System;
using System.Collections.Generic;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Gameplay.Activities;
using CleanMyRoom.Gameplay.Placement;
using CleanMyRoom.Net;
using CleanMyRoom.Player.Input;
using NUnit.Framework;
using UnityEngine;

namespace CleanMyRoom.Tests
{
    [Category("Issue20")]
    public sealed class Issue20_MouseDragPlacementTests
    {
        private GameObject root;
        private MouseDragPlacementController controller;
        private MutableMouseSource mouse;
        private StubTransport transport;
        private ActivityPlacementGateway gateway;
        private List<DragPlacementResult> results;
        private DraggableItem item;
        private DropTarget target;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("DragFixture");
            controller = root.AddComponent<MouseDragPlacementController>();
            controller.ActivityId = ActivityIds.Color;
            controller.UseScreenBoundsForNormalization = false;
            controller.ActivityCanvasRect = new Rect(0f, 0f, 500f, 500f);
            controller.ScreenRayProvider = screen =>
                new Ray(new Vector3(screen.x / 100f, 10f, screen.y / 100f), Vector3.down);

            mouse = new MutableMouseSource();
            controller.MouseSource = mouse;

            transport = new StubTransport();
            controller.Transport = transport;

            gateway = new ActivityPlacementGateway(transport, 8f, () => 0f);
            controller.Gateway = gateway;

            results = new List<DragPlacementResult>();
            controller.ResultReceived += result => results.Add(result);

            item = CreateItem("apple", new Vector3(1f, 0f, 2f));
            target = CreateTarget("basket_red", new Vector3(4f, 0f, 4f));
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PressSelectsItemAndHoldingMouseMovesIt()
        {
            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), true, true, false));
            Assert.IsTrue(controller.IsDragging);
            Assert.AreEqual(DragPlacementStatus.Started, LastResult().Status);

            Step(MouseInputSnapshot.Create(new Vector2(200f, 250f), false, true, false));

            Assert.IsTrue(controller.IsDragging);
            Assert.AreEqual(new Vector3(2f, 0f, 2.5f), item.transform.position);
        }

        [Test]
        public void ReleaseOnTargetSendsExistingPlaceRequestFieldsAndKeepsCorrectPlacement()
        {
            DropOnTarget();

            Assert.AreEqual(1, transport.Calls.Count);
            Assert.AreEqual(ActivityIds.Color, transport.Calls[0].ActivityId);
            Assert.AreEqual("apple", transport.Calls[0].ItemId);
            Assert.AreEqual("basket_red", transport.Calls[0].TargetId);
            Assert.AreEqual(0.8f, transport.Calls[0].X);
            Assert.AreEqual(0.2f, transport.Calls[0].Y);
            Assert.IsTrue(controller.HasPendingValidation);
            Assert.AreEqual(new Vector3(4f, 0f, 4f), item.transform.position);

            EmitPlaceResponse(true, "Correct placement", 1);

            Assert.IsFalse(controller.HasPendingValidation);
            Assert.AreEqual(DragPlacementStatus.CompletedCorrect, LastResult().Status);
            Assert.AreEqual(new Vector3(4f, 0f, 4f), item.transform.position);
        }

        [Test]
        public void IncorrectPlacementRestoresOriginalPose()
        {
            DropOnTarget();

            EmitPlaceResponse(false, "Incorrect placement. Try again.", 0);

            Assert.IsFalse(controller.HasPendingValidation);
            Assert.AreEqual(DragPlacementStatus.CompletedIncorrect, LastResult().Status);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), item.transform.position);
        }

        [Test]
        public void MatlabErrorRestoresOriginalPoseAndIsReportedAsRejected()
        {
            DropOnTarget();

            transport.EmitMessage(new WireMessage
            {
                type = ProtocolConstants.TypeError,
                requestId = gateway.CurrentRequestId,
                code = "UNKNOWN_OBJECT",
                message = "Unknown object 'apple'."
            });

            Assert.AreEqual(DragPlacementStatus.Rejected, LastResult().Status);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), item.transform.position);
            Assert.IsFalse(controller.HasPendingValidation);
        }

        [Test]
        public void ReleaseWithoutTargetCancelsAndRestoresOriginalPose()
        {
            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), true, true, false));
            Step(MouseInputSnapshot.Create(new Vector2(50f, 50f), false, false, true));

            Assert.AreEqual(DragPlacementStatus.Cancelled, LastResult().Status);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), item.transform.position);
            Assert.AreEqual(0, transport.Calls.Count);
        }

        [Test]
        public void SecondObjectCannotBeDraggedWhileFirstIsHeld()
        {
            DraggableItem secondItem = CreateItem("carrot", new Vector3(3f, 0f, 1f));
            Physics.SyncTransforms();

            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), true, true, false));
            Step(MouseInputSnapshot.Create(new Vector2(300f, 100f), true, true, false));

            Assert.IsTrue(controller.IsDragging);
            Assert.AreEqual(1, CountResults(DragPlacementStatus.Started));
            Assert.AreEqual(new Vector3(3f, 0f, 1f), secondItem.transform.position);
            Assert.AreEqual("apple", LastResult().ItemId);
        }

        [Test]
        public void CommunicationFailureAtReleaseRestoresOriginalPose()
        {
            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), true, true, false));
            transport.IsAvailable = false;

            Step(MouseInputSnapshot.Create(new Vector2(400f, 400f), false, false, true));

            Assert.AreEqual(DragPlacementStatus.Unavailable, LastResult().Status);
            Assert.AreEqual(1, CountResults(DragPlacementStatus.Unavailable));
            Assert.AreEqual(new Vector3(1f, 0f, 2f), item.transform.position);
            Assert.AreEqual(0, transport.Calls.Count);
        }

        [Test]
        public void DisconnectWhileDraggingCancelsAndRestoresOriginalPose()
        {
            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), true, true, false));

            transport.EmitConnection(ConnectionState.Disconnected, "MATLAB stopped responding.");

            Assert.IsFalse(controller.IsDragging);
            Assert.AreEqual(DragPlacementStatus.Cancelled, LastResult().Status);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), item.transform.position);
        }

        [Test]
        public void DisablingControllerCancelsDragAndRequiresFreshPress()
        {
            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), true, true, false));

            // Unity EditMode does not call OnDisable synchronously after
            // SetActive(false), so exercise the exact handler OnDisable uses.
            controller.HandleActivityDisabled();

            Assert.AreEqual(DragPlacementStatus.Cancelled, LastResult().Status);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), item.transform.position);

            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), true, true, false));

            Assert.IsFalse(controller.IsDragging);
            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), false, false, true));
            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), true, true, false));

            Assert.IsTrue(controller.IsDragging);
        }

        private void DropOnTarget()
        {
            Step(MouseInputSnapshot.Create(new Vector2(100f, 200f), true, true, false));
            Step(MouseInputSnapshot.Create(new Vector2(400f, 400f), false, true, false));
            Step(MouseInputSnapshot.Create(new Vector2(400f, 400f), false, false, true));
        }

        private void EmitPlaceResponse(bool correct, string message, int score)
        {
            transport.EmitMessage(new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                requestId = gateway.CurrentRequestId,
                activity = ActivityIds.Color,
                objectId = "apple",
                targetId = "basket_red",
                valid = true,
                correct = correct,
                score = score,
                message = message
            });
        }

        private void Step(MouseInputSnapshot snapshot)
        {
            mouse.Snapshot = snapshot;
            controller.ProcessFrame(snapshot);
        }

        private DraggableItem CreateItem(string itemId, Vector3 position)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = "Item_" + itemId;
            gameObject.transform.SetParent(root.transform);
            gameObject.transform.position = position;
            DraggableItem draggable = gameObject.AddComponent<DraggableItem>();
            draggable.Configure(itemId);
            return draggable;
        }

        private DropTarget CreateTarget(string targetId, Vector3 position)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = "Target_" + targetId;
            gameObject.transform.SetParent(root.transform);
            gameObject.transform.position = position;
            DropTarget dropTarget = gameObject.AddComponent<DropTarget>();
            dropTarget.Configure(targetId);
            return dropTarget;
        }

        private DragPlacementResult LastResult()
        {
            Assert.IsNotEmpty(results, "Expected at least one drag result.");
            return results[results.Count - 1];
        }

        private int CountResults(DragPlacementStatus status)
        {
            int count = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Status == status)
                {
                    count++;
                }
            }

            return count;
        }

        private sealed class MutableMouseSource : IMouseInputSource
        {
            public MutableMouseSource()
            {
                Snapshot = default;
            }

            public MouseInputSnapshot Snapshot { get; set; }

            public MouseInputSnapshot ReadSnapshot()
            {
                return Snapshot;
            }
        }

        private sealed class StubTransport : IPlacementTransport
        {
            public StubTransport()
            {
                Calls = new List<PlacementCall>();
                IsAvailable = true;
                LastError = string.Empty;
            }

            public bool IsAvailable { get; set; }

            public string LastError { get; set; }

            public List<PlacementCall> Calls { get; private set; }

            public event Action<WireMessage> MessageReceived;

            public event Action<ConnectionState, string> ConnectionChanged;

            public string SendPlaceRequest(
                string activityId,
                string objectId,
                string targetId,
                float normalizedX,
                float normalizedY)
            {
                if (!IsAvailable)
                {
                    LastError = "MATLAB is disconnected.";
                    return null;
                }

                string requestId = "req_drag_" + (Calls.Count + 1).ToString("D3");
                Calls.Add(new PlacementCall(
                    requestId, activityId, objectId, targetId, normalizedX, normalizedY));
                return requestId;
            }

            public void EmitMessage(WireMessage message)
            {
                Action<WireMessage> handler = MessageReceived;
                if (handler != null)
                {
                    handler(message);
                }
            }

            public void EmitConnection(ConnectionState state, string detail)
            {
                Action<ConnectionState, string> handler = ConnectionChanged;
                if (handler != null)
                {
                    handler(state, detail);
                }
            }
        }

        private readonly struct PlacementCall
        {
            public PlacementCall(
                string requestId,
                string activityId,
                string itemId,
                string targetId,
                float x,
                float y)
            {
                RequestId = requestId;
                ActivityId = activityId;
                ItemId = itemId;
                TargetId = targetId;
                X = x;
                Y = y;
            }

            public string RequestId { get; }

            public string ActivityId { get; }

            public string ItemId { get; }

            public string TargetId { get; }

            public float X { get; }

            public float Y { get; }
        }
    }
}
