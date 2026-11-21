using UnityEngine;

namespace CleanMyRoom.Player
{
    /// <summary>
    /// Keeps the player framed while preserving the original room-wide view.
    /// Only the follow target changes; the camera keeps a fixed offset and looks
    /// at a point slightly above the player's feet.
    ///
    /// SmoothDamp avoids frame-rate dependent jitter. The minimum height keeps
    /// the camera above the five-unit bedroom walls, preventing wall clipping.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 6.5f, -9.5f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 0f);
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.18f;
        [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.18f;
        [SerializeField, Min(0f)] private float minimumHeight = 5.6f;

        private Vector3 positionVelocity;

        public Transform Target
        {
            get { return target; }
            set { target = value; }
        }

        public Vector3 Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        public Vector3 LookOffset
        {
            get { return lookOffset; }
            set { lookOffset = value; }
        }

        public float MinimumHeight
        {
            get { return minimumHeight; }
            set { minimumHeight = Mathf.Max(0f, value); }
        }

        private void LateUpdate()
        {
            Follow(Time.deltaTime);
        }

        /// <summary>Move the camera directly to its ideal pose (used on scene start).</summary>
        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            positionVelocity = Vector3.zero;
            transform.position = DesiredPosition();
            transform.rotation = DesiredRotation();
        }

        /// <summary>Advance the smoothed follow by one frame.</summary>
        public void Follow(float deltaTime)
        {
            if (target == null || deltaTime <= 0f)
            {
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                DesiredPosition(),
                ref positionVelocity,
                positionSmoothTime,
                Mathf.Infinity,
                deltaTime);

            // Exponential smoothing is frame-rate independent and cannot
            // overshoot, so the view stays calm when the player changes direction.
            float blend = 1f - Mathf.Exp(-deltaTime / rotationSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, DesiredRotation(), blend);
        }

        private Vector3 DesiredPosition()
        {
            Vector3 desired = target.position + offset;
            desired.y = Mathf.Max(desired.y, minimumHeight);
            return desired;
        }

        private Quaternion DesiredRotation()
        {
            Vector3 lookAt = target.position + lookOffset;
            Vector3 direction = lookAt - transform.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return transform.rotation;
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
