using System;
using System.Collections.Generic;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Net;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanMyRoom.UI.Taste
{
    /// <summary>
    /// Mouse-only interaction layer for the TASTE activity.
    ///
    /// It collects pointer input, converts it to normalized canvas coordinates
    /// and sends a PLACE_REQ through the existing network client. Every verdict
    /// is read from MATLAB's PLACE_RES; this class never decides whether a food
    /// item is correct and never awards a star (AGENTS.md sections 3, 4 and 5).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TasteActivityController : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private TasteFoodItem[] foodItems = new TasteFoodItem[0];
        [SerializeField] private TasteDropZone[] dropZones = new TasteDropZone[0];
        [SerializeField] private UiTextCatalog textCatalog;
        [SerializeField] private FeedbackPanelController feedbackPanel;

        private readonly Dictionary<string, TasteFoodItem> pending =
            new Dictionary<string, TasteFoodItem>(StringComparer.Ordinal);
        private readonly HashSet<string> displayedCompleted =
            new HashSet<string>(StringComparer.Ordinal);

        private TasteFoodItem draggedItem;
        private bool networkSubscribed;

        public int PendingCount
        {
            get { return pending.Count; }
        }

        public int DisplayedCompletedCount
        {
            get { return displayedCompleted.Count; }
        }

        public TasteFoodItem DraggedItem
        {
            get { return draggedItem; }
        }

        public int FoodItemCount
        {
            get { return foodItems.Length; }
        }

        public int DropZoneCount
        {
            get { return dropZones.Length; }
        }

        /// <summary>
        /// Convert a canvas-local point to the protocol's normalized coordinates:
        /// origin top-left, x right, y down, both clamped to [0, 1].
        /// </summary>
        public static Vector2 ToNormalized(Vector2 localPoint, Rect canvas)
        {
            if (canvas.width <= 0f || canvas.height <= 0f)
            {
                return Vector2.zero;
            }

            float x = (localPoint.x - canvas.xMin) / canvas.width;
            float y = (canvas.yMax - localPoint.y) / canvas.height;
            return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
        }

        /// <summary>Wire up the scene objects; also used by EditMode tests.</summary>
        public void Configure(
            RectTransform newCanvasRect,
            TasteFoodItem[] newFoodItems,
            TasteDropZone[] newDropZones,
            UiTextCatalog newTextCatalog,
            FeedbackPanelController newFeedbackPanel)
        {
            canvasRect = newCanvasRect;
            foodItems = newFoodItems ?? new TasteFoodItem[0];
            dropZones = newDropZones ?? new TasteDropZone[0];
            textCatalog = newTextCatalog;
            feedbackPanel = newFeedbackPanel;
            ApplyZoneLabels();
        }

        private static string ZoneLabelKey(string targetId)
        {
            switch (targetId)
            {
                case "sweet": return UiTextKeys.TasteSweet;
                case "sour": return UiTextKeys.TasteSour;
                case "bitter": return UiTextKeys.TasteBitter;
                case "salty": return UiTextKeys.TasteSalty;
                default: return string.Empty;
            }
        }

        private void ApplyZoneLabels()
        {
            if (textCatalog == null)
            {
                return;
            }

            for (int i = 0; i < dropZones.Length; i++)
            {
                TasteDropZone zone = dropZones[i];
                if (zone == null)
                {
                    continue;
                }

                string key = ZoneLabelKey(zone.TargetId);
                if (!string.IsNullOrEmpty(key))
                {
                    zone.SetLabel(textCatalog.Get(key));
                }
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
            ApplyZoneLabels();
        }

        private void OnDisable()
        {
            NetworkManager manager = NetworkManager.Instance;
            if (manager != null && networkSubscribed)
            {
                manager.MessageReceived -= HandleMessage;
            }
            networkSubscribed = false;
        }

        private void Subscribe()
        {
            NetworkManager manager = NetworkManager.Instance;
            if (manager == null || networkSubscribed)
            {
                return;
            }

            manager.MessageReceived -= HandleMessage;
            manager.MessageReceived += HandleMessage;
            networkSubscribed = true;
        }

        private void Update()
        {
            if (!networkSubscribed)
            {
                Subscribe();
            }

            NetworkManager manager = NetworkManager.Instance;
            if (manager == null || !manager.ActivitiesEnabled)
            {
                if (draggedItem != null)
                {
                    CancelDrag();
                }
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || canvasRect == null)
            {
                return;
            }

            Vector2 screenPoint = mouse.position.ReadValue();
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPoint, CanvasCamera(), out localPoint))
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                BeginDragAt(localPoint);
            }

            if (draggedItem == null)
            {
                return;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                DropAt(localPoint);
            }
            else if (mouse.leftButton.isPressed)
            {
                MoveDragged(localPoint);
            }
            else
            {
                CancelDrag();
            }
        }

        private Camera CanvasCamera()
        {
            Canvas canvas = canvasRect != null ? canvasRect.GetComponent<Canvas>() : null;
            return canvas != null ? canvas.worldCamera : null;
        }

        /// <summary>Select a food item at a canvas-local point. One drag at a time.</summary>
        public bool BeginDragAt(Vector2 localPoint)
        {
            if (draggedItem != null)
            {
                return false;
            }

            for (int i = 0; i < foodItems.Length; i++)
            {
                TasteFoodItem item = foodItems[i];
                if (item == null || item.IsCompleted || item.IsDragging)
                {
                    continue;
                }

                if (item.CanvasRect.Contains(localPoint))
                {
                    draggedItem = item;
                    item.BeginDrag();
                    item.MoveTo(localPoint);
                    return true;
                }
            }
            return false;
        }

        public void MoveDragged(Vector2 localPoint)
        {
            if (draggedItem != null)
            {
                draggedItem.MoveTo(localPoint);
            }
        }

        /// <summary>Return the zone under a canvas-local point, or null.</summary>
        public TasteDropZone FindZone(Vector2 localPoint)
        {
            for (int i = 0; i < dropZones.Length; i++)
            {
                TasteDropZone zone = dropZones[i];
                if (zone != null && zone.CanvasRect.Contains(localPoint))
                {
                    return zone;
                }
            }
            return null;
        }

        /// <summary>
        /// Release the dragged item. Unity only identifies the target area and
        /// sends the request; MATLAB performs all validation.
        /// </summary>
        public bool DropAt(Vector2 localPoint)
        {
            if (draggedItem == null)
            {
                return false;
            }

            TasteFoodItem item = draggedItem;
            draggedItem = null;
            item.EndDrag();

            NetworkManager manager = NetworkManager.Instance;
            TasteDropZone zone = FindZone(localPoint);
            if (zone == null || manager == null || !manager.ActivitiesEnabled || canvasRect == null)
            {
                item.ReturnHome();
                return false;
            }

            Vector2 normalized = ToNormalized(localPoint, canvasRect.rect);
            string requestId = manager.SendPlaceRequest(
                ActivityIds.Taste,
                item.ObjectId,
                zone.TargetId,
                normalized.x,
                normalized.y);

            if (string.IsNullOrEmpty(requestId))
            {
                item.ReturnHome();
                return false;
            }

            RegisterPending(requestId, item);
            return true;
        }

        /// <summary>Correlate a MATLAB response with the food item it belongs to.</summary>
        public void RegisterPending(string requestId, TasteFoodItem item)
        {
            if (string.IsNullOrEmpty(requestId) || item == null)
            {
                return;
            }
            pending[requestId] = item;
        }

        private void CancelDrag()
        {
            if (draggedItem != null)
            {
                draggedItem.ReturnHome();
                draggedItem = null;
            }
        }

        private void HandleMessage(WireMessage message)
        {
            HandleResponse(message);
        }

        /// <summary>Display one authoritative PLACE_RES. No local validation.</summary>
        public void HandleResponse(WireMessage message)
        {
            if (message == null || !message.IsType(ProtocolConstants.TypePlaceResponse))
            {
                return;
            }
            if (!string.Equals(message.activity, ActivityIds.Taste, StringComparison.Ordinal))
            {
                return;
            }

            TasteFoodItem item;
            if (!pending.TryGetValue(message.requestId, out item))
            {
                return;
            }
            pending.Remove(message.requestId);

            if (!message.valid)
            {
                // FeedbackPanelController and AudioManager already handle invalid
                // protocol replies globally. Only restore the item here.
                item.ReturnHome();
                return;
            }

            if (message.correct)
            {
                if (displayedCompleted.Add(item.ObjectId))
                {
                    item.SetCompletedDisplay(true);
                }
                ShowFeedback(true, message.message);
                AudioManager.Instance.PlaySuccess();
            }
            else
            {
                item.ReturnHome();
                ShowFeedback(false, message.message);
                AudioManager.Instance.PlayError();
            }
        }

        private void ShowFeedback(bool success, string detail)
        {
            if (feedbackPanel == null)
            {
                return;
            }

            string text = string.IsNullOrEmpty(detail) ? string.Empty : detail;
            if (success)
            {
                feedbackPanel.ShowSuccess(text);
            }
            else
            {
                feedbackPanel.ShowError(text);
            }
        }
    }
}
