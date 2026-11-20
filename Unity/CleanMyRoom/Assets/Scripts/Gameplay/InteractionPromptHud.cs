using UnityEngine;

namespace CleanMyRoom.Gameplay
{
    /// <summary>
    /// Lightweight IMGUI prompt that follows the same no-package approach as
    /// ConnectionStatusHud. It only reads InteractionController state, so it
    /// cannot decide when a prompt should be visible.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractionController))]
    public sealed class InteractionPromptHud : MonoBehaviour
    {
        [SerializeField] private Vector2 panelSize = new Vector2(420f, 46f);
        [SerializeField] private float bottomMargin = 110f;

        private InteractionController controller;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;

        private void Awake()
        {
            controller = GetComponent<InteractionController>();
        }

        private void OnGUI()
        {
            if (controller == null || !controller.IsPromptVisible)
            {
                return;
            }

            EnsureStyles();

            float width = Mathf.Min(panelSize.x, Screen.width - 24f);
            float height = panelSize.y;
            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                Mathf.Max(8f, Screen.height - bottomMargin - height),
                width,
                height);

            GUI.Box(rect, GUIContent.none, panelStyle);
            GUI.Label(rect, controller.PromptText, labelStyle);
        }

        private void EnsureStyles()
        {
            if (panelStyle != null && labelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakeTexture(new Color(0.05f, 0.08f, 0.12f, 0.78f));

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 20;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = Color.white;
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
