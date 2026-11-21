using System;
using System.Collections.Generic;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Gameplay.Placement;
using CleanMyRoom.Net;
using CleanMyRoom.Player.Input;
using NUnit.Framework;
using UnityEngine;

namespace CleanMyRoom.Tests
{
    [Category("Issue19")]
    public sealed class Issue19_ActivityPlacementGatewayTests
    {
        private StubTransport transport;
        private ActivityPlacementGateway gateway;
        private List<PlacementOutcome> outcomes;

        [SetUp]
        public void SetUp()
        {
            transport = new StubTransport { IsAvailable = true, NextRequestId = "req_000001" };
            gateway = new ActivityPlacementGateway(transport, 8f, () => 0f);
            outcomes = new List<PlacementOutcome>();
            gateway.ResultReceived += outcome => outcomes.Add(outcome);
            gateway.Attach();
        }

        [TearDown]
        public void TearDown()
        {
            gateway.Detach();
        }

        [Test]
        public void NormalizeUsesTopLeftCanvasOriginAndClampsToCanvas()
        {
            Rect rect = new Rect(100f, 200f, 400f, 200f);

            Vector2 topLeft = MouseCoordinates.Normalize(new Vector2(100f, 400f), rect);
            Vector2 center = MouseCoordinates.Normalize(new Vector2(300f, 300f), rect);
            Vector2 outside = MouseCoordinates.Normalize(new Vector2(-50f, 50f), rect);

            Assert.AreEqual(new Vector2(0f, 0f), topLeft);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), center);
            Assert.AreEqual(new Vector2(0f, 1f), outside);
        }

        [Test]
        public void NormalizeReturnsZeroForDegenerateRect()
        {
            Assert.AreEqual(
                Vector2.zero,
                MouseCoordinates.Normalize(new Vector2(5f, 5f), new Rect(0f, 0f, 0f, 10f)));
            Assert.AreEqual(
                Vector2.zero,
                MouseCoordinates.Normalize(new Vector2(5f, 5f), new Rect(0f, 0f, 10f, 0f)));
        }

        [Test]
        public void OfflineGatewayRefusesPlacementWithoutLocalValidation()
        {
            transport.IsAvailable = false;
            transport.LastError = "MATLAB is disconnected.";

            string requestId;
            string error;
            bool sent = gateway.TrySend(
                ActivityIds.Color, "apple", "basket_red", new Vector2(0.25f, 0.75f),
                out requestId, out error);

            Assert.IsFalse(sent);
            Assert.IsNull(requestId);
            Assert.AreEqual("MATLAB is disconnected.", error);
            Assert.AreEqual(0, transport.Calls.Count);
            Assert.AreEqual(0, outcomes.Count);
        }

        [Test]
        public void TrySendUsesExistingPlaceRequestFieldsAndCorrelatesCorrectResult()
        {
            string requestId;
            string error;

            bool sent = gateway.TrySend(
                ActivityIds.Color,
                "apple",
                "basket_red",
                new Vector2(0.25f, 0.75f),
                out requestId,
                out error);

            Assert.IsTrue(sent);
            Assert.AreEqual("req_000001", requestId);
            Assert.AreEqual(string.Empty, error);
            Assert.AreEqual(1, transport.Calls.Count);
            Assert.AreEqual(ActivityIds.Color, transport.Calls[0].ActivityId);
            Assert.AreEqual("apple", transport.Calls[0].ItemId);
            Assert.AreEqual("basket_red", transport.Calls[0].TargetId);
            Assert.AreEqual(0.25f, transport.Calls[0].X);
            Assert.AreEqual(0.75f, transport.Calls[0].Y);
            Assert.IsTrue(gateway.HasPendingRequest);

            transport.EmitMessage(new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                requestId = requestId,
                activity = ActivityIds.Color,
                objectId = "apple",
                targetId = "basket_red",
                valid = true,
                correct = true,
                score = 3,
                message = "Correct placement"
            });

            Assert.IsFalse(gateway.HasPendingRequest);
            Assert.AreEqual(1, outcomes.Count);
            Assert.AreEqual(PlacementOutcomeKind.AcceptedCorrect, outcomes[0].Kind);
            Assert.IsTrue(outcomes[0].IsCorrect);
            Assert.AreEqual(3, outcomes[0].Score);
            Assert.AreEqual("apple", outcomes[0].ItemId);
            Assert.AreEqual("basket_red", outcomes[0].TargetId);
        }

        [Test]
        public void IncorrectPlaceResponseIsAcceptedButReportedAsIncorrect()
        {
            Send("apple", "basket_blue");

            transport.EmitMessage(new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                requestId = gateway.CurrentRequestId,
                valid = true,
                correct = false,
                score = 0,
                message = "Incorrect placement. Try again."
            });

            Assert.AreEqual(PlacementOutcomeKind.AcceptedIncorrect, outcomes[0].Kind);
            Assert.IsTrue(outcomes[0].IsAccepted);
            Assert.IsFalse(outcomes[0].IsCorrect);
        }

        [Test]
        public void ErrorResponseIsRejectedWithoutTreatingItAsCorrectness()
        {
            Send("apple", "basket_red");

            transport.EmitMessage(new WireMessage
            {
                type = ProtocolConstants.TypeError,
                requestId = gateway.CurrentRequestId,
                code = "UNKNOWN_OBJECT",
                message = "Unknown object 'apple'."
            });

            Assert.AreEqual(PlacementOutcomeKind.Rejected, outcomes[0].Kind);
            Assert.AreEqual("UNKNOWN_OBJECT", outcomes[0].Code);
            Assert.IsFalse(gateway.HasPendingRequest);
        }

        [Test]
        public void ResponsesForOtherRequestsAreIgnored()
        {
            Send("apple", "basket_red");

            transport.EmitMessage(new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                requestId = "req_other",
                valid = true,
                correct = true
            });

            Assert.AreEqual(0, outcomes.Count);
            Assert.IsTrue(gateway.HasPendingRequest);
        }

        [Test]
        public void DisconnectInterruptsPendingPlacement()
        {
            Send("apple", "basket_red");

            transport.EmitConnection(ConnectionState.Disconnected, "MATLAB stopped responding.");

            Assert.AreEqual(1, outcomes.Count);
            Assert.AreEqual(PlacementOutcomeKind.Interrupted, outcomes[0].Kind);
            Assert.AreEqual("MATLAB stopped responding.", outcomes[0].Message);
            Assert.IsFalse(gateway.HasPendingRequest);
        }

        [Test]
        public void TimeoutInterruptsPendingPlacementWithoutBlocking()
        {
            MutableClock clock = new MutableClock();
            ActivityPlacementGateway timedGateway = new ActivityPlacementGateway(
                transport, 2f, () => clock.Now);
            timedGateway.ResultReceived += outcome => outcomes.Add(outcome);
            timedGateway.Attach();

            string requestId;
            string error;
            Assert.IsTrue(timedGateway.TrySend(
                ActivityIds.Color, "apple", "basket_red", Vector2.zero,
                out requestId, out error));

            clock.Now = 1.9f;
            timedGateway.Update();
            Assert.IsTrue(timedGateway.HasPendingRequest);
            Assert.AreEqual(0, outcomes.Count);

            clock.Now = 2f;
            timedGateway.Update();

            Assert.AreEqual(1, outcomes.Count);
            Assert.AreEqual(PlacementOutcomeKind.TimedOut, outcomes[0].Kind);
            Assert.IsFalse(timedGateway.HasPendingRequest);
            timedGateway.Detach();
        }

        [Test]
        public void SecondPlacementIsRefusedWhileFirstIsPending()
        {
            Send("apple", "basket_red");

            string requestId;
            string error;
            bool sent = gateway.TrySend(
                ActivityIds.Color, "carrot", "basket_orange", Vector2.zero,
                out requestId, out error);

            Assert.IsFalse(sent);
            Assert.IsNull(requestId);
            Assert.AreEqual("Another placement is waiting for MATLAB.", error);
            Assert.AreEqual(1, transport.Calls.Count);
        }

        private void Send(string itemId, string targetId)
        {
            string requestId;
            string error;
            Assert.IsTrue(gateway.TrySend(
                ActivityIds.Color, itemId, targetId, new Vector2(0.5f, 0.5f),
                out requestId, out error));
        }

        private sealed class MutableClock
        {
            public float Now { get; set; }
        }

        private sealed class StubTransport : IPlacementTransport
        {
            public StubTransport()
            {
                Calls = new List<PlacementCall>();
                LastError = string.Empty;
            }

            public bool IsAvailable { get; set; }

            public string LastError { get; set; }

            public string NextRequestId { get; set; }

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
                Calls.Add(new PlacementCall(
                    activityId, objectId, targetId, normalizedX, normalizedY));
                return IsAvailable ? NextRequestId : null;
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
                string activityId,
                string itemId,
                string targetId,
                float x,
                float y)
            {
                ActivityId = activityId;
                ItemId = itemId;
                TargetId = targetId;
                X = x;
                Y = y;
            }

            public string ActivityId { get; }

            public string ItemId { get; }

            public string TargetId { get; }

            public float X { get; }

            public float Y { get; }
        }
    }
}
