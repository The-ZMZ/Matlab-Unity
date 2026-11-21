using UnityEngine;

namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Reusable mouse press/hold/release state machine.
    ///
    /// The machine consumes one snapshot per frame. It emits a press at most
    /// once per physical button-down, even when several systems poll it, and
    /// re-arms only after the button is up. A RELEASE state is exposed for one
    /// update so a drag can be completed before the machine returns to IDLE.
    ///
    /// No hand distance, pinch threshold, camera, or gesture calculation is
    /// involved. The optional interactable flag keeps a press that did not hit
    /// anything from becoming a grab.
    /// </summary>
    public sealed class MouseInteractionStateMachine
    {
        private bool pressSignaled;
        private bool holdSignaled;
        private bool releaseSignaled;
        private bool requiresRelease;
        private bool pendingPress;
        private bool pendingHold;
        private bool pendingRelease;

        public MouseInteractionState State { get; private set; } = MouseInteractionState.IDLE;

        public bool IsPressActive
        {
            get { return State == MouseInteractionState.PRESSED || State == MouseInteractionState.HOLDING; }
        }

        public bool IsHolding { get { return State == MouseInteractionState.HOLDING; } }

        public Vector2 LastScreenPosition { get; private set; }

        /// <summary>Update from a snapshot whose press is known to be valid.</summary>
        public void Update(MouseInputSnapshot snapshot)
        {
            Update(snapshot, true);
        }

        /// <summary>
        /// Update from a snapshot, allowing the caller to reject presses that
        /// did not land on an interactable object.
        /// </summary>
        public void Update(MouseInputSnapshot snapshot, bool pressTargetsInteractable)
        {
            LastScreenPosition = snapshot.ScreenPosition;

            // One-shot events remain latched until their consumer calls the
            // matching Consume... method. They are not overwritten by later
            // frames, so a press cannot be lost between two update calls.
            bool down = snapshot.PressDown;
            bool held = snapshot.PressHeld;
            bool released = snapshot.PressReleased;
            bool buttonDown = down || held;

            if (requiresRelease)
            {
                if (buttonDown)
                {
                    return;
                }

                requiresRelease = false;
            }

            if (down && !pressSignaled)
            {
                pressSignaled = true;
                holdSignaled = false;
                releaseSignaled = false;

                if (pressTargetsInteractable)
                {
                    State = MouseInteractionState.PRESSED;
                    pendingPress = true;
                }
                else
                {
                    State = MouseInteractionState.IDLE;
                }
            }
            else if (State == MouseInteractionState.PRESSED && !down && held && !holdSignaled)
            {
                holdSignaled = true;
                pendingHold = true;
                State = MouseInteractionState.HOLDING;
            }
            else if (State == MouseInteractionState.PRESSED && !down && held)
            {
                State = MouseInteractionState.HOLDING;
            }

            if (IsPressActive && (released || !buttonDown))
            {
                State = MouseInteractionState.RELEASE;
                if (!releaseSignaled)
                {
                    releaseSignaled = true;
                    pressSignaled = false;
                    holdSignaled = false;
                    pendingRelease = true;
                }
            }
            else if (State == MouseInteractionState.RELEASE && !buttonDown)
            {
                State = MouseInteractionState.IDLE;
            }

            // A fully released button re-arms the next deliberate press.
            if (!buttonDown)
            {
                pressSignaled = false;
            }
        }

        /// <summary>Consume the latched one-shot press event, if any.</summary>
        public bool ConsumePressStarted()
        {
            bool value = pendingPress;
            pendingPress = false;
            return value;
        }

        /// <summary>Consume the latched one-shot hold event, if any.</summary>
        public bool ConsumeHoldStarted()
        {
            bool value = pendingHold;
            pendingHold = false;
            return value;
        }

        /// <summary>Consume the latched one-shot release event, if any.</summary>
        public bool ConsumeReleaseStarted()
        {
            bool value = pendingRelease;
            pendingRelease = false;
            return value;
        }

        /// <summary>Clear all state and one-shot events.</summary>
        public void Reset()
        {
            State = MouseInteractionState.IDLE;
            pressSignaled = false;
            holdSignaled = false;
            releaseSignaled = false;
            requiresRelease = false;
            pendingPress = false;
            pendingHold = false;
            pendingRelease = false;
        }

        /// <summary>
        /// Reset after a disable or interruption. A button that is still down
        /// must be released before it can start a new interaction.
        /// </summary>
        public void Interrupt()
        {
            Reset();
            pressSignaled = true;
            requiresRelease = true;
        }
    }
}
