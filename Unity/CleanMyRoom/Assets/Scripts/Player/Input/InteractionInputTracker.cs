namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Converts the E-key transition into one consumable interaction request.
    ///
    /// A key-down is reported at most once, even if several systems poll in the
    /// same frame or a caller asks again while the key is still held. The latch
    /// is released when E goes up, so a later deliberate press works normally.
    /// </summary>
    public sealed class InteractionInputTracker
    {
        private bool pressConsumed;

        public bool IsInteractHeld { get; private set; }

        public bool WasInteractReleased { get; private set; }

        /// <summary>Read the current keyboard source and consume a new press if present.</summary>
        public bool ConsumePress()
        {
            return ConsumePress(KeyboardInput.Read());
        }

        /// <summary>Consume a press from an already captured snapshot.</summary>
        public bool ConsumePress(KeyboardInputSnapshot snapshot)
        {
            IsInteractHeld = snapshot.InteractHeld;
            WasInteractReleased = snapshot.InteractReleased;

            if (!snapshot.InteractHeld)
            {
                pressConsumed = false;
            }

            if (!snapshot.InteractDown || pressConsumed)
            {
                return false;
            }

            pressConsumed = true;
            return true;
        }

        public void Reset()
        {
            pressConsumed = false;
            IsInteractHeld = false;
            WasInteractReleased = false;
        }
    }
}
