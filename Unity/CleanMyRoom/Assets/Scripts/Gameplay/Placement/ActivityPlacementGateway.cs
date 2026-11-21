using System;
using CleanMyRoom.Net;
using UnityEngine;

namespace CleanMyRoom.Gameplay.Placement
{
    /// <summary>
    /// Bridges keyboard/mouse activity input to the existing PLACE_REQ path.
    ///
    /// It only sends the identifiers and normalized pointer position, then
    /// correlates PLACE_RES / ERROR by requestId. It never validates an answer
    /// and never blocks gameplay waiting for MATLAB: results arrive as events,
    /// with disconnect and timeout paths that let the caller safely undo a drag.
    /// </summary>
    public sealed class ActivityPlacementGateway
    {
        private readonly IPlacementTransport transport;
        private readonly float timeoutSeconds;
        private readonly Func<float> timeProvider;
        private PendingPlacement pending;
        private bool attached;

        public ActivityPlacementGateway(
            IPlacementTransport transport,
            float timeoutSeconds = 8f,
            Func<float> timeProvider = null)
        {
            if (transport == null)
            {
                throw new ArgumentNullException(nameof(transport));
            }

            this.transport = transport;
            this.timeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            this.timeProvider = timeProvider ?? (() => Time.unscaledTime);
        }

        public event Action<PlacementOutcome> ResultReceived;

        public bool IsAvailable { get { return transport.IsAvailable; } }

        public bool HasPendingRequest { get { return pending != null; } }

        public bool CanAcceptRequest { get { return IsAvailable && pending == null; } }

        public string CurrentRequestId { get { return pending == null ? null : pending.RequestId; } }

        public void Attach()
        {
            if (attached)
            {
                return;
            }

            transport.MessageReceived += HandleMessage;
            transport.ConnectionChanged += HandleConnectionChanged;
            attached = true;
        }

        public void Detach()
        {
            if (!attached)
            {
                return;
            }

            transport.MessageReceived -= HandleMessage;
            transport.ConnectionChanged -= HandleConnectionChanged;
            attached = false;
        }

        /// <summary>Queue a placement through the existing NetworkManager API.</summary>
        public bool TrySend(
            string activityId,
            string itemId,
            string targetId,
            Vector2 normalizedPosition,
            out string requestId,
            out string error)
        {
            requestId = null;
            error = string.Empty;

            if (!IsAvailable)
            {
                error = string.IsNullOrEmpty(transport.LastError)
                    ? "MATLAB is disconnected."
                    : transport.LastError;
                return false;
            }

            if (pending != null)
            {
                error = "Another placement is waiting for MATLAB.";
                return false;
            }

            if (string.IsNullOrEmpty(activityId) ||
                string.IsNullOrEmpty(itemId) ||
                string.IsNullOrEmpty(targetId))
            {
                error = "A placement needs activity, item and target identifiers.";
                return false;
            }

            try
            {
                requestId = transport.SendPlaceRequest(
                    activityId,
                    itemId,
                    targetId,
                    normalizedPosition.x,
                    normalizedPosition.y);
            }
            catch (Exception ex)
            {
                requestId = null;
                error = "Placement request failed: " + ex.Message;
                return false;
            }

            if (string.IsNullOrEmpty(requestId))
            {
                error = string.IsNullOrEmpty(transport.LastError)
                    ? "MATLAB did not accept the placement request."
                    : transport.LastError;
                return false;
            }

            pending = new PendingPlacement(
                requestId,
                activityId,
                itemId,
                targetId,
                timeProvider());
            return true;
        }

        /// <summary>Call once per frame from the owning component.</summary>
        public void Update()
        {
            if (pending == null || timeoutSeconds <= 0f)
            {
                return;
            }

            if (timeProvider() - pending.SentAt >= timeoutSeconds)
            {
                Complete(
                    PlacementOutcomeKind.TimedOut,
                    "MATLAB did not answer the placement request in time.");
            }
        }

        private void HandleMessage(WireMessage message)
        {
            if (pending == null || message == null || string.IsNullOrEmpty(message.requestId))
            {
                return;
            }

            if (!string.Equals(message.requestId, pending.RequestId, StringComparison.Ordinal))
            {
                return;
            }

            if (message.IsType(ProtocolConstants.TypePlaceResponse))
            {
                PlacementOutcomeKind kind = message.valid
                    ? (message.correct
                        ? PlacementOutcomeKind.AcceptedCorrect
                        : PlacementOutcomeKind.AcceptedIncorrect)
                    : PlacementOutcomeKind.Rejected;
                Complete(kind, message.message, string.Empty, message.score);
                return;
            }

            if (message.IsType(ProtocolConstants.TypeError))
            {
                Complete(PlacementOutcomeKind.Rejected, message.message, message.code);
            }
        }

        private void HandleConnectionChanged(ConnectionState state, string detail)
        {
            if (state != ConnectionState.Connected && pending != null)
            {
                Complete(
                    PlacementOutcomeKind.Interrupted,
                    string.IsNullOrEmpty(detail) ? "MATLAB disconnected." : detail);
            }
        }

        private void Complete(PlacementOutcomeKind kind, string message, string code = "", int score = 0)
        {
            PendingPlacement completed = pending;
            pending = null;
            if (completed == null)
            {
                return;
            }

            Action<PlacementOutcome> handler = ResultReceived;
            if (handler != null)
            {
                handler(new PlacementOutcome(
                    kind,
                    completed.RequestId,
                    completed.ActivityId,
                    completed.ItemId,
                    completed.TargetId,
                    score,
                    code ?? string.Empty,
                    message ?? string.Empty));
            }
        }

        private sealed class PendingPlacement
        {
            public PendingPlacement(
                string requestId,
                string activityId,
                string itemId,
                string targetId,
                float sentAt)
            {
                RequestId = requestId;
                ActivityId = activityId;
                ItemId = itemId;
                TargetId = targetId;
                SentAt = sentAt;
            }

            public string RequestId { get; private set; }

            public string ActivityId { get; private set; }

            public string ItemId { get; private set; }

            public string TargetId { get; private set; }

            public float SentAt { get; private set; }
        }
    }
}
