using UnityEngine;
using UnityEngine.UI;

namespace CleanMyRoom.UI.Taste
{
    /// <summary>
    /// One draggable food object in the taste activity. It stores only its
    /// identity and visual state; the correct taste category is never stored
    /// here or anywhere else in Unity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TasteFoodItem : MonoBehaviour
    {
        [SerializeField] private string objectId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private Text labelText;
        [SerializeField] private Image background;

        private RectTransform cachedRect;
        private Vector2 homePosition;
        private bool dragging;
        private bool completed;

        public string ObjectId
        {
            get { return objectId; }
        }

        public string DisplayName
        {
            get { return displayName; }
        }

        public bool IsDragging
        {
            get { return dragging; }
        }

        public bool IsCompleted
        {
            get { return completed; }
        }

        public RectTransform RectTransform
        {
            get
            {
                if (cachedRect == null)
                {
                    cachedRect = GetComponent<RectTransform>();
                }
                return cachedRect;
            }
        }

        /// <summary>Axis-aligned rectangle in canvas-local coordinates.</summary>
        public Rect CanvasRect
        {
            get
            {
                RectTransform rect = RectTransform;
                Vector2 size = rect.rect.size;
                Vector2 centre = rect.anchoredPosition;
                return new Rect(
                    centre.x - size.x * 0.5f,
                    centre.y - size.y * 0.5f,
                    size.x,
                    size.y);
            }
        }

        public Vector2 HomePosition
        {
            get { return homePosition; }
        }

        public void Configure(string newObjectId, string newDisplayName, Text newLabelText, Image newBackground)
        {
            objectId = newObjectId ?? string.Empty;
            displayName = newDisplayName ?? string.Empty;
            labelText = newLabelText;
            background = newBackground;
            homePosition = RectTransform.anchoredPosition;
            SetLabel(displayName);
        }

        public void SetLabel(string text)
        {
            if (labelText != null)
            {
                labelText.text = text ?? string.Empty;
            }
        }

        public void BeginDrag()
        {
            dragging = true;
            transform.SetAsLastSibling();
        }

        public void MoveTo(Vector2 canvasLocalPoint)
        {
            RectTransform.anchoredPosition = canvasLocalPoint;
        }

        public void EndDrag()
        {
            dragging = false;
        }

        public void ReturnHome()
        {
            RectTransform.anchoredPosition = homePosition;
            dragging = false;
        }

        /// <summary>Visual mark only; called after MATLAB reports a correct placement.</summary>
        public void SetCompletedDisplay(bool value)
        {
            completed = value;
            if (background != null)
            {
                background.color = value
                    ? new Color(0.72f, 0.88f, 0.70f, 1f)
                    : new Color(1f, 1f, 1f, 1f);
            }
        }
    }
}
