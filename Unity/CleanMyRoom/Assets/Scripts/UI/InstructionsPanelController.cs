using UnityEngine;
using UnityEngine.UI;

namespace CleanMyRoom.UI
{
    /// <summary>
    /// One reusable instructions panel for all three activity levels. Only the
    /// configured text key differs between scenes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InstructionsPanelController : MonoBehaviour
    {
        [SerializeField] private UiTextCatalog textCatalog;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button closeButton;
        [SerializeField] private string textKey = UiTextKeys.InstructionsColor;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            ApplyText();
            Show();
        }

        public void Show()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        public void Hide()
        {
            AudioManager.Instance.PlayButtonClick();
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void SetTextKey(string key)
        {
            textKey = key;
            ApplyText();
        }

        public string TextKey
        {
            get { return textKey; }
        }

        public UiTextCatalog TextCatalog
        {
            get { return textCatalog; }
            set { textCatalog = value; ApplyText(); }
        }

        private void ApplyText()
        {
            if (textCatalog == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = textCatalog.Get(UiTextKeys.InstructionsTitle);
            }
            if (bodyText != null)
            {
                bodyText.text = textCatalog.Get(textKey);
            }
            if (closeButton != null)
            {
                Text label = closeButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = textCatalog.Get(UiTextKeys.InstructionsClose);
                }
            }
        }
    }
}
