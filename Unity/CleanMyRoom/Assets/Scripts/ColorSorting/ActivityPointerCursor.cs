using UnityEngine;

namespace CleanMyRoom.ColorSorting
{
    /// <summary>
    /// Visible pointer used to select and drop objects.
    ///
    /// This is a mouse/keyboard cursor ONLY. In line with AGENTS.md sections 5
    /// and 14, the project has no camera, webcam, hand tracking, gesture or
    /// pinch input. The cursor is moved by the approved mouse and keyboard
    /// inputs; it does not track a physical hand.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActivityPointerCursor : MonoBehaviour
    {
        public enum PointerMode
        {
            Normal,
            Grabbing,
            Disabled
        }

        [SerializeField] private Renderer[] tintRenderers;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color grabbingColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.55f, 0.55f, 0.6f, 1f);

        private PointerMode mode = PointerMode.Normal;

        public void Configure(Renderer[] renderers)
        {
            tintRenderers = renderers;
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetMode(PointerMode value)
        {
            if (mode == value)
            {
                return;
            }
            mode = value;

            if (tintRenderers == null)
            {
                return;
            }
            Color color = value == PointerMode.Grabbing
                ? grabbingColor
                : (value == PointerMode.Disabled ? disabledColor : normalColor);

            for (int i = 0; i < tintRenderers.Length; i++)
            {
                Renderer renderer = tintRenderers[i];
                if (renderer == null)
                {
                    continue;
                }
                // Use the project material in the Editor so screenshot staging
                // cannot leak instantiated materials into the scene. Play Mode
                // keeps a per-instance material so gameplay cannot edit assets.
                Material material = Application.isPlaying
                    ? renderer.material
                    : renderer.sharedMaterial;
                if (material != null)
                {
                    material.color = color;
                }
            }
        }
    }
}
