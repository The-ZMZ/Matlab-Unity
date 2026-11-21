using UnityEngine;
using UnityEngine.UI;

namespace CleanMyRoom.UI
{
    /// <summary>
    /// Binds the English catalog to the Main Menu labels and plays the button
    /// click cue. Scene button callbacks remain wired to MainMenuController,
    /// so existing Start Game behaviour is preserved.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private UiTextCatalog textCatalog;
        [SerializeField] private Text titleText;
        [SerializeField] private Text startLabel;
        [SerializeField] private Text quitLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        public UiTextCatalog TextCatalog
        {
            get { return textCatalog; }
        }

        private void Awake()
        {
            ApplyText();

            if (startButton != null)
            {
                startButton.onClick.AddListener(HandleButtonClicked);
            }
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(HandleButtonClicked);
            }
        }

        private void HandleButtonClicked()
        {
            AudioManager.Instance.PlayButtonClick();
        }

        private void ApplyText()
        {
            if (textCatalog == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = textCatalog.Get(UiTextKeys.MainTitle);
            }
            if (startLabel != null)
            {
                startLabel.text = textCatalog.Get(UiTextKeys.MainStart);
            }
            if (quitLabel != null)
            {
                quitLabel.text = textCatalog.Get(UiTextKeys.MainQuit);
            }
        }
    }
}
