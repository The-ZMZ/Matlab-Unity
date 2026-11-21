using System;

namespace CleanMyRoom.Gameplay
{
    /// <summary>
    /// Canonical activity ids shared with MATLAB. These values are part of the
    /// TCP protocol and must not be renamed without updating MATLAB.
    /// </summary>
    public static class ActivityIds
    {
        public const string Color = "COLOR";
        public const string Taste = "TASTE";
        public const string Math = "MATH";

        public static readonly string[] All = { Color, Taste, Math };

        public static bool IsKnown(string activityId)
        {
            if (string.IsNullOrEmpty(activityId))
            {
                return false;
            }

            for (int i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i], activityId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        public static string DisplayName(string activityId)
        {
            if (string.Equals(activityId, Color, StringComparison.Ordinal))
            {
                return "Color Sorting";
            }
            if (string.Equals(activityId, Taste, StringComparison.Ordinal))
            {
                return "Taste Classification";
            }
            if (string.Equals(activityId, Math, StringComparison.Ordinal))
            {
                return "Mathematics Activity";
            }
            return "Activity";
        }
    }
}
