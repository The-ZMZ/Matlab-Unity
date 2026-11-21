using UnityEngine;

namespace CleanMyRoom.Gameplay
{
    /// <summary>
    /// Keeps the elevated follow camera from looking at the underside of the
    /// bedroom ceiling.
    ///
    /// The room is a closed box (walls up to y = 5, ceiling at y = 5.1), while
    /// the player's follow camera deliberately sits above the walls for a
    /// room-wide view. A solid ceiling would fill that view with white. The
    /// ceiling GameObject and its light remain in the scene at edit time and
    /// for all validation; only its renderer is hidden while the game runs.
    ///
    /// Attach this to the "Ceiling" GameObject in Bedroom.unity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayModeCeilingHider : MonoBehaviour
    {
        private void Awake()
        {
            MeshRenderer ceilingRenderer = GetComponent<MeshRenderer>();
            if (ceilingRenderer != null)
            {
                ceilingRenderer.enabled = false;
            }
        }
    }
}
