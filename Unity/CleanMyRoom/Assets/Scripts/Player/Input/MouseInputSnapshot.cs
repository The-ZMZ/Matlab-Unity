using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Immutable view of the approved mouse input for one frame.
    ///
    /// Only the primary (left) button and the normal system pointer are read.
    /// There is no camera, hand, gesture, pinch, or virtual cursor input.
    /// </summary>
    public readonly struct MouseInputSnapshot
    {
        private MouseInputSnapshot(
            Vector2 screenPosition,
            bool pressDown,
            bool pressHeld,
            bool pressReleased)
        {
            ScreenPosition = screenPosition;
            PressDown = pressDown;
            PressHeld = pressHeld;
            PressReleased = pressReleased;
        }

        /// <summary>Pointer position in Unity screen pixels.</summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>True only on the frame the primary button went down.</summary>
        public bool PressDown { get; }

        /// <summary>True while the primary button remains held.</summary>
        public bool PressHeld { get; }

        /// <summary>True only on the frame the primary button went up.</summary>
        public bool PressReleased { get; }

        /// <summary>True when any primary-button transition is present.</summary>
        public bool HasButtonActivity
        {
            get { return PressDown || PressHeld || PressReleased; }
        }

        /// <summary>Build an immutable snapshot from the live Input System mouse.</summary>
        public static MouseInputSnapshot ReadCurrent()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return default;
            }

            return new MouseInputSnapshot(
                mouse.position.ReadValue(),
                mouse.leftButton.wasPressedThisFrame,
                mouse.leftButton.isPressed,
                mouse.leftButton.wasReleasedThisFrame);
        }

        /// <summary>Explicit snapshot factory for tests and non-Input-System sources.</summary>
        public static MouseInputSnapshot Create(
            Vector2 screenPosition,
            bool pressDown,
            bool pressHeld,
            bool pressReleased)
        {
            return new MouseInputSnapshot(screenPosition, pressDown, pressHeld, pressReleased);
        }
    }
}
