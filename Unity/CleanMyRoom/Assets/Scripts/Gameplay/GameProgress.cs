using System;
using System.Collections.Generic;
using CleanMyRoom.Net;
using UnityEngine;

namespace CleanMyRoom.Gameplay
{
    /// <summary>
    /// Session-only completion state for the three bedroom activities.
    ///
    /// Completion is accepted only from authoritative MATLAB messages
    /// (ACTIVITY_RESULT with completed=true, or GAME_PROGRESS) or from explicit
    /// test/setup calls. Opening a scene, closing a scene, or a failed placement
    /// never marks an activity complete. The static set survives scene reloads,
    /// so stars stay collected for the current application session.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class GameProgress : MonoBehaviour
    {
        private static readonly HashSet<string> CompletedActivities =
            new HashSet<string>(StringComparer.Ordinal);

        private static GameProgress instance;
        private bool networkSubscribed;

        public static event Action<string> ActivityCompleted;
        public static event Action<string, bool> CompletionChanged;

        public static GameProgress Instance
        {
            get { return instance; }
        }

        public static string ActiveActivityId { get; private set; }

        public static bool AllActivitiesCompleted
        {
            get
            {
                for (int i = 0; i < ActivityIds.All.Length; i++)
                {
                    if (!IsCompleted(ActivityIds.All[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (FindAnyObjectByType<GameProgress>() != null)
            {
                return;
            }

            GameObject root = new GameObject("Game Progress");
            root.AddComponent<GameProgress>();
            if (FindAnyObjectByType<SceneTransitionController>() == null)
            {
                root.AddComponent<SceneTransitionController>();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[Issue14] Duplicate GameProgress removed.");
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            SubscribeToNetwork();
        }

        private void Update()
        {
            // NetworkManager may be created by its own RuntimeInitialize method
            // after this component has already been enabled.
            if (!networkSubscribed)
            {
                SubscribeToNetwork();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromNetwork();
        }

        private void OnDestroy()
        {
            UnsubscribeFromNetwork();
            if (instance == this)
            {
                instance = null;
            }
        }

        private void SubscribeToNetwork()
        {
            NetworkManager manager = NetworkManager.Instance;
            if (manager == null || networkSubscribed)
            {
                return;
            }

            manager.MessageReceived -= HandleNetworkMessage;
            manager.MessageReceived += HandleNetworkMessage;
            networkSubscribed = true;
        }

        private void UnsubscribeFromNetwork()
        {
            NetworkManager manager = NetworkManager.Instance;
            if (manager != null && networkSubscribed)
            {
                manager.MessageReceived -= HandleNetworkMessage;
            }
            networkSubscribed = false;
        }

        private void HandleNetworkMessage(WireMessage message)
        {
            ProcessMessage(message);
        }

        /// <summary>
        /// Apply one MATLAB message. Returns true only when this message newly
        /// completed an activity. Unity never validates answers locally.
        /// </summary>
        public bool ProcessMessage(WireMessage message)
        {
            if (message == null)
            {
                return false;
            }

            if (message.IsType(ProtocolConstants.TypeActivityResult))
            {
                if (!message.completed)
                {
                    return false;
                }
                if (!string.IsNullOrEmpty(ActiveActivityId) &&
                    !string.Equals(ActiveActivityId, message.activity, StringComparison.Ordinal))
                {
                    Debug.LogWarning(
                        "[Issue14] Ignoring completion for " + message.activity +
                        " while " + ActiveActivityId + " is active.");
                    return false;
                }
                return TryComplete(message.activity);
            }

            if (message.IsType(ProtocolConstants.TypeGameProgress) &&
                message.completedActivities != null)
            {
                bool changed = false;
                for (int i = 0; i < message.completedActivities.Length; i++)
                {
                    if (TryComplete(message.completedActivities[i]))
                    {
                        changed = true;
                    }
                }
                return changed;
            }

            return false;
        }

        public static bool TryComplete(string activityId)
        {
            if (!ActivityIds.IsKnown(activityId) || !CompletedActivities.Add(activityId))
            {
                return false;
            }

            Action<string> completed = ActivityCompleted;
            if (completed != null)
            {
                completed(activityId);
            }

            Action<string, bool> changed = CompletionChanged;
            if (changed != null)
            {
                changed(activityId, true);
            }
            return true;
        }

        public static bool IsCompleted(string activityId)
        {
            return ActivityIds.IsKnown(activityId) && CompletedActivities.Contains(activityId);
        }

        public static void SetActiveActivity(string activityId)
        {
            ActiveActivityId = ActivityIds.IsKnown(activityId) ? activityId : null;
        }

        public static void ClearActiveActivity()
        {
            ActiveActivityId = null;
        }

#if UNITY_EDITOR
        /// <summary>Clears session state between EditMode tests only.</summary>
        public static void ResetSessionForTests()
        {
            CompletedActivities.Clear();
            ActiveActivityId = null;
            instance = null;
        }

        /// <summary>Removes stale listeners between EditMode tests.</summary>
        public static void ClearListenersForTests()
        {
            ActivityCompleted = null;
            CompletionChanged = null;
        }
#endif
    }
}
