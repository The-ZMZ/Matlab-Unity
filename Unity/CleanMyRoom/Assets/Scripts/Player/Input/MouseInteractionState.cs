namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Mouse-only replacement for the retired pinch state model.
    ///
    /// PRESSED means a primary mouse button went down over an interactable,
    /// HOLDING means the button is still down, and RELEASE is a one-frame state
    /// raised when the button goes up so consumers can finish a drag.
    /// </summary>
    public enum MouseInteractionState
    {
        IDLE,
        PRESSED,
        HOLDING,
        RELEASE
    }
}
