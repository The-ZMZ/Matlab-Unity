using UnityEngine;

namespace CleanMyRoom.Core
{
    /// <summary>
    /// Wires the Main Menu UI to the validated scene-loading API.
    ///
    /// The menu scene itself stays at build index 0; pressing the Start Game
    /// button hands control to the existing Bedroom hub through SceneLoader.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        /// <summary>UI callback for the Start Game button.</summary>
        public void StartGame()
        {
            SceneLoader.LoadBedroom();
        }

        /// <summary>UI callback for the Quit button.</summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
