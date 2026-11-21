using CleanMyRoom.Player.Input;
using UnityEngine;

namespace CleanMyRoom.Player
{
    /// <summary>
    /// Keyboard-driven movement for the child character.
    ///
    /// WASD and the arrow keys are equivalent. The input vector is normalised
    /// before use so diagonal movement is not faster than axis-aligned movement.
    /// Movement is performed by a CharacterController, which makes the existing
    /// room colliders stop the player. A small X/Z play-area clamp also keeps the
    /// character on the bedroom floor at the open (front) edge.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const string DefaultSpeedParameter = "Speed";

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkingSpeed = 3.2f;
        [SerializeField, Min(0f)] private float turnSpeed = 12f;
        [SerializeField] private float gravity = -18f;

        // Vector2 components are X and Z. The front of the room is open, so the
        // negative Z limit is tighter than the wall-limited sides and back.
        [Header("Play area (X / Z limits)")]
        [SerializeField] private Vector2 minPlayArea = new Vector2(-9.6f, -9.2f);
        [SerializeField] private Vector2 maxPlayArea = new Vector2(9.6f, 9.6f);

        [Header("Animation")]
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private string speedParameter = DefaultSpeedParameter;

        private CharacterController characterController;
        private Vector2 moveInput;
        private float verticalVelocity = -2f;
        private int speedParameterId;

        public float WalkingSpeed
        {
            get { return walkingSpeed; }
            set { walkingSpeed = Mathf.Max(0f, value); }
        }

        public Vector2 MinPlayArea
        {
            get { return minPlayArea; }
            set { minPlayArea = value; }
        }

        public Vector2 MaxPlayArea
        {
            get { return maxPlayArea; }
            set { maxPlayArea = value; }
        }

        public Animator CharacterAnimator
        {
            get { return characterAnimator; }
            set { characterAnimator = value; }
        }

        /// <summary>Planar speed produced by the most recent movement step.</summary>
        public float CurrentSpeed { get; private set; }

        public bool IsMoving { get { return CurrentSpeed > 0.01f; } }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterAnimator == null)
            {
                characterAnimator = GetComponent<Animator>();
            }

            speedParameterId = Animator.StringToHash(
                string.IsNullOrEmpty(speedParameter) ? DefaultSpeedParameter : speedParameter);
        }

        private void Start()
        {
            ClampToPlayArea();
        }

        private void Update()
        {
            moveInput = ReadKeyboardMovement();
            ApplyMovement(moveInput, Time.deltaTime);
        }

        /// <summary>
        /// Read WASD and arrow keys from the new Input System. Both arrows and
        /// letters can be held together; opposing keys cancel each other out.
        /// </summary>
        public static Vector2 ReadKeyboardMovement()
        {
            return KeyboardInput.ReadMovement();
        }

        /// <summary>
        /// Keep combined input within unit length. This is what prevents a
        /// diagonal (1, 1) input from moving sqrt(2) times faster.
        /// </summary>
        public static Vector2 NormalizeForDiagonalMovement(Vector2 input)
        {
            return KeyboardInput.NormalizeMovement(input);
        }

        /// <summary>
        /// Apply one movement step. Public so tests and future input sources can
        /// drive the same code path that Update() uses.
        /// </summary>
        public void ApplyMovement(Vector2 input, float deltaTime)
        {
            // Edit-mode tests can construct the component tree directly; fetch
            // the controller here too so this path never depends on Awake order.
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
            if (characterController == null || deltaTime <= 0f)
            {
                return;
            }

            Vector2 clampedInput = NormalizeForDiagonalMovement(input);
            Vector3 planarDirection = new Vector3(clampedInput.x, 0f, clampedInput.y);

            Vector3 target = transform.position + planarDirection * (walkingSpeed * deltaTime);
            target.x = Mathf.Clamp(target.x, minPlayArea.x, maxPlayArea.x);
            target.z = Mathf.Clamp(target.z, minPlayArea.y, maxPlayArea.y);

            Vector3 delta = target - transform.position;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * deltaTime;
            }
            delta.y = verticalVelocity * deltaTime;

            CollisionFlags flags = characterController.Move(delta);
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            {
                verticalVelocity = 0f;
            }

            ClampToPlayArea();

            CurrentSpeed = planarDirection.sqrMagnitude > 0.0001f
                ? walkingSpeed * clampedInput.magnitude
                : 0f;

            if (planarDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(
                    planarDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, desiredRotation, turnSpeed * deltaTime);
            }

            UpdateAnimator();
        }

        private void ClampToPlayArea()
        {
            Vector3 position = transform.position;
            float clampedX = Mathf.Clamp(position.x, minPlayArea.x, maxPlayArea.x);
            float clampedZ = Mathf.Clamp(position.z, minPlayArea.y, maxPlayArea.y);

            if (Mathf.Approximately(clampedX, position.x) &&
                Mathf.Approximately(clampedZ, position.z))
            {
                return;
            }

            // Disable the controller for the teleport so it does not resolve the
            // correction as a second movement step in the same frame.
            bool wasEnabled = characterController != null && characterController.enabled;
            if (wasEnabled)
            {
                characterController.enabled = false;
            }

            transform.position = new Vector3(clampedX, position.y, clampedZ);

            if (wasEnabled)
            {
                characterController.enabled = true;
            }
        }

        private void UpdateAnimator()
        {
            if (characterAnimator == null)
            {
                return;
            }

            float normalizedSpeed = walkingSpeed > 0f
                ? Mathf.Clamp01(CurrentSpeed / walkingSpeed)
                : 0f;
            characterAnimator.SetFloat(speedParameterId, normalizedSpeed, 0.12f, Time.deltaTime);
        }
    }
}
