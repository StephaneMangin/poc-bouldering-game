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
                return;
            }

            ApplyGravity();

            var wantsWalk = state == PlayerStateId.IdleGround
                            && inputReader != null
                            && inputReader.ReadWalkHeld();

            var horizontalMovement = Vector3.zero;

            if (wantsWalk && cameraFollowController != null)
            {
                var direction = Quaternion.Euler(0f, cameraFollowController.OrbitYaw, 0f) * Vector3.forward;
                horizontalMovement = direction * (walkSpeed * Time.deltaTime);

                // Rotate body toward movement direction
                var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }

            IsWalking = wantsWalk;

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
            }
        }
    }
}
