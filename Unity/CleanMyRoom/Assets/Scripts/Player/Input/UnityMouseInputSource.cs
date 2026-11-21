namespace CleanMyRoom.Player.Input
{
    /// <summary>Adapts the Unity Input System mouse to the game input contract.</summary>
    public sealed class UnityMouseInputSource : IMouseInputSource
    {
        public MouseInputSnapshot ReadSnapshot()
        {
            return MouseInputSnapshot.ReadCurrent();
        }
    }
}
