using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Core
{
    /// <summary>
    /// Centralized scene-transition entry point.
    ///
    /// Every load goes through a name check and a build-settings check, so an
    /// unknown name or a missing build entry produces a clear Debug.LogError
    /// instead of silently doing nothing. Loading the already active scene is
    /// ignored to avoid unnecessary reloads.
    ///
    /// Only navigation lives here. This class never decides whether an
    /// activity answer is correct and never changes game progress.
    /// </summary>
    public static class SceneLoader
    {
        // Convenience entry points for the six registered scenes.

        public static bool LoadMainMenu() { return Load(SceneNames.MainMenu); }
        public static bool LoadBedroom() { return Load(SceneNames.Bedroom); }
        public static bool LoadColorSorting() { return Load(SceneNames.ColorSorting); }
        public static bool LoadTasteSorting() { return Load(SceneNames.TasteSorting); }
        public static bool LoadMathActivity() { return Load(SceneNames.MathActivity); }
        public static bool LoadEnding() { return Load(SceneNames.Ending); }

        /// <summary>
        /// Loads a registered scene by build index.
        /// Returns false when the request is invalid, cannot be resolved, or
        /// would reload the active scene. Errors are always logged.
        /// </summary>
        public static bool Load(string sceneName)
        {
            int buildIndex;
            string error;
            if (!TryResolve(sceneName, out buildIndex, out error))
            {
                Debug.LogError("[Scenes] " + error);
                return false;
            }

            if (IsSameScene(SceneManager.GetActiveScene(), sceneName))
            {
                return false;
            }

            SceneManager.LoadScene(buildIndex);
            return true;
        }

        /// <summary>
        /// Asynchronous variant for callers that need the AsyncOperation
        /// (for example a loading screen). The operation is null when no load
        /// was started.
        /// </summary>
        public static bool LoadAsync(string sceneName, out AsyncOperation operation)
        {
            operation = null;

            int buildIndex;
            string error;
            if (!TryResolve(sceneName, out buildIndex, out error))
            {
                Debug.LogError("[Scenes] " + error);
                return false;
            }

            if (IsSameScene(SceneManager.GetActiveScene(), sceneName))
            {
                return false;
            }

            operation = SceneManager.LoadSceneAsync(buildIndex);
            return operation != null;
        }

        /// <summary>
        /// Resolves a scene name to its build index without performing a load.
        /// Exposed for validation, tooling and tests.
        /// </summary>
        public static bool TryResolve(string sceneName, out int buildIndex, out string error)
        {
            buildIndex = -1;

            if (!SceneNames.IsKnown(sceneName))
            {
                error = "Scene '" + (sceneName ?? "<null>") +
                        "' is not a registered game scene. Valid names: " +
                        JoinRegisteredNames() + ".";
                return false;
            }

            string scenePath = SceneNames.GetPath(sceneName);
            buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            if (buildIndex < 0)
            {
                error = "Scene '" + sceneName + "' is registered but missing from " +
                        "the build settings (expected path: " + scenePath + ").";
                buildIndex = -1;
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>True when loading <paramref name="sceneName"/> would be a reload.</summary>
        public static bool IsSameScene(Scene activeScene, string sceneName)
        {
            return activeScene.IsValid() &&
                   string.Equals(activeScene.name, sceneName, StringComparison.Ordinal);
        }

        private static string JoinRegisteredNames()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < SceneNames.All.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(SceneNames.All[i]);
            }
            return builder.ToString();
        }
    }
}
