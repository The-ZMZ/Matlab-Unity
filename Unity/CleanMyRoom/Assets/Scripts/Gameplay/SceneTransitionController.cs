using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Gameplay
{
    /// <summary>
    /// Single scene-transition owner for Issue #14. It survives scene loads,
    /// validates that a target scene is in Build Settings, and returns to the
    /// bedroom only after MATLAB reports activity completion.
    ///
    /// Activity scene paths are intentionally left empty until the real scenes
    /// are supplied; an empty or invalid path logs an error and loads nothing.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class SceneTransitionController : MonoBehaviour
    {
        [SerializeField] private string bedroomScenePath = ActivityNavigation.BedroomScenePath;

        private static SceneTransitionController instance;
        private bool subscribed;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[Issue14] Duplicate SceneTransitionController removed.");
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
            EnsureSubscribed();
        }

        /// <summary>
        /// Idempotent subscription setup. Runtime calls this from OnEnable;
        /// EditMode tests call it explicitly because lifecycle methods are not
        /// invoked for components added from editor code.
        /// </summary>
        public void EnsureSubscribed()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                return;
            }

            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            ActivityNavigation.OpenRequested += HandleOpenRequested;
            ActivityNavigation.ReturnToBedroomRequested += HandleReturnRequested;
            GameProgress.ActivityCompleted += HandleActivityCompleted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            ActivityNavigation.OpenRequested -= HandleOpenRequested;
            ActivityNavigation.ReturnToBedroomRequested -= HandleReturnRequested;
            GameProgress.ActivityCompleted -= HandleActivityCompleted;
            subscribed = false;
        }

        private void HandleOpenRequested(string activityId, string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                Debug.LogError("[Issue14] Refusing to load an empty activity scene path.");
                return;
            }

            if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
            {
                Debug.LogError(
                    "[Issue14] Activity scene is not in Build Settings: " + scenePath +
                    ". No scene was loaded.");
                return;
            }

            GameProgress.SetActiveActivity(activityId);
            if (!Application.isPlaying)
            {
                return;
            }

            SceneManager.LoadScene(scenePath);
        }

        private void HandleReturnRequested()
        {
            GameProgress.ClearActiveActivity();

            if (SceneUtility.GetBuildIndexByScenePath(bedroomScenePath) < 0)
            {
                Debug.LogError("[Issue14] Bedroom scene is missing from Build Settings.");
                return;
            }

            if (!Application.isPlaying ||
                SceneManager.GetActiveScene().path == bedroomScenePath)
            {
                return;
            }

            SceneManager.LoadScene(bedroomScenePath);
        }

        private void HandleActivityCompleted(string activityId)
        {
            if (string.Equals(activityId, GameProgress.ActiveActivityId, System.StringComparison.Ordinal))
            {
                ActivityNavigation.RequestReturnToBedroom();
            }
        }

#if UNITY_EDITOR
        /// <summary>Clears the singleton reference between EditMode tests.</summary>
        public static void ResetInstanceForTests()
        {
            instance = null;
        }
#endif
    }
}
