using System;
using UnityEngine;

namespace CleanMyRoom.Gameplay
{
    /// <summary>
    /// Decouples furniture interaction from scene loading. Interaction code only
    /// raises requests; SceneTransitionController decides if and how to load a
    /// scene. This keeps scene paths in one place and makes the behaviour testable
    /// without actually loading a scene.
    /// </summary>
    public static class ActivityNavigation
    {
        public const string BedroomScenePath = "Assets/Scenes/Bedroom.unity";

        /// <summary>Arguments: activity id, configured activity scene path.</summary>
        public static event Action<string, string> OpenRequested;

        /// <summary>Raised after an activity reports successful completion.</summary>
        public static event Action ReturnToBedroomRequested;

        /// <summary>
        /// Request an activity scene. Empty paths are rejected so the player is
        /// never sent to a placeholder or unrelated scene while activities are
        /// still being implemented.
        /// </summary>
        public static bool RequestOpen(string activityId, string scenePath)
        {
            if (!ActivityIds.IsKnown(activityId))
            {
                Debug.LogWarning("[Issue14] Ignoring activity request with unknown id: " + activityId);
                return false;
            }
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                Debug.LogWarning(
                    "[Issue14] No scene is configured for " + activityId +
                    ". The activity was not opened.");
                return false;
            }

            Action<string, string> handler = OpenRequested;
            if (handler != null)
            {
                handler(activityId, scenePath);
            }
            return true;
        }

        public static void RequestReturnToBedroom()
        {
            Action handler = ReturnToBedroomRequested;
            if (handler != null)
            {
                handler();
            }
        }

#if UNITY_EDITOR
        /// <summary>Removes stale listeners between EditMode tests.</summary>
        public static void ClearListenersForTests()
        {
            OpenRequested = null;
            ReturnToBedroomRequested = null;
        }
#endif
    }
}
