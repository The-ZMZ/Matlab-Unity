using UnityEngine;

namespace CleanMyRoom.ColorSorting
{
    /// <summary>
    /// One draggable object. It stores its identifier and its home position so
    /// a rejected or interrupted placement can be returned safely. It does not
    /// know the correct basket and performs no validation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SortableObject : MonoBehaviour
    {
        [SerializeField] private string objectId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private Vector3 homePosition;
        [SerializeField] private string placedInBasket = string.Empty;

        public string ObjectId { get { return objectId; } }
        public string DisplayName { get { return displayName; } }
        public Vector3 HomePosition { get { return homePosition; } }
        public bool IsPlaced { get { return !string.IsNullOrEmpty(placedInBasket); } }
        public string PlacedInBasket { get { return placedInBasket; } }

        public void Configure(string id, string name)
        {
            objectId = id;
            displayName = name;
            homePosition = transform.position;
        }

        public void BeginDrag(float height)
        {
            Vector3 position = transform.position;
            position.y = height;
            transform.position = position;
        }

        public void MoveTo(Vector3 position)
        {
            transform.position = position;
        }

        public void ReturnHome()
        {
            transform.position = homePosition;
            placedInBasket = string.Empty;
        }

        public void MarkPlaced(string basketId, Vector3 snapPoint)
        {
            placedInBasket = basketId;
            transform.position = snapPoint;
        }
    }
}
