using UnityEngine;

namespace CleanMyRoom.Gameplay
{
    /// <summary>
    /// One bedroom object that can be approached and activated with E.
    ///
    /// The solid BoxCollider blocks movement. The SphereCollider is a trigger
    /// used to discover the object and never blocks the character. The actual
    /// range check uses the interaction anchor, so furniture with a tall visual
    /// still has a predictable, ground-level interaction area.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableFurniture : MonoBehaviour
    {
        [SerializeField] private string activityId = ActivityIds.Color;
        [SerializeField] private string displayName = "Furniture";

        // Deliberately empty: Issue #14 must not guess activity scene paths.
        [SerializeField] private string scenePath = string.Empty;

        [SerializeField, Min(0.5f)] private float interactionRadius = 2.6f;
        [SerializeField] private Transform interactionAnchor;

        private SphereCollider trigger;

        public string ActivityId
        {
            get { return activityId; }
            set { activityId = ActivityIds.IsKnown(value) ? value : ActivityIds.Color; }
        }

        public string DisplayName
        {
            get { return displayName; }
            set { displayName = string.IsNullOrWhiteSpace(value) ? "Furniture" : value; }
        }

        public string ScenePath
        {
            get { return scenePath; }
            set { scenePath = value ?? string.Empty; }
        }

        public float InteractionRadius
        {
            get { return interactionRadius; }
            set { interactionRadius = Mathf.Max(0.5f, value); }
        }

        public Transform InteractionAnchor
        {
            get { return interactionAnchor; }
            set
            {
                interactionAnchor = value;
                EnsureTrigger();
            }
        }

        public Vector3 InteractionPoint
        {
            get { return interactionAnchor != null ? interactionAnchor.position : transform.position; }
        }

        public bool HasConfiguredActivityScene
        {
            get { return !string.IsNullOrWhiteSpace(scenePath); }
        }

        public string PromptText
        {
            get
            {
                return "Press E to interact: " + displayName + " - " +
                       ActivityIds.DisplayName(activityId);
            }
        }

        /// <summary>Used by editor setup and tests to configure one object.</summary>
        public void Configure(
            string newActivityId,
            string newDisplayName,
            string newScenePath,
            float newInteractionRadius = 2.6f)
        {
            ActivityId = newActivityId;
            DisplayName = newDisplayName;
            ScenePath = newScenePath;
            InteractionRadius = newInteractionRadius;
            EnsureTrigger();
        }

        public bool IsInRange(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(playerPosition, InteractionPoint);
            return distance <= interactionRadius;
        }

        public float SquaredDistanceTo(Vector3 playerPosition)
        {
            Vector3 delta = playerPosition - InteractionPoint;
            return delta.sqrMagnitude;
        }

        private void Awake()
        {
            EnsureTrigger();
        }

        private void OnValidate()
        {
            EnsureTrigger();
        }

        private void EnsureTrigger()
        {
            trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.radius = interactionRadius;
            trigger.center = interactionAnchor != null
                ? transform.InverseTransformPoint(interactionAnchor.position)
                : Vector3.up * 0.8f;
        }
    }
}
