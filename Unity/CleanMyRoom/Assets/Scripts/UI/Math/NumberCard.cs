using UnityEngine;
using UnityEngine.UI;

namespace CleanMyRoom.UI.Math
{
    /// <summary>
    /// One draggable number card in the MATH activity. It stores only its
    /// identity and visual state; the correct dot group is never stored here
    /// or anywhere else in Unity (AGENTS.md sections 3 and 4).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NumberCard : MonoBehaviour
    {
        [SerializeField] private string objectId = string.Empty;
        [SerializeField] private string displayNumber = string.Empty;
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

        public string DisplayNumber
        {
            get { return displayNumber; }
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

        public void Configure(string newObjectId, string newDisplayNumber, Text newLabelText, Image newBackground)
        {
            objectId = newObjectId ?? string.Empty;
            displayNumber = newDisplayNumber ?? string.Empty;
            labelText = newLabelText;
            background = newBackground;
            homePosition = RectTransform.anchoredPosition;
            SetLabel(displayNumber);
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
