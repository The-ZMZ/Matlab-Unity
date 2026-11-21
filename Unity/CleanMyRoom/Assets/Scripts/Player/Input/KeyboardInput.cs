using UnityEngine;

namespace CleanMyRoom.Player.Input
{
    /// <summary>
    /// Single entry point for approved keyboard input.
    ///
    /// Systems ask for one snapshot per frame instead of each polling keys in
    /// their own way. The default source is the Input System; tests can inject a
    /// stub source and restore the real one afterwards.
    /// </summary>
    public static class KeyboardInput
    {
        private static readonly IKeyboardInputSource UnitySource = new UnityKeyboardInputSource();
        private static IKeyboardInputSource source;

        public static IKeyboardInputSource Source
        {
            get { return source ?? UnitySource; }
            set { source = value ?? UnitySource; }
        }

        public static KeyboardInputSnapshot Read()
        {
            return Source.ReadSnapshot();
        }

        public static Vector2 ReadMovement()
        {
            return Read().Movement;
        }

        public static bool ReadInteractPressed()
        {
            return Read().InteractDown;
        }

        /// <summary>
        /// Keep combined movement within unit length so diagonals are not faster.
        /// Exposed here so the input snapshot and PlayerController share one rule.
        /// </summary>
        public static Vector2 NormalizeMovement(Vector2 input)
        {
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        /// <summary>Restore the live Input System source after test injection.</summary>
        public static void ResetToUnitySource()
        {
            source = null;
        }
    }
}
