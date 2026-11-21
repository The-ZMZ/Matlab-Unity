using UnityEngine;

namespace CleanMyRoom.Gameplay.Activities
{
    /// <summary>
    /// Generic drop-area identity. The component reports a target id to the
    /// drag controller; MATLAB alone decides whether that target is correct.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DropTarget : MonoBehaviour
    {
        [SerializeField] private string targetId;

        public string TargetId
        {
            get { return targetId ?? string.Empty; }
        }

        public void Configure(string newTargetId)
        {
            targetId = newTargetId ?? string.Empty;
        }
    }
}
