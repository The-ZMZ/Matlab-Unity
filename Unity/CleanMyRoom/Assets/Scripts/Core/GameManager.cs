using System;
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
    /// This manager deliberately holds no answers, scores or star counts:
    /// MATLAB owns progress and validation. Here it only tracks which scene
    /// is active so presentation code has one place to listen for changes.
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
