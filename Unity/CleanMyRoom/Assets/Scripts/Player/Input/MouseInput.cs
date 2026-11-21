namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Single entry point for approved mouse input. Components may instead
    /// inject <see cref="IMouseInputSource"/> directly for isolated tests.
    /// </summary>
    public static class MouseInput
    {
        private static readonly IMouseInputSource UnitySource = new UnityMouseInputSource();
        private static IMouseInputSource source;

        public static IMouseInputSource Source
        {
            get { return source ?? UnitySource; }
            set { source = value ?? UnitySource; }
        }

        public static MouseInputSnapshot Read()
        {
            return Source.ReadSnapshot();
        }

        /// <summary>Restore the live Input System source after test injection.</summary>
        public static void ResetToUnitySource()
        {
            source = null;
        }
    }
}
