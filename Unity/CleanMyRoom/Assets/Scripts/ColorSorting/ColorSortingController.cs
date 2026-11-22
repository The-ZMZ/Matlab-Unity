using System;
using System.Collections.Generic;
using CleanMyRoom.Core;
using CleanMyRoom.Net;
using UnityEngine;

namespace CleanMyRoom.ColorSorting
{
    /// <summary>
    /// Drives the playable Color Sorting activity.
    ///
    /// Unity collects mouse/keyboard input, converts the drop point into
    /// normalized canvas coordinates and asks MATLAB to validate the placement.
    /// Every correctness flag, score and star count used here comes from a
    /// MATLAB message. Unity never validates an answer locally and never
    /// completes the activity without MATLAB.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ColorSortingController : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private Camera activityCamera;
        [SerializeField] private ActivityPointerCursor pointerCursor;
        [SerializeField] private ColorSortingFeedback feedback;
        [SerializeField] private SortableObject[] sortableObjects;
        [SerializeField] private ColorBasket[] baskets;

        [Header("Interaction")]
        [SerializeField] private float dragHeight = 1.85f;
        [SerializeField] private float restHeight = 1.42f;
        [SerializeField] private float boardMinX = -6.4f;
        [SerializeField] private float boardMaxX = 6.4f;
        [SerializeField] private float boardMinZ = -4.3f;
        [SerializeField] private float boardMaxZ = 4.3f;
        [SerializeField] private float keyboardStep = 0.22f;
        [SerializeField] private float dropRadius = 1.7f;

        [Header("MATLAB")]
        [SerializeField, Min(1f)] private float validationTimeoutSeconds = 8f;
        [SerializeField] private bool autoReturnToBedroom = true;
        [SerializeField, Min(0f)] private float autoReturnDelaySeconds = 4f;

        private readonly Dictionary<string, SortableObject> objectsById =
            new Dictionary<string, SortableObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, ColorBasket> basketsById =
            new Dictionary<string, ColorBasket>(StringComparer.Ordinal);
        private readonly Dictionary<string, SortableObject> pendingObjects =
            new Dictionary<string, SortableObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, ColorBasket> pendingBaskets =
            new Dictionary<string, ColorBasket>(StringComparer.Ordinal);

        private ISortingGateway gateway;
        private NetworkManager subscribedNetwork;
        private SortableObject dragging;
        private Vector3 pointerWorld;
        private bool pointerReady;
        private float returnAt = -1f;
        private bool completionHandled;

        public ColorSortingProgress Progress { get; private set; }

        /// <summary>
        /// Replaceable network seam. Production uses MatlabSortingGateway;
        /// tests can provide a deterministic gateway. No validation is possible
        /// through this interface - it only sends a request.
        /// </summary>
        public ISortingGateway Gateway
        {
            get { return gateway; }
            set { gateway = value ?? new MatlabSortingGateway(); }
        }

        public IReadOnlyList<SortableObject> SortableObjects
        {
            get { return Array.AsReadOnly(sortableObjects ?? new SortableObject[0]); }
        }

        public IReadOnlyList<ColorBasket> Baskets
        {
            get { return Array.AsReadOnly(baskets ?? new ColorBasket[0]); }
        }

        /// <summary>Wire scene references from the idempotent editor builder.</summary>
        public void Configure(
            Camera camera, ActivityPointerCursor cursor, ColorSortingFeedback ui,
            SortableObject[] objects, ColorBasket[] targetBaskets)
        {
            activityCamera = camera;
            pointerCursor = cursor;
            feedback = ui;
            sortableObjects = objects;
            baskets = targetBaskets;
            RebuildLookups();
        }

        private void Awake()
        {
            Progress = new ColorSortingProgress();
            gateway = new MatlabSortingGateway();

            if (activityCamera == null)
            {
                activityCamera = Camera.main;
            }
            if (pointerCursor == null)
            {
                pointerCursor = FindAnyObjectByType<ActivityPointerCursor>();
            }
            if (feedback == null)
            {
                feedback = FindAnyObjectByType<ColorSortingFeedback>();
            }

            // Prefer the references saved by the scene builder so additive scenes
            // cannot inject foreign toys or baskets. Discovery is only a fallback
            // for controllers created without serialized references.
            if (!HasConfiguredEntries(sortableObjects))
            {
                sortableObjects = FindObjectsByType<SortableObject>(FindObjectsSortMode.None);
            }
            if (!HasConfiguredEntries(baskets))
            {
                baskets = FindObjectsByType<ColorBasket>(FindObjectsSortMode.None);
            }
            RebuildLookups();
        }

        private static bool HasConfiguredEntries<T>(T[] entries) where T : UnityEngine.Object
        {
            if (entries == null)
            {
                return false;
            }
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void Start()
        {
            if (feedback != null)
            {
                feedback.Reset();
            }
            EnsureNetworkSubscription();
            UpdateConnectionDisplay();
            pointerWorld = new Vector3(0f, dragHeight, 0f);
        }

        private void OnEnable()
        {
            EnsureNetworkSubscription();
        }

        private void OnDisable()
        {
            DetachNetwork();
        }

        private void OnDestroy()
        {
            DetachNetwork();
        }

        private void Update()
        {
            EnsureNetworkSubscription();

            SortingEvent[] expired = Progress.CheckTimeouts(
                Time.unscaledTime, validationTimeoutSeconds);
            for (int i = 0; i < expired.Length; i++)
            {
                ApplyOutcome(expired[i]);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnToBedroom();
                return;
            }

            if (completionHandled && autoReturnToBedroom && returnAt >= 0f &&
                Time.unscaledTime >= returnAt)
            {
                returnAt = -1f;
                ReturnToBedroom();
                return;
            }

            bool enabled = gateway != null && gateway.ActivitiesEnabled;
            UpdateConnectionDisplay();

            if (pointerCursor != null)
            {
                pointerCursor.SetMode(!enabled
                    ? ActivityPointerCursor.PointerMode.Disabled
                    : (dragging != null
                        ? ActivityPointerCursor.PointerMode.Grabbing
                        : ActivityPointerCursor.PointerMode.Normal));
            }

            if (!enabled)
            {
                if (dragging != null)
                {
                    CleanupPending("MATLAB disconnected - the toy went back to its place.");
                }
                return;
            }

            HandlePointerInput();
            HandleKeyboardInput();
            SyncPointerCursor();
        }

        // -----------------------------------------------------------------
        // Input collection (Unity responsibility)
        // -----------------------------------------------------------------

        private void HandlePointerInput()
        {
            if (activityCamera == null)
            {
                return;
            }

            Ray ray = activityCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 point;
            if (!TryProjectToBoard(ray, dragHeight, out point))
            {
                return;
            }
            pointerWorld = ClampToBoard(point);
            pointerReady = true;

            if (Input.GetMouseButtonDown(0))
            {
                SortableObject picked = FindObjectUnderPointer(ray);
                if (picked != null && Progress.CanStartPlacement(picked.ObjectId))
                {
                    StartDrag(picked);
                }
            }

            if (dragging == null)
            {
                return;
            }

            dragging.MoveTo(new Vector3(pointerWorld.x, dragHeight, pointerWorld.z));
            ColorBasket hovered = FindBasketUnderPointer(ray);
            SetBasketHighlight(hovered);

            if (Input.GetMouseButtonUp(0))
            {
                SubmitDrop(dragging, hovered, pointerWorld);
            }
        }

        private void HandleKeyboardInput()
        {
            Vector3 step = Vector3.zero;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                step.x -= 1f;
            }
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                step.x += 1f;
            }
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                step.z += 1f;
            }
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                step.z -= 1f;
            }
            if (step.sqrMagnitude > 0f)
            {
                pointerWorld = ClampToBoard(
                    pointerWorld + step.normalized * keyboardStep);
                pointerReady = true;
            }

            if (!Input.GetKeyDown(KeyCode.Space))
            {
                return;
            }

            if (dragging == null)
            {
                SortableObject picked = FindNearestObject(pointerWorld, 1.5f);
                if (picked != null && Progress.CanStartPlacement(picked.ObjectId))
                {
                    StartDrag(picked);
                }
                else
                {
                    ShowNotice("Move over a toy, then press Space to pick it up.");
                }
            }
            else
            {
                ColorBasket target = FindNearestBasket(pointerWorld, dropRadius);
                SubmitDrop(dragging, target, pointerWorld);
            }
        }

        private void StartDrag(SortableObject target)
        {
            dragging = target;
            target.BeginDrag(dragHeight);
            ShowNotice("Now move to a basket and release.");
        }

        private void SubmitDrop(SortableObject target, ColorBasket basket, Vector3 dropPoint)
        {
            dragging = null;
            SetBasketHighlight(null);
            if (target == null)
            {
                return;
            }
            if (basket == null)
            {
                target.ReturnHome();
                ShowNotice("Drop the toy on a basket.");
                return;
            }

            float x = NormalizeX(dropPoint.x);
            float y = NormalizeY(dropPoint.z);
            if (!TrySubmitPlacement(target.ObjectId, basket.BasketId, x, y))
            {
                target.ReturnHome();
            }
        }

        /// <summary>
        /// Programmatic input path used by keyboard fallback and automated tests.
        /// It only sends a request; MATLAB decides the answer.
        /// </summary>
        public bool TrySubmitPlacement(string objectId, string basketId, float x, float y)
        {
            SortableObject item;
            ColorBasket basket;
            if (gateway == null || !gateway.ActivitiesEnabled ||
                string.IsNullOrEmpty(objectId) || string.IsNullOrEmpty(basketId) ||
                !objectsById.TryGetValue(objectId, out item) ||
                !basketsById.TryGetValue(basketId, out basket) ||
                item.IsPlaced || !Progress.CanStartPlacement(objectId))
            {
                ShowNotice("Sorting is not ready for that toy right now.");
                return false;
            }

            string requestId = gateway.SendPlacement(objectId, basketId, x, y);
            if (string.IsNullOrEmpty(requestId))
            {
                ShowNotice("MATLAB is not available. No answer was checked.");
                return false;
            }

            float now = Time.unscaledTime;
            if (!Progress.RegisterPending(requestId, objectId, basketId, now))
            {
                ShowNotice("That toy already has a request waiting for MATLAB.");
                return false;
            }
            pendingObjects[requestId] = item;
            pendingBaskets[requestId] = basket;
            ShowNotice("Asking MATLAB to check that placement...");
            return true;
        }

        // -----------------------------------------------------------------
        // MATLAB responses (display only)
        // -----------------------------------------------------------------

        private void EnsureNetworkSubscription()
        {
            NetworkManager network = NetworkManager.Instance;
            if (network == null)
            {
                DetachNetwork();
                return;
            }
            if (ReferenceEquals(network, subscribedNetwork))
            {
                return;
            }
            DetachNetwork();
            network.MessageReceived += HandleNetworkMessage;
            network.ConnectionChanged += HandleConnectionChanged;
            subscribedNetwork = network;
        }

        private void DetachNetwork()
        {
            if (subscribedNetwork == null)
            {
                return;
            }
            subscribedNetwork.MessageReceived -= HandleNetworkMessage;
            subscribedNetwork.ConnectionChanged -= HandleConnectionChanged;
            subscribedNetwork = null;
        }

        private void HandleNetworkMessage(WireMessage message)
        {
            HandleIncomingMessage(message);
        }

        /// <summary>
        /// Apply a MATLAB message. Public so automated tests can inject real
        /// protocol-shaped messages without opening a TCP socket.
        /// </summary>
        public SortingEvent HandleIncomingMessage(WireMessage message)
        {
            if (message == null)
            {
                return SortingEvent.None();
            }

            SortingEvent sortingEvent;
            if (message.IsType(ProtocolConstants.TypePlaceResponse))
            {
                sortingEvent = Progress.HandlePlaceResponse(message);
            }
            else if (message.IsType(ProtocolConstants.TypeActivityResult))
            {
                sortingEvent = Progress.HandleActivityResult(message);
                // Only a result that the progress state accepted may update
                // GameManager. Rejected duplicates and results for other
                // activities must not change mirrored stars or completion.
                if (sortingEvent.Handled &&
                    string.Equals(message.activity, ColorSortingCatalog.ActivityId,
                        StringComparison.Ordinal))
                {
                    GameManager manager = GameManager.Instance;
                    if (manager != null)
                    {
                        manager.ApplyMatlabActivityResult(
                            message.activity, message.completed, message.stars);
                    }
                }
            }
            else if (message.IsType(ProtocolConstants.TypeGameProgress))
            {
                sortingEvent = Progress.HandleGameProgress(message);
                GameManager manager = GameManager.Instance;
                if (manager != null)
                {
                    manager.ApplyMatlabGameProgress(
                        message.stars, message.completedActivities, message.gameCompleted);
                }
            }
            else if (message.IsType(ProtocolConstants.TypeError))
            {
                sortingEvent = Progress.HandleError(message);
            }
            else
            {
                return SortingEvent.None();
            }

            ApplyOutcome(sortingEvent);
            return sortingEvent;
        }

        private void HandleConnectionChanged(ConnectionState state, string detail)
        {
            if (state == ConnectionState.Connected)
            {
                ShowNotice("MATLAB connected - sorting is ready.");
            }
            else
            {
                CleanupPending("MATLAB disconnected. No answer was checked.");
                ShowNotice("MATLAB disconnected - sorting disabled until reconnected.");
            }
            UpdateConnectionDisplay();
        }

        private void ApplyOutcome(SortingEvent sortingEvent)
        {
            if (sortingEvent == null || !sortingEvent.Handled)
            {
                return;
            }

            SortableObject item;
            ColorBasket basket;
            bool hasItem = pendingObjects.TryGetValue(sortingEvent.RequestId, out item);
            bool hasBasket = pendingBaskets.TryGetValue(sortingEvent.RequestId, out basket);

            switch (sortingEvent.Outcome)
            {
                case SortingOutcome.Accepted:
                    if (hasItem && hasBasket)
                    {
                        item.MarkPlaced(
                            sortingEvent.TargetId, basket.SnapPoint(restHeight));
                    }
                    ForgetPending(sortingEvent.RequestId);
                    ShowFeedback(sortingEvent.Message, true);
                    PlayCorrect();
                    break;

                case SortingOutcome.Rejected:
                    if (hasItem)
                    {
                        item.ReturnHome();
                    }
                    ForgetPending(sortingEvent.RequestId);
                    ShowFeedback(sortingEvent.Message, false);
                    PlayWrong();
                    break;

                case SortingOutcome.InvalidResponse:
                case SortingOutcome.ProtocolError:
                case SortingOutcome.Timeout:
                case SortingOutcome.Disconnected:
                    if (hasItem)
                    {
                        item.ReturnHome();
                    }
                    ForgetPending(sortingEvent.RequestId);
                    ShowNotice(sortingEvent.Message);
                    if (sortingEvent.Outcome != SortingOutcome.Disconnected)
                    {
                        PlayWrong();
                    }
                    break;

                case SortingOutcome.ScoreUpdated:
                    UpdateProgressDisplay();
                    break;

                case SortingOutcome.Completed:
                    HandleCompletion(sortingEvent);
                    break;
            }
        }

        private void HandleCompletion(SortingEvent sortingEvent)
        {
            if (completionHandled)
            {
                return;
            }
            completionHandled = true;
            UpdateProgressDisplay();
            if (feedback != null)
            {
                feedback.ShowCompletion(
                    "All toys are in the right baskets! First star earned!");
            }
            PlayCompletion();
            if (autoReturnToBedroom)
            {
                returnAt = Time.unscaledTime + autoReturnDelaySeconds;
            }
        }

        private void CleanupPending(string reason)
        {
            SortingEvent[] cancelled = Progress.CancelAllPending(
                SortingOutcome.Disconnected, reason);
            for (int i = 0; i < cancelled.Length; i++)
            {
                ApplyOutcome(cancelled[i]);
            }
            if (dragging != null)
            {
                dragging.ReturnHome();
                dragging = null;
            }
            SetBasketHighlight(null);
        }

        private void ForgetPending(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                return;
            }
            pendingObjects.Remove(requestId);
            pendingBaskets.Remove(requestId);
        }

        // -----------------------------------------------------------------
        // Navigation and presentation helpers
        // -----------------------------------------------------------------

        /// <summary>Return through the existing #011 scene navigation system.</summary>
        public bool ReturnToBedroom()
        {
            CleanupPending("Leaving the activity; no answer was checked.");
            return SceneLoader.LoadBedroom();
        }

        private void UpdateConnectionDisplay()
        {
            if (feedback == null)
            {
                return;
            }
            NetworkManager network = NetworkManager.Instance;
            bool enabled = gateway != null && gateway.ActivitiesEnabled;
            string status = network == null
                ? "DISCONNECTED"
                : (network.State == ConnectionState.Connected ? "CONNECTED" : "DISCONNECTED");
            feedback.UpdateConnection(status, enabled);
        }

        private void UpdateProgressDisplay()
        {
            if (feedback != null)
            {
                feedback.UpdateStars(Progress.CorrectlyPlacedCount, Progress.TotalStars);
            }
        }

        private void ShowFeedback(string message, bool correct)
        {
            if (feedback != null)
            {
                feedback.ShowFeedback(message, correct);
            }
        }

        private void ShowNotice(string message)
        {
            if (feedback != null)
            {
                feedback.ShowNotice(message);
            }
        }

        private void PlayCorrect()
        {
            ActivityAudioManager audio = ActivityAudioManager.Instance;
            if (audio != null)
            {
                audio.PlayCorrect();
            }
        }

        private void PlayWrong()
        {
            ActivityAudioManager audio = ActivityAudioManager.Instance;
            if (audio != null)
            {
                audio.PlayWrong();
            }
        }

        private void PlayCompletion()
        {
            ActivityAudioManager audio = ActivityAudioManager.Instance;
            if (audio != null)
            {
                audio.PlayCompletion();
            }
        }

        // -----------------------------------------------------------------
        // Geometry helpers - input mapping only, never answer validation
        // -----------------------------------------------------------------

        private void RebuildLookups()
        {
            objectsById.Clear();
            basketsById.Clear();
            if (sortableObjects != null)
            {
                for (int i = 0; i < sortableObjects.Length; i++)
                {
                    if (sortableObjects[i] != null)
                    {
                        objectsById[sortableObjects[i].ObjectId] = sortableObjects[i];
                    }
                }
            }
            if (baskets != null)
            {
                for (int i = 0; i < baskets.Length; i++)
                {
                    if (baskets[i] != null)
                    {
                        basketsById[baskets[i].BasketId] = baskets[i];
                    }
                }
            }
        }

        private bool TryProjectToBoard(Ray ray, float height, out Vector3 point)
        {
            point = Vector3.zero;
            Plane plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            float distance;
            if (!plane.Raycast(ray, out distance))
            {
                return false;
            }
            point = ray.GetPoint(distance);
            return true;
        }

        private Vector3 ClampToBoard(Vector3 point)
        {
            return new Vector3(
                Mathf.Clamp(point.x, boardMinX, boardMaxX),
                dragHeight,
                Mathf.Clamp(point.z, boardMinZ, boardMaxZ));
        }

        private void SyncPointerCursor()
        {
            if (pointerCursor != null && pointerReady)
            {
                pointerCursor.SetPosition(pointerWorld);
            }
        }

        private SortableObject FindObjectUnderPointer(Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                SortableObject item = hits[i].collider.GetComponentInParent<SortableObject>();
                if (item != null && !item.IsPlaced)
                {
                    return item;
                }
            }
            return null;
        }

        private ColorBasket FindBasketUnderPointer(Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                ColorBasket basket = hits[i].collider.GetComponentInParent<ColorBasket>();
                if (basket != null)
                {
                    return basket;
                }
            }
            return null;
        }

        private SortableObject FindNearestObject(Vector3 point, float radius)
        {
            SortableObject best = null;
            float bestDistance = radius * radius;
            if (sortableObjects == null)
            {
                return null;
            }
            for (int i = 0; i < sortableObjects.Length; i++)
            {
                SortableObject item = sortableObjects[i];
                if (item == null || item.IsPlaced ||
                    !Progress.CanStartPlacement(item.ObjectId))
                {
                    continue;
                }
                Vector2 delta = new Vector2(
                    item.transform.position.x - point.x,
                    item.transform.position.z - point.z);
                float distance = delta.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = item;
                }
            }
            return best;
        }

        private ColorBasket FindNearestBasket(Vector3 point, float radius)
        {
            ColorBasket best = null;
            float bestDistance = radius * radius;
            if (baskets == null)
            {
                return null;
            }
            for (int i = 0; i < baskets.Length; i++)
            {
                if (baskets[i] == null)
                {
                    continue;
                }
                Vector2 delta = new Vector2(
                    baskets[i].transform.position.x - point.x,
                    baskets[i].transform.position.z - point.z);
                float distance = delta.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = baskets[i];
                }
            }
            return best;
        }

        private void SetBasketHighlight(ColorBasket active)
        {
            if (baskets == null)
            {
                return;
            }
            for (int i = 0; i < baskets.Length; i++)
            {
                if (baskets[i] != null)
                {
                    baskets[i].SetHighlighted(ReferenceEquals(baskets[i], active));
                }
            }
        }

        private float NormalizeX(float worldX)
        {
            return Mathf.InverseLerp(boardMinX, boardMaxX, worldX);
        }

        private float NormalizeY(float worldZ)
        {
            // Protocol origin is top-left, so +z (toward the baskets) maps to 0.
            return Mathf.InverseLerp(boardMaxZ, boardMinZ, worldZ);
        }
    }
}
