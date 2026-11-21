using System.Collections;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Net;
using UnityEngine;
using UnityEngine.UI;

namespace CleanMyRoom.UI
{
    /// <summary>
    /// Success and error feedback panel. It only displays fields supplied by
    /// MATLAB messages or authoritative completion events; it never validates
    /// an answer in Unity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeedbackPanelController : MonoBehaviour
    {
        [SerializeField] private UiTextCatalog textCatalog;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button closeButton;
        [SerializeField, Min(1f)] private float displaySeconds = 4f;

        private Coroutine hideRoutine;
        private bool networkSubscribed;

        private void OnEnable()
        {
            GameProgress.ActivityCompleted -= HandleActivityCompleted;
            GameProgress.ActivityCompleted += HandleActivityCompleted;
            SubscribeNetworkEvents();
        }

        private void Start()
        {
            SubscribeNetworkEvents();
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void Update()
        {
            if (!networkSubscribed)
            {
                SubscribeNetworkEvents();
            }
        }

        private void OnDisable()
        {
            GameProgress.ActivityCompleted -= HandleActivityCompleted;
            NetworkManager manager = NetworkManager.Instance;
            if (manager != null && networkSubscribed)
            {
                manager.MessageReceived -= HandleMessage;
            }
            networkSubscribed = false;
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }
        }

        private void SubscribeNetworkEvents()
        {
            NetworkManager manager = NetworkManager.Instance;
            if (manager == null || networkSubscribed)
            {
                return;
            }
            manager.MessageReceived -= HandleMessage;
            manager.MessageReceived += HandleMessage;
            networkSubscribed = true;
        }

        private void HandleMessage(WireMessage message)
        {
            ProcessWireMessage(message);
        }

        /// <summary>Displays the authoritative MATLAB verdict; performs no validation.</summary>
        public void ProcessWireMessage(WireMessage message)
        {
            if (message == null)
            {
                return;
            }

            if (message.IsType(ProtocolConstants.TypeError))
            {
                ShowError(message.message);
                return;
            }

            if (message.IsType(ProtocolConstants.TypePlaceResponse) && !message.valid)
            {
                ShowError(message.message);
                return;
            }

            if (message.IsType(ProtocolConstants.TypeActivityResult) && message.completed)
            {
                ShowSuccess(message.message);
            }
        }

        private void HandleActivityCompleted(string activityId)
        {
            ShowSuccess(string.Empty);
        }

        public void ShowSuccess(string detail)
        {
            Show(textCatalog != null ? textCatalog.Get(UiTextKeys.FeedbackSuccess) : "Well done!", detail);
        }

        public void ShowError(string detail)
        {
            Show(textCatalog != null ? textCatalog.Get(UiTextKeys.FeedbackError) : "Please try again.", detail);
        }

        /// <summary>Manual close action for the feedback panel.</summary>
        public void Hide()
        {
            AudioManager.Instance.PlayButtonClick();
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void Show(string title, string detail)
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
            if (titleText != null)
            {
                titleText.text = title;
            }
            if (messageText != null)
            {
                messageText.text = string.IsNullOrEmpty(detail) ? string.Empty : detail;
            }
            if (closeButton != null)
            {
                Text label = closeButton.GetComponentInChildren<Text>();
                if (label != null && textCatalog != null)
                {
                    label.text = textCatalog.Get(UiTextKeys.FeedbackClose);
                }
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(displaySeconds);
            if (panel != null)
            {
                panel.SetActive(false);
            }
            hideRoutine = null;
        }
    }
}
