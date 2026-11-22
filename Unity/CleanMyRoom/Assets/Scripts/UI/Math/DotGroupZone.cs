using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CleanMyRoom.UI.Math
{
    /// <summary>
    /// One dot-group drop area. The zone parses its dot count from the target
    /// id (for example "dots_7") and renders that many dots. It never decides
    /// whether a number belongs here; MATLAB owns that decision.
    ///
    /// The approved minimal approach keeps the wire protocol unchanged: no
    /// layout data is sent by MATLAB, and Unity only draws the dots for display.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DotGroupZone : MonoBehaviour
    {
        private const int MaxDots = 10;
        private const int MaxColumns = 5;
        private const float DotSpacing = 20f;
        private const float DotSize = 12f;

        [SerializeField] private string targetId = "dots_1";
        [SerializeField] private RectTransform dotsContainer;

        private readonly List<GameObject> dotObjects = new List<GameObject>();
        private RectTransform cachedRect;

        public string TargetId
        {
            get { return targetId; }
        }

        /// <summary>Dot count derived from the target id, or zero if invalid.</summary>
        public int DotCount
        {
            get
            {
                int separator = targetId.LastIndexOf('_');
                if (separator < 0 || separator >= targetId.Length - 1)
                {
                    return 0;
                }

                int value;
                string suffix = targetId.Substring(separator + 1);
                if (!int.TryParse(suffix, out value) || value < 1 || value > MaxDots)
                {
                    return 0;
                }
                return value;
            }
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

        public void Configure(string newTargetId, RectTransform newDotsContainer)
        {
            targetId = newTargetId ?? string.Empty;
            dotsContainer = newDotsContainer;
        }

        /// <summary>Create the visual dots for the current target id.</summary>
        public void RebuildDots()
        {
            ClearDots();
            if (dotsContainer == null)
            {
                return;
            }

            int count = DotCount;
            int columns = System.Math.Min(count, MaxColumns);
            if (columns <= 0)
            {
                return;
            }

            int rows = (count + columns - 1) / columns;
            for (int i = 0; i < count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                float x = (column - (columns - 1) * 0.5f) * DotSpacing;
                float y = ((rows - 1) * 0.5f - row) * DotSpacing;

                GameObject dot = new GameObject("Dot_" + (i + 1),
                    typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(dotsContainer, false);
                RectTransform rect = dot.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(DotSize, DotSize);
                rect.anchoredPosition = new Vector2(x, y);

                Image image = dot.GetComponent<Image>();
                image.color = new Color(0.16f, 0.32f, 0.55f, 1f);
                dotObjects.Add(dot);
            }
        }

        public int RenderedDotCount
        {
            get { return dotObjects.Count; }
        }

        private void ClearDots()
        {
            for (int i = 0; i < dotObjects.Count; i++)
            {
                if (dotObjects[i] != null)
                {
                    DestroyImmediate(dotObjects[i]);
                }
            }
            dotObjects.Clear();
        }
    }
}
