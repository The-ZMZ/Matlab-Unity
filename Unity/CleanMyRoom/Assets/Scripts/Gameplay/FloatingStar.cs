using System;
using UnityEngine;

namespace CleanMyRoom.Gameplay
{
    /// <summary>
    /// One reusable floating star. All three bedroom stars use this prefab and
    /// only differ by their configured activity id. Completion state comes from
    /// GameProgress; the star never decides whether an activity was successful.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloatingStar : MonoBehaviour
    {
        /// <summary>
        /// Raised only when a live MATLAB-driven completion collects this star.
        /// Audio listens to this event; star behaviour itself is unchanged.
        /// </summary>
        public static event Action<string> StarCollected;

        [SerializeField] private string activityId = ActivityIds.Color;
        [SerializeField] private Transform visual;
        [SerializeField] private Light glowLight;

        [SerializeField, Min(0f)] private float bobAmplitude = 0.12f;
        [SerializeField, Min(0f)] private float bobSpeed = 1.6f;
        [SerializeField, Min(0f)] private float rotationSpeed = 35f;

        private Vector3 baseLocalPosition;
        private bool subscribed;

        public string ActivityId
        {
            get { return activityId; }
            set { activityId = ActivityIds.IsKnown(value) ? value : ActivityIds.Color; }
        }

        public bool IsCompleted { get; private set; }

        public Transform Visual
        {
            get { return visual; }
            set { visual = value; }
        }

        public Light GlowLight
        {
            get { return glowLight; }
            set { glowLight = value; }
        }

        /// <summary>Used by editor setup and tests to configure one prefab instance.</summary>
        public void Configure(string newActivityId, Transform newVisual, Light newGlowLight)
        {
            ActivityId = newActivityId;
            visual = newVisual;
            glowLight = newGlowLight;
        }

        private void Awake()
        {
            if (visual == null)
            {
                visual = transform;
            }
            baseLocalPosition = visual.localPosition;
        }

        private void OnEnable()
        {
            EnsureSubscribed();
        }

        /// <summary>
        /// Idempotent subscription setup. Runtime calls this from OnEnable;
        /// EditMode tests call it explicitly because lifecycle methods are not
        /// invoked for components added from editor code.
        /// </summary>
        public void EnsureSubscribed()
        {
            if (!subscribed)
            {
                GameProgress.CompletionChanged += HandleProgressChanged;
                subscribed = true;
            }
            RefreshFromProgress();
        }

        private void OnDisable()
        {
            if (subscribed)
            {
                GameProgress.CompletionChanged -= HandleProgressChanged;
                subscribed = false;
            }
        }

        private void Update()
        {
            if (IsCompleted || visual == null)
            {
                return;
            }

            float time = Time.unscaledTime;
            Vector3 offset = Vector3.up * (Mathf.Sin(time * bobSpeed) * bobAmplitude);
            visual.localPosition = baseLocalPosition + offset;
            visual.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime, Space.Self);

            if (glowLight != null)
            {
                glowLight.intensity = 0.75f + Mathf.Sin(time * bobSpeed * 1.7f) * 0.15f;
            }
        }

        private void HandleProgressChanged(string changedActivityId, bool completed)
        {
            // EditMode scene unloads can leave a stale subscriber behind. Never
            // let one broken listener prevent other stars from updating.
            if (this == null || gameObject == null)
            {
                GameProgress.CompletionChanged -= HandleProgressChanged;
                subscribed = false;
                return;
            }

            if (string.Equals(changedActivityId, activityId, System.StringComparison.Ordinal))
            {
                if (completed && !IsCompleted)
                {
                    Action<string> handler = StarCollected;
                    if (handler != null)
                    {
                        handler(activityId);
                    }
                }
                SetCompleted(completed);
            }
        }

        public void RefreshFromProgress()
        {
            SetCompleted(GameProgress.IsCompleted(activityId));
        }

        public void SetCompleted(bool completed)
        {
            IsCompleted = completed;
            gameObject.SetActive(!completed);
        }
    }
}
