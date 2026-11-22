using UnityEngine;
using UnityEngine.UI;

namespace CleanMyRoom.UI.Taste
{
    /// <summary>
    /// One taste classification area. The zone only identifies itself as a
    /// drop target; it never decides whether a food item belongs here.
    /// MATLAB owns that decision (AGENTS.md sections 3 and 4).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TasteDropZone : MonoBehaviour
    {
        [SerializeField] private string targetId = "sweet";
        [SerializeField] private Text labelText;

        private RectTransform cachedRect;

        public string TargetId
        {
            get { return targetId; }
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

        public void Configure(string newTargetId, Text newLabelText)
        {
            targetId = newTargetId;
            labelText = newLabelText;
        }

        public void SetLabel(string label)
        {
            if (labelText != null)
            {
                labelText.text = label ?? string.Empty;
            }
        }
    }
}
