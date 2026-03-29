using Project.Core.Domain;
using Project.Core.Input;
using Project.Features.Camera.Presentation;
using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using UnityEngine;

namespace Project.Features.Locomotion.Presentation
{
    /// <summary>
    /// Programmatic ground locomotion via CharacterController.
    /// Handles: walk movement, body rotation, gravity, and auto-landing.
    /// Animation is cosmetic — code drives movement, not root motion.
    /// </summary>
    public sealed class GroundLocomotionController : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private CameraFollowController cameraFollowController;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerStateMachineDriver driver;
        [SerializeField] private TuningConfig tuningConfig;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float turnSpeed = 8f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundedPull = -2f;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField] private LayerMask groundCheckMask = ~0;

        private float _verticalVelocity;
        private int _groundedFrames;

        public bool IsWalking { get; private set; }
        public bool IsWalkingBackward { get; private set; }

        private void Awake()
        {
            _verticalVelocity = groundedPull;

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (driver == null)
            {
                driver = GetComponent<PlayerStateMachineDriver>();
            }

            if (inputReader == null)
            {
                inputReader = GetComponent<InputReader>();
            }

            if (cameraFollowController == null)
            {
                cameraFollowController = FindFirstObjectByType<CameraFollowController>();
            }

            if (tuningConfig != null)
            {
                walkSpeed = tuningConfig.hipsMoveSpeed;
            }
        }

        private void Update()
        {
            if (characterController == null || !characterController.enabled)
            {
                return;
            }

            if (driver == null || driver.StateMachine == null)
            {
                return;
            }

            var state = driver.StateMachine.CurrentState.Id;

            // Only control movement during ground and falling states
            if (state != PlayerStateId.IdleGround && state != PlayerStateId.Falling)
            {
                IsWalking = false;
                IsWalkingBackward = false;
                return;
            }

            ApplyGravity();

            var moveInput = state == PlayerStateId.IdleGround && inputReader != null
                ? inputReader.ReadMove()
                : Vector2.zero;

            var hasInput = moveInput.sqrMagnitude > 0.01f;
            var horizontalMovement = Vector3.zero;

            if (hasInput && cameraFollowController != null)
            {
                var yawRotation = Quaternion.Euler(0f, cameraFollowController.OrbitYaw, 0f);
                var direction = yawRotation * new Vector3(moveInput.x, 0f, moveInput.y);
                direction.Normalize();
                horizontalMovement = direction * (walkSpeed * Time.deltaTime);

                // Rotate body toward camera forward (not movement direction)
                // so backward walking plays correctly without spinning the character
                var facingDirection = yawRotation * Vector3.forward;
                var targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }

            IsWalkingBackward = hasInput && moveInput.y < -0.1f && Mathf.Abs(moveInput.x) < Mathf.Abs(moveInput.y);
            IsWalking = hasInput && !IsWalkingBackward;

            var verticalMovement = new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f);
            characterController.Move(horizontalMovement + verticalMovement);

            // Auto-landing: if falling and we hit ground, transition to IdleGround
            if (state == PlayerStateId.Falling && characterController.isGrounded)
            {
                driver.StateMachine.Fire(PlayerTrigger.Landed);
            }
        }

        /// <summary>
        /// Combines CC.isGrounded with a spherecast backup.
        /// CC.isGrounded can flap false during horizontal movement on flat surfaces.
        /// </summary>
        private bool CheckGrounded()
        {
            if (characterController.isGrounded)
            {
                return true;
            }

            // Spherecast from capsule bottom to detect ground within tolerance
            var bottom = transform.position + characterController.center
                         + Vector3.down * (characterController.height * 0.5f - characterController.radius);
            return UnityEngine.Physics.SphereCast(
                bottom, characterController.radius * 0.9f,
                Vector3.down, out _,
                groundCheckDistance + characterController.skinWidth,
                groundCheckMask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyGravity()
        {
            var grounded = CheckGrounded();

            if (grounded)
            {
                _groundedFrames++;
                _verticalVelocity = groundedPull;
            }
            else
            {
                _groundedFrames = 0;
                _verticalVelocity += gravity * Time.deltaTime;
                // Clamp terminal velocity to avoid clipping through thin geometry
                _verticalVelocity = Mathf.Max(_verticalVelocity, -15f);
            }
        }
    }
}
