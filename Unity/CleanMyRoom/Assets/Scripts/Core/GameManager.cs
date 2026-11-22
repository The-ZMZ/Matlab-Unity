using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Core
{
    /// <summary>
    /// Persistent, scene-independent lifecycle owner for the game.
    ///
    /// It is created automatically after the first scene loads, follows the
    /// same pattern as CleanMyRoom.Net.NetworkManager, and uses
    /// DontDestroyOnLoad so it survives every scene transition. A second
    /// instance (for example one placed in a scene by mistake) is removed.
    ///
    /// This manager holds no answers and makes no correctness decisions:
    /// MATLAB owns validation and progress. It records the scene and mirrors
    /// the star/completion values that MATLAB sends, so every activity has one
    /// shared place to display progress and one completion event to observe.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Raised once per scene change, after the new scene is loaded.</summary>
        public event Action<Scene, LoadSceneMode> SceneChanged;

        /// <summary>Name of the most recently loaded scene.</summary>
        public string CurrentSceneName { get; private set; } = string.Empty;

        private readonly HashSet<string> completedActivities =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Raised when MATLAB reports that an activity has been completed.
        /// The argument is the MATLAB activity id (for example "COLOR").
        /// </summary>
        public event Action<string, int> ActivityCompletedFromMatlab;

        /// <summary>Total stars reported by MATLAB for the current session.</summary>
        public int TotalStars { get; private set; }

        /// <summary>True when MATLAB reports the whole game as completed.</summary>
        public bool GameCompleted { get; private set; }

        /// <summary>True when MATLAB has reported this activity as completed.</summary>
        public bool IsActivityComplete(string activityId)
        {
            return !string.IsNullOrEmpty(activityId) && completedActivities.Contains(activityId);
        }

        /// <summary>
        /// Mirror one ACTIVITY_RESULT message. Never call this from local answer
        /// checking: the arguments must come from MATLAB.
        /// </summary>
        public void ApplyMatlabActivityResult(string activityId, bool completed, int totalStars)
        {
            if (totalStars > TotalStars)
            {
                TotalStars = totalStars;
            }
            if (!completed || string.IsNullOrEmpty(activityId))
            {
                return;
            }
            if (!completedActivities.Add(activityId))
            {
                return;
            }

            Action<string, int> handler = ActivityCompletedFromMatlab;
            if (handler != null)
            {
                handler(activityId, TotalStars);
            }
        }

        /// <summary>
        /// Mirror one GAME_PROGRESS message. Values are MATLAB's, not Unity's.
        /// </summary>
        public void ApplyMatlabGameProgress(
            int totalStars, string[] finishedActivities, bool gameCompleted)
        {
            if (totalStars > TotalStars)
            {
                TotalStars = totalStars;
            }
            GameCompleted = gameCompleted;

            if (finishedActivities == null)
            {
                return;
            }
            for (int i = 0; i < finishedActivities.Length; i++)
            {
                string activityId = finishedActivities[i];
                if (string.IsNullOrEmpty(activityId) || !completedActivities.Add(activityId))
                {
                    continue;
                }
                Action<string, int> handler = ActivityCompletedFromMatlab;
                if (handler != null)
                {
                    handler(activityId, TotalStars);
                }
            }
        }

        /// <summary>
        /// Clear mirrored progress to give automated tests a clean baseline (and
        /// for a future new-game flow). It never validates answers.
        /// </summary>
        public void ResetProgress()
        {
            TotalStars = 0;
            GameCompleted = false;
            completedActivities.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (UnityEngine.Object.FindAnyObjectByType<GameManager>() != null)
            {
                return;
            }

            GameObject root = new GameObject("Game Manager");
            root.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (!Register(this))
            {
                return;
            }

            PersistAcrossScenes();
            CurrentSceneName = SceneManager.GetActiveScene().name;
        }

        private void OnDestroy()
        {
            Unregister(this);
        }

        /// <summary>
        /// Registers a candidate as the single GameManager. The -= before +=
        /// in AttachSceneEvents makes repeated calls idempotent, so duplicate
        /// event subscriptions cannot accumulate.
        /// </summary>
        internal static bool Register(GameManager candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (Instance != null && !ReferenceEquals(Instance, candidate))
            {
                Debug.LogWarning("[GameManager] Duplicate GameManager removed.");
                if (Application.isPlaying)
                {
                    Destroy(candidate.gameObject);
                }
                else
                {
                    DestroyImmediate(candidate.gameObject);
                }
                return false;
            }

            Instance = candidate;
            candidate.AttachSceneEvents();
            return true;
        }

        /// <summary>Detaches events and clears the static reference if it points here.</summary>
        internal static void Unregister(GameManager candidate)
        {
            if (candidate == null)
            {
                return;
            }

            candidate.DetachSceneEvents();
            if (ReferenceEquals(Instance, candidate))
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Persistence precondition: Unity only keeps root objects alive across
        /// scene loads. Kept separate so it can be checked without entering
        /// Play Mode (DontDestroyOnLoad is a runtime-only Unity call).
        /// </summary>
        internal static bool CanPersist(GameManager candidate)
        {
            return candidate != null && candidate.transform.parent == null;
        }

        /// <summary>Keeps this root object alive across scene loads.</summary>
        internal void PersistAcrossScenes()
        {
            if (!CanPersist(this))
            {
                Debug.LogWarning(
                    "[GameManager] DontDestroyOnLoad requires a root GameObject; " +
                    "the manager will not persist while parented.");
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void AttachSceneEvents()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void DetachSceneEvents()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        internal void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentSceneName = scene.name;

            Action<Scene, LoadSceneMode> handler = SceneChanged;
            if (handler != null)
            {
                handler(scene, mode);
            }
        }
    }
}
