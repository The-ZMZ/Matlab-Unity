using System;
using CleanMyRoom.Gameplay.Placement;
using CleanMyRoom.Net;
using CleanMyRoom.Player.Input;
using UnityEngine;

namespace CleanMyRoom.Gameplay.Activities
{
    /// <summary>
    /// Reusable mouse drag-and-drop for any activity.
    ///
    /// Unity only selects, moves and sends identifiers plus a normalized drop
    /// point. MATLAB returns PLACE_RES / ERROR through the existing TCP path,
    /// and this controller applies the presentation outcome: keep the object
    /// where it was dropped only when MATLAB says the placement is correct,
    /// otherwise restore its original pose. No activity-specific mapping or
    /// local answer checking lives here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MouseDragPlacementController : MonoBehaviour
    {
        [Header("Activity")]
        [SerializeField] private string activityId = ActivityIds.Color;
        [SerializeField] private Camera activityCamera;
        [SerializeField, Min(1f)] private float rayDistance = 200f;
        [SerializeField, Min(0.1f)] private float placementTimeoutSeconds = 8f;

        [Header("Drop coordinates")]
        [SerializeField] private bool useScreenBoundsForNormalization = true;
        [SerializeField] private Rect activityCanvasRect = new Rect(0f, 0f, 800f, 600f);

        private readonly MouseInteractionStateMachine interaction = new MouseInteractionStateMachine();

        private IPlacementTransport transport;
        private ActivityPlacementGateway gateway;
        private bool gatewayAttached;
        private bool transportSubscribed;

        private DraggableItem activeItem;
        private Vector3 grabOffset;
        private Plane dragPlane;

        private DraggableItem pendingItem;
        private bool pendingValidation;

        public event Action<DragPlacementResult> ResultReceived;

        public string ActivityId
        {
            get { return activityId; }
            set { activityId = value ?? string.Empty; }
        }

        public Camera ActivityCamera
        {
            get { return activityCamera; }
            set { activityCamera = value; }
        }

        /// <summary>Optional test seam; production reads MouseInput.Source.</summary>
        public IMouseInputSource MouseSource { get; set; }

        /// <summary>Optional test seam for choosing a transport implementation.</summary>
        public IPlacementTransport Transport
        {
            get { return transport; }
            set
            {
                if (ReferenceEquals(transport, value))
                {
                    return;
                }

                UnsubscribeTransportConnection();
                transport = value;
                SubscribeTransportConnection();
            }
        }

        /// <summary>Optional test seam; replaces the gateway around the same protocol.</summary>
        public ActivityPlacementGateway Gateway
        {
            get { return gateway; }
            set
            {
                if (ReferenceEquals(gateway, value))
                {
                    return;
                }

                DetachGateway();
                gateway = value;
                AttachGateway();
            }
        }

        public bool UseScreenBoundsForNormalization
        {
            get { return useScreenBoundsForNormalization; }
            set { useScreenBoundsForNormalization = value; }
        }

        public Rect ActivityCanvasRect
        {
            get { return activityCanvasRect; }
            set { activityCanvasRect = value; }
        }

        /// <summary>Optional deterministic ray factory used by EditMode fixtures.</summary>
        public Func<Vector2, Ray> ScreenRayProvider { get; set; }

        public bool IsDragging { get { return activeItem != null; } }

        public bool HasPendingValidation { get { return pendingValidation; } }

        private void Awake()
        {
            interaction.Reset();
        }

        private void OnEnable()
        {
            EnsureGateway();
        }

        private void OnDisable()
        {
            HandleActivityDisabled();
        }

        /// <summary>
        /// Deterministic equivalent of the disable callback. Unity does not
        /// invoke OnDisable synchronously for SetActive(false) in EditMode, so
        /// tests and UI code can call this directly through the same code path.
        /// </summary>
        public void HandleActivityDisabled()
        {
            CancelActiveDrag("Drag interrupted because the activity was disabled.");
            interaction.Interrupt();
            DetachGateway();
            UnsubscribeTransportConnection();
        }

        private void OnDestroy()
        {
            CancelActiveDrag("Drag interrupted because the activity was closed.");
            DetachGateway();
            UnsubscribeTransportConnection();
        }

        private void Update()
        {
            ProcessFrame((MouseSource ?? MouseInput.Source).ReadSnapshot());
        }

        /// <summary>
        /// Process one input snapshot. Public so EditMode fixtures can drive a
        /// frame deterministically without a running player loop.
        /// </summary>
        public void ProcessFrame(MouseInputSnapshot snapshot)
        {
            EnsureGateway();
            gateway.Update();

            bool pressIsValid = activeItem == null &&
                                !pendingValidation &&
                                gateway.CanAcceptRequest &&
                                FindDraggable(snapshot.ScreenPosition) != null;

            interaction.Update(snapshot, pressIsValid);

            if (interaction.ConsumePressStarted())
            {
                BeginDrag(snapshot);
            }

            if (activeItem != null && interaction.IsPressActive)
            {
                MoveActiveItem(snapshot);
            }

            if (interaction.ConsumeReleaseStarted())
            {
                FinishDrag(snapshot);
            }
        }

        /// <summary>Expose a safe cancellation path for pause/menu/UI code.</summary>
        public void CancelActiveDrag(string reason)
        {
            CancelActiveDrag(reason, DragPlacementStatus.Cancelled);
        }

        private void EnsureGateway()
        {
            if (gateway == null)
            {
                Gateway = new ActivityPlacementGateway(
                    transport ?? new NetworkPlacementTransport(),
                    placementTimeoutSeconds);
            }
            else
            {
                AttachGateway();
            }

            SubscribeTransportConnection();
        }

        private void AttachGateway()
        {
            if (gateway == null || gatewayAttached)
            {
                return;
            }

            gateway.ResultReceived += HandlePlacementOutcome;
            gateway.Attach();
            gatewayAttached = true;
        }

        private void DetachGateway()
        {
            if (gateway == null || !gatewayAttached)
            {
                return;
            }

            gateway.ResultReceived -= HandlePlacementOutcome;
            gateway.Detach();
            gatewayAttached = false;
        }

        private void SubscribeTransportConnection()
        {
            if (transport == null || transportSubscribed)
            {
                return;
            }

            transport.ConnectionChanged += HandleConnectionChanged;
            transportSubscribed = true;
        }

        private void UnsubscribeTransportConnection()
        {
            if (transport == null || !transportSubscribed)
            {
                return;
            }

            transport.ConnectionChanged -= HandleConnectionChanged;
            transportSubscribed = false;
        }

        private void BeginDrag(MouseInputSnapshot snapshot)
        {
            DraggableItem item = FindDraggable(snapshot.ScreenPosition);
            if (item == null)
            {
                return;
            }

            activeItem = item;
            item.CaptureOriginalPose();
            dragPlane = new Plane(Vector3.up, item.transform.position);
            grabOffset = Vector3.zero;

            Ray ray;
            float distance;
            if (TryCreateRay(snapshot.ScreenPosition, out ray) &&
                dragPlane.Raycast(ray, out distance))
            {
                grabOffset = item.transform.position - ray.GetPoint(distance);
            }

            RaiseResult(
                DragPlacementStatus.Started,
                item.ItemId,
                string.Empty,
                "Object selected.",
                string.Empty);
        }

        private void MoveActiveItem(MouseInputSnapshot snapshot)
        {
            Ray ray;
            float distance;
            if (!TryCreateRay(snapshot.ScreenPosition, out ray) ||
                !dragPlane.Raycast(ray, out distance))
            {
                return;
            }

            activeItem.MoveTo(ray.GetPoint(distance) + grabOffset);
        }

        private void FinishDrag(MouseInputSnapshot snapshot)
        {
            if (activeItem == null)
            {
                return;
            }

            DraggableItem item = activeItem;
            DropTarget target = FindDropTarget(snapshot.ScreenPosition);
            if (target == null || string.IsNullOrEmpty(target.TargetId))
            {
                CancelActiveDrag("No drop target was under the release position.");
                return;
            }

            Vector2 normalized = UseScreenBoundsForNormalization
                ? MouseCoordinates.NormalizeToScreen(snapshot.ScreenPosition)
                : MouseCoordinates.Normalize(snapshot.ScreenPosition, activityCanvasRect);

            string requestId;
            string error;
            if (!gateway.TrySend(
                    activityId,
                    item.ItemId,
                    target.TargetId,
                    normalized,
                    out requestId,
                    out error))
            {
                CancelActiveDrag(error, DragPlacementStatus.Unavailable, false);
                RaiseResult(
                    DragPlacementStatus.Unavailable,
                    item.ItemId,
                    target.TargetId,
                    error,
                    string.Empty);
                return;
            }

            // The item stays at the released position while MATLAB validates.
            // It is restored automatically if MATLAB rejects or fails to reply.
            pendingItem = item;
            pendingValidation = true;
            activeItem = null;
        }

        private void HandlePlacementOutcome(PlacementOutcome outcome)
        {
            if (!pendingValidation || pendingItem == null)
            {
                return;
            }

            DraggableItem item = pendingItem;
            pendingItem = null;
            pendingValidation = false;

            if (outcome.Kind == PlacementOutcomeKind.AcceptedCorrect)
            {
                RaiseResult(
                    DragPlacementStatus.CompletedCorrect,
                    outcome.ItemId,
                    outcome.TargetId,
                    outcome.Message,
                    outcome.RequestId);
                return;
            }

            item.RestoreOriginalPose();

            DragPlacementStatus status;
            switch (outcome.Kind)
            {
                case PlacementOutcomeKind.AcceptedIncorrect:
                    status = DragPlacementStatus.CompletedIncorrect;
                    break;
                case PlacementOutcomeKind.Rejected:
                    status = DragPlacementStatus.Rejected;
                    break;
                default:
                    status = DragPlacementStatus.Cancelled;
                    break;
            }

            RaiseResult(status, outcome.ItemId, outcome.TargetId, outcome.Message, outcome.RequestId);
        }

        private void HandleConnectionChanged(ConnectionState state, string detail)
        {
            if (state == ConnectionState.Connected || activeItem == null)
            {
                return;
            }

            CancelActiveDrag(
                string.IsNullOrEmpty(detail) ? "MATLAB disconnected." : detail);
        }

        private void CancelActiveDrag(
            string reason,
            DragPlacementStatus status,
            bool raiseResult = true)
        {
            if (activeItem == null)
            {
                return;
            }

            DraggableItem item = activeItem;
            activeItem = null;
            item.RestoreOriginalPose();
            if (raiseResult)
            {
                RaiseResult(status, item.ItemId, string.Empty, reason, string.Empty);
            }
        }

        private DraggableItem FindDraggable(Vector2 screenPosition)
        {
            RaycastHit[] hits = GetOrderedHits(screenPosition);
            for (int i = 0; i < hits.Length; i++)
            {
                DraggableItem item = hits[i].collider.GetComponentInParent<DraggableItem>();
                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }

        private DropTarget FindDropTarget(Vector2 screenPosition)
        {
            RaycastHit[] hits = GetOrderedHits(screenPosition);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.GetComponentInParent<DraggableItem>() != null)
                {
                    continue;
                }

                DropTarget target = hits[i].collider.GetComponentInParent<DropTarget>();
                if (target != null)
                {
                    return target;
                }
            }

            return null;
        }

        private RaycastHit[] GetOrderedHits(Vector2 screenPosition)
        {
            Ray ray;
            if (!TryCreateRay(screenPosition, out ray))
            {
                return new RaycastHit[0];
            }

            RaycastHit[] hits = Physics.RaycastAll(
                ray, rayDistance, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            return hits;
        }

        private bool TryCreateRay(Vector2 screenPosition, out Ray ray)
        {
            if (ScreenRayProvider != null)
            {
                ray = ScreenRayProvider(screenPosition);
                return true;
            }

            Camera camera = activityCamera != null ? activityCamera : Camera.main;
            if (camera == null)
            {
                ray = default;
                return false;
            }

            ray = camera.ScreenPointToRay(screenPosition);
            return true;
        }

        private void RaiseResult(
            DragPlacementStatus status,
            string itemId,
            string targetId,
            string message,
            string requestId)
        {
            Action<DragPlacementResult> handler = ResultReceived;
            if (handler != null)
            {
                handler(new DragPlacementResult(status, itemId, targetId, message, requestId));
            }
        }
    }
}
