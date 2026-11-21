using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Immutable view of the keyboard keys approved for this game.
    ///
    /// Movement uses WASD and the arrow keys. Interaction uses E. The snapshot
    /// exposes key-down (pressed this frame), key-held and key-up (released this
    /// frame) for interaction, and a combined movement vector for player motion.
    /// Several keys can be held at once; opposing movement keys cancel out and
    /// diagonal movement is normalised through <see cref="KeyboardInput"/>.
    /// </summary>
    public readonly struct KeyboardInputSnapshot
    {
        private const float MovementEpsilon = 0.0001f;

        private KeyboardInputSnapshot(
            Vector2 movement,
            bool moveLeftHeld,
            bool moveRightHeld,
            bool moveDownHeld,
            bool moveUpHeld,
            bool interactDown,
            bool interactHeld,
            bool interactReleased,
            int heldGameKeyCount)
        {
            Movement = movement;
            MoveLeftHeld = moveLeftHeld;
            MoveRightHeld = moveRightHeld;
            MoveDownHeld = moveDownHeld;
            MoveUpHeld = moveUpHeld;
            InteractDown = interactDown;
            InteractHeld = interactHeld;
            InteractReleased = interactReleased;
            HeldGameKeyCount = heldGameKeyCount;
        }

        public Vector2 Movement { get; }

        public bool MoveLeftHeld { get; }

        public bool MoveRightHeld { get; }

        public bool MoveDownHeld { get; }

        public bool MoveUpHeld { get; }

        public bool InteractDown { get; }

        public bool InteractHeld { get; }

        public bool InteractReleased { get; }

        /// <summary>Number of approved keys held when the snapshot was taken.</summary>
        public int HeldGameKeyCount { get; }

        public bool IsMoving { get { return Movement.sqrMagnitude > MovementEpsilon; } }

        public bool HasInput
        {
            get { return HeldGameKeyCount > 0 || InteractHeld || IsMoving; }
        }

        public bool IsIdle { get { return !HasInput; } }

        /// <summary>Build an immutable snapshot from the live keyboard device.</summary>
        public static KeyboardInputSnapshot ReadCurrent()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return default;
            }

            bool moveLeft = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool moveRight = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            bool moveDown = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
            bool moveUp = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;

            float x = (moveRight ? 1f : 0f) - (moveLeft ? 1f : 0f);
            float y = (moveUp ? 1f : 0f) - (moveDown ? 1f : 0f);
            Vector2 movement = KeyboardInput.NormalizeMovement(new Vector2(x, y));

            bool interactDown = keyboard.eKey.wasPressedThisFrame;
            bool interactHeld = keyboard.eKey.isPressed;
            bool interactReleased = keyboard.eKey.wasReleasedThisFrame;

            int heldCount = (moveLeft ? 1 : 0) +
                            (moveRight ? 1 : 0) +
                            (moveDown ? 1 : 0) +
                            (moveUp ? 1 : 0) +
                            (interactHeld ? 1 : 0);

            return new KeyboardInputSnapshot(
                movement, moveLeft, moveRight, moveDown, moveUp,
                interactDown, interactHeld, interactReleased, heldCount);
        }

        /// <summary>
        /// Explicit snapshot factory for tests and for future non-Input-System sources.
        /// </summary>
        public static KeyboardInputSnapshot Create(
            Vector2 movement,
            bool moveLeftHeld,
            bool moveRightHeld,
            bool moveDownHeld,
            bool moveUpHeld,
            bool interactDown,
            bool interactHeld,
            bool interactReleased,
            int heldGameKeyCount)
        {
            Vector2 normalized = KeyboardInput.NormalizeMovement(movement);
            int count = Mathf.Max(0, heldGameKeyCount);
            if (interactHeld && count == 0)
            {
                count = 1;
            }

            return new KeyboardInputSnapshot(
                normalized, moveLeftHeld, moveRightHeld, moveDownHeld, moveUpHeld,
                interactDown, interactHeld, interactReleased, count);
        }
    }
}
