using UnityEngine;

namespace CleanMyRoom.Gameplay.Activities
{
    /// <summary>
    /// Generic identity/presentation component for an item that can be moved
    /// with the mouse. It stores no correct target and performs no validation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DraggableItem : MonoBehaviour
    {
        [SerializeField] private string itemId;

        public string ItemId
        {
            get { return itemId ?? string.Empty; }
        }

        public Vector3 OriginalPosition { get; private set; }

        public Quaternion OriginalRotation { get; private set; }

        public bool HasOriginalPose { get; private set; }

        /// <summary>Set the protocol item id from scene data or a test fixture.</summary>
        public void Configure(string newItemId)
        {
            itemId = newItemId ?? string.Empty;
        }

        public void CaptureOriginalPose()
        {
            OriginalPosition = transform.position;
            OriginalRotation = transform.rotation;
            HasOriginalPose = true;
        }

        public void RestoreOriginalPose()
        {
            if (!HasOriginalPose)
            {
                return;
            }

            transform.position = OriginalPosition;
            transform.rotation = OriginalRotation;
            Physics.SyncTransforms();
        }

        public void MoveTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            Physics.SyncTransforms();
        }
    }
}
