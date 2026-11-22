using UnityEngine;

namespace CleanMyRoom.ColorSorting
{
    /// <summary>
    /// A drop target. The basket knows its identity and how to highlight it;
    /// it never decides whether a placed object belongs to it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ColorBasket : MonoBehaviour
    {
        [SerializeField] private string basketId = string.Empty;
        [SerializeField] private Renderer[] highlightRenderers;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 1f);

        private Color[] baseColors;
        private bool highlighted;

        public string BasketId { get { return basketId; } }

        public void Configure(string id, Renderer[] renderers)
        {
            basketId = id;
            highlightRenderers = renderers;
            CaptureBaseColors();
        }

        private void Awake()
        {
            CaptureBaseColors();
        }

        private void CaptureBaseColors()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                baseColors = new Color[0];
                return;
            }

            baseColors = new Color[highlightRenderers.Length];
            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                Renderer renderer = highlightRenderers[i];
                Material material = renderer != null ? renderer.sharedMaterial : null;
                baseColors[i] = material != null ? material.color : Color.white;
            }
        }

        public void SetHighlighted(bool value)
        {
            if (highlighted == value)
            {
                return;
            }
            highlighted = value;

            if (baseColors == null || baseColors.Length != highlightRenderers.Length)
            {
                CaptureBaseColors();
            }

            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                Renderer renderer = highlightRenderers[i];
                if (renderer == null)
                {
                    continue;
                }
                // In the editor, changing renderer.material would instantiate a
                // new asset on every call. Use the shared material while editing
                // (the screenshot builder restores it) and a runtime instance in
                // Play Mode so gameplay cannot mutate project assets.
                Material material = Application.isPlaying
                    ? renderer.material
                    : renderer.sharedMaterial;
                if (material == null)
                {
                    continue;
                }
                material.color = value
                    ? Color.Lerp(baseColors[i], highlightColor, 0.55f)
                    : baseColors[i];
            }
        }

        /// <summary>World-space point where a correctly validated object rests.</summary>
        public Vector3 SnapPoint(float height)
        {
            return new Vector3(transform.position.x, height, transform.position.z);
        }
    }
}
