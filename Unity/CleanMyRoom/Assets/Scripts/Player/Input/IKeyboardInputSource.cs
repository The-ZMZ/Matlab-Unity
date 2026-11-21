namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Supplies one keyboard snapshot to the rest of the game.
    ///
    /// Keeping the source behind an interface lets EditMode tests replace the
    /// real Input System device with a deterministic stub without changing the
    /// movement or interaction code that consumes the snapshot.
    /// </summary>
    public interface IKeyboardInputSource
    {
        KeyboardInputSnapshot ReadSnapshot();
    }
}
