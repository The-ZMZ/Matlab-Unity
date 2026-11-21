namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Reads the game's approved keyboard keys from Unity's Input System.
    /// No camera, gesture or hand input is used anywhere in this source.
    /// </summary>
    public sealed class UnityKeyboardInputSource : IKeyboardInputSource
    {
        public KeyboardInputSnapshot ReadSnapshot()
        {
            return KeyboardInputSnapshot.ReadCurrent();
        }
    }
}
