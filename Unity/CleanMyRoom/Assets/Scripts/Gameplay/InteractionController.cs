using System;
using System.Collections.Generic;
using CleanMyRoom.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanMyRoom.Gameplay
{
    /// <summary>
    /// Selects the one nearby furniture object the player can interact with.
    ///
    /// It is an additive component on the Issue #13 player: movement stays in
    /// PlayerController. Selection is deterministic (nearest, then activity id,
    /// then instance id) so two overlapping ranges never produce an ambiguous
    /// prompt. The prompt state lives here so the HUD cannot drift out of sync.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float searchRadius = 8f;

        private readonly List<InteractableFurniture> candidates = new List<InteractableFurniture>();

        public InteractableFurniture CurrentTarget { get; private set; }

        public bool IsPromptVisible
        {
            get { return CurrentTarget != null; }
        }

        public string PromptText
        {
            get { return CurrentTarget != null ? CurrentTarget.PromptText : string.Empty; }
        }

        private void Update()
        {
            Refresh();
            HandleInteractionInput();
        }

        private void OnDisable()
        {
            // The player is destroyed when an activity scene loads. Clearing here
            // guarantees the prompt cannot survive the transition.
            CurrentTarget = null;
        }

        /// <summary>Recompute the current target from nearby trigger colliders.</summary>
        public void Refresh()
        {
            FindCandidates();
            CurrentTarget = SelectNearest(candidates, transform.position);
        }

        /// <summary>
        /// Handle one E-key press. Public so EditMode tests can drive exactly the
        /// same path that Update() uses.
        /// </summary>
        public bool HandleInteractionInput()
        {
            if (!ReadInteractPressed())
            {
                return false;
            }
            return TryInteract();
        }

        /// <summary>Activate the currently selected furniture, if any.</summary>
        public bool TryInteract()
        {
            if (CurrentTarget == null)
            {
                return false;
            }

            if (!CurrentTarget.HasConfiguredActivityScene)
            {
                Debug.LogWarning(
                    "[Issue14] " + CurrentTarget.DisplayName +
                    " has no activity scene configured; interaction ignored.");
                return false;
            }

            return ActivityNavigation.RequestOpen(
                CurrentTarget.ActivityId, CurrentTarget.ScenePath);
        }

        public static bool ReadInteractPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.eKey.wasPressedThisFrame;
        }

        /// <summary>
        /// Deterministic selection: nearest in-range object wins. Exact ties are
        /// broken by activity id, then instance id, so the prompt never flickers
        /// between overlapping targets.
        /// </summary>
        public static InteractableFurniture SelectNearest(
            IEnumerable<InteractableFurniture> source, Vector3 playerPosition)
        {
            InteractableFurniture best = null;
            float bestDistance = float.PositiveInfinity;

            if (source == null)
            {
                return null;
            }

            foreach (InteractableFurniture candidate in source)
            {
                if (candidate == null || !candidate.isActiveAndEnabled ||
                    !candidate.IsInRange(playerPosition))
                {
                    continue;
                }

                float distance = candidate.SquaredDistanceTo(playerPosition);
                bool isBetter = distance < bestDistance - 0.0001f;
                if (!isBetter && Mathf.Abs(distance - bestDistance) <= 0.0001f && best != null)
                {
                    int idOrder = string.Compare(
                        candidate.ActivityId, best.ActivityId, StringComparison.Ordinal);
                    if (idOrder < 0 ||
                        (idOrder == 0 &&
                         candidate.GetEntityId().GetHashCode() < best.GetEntityId().GetHashCode()))
                    {
                        isBetter = true;
                    }
                }

                if (isBetter)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void FindCandidates()
        {
            candidates.Clear();

            Collider[] hits = Physics.OverlapSphere(
                transform.position, searchRadius, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits.Length; i++)
            {
                InteractableFurniture furniture = hits[i].GetComponent<InteractableFurniture>();
                if (furniture == null)
                {
                    furniture = hits[i].GetComponentInParent<InteractableFurniture>();
                }
                if (furniture == null || candidates.Contains(furniture))
                {
                    continue;
                }

                candidates.Add(furniture);
            }

            // Also include active objects whose anchor is in range but whose
            // trigger was temporarily disabled by a scene setup script.
            InteractableFurniture[] all = FindObjectsByType<InteractableFurniture>();
            for (int i = 0; i < all.Length; i++)
            {
                if (!candidates.Contains(all[i]))
                {
                    candidates.Add(all[i]);
                }
            }
        }
    }
}
