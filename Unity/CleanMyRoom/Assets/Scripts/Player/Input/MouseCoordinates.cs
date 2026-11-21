using UnityEngine;

namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Converts the normal Unity mouse cursor position into the normalized
    /// activity-canvas coordinates required by the existing PLACE_REQ message.
    ///
    /// The canvas origin is top-left, y grows downward, and results are clamped
    /// to [0, 1]. MATLAB remains responsible for any authoritative hit testing
    /// or activity-space calculations; this helper only formats input data.
    /// </summary>
    public static class MouseCoordinates
    {
        public static Vector2 Normalize(Vector2 screenPosition, Rect activityRect)
        {
            if (activityRect.width <= 0f || activityRect.height <= 0f)
            {
                return Vector2.zero;
            }

            float x = (screenPosition.x - activityRect.xMin) / activityRect.width;
            float y = 1f - ((screenPosition.y - activityRect.yMin) / activityRect.height);

            return new Vector2(Clamp01(x), Clamp01(y));
        }

        public static Vector2 NormalizeToScreen(Vector2 screenPosition)
        {
            return Normalize(screenPosition, new Rect(0f, 0f, Screen.width, Screen.height));
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
