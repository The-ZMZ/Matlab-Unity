namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Supplies one mouse snapshot per frame. Tests inject a deterministic
    /// stub; production uses <see cref="UnityMouseInputSource"/>.
    /// </summary>
    public interface IMouseInputSource
    {
        MouseInputSnapshot ReadSnapshot();
    }
}
