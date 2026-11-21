using System;
using System.Collections.Generic;

namespace CleanMyRoom.Core
{
    /// <summary>
    /// Single source of truth for the game's scene names and asset paths.
    ///
    /// Every scene uses PascalCase and lives in Assets/Scenes. Keeping the
    /// registry here means UI code, tests and the loader cannot drift apart:
    /// a typo is rejected before a load is attempted.
    ///
    /// This class contains no gameplay or validation logic. Educational
    /// decisions remain in MATLAB (see AGENTS.md sections 3 and 4).
    /// </summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Bedroom = "Bedroom";
        public const string ColorSorting = "ColorSorting";
        public const string TasteSorting = "TasteSorting";
        public const string MathActivity = "MathActivity";
        public const string Ending = "Ending";

        /// <summary>Project-relative folder that contains every game scene.</summary>
        public const string ScenesFolder = "Assets/Scenes/";

        private static readonly IReadOnlyList<string> OrderedNames = Array.AsReadOnly(new[]
        {
            MainMenu,
            Bedroom,
            ColorSorting,
            TasteSorting,
            MathActivity,
            Ending
        });

        /// <summary>All registered scene names, in build order.</summary>
        public static IReadOnlyList<string> All
        {
            get { return OrderedNames; }
        }

        /// <summary>
        /// Exact, case-sensitive check. Case sensitivity is deliberate: it
        /// enforces the PascalCase naming convention for new scenes.
        /// </summary>
        public static bool IsKnown(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            for (int i = 0; i < OrderedNames.Count; i++)
            {
                if (string.Equals(OrderedNames[i], sceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns the canonical asset path for a registered scene name.</summary>
        public static bool TryGetPath(string sceneName, out string scenePath)
        {
            if (!IsKnown(sceneName))
            {
                scenePath = null;
                return false;
            }

            scenePath = ScenesFolder + sceneName + ".unity";
            return true;
        }

        /// <summary>Strict variant of <see cref="TryGetPath"/>.</summary>
        public static string GetPath(string sceneName)
        {
            string scenePath;
            if (!TryGetPath(sceneName, out scenePath))
            {
                throw new ArgumentException(
                    "Unknown scene name '" + (sceneName ?? "<null>") + "'.",
                    nameof(sceneName));
            }

            return scenePath;
        }
    }
}
