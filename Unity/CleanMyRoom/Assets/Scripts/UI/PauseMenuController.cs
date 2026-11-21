using CleanMyRoom.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CleanMyRoom.UI
{
    /// <summary>
    /// Esc-only pause menu. It operates exclusively in the four gameplay
    /// scenes and owns Time.timeScale so a scene exit cannot leave the game
    /// paused. Resume and Main Menu buttons also play the button click cue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private UiTextCatalog textCatalog;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text resumeLabel;
        [SerializeField] private Text mainMenuLabel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        private bool paused;

        private static bool IsGameplayScene(string sceneName)
        {
            return string.Equals(sceneName, SceneNames.Bedroom) ||
                   string.Equals(sceneName, SceneNames.ColorSorting) ||
                   string.Equals(sceneName, SceneNames.TasteSorting) ||
                   string.Equals(sceneName, SceneNames.MathActivity);
        }

        private void Awake()
        {
            ApplyText();

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(Resume);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }

            paused = false;
            if (panel != null)
            {
                panel.SetActive(false);
            }
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (paused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        private void OnDisable()
        {
            // A scene unload while paused must restore normal time flow.
            Time.timeScale = 1f;
            paused = false;
        }

        public void Pause()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
            Time.timeScale = 0f;
            paused = true;
        }

        public void Resume()
        {
            AudioManager.Instance.PlayButtonClick();
            Hide();
        }

        public void ReturnToMainMenu()
        {
            AudioManager.Instance.PlayButtonClick();
            Hide();
            SceneLoader.LoadMainMenu();
        }

        private void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
            Time.timeScale = 1f;
            paused = false;
        }

        private void ApplyText()
        {
            if (textCatalog == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = textCatalog.Get(UiTextKeys.PauseTitle);
            }
            if (resumeLabel != null)
            {
                resumeLabel.text = textCatalog.Get(UiTextKeys.PauseResume);
            }
            if (mainMenuLabel != null)
            {
                mainMenuLabel.text = textCatalog.Get(UiTextKeys.PauseMainMenu);
            }
        }
    }
}
