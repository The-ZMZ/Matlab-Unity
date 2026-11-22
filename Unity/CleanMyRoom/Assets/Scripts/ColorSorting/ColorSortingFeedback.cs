using UnityEngine;

namespace CleanMyRoom.ColorSorting
{
    /// <summary>
    /// Visual feedback for the Color Sorting activity. All text is a direct
    /// presentation of MATLAB replies; this component never evaluates answers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ColorSortingFeedback : MonoBehaviour
    {
        [SerializeField] private TextMesh instructionLabel;
        [SerializeField] private TextMesh feedbackLabel;
        [SerializeField] private TextMesh progressLabel;
        [SerializeField] private TextMesh connectionLabel;
        [SerializeField] private GameObject completionBanner;
        [SerializeField] private GameObject correctBadge;
        [SerializeField] private GameObject errorBadge;
        [SerializeField] private float feedbackSeconds = 2.2f;

        private float feedbackExpiresAt = -1f;
        private readonly Color neutralText = new Color(0.16f, 0.20f, 0.32f);

        public void Configure(
            TextMesh instruction, TextMesh feedback, TextMesh progress,
            TextMesh connection, GameObject banner, GameObject correct, GameObject error)
        {
            instructionLabel = instruction;
            feedbackLabel = feedback;
            progressLabel = progress;
            connectionLabel = connection;
            completionBanner = banner;
            correctBadge = correct;
            errorBadge = error;
            Reset();
        }

        private void Update()
        {
            if (feedbackLabel != null && feedbackExpiresAt >= 0f &&
                Time.unscaledTime >= feedbackExpiresAt)
            {
                feedbackLabel.text = string.Empty;
                feedbackExpiresAt = -1f;
            }
        }

        public void Reset()
        {
            feedbackExpiresAt = -1f;
            if (feedbackLabel != null)
            {
                feedbackLabel.text = string.Empty;
                feedbackLabel.color = neutralText;
            }
            if (instructionLabel != null)
            {
                instructionLabel.text =
                    "Drag each toy to the basket of the same colour!\n" +
                    "Mouse: click, drag and release   |   Keyboard: arrows + Space   |   Esc: Bedroom";
            }
            UpdateProgress(0, ColorSortingCatalog.ObjectCount);
            if (completionBanner != null)
            {
                completionBanner.SetActive(false);
            }
            SetBadge(correctBadge, false);
            SetBadge(errorBadge, false);
        }

        public void ShowFeedback(string message, bool correct)
        {
            if (feedbackLabel == null)
            {
                return;
            }
            feedbackLabel.text = message ?? string.Empty;
            feedbackLabel.color = correct
                ? new Color(0.05f, 0.48f, 0.20f)
                : new Color(0.70f, 0.12f, 0.16f);
            feedbackExpiresAt = Time.unscaledTime + feedbackSeconds;
            SetBadge(correctBadge, correct);
            SetBadge(errorBadge, !correct);
        }

        public void ShowNotice(string message)
        {
            if (feedbackLabel == null)
            {
                return;
            }
            feedbackLabel.text = message ?? string.Empty;
            feedbackLabel.color = new Color(0.28f, 0.30f, 0.40f);
            feedbackExpiresAt = Time.unscaledTime + feedbackSeconds;
            SetBadge(correctBadge, false);
            SetBadge(errorBadge, false);
        }

        public void UpdateProgress(int correct, int total)
        {
            if (progressLabel != null)
            {
                progressLabel.text = "Matched: " + correct + " / " + total;
            }
        }

        public void UpdateStars(int correct, int stars)
        {
            if (progressLabel != null)
            {
                progressLabel.text = "Matched: " + correct + " / " +
                    ColorSortingCatalog.ObjectCount + "   Stars: " + stars;
            }
        }

        public void UpdateConnection(string status, bool activitiesEnabled)
        {
            if (connectionLabel == null)
            {
                return;
            }
            connectionLabel.text = "MATLAB: " + status +
                (activitiesEnabled ? "" : "   (sorting disabled - waiting for MATLAB)");
            connectionLabel.color = activitiesEnabled
                ? new Color(0.10f, 0.42f, 0.24f)
                : new Color(0.72f, 0.18f, 0.15f);
        }

        public void ShowCompletion(string message)
        {
            SetBadge(correctBadge, false);
            SetBadge(errorBadge, false);
            if (completionBanner != null)
            {
                completionBanner.SetActive(true);
            }
            if (feedbackLabel != null)
            {
                feedbackLabel.text = message ?? "Well done - all toys are sorted!";
                feedbackLabel.color = new Color(0.05f, 0.48f, 0.20f);
                feedbackExpiresAt = -1f;
            }
        }

        private static void SetBadge(GameObject badge, bool active)
        {
            if (badge != null)
            {
                badge.SetActive(active);
            }
        }
    }
}
