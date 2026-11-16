using Project.Core.Domain;
using Project.Core.Input;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    /// <summary>Walking on the mats, camera relative, with the locomotion animator driven from speed.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ClimberLocomotion : MonoBehaviour
    {
        private const float TurnSharpness = 10f;
        private const float Gravity = -9.81f;
        private const float GroundedPull = -1f;
        private const float InputDeadZone = 0.1f;
        private static readonly int WalkingParameter = Animator.StringToHash("is_walking");

        private CharacterController _controller;
        private Animator _animator;
        private InputReader _input;
        private Transform _camera;
        private TuningConfig _tuning;
        private float _verticalSpeed;

        public void Initialize(InputReader input, Animator animator, Transform cameraTransform, TuningConfig tuning)
        {
            _controller = GetComponent<CharacterController>();
            _input = input;
            _animator = animator;
            _camera = cameraTransform;
            _tuning = tuning;
            _animator.applyRootMotion = false;
        }

        public void Tick(float deltaTime)
        {
            var move = _input.ReadMove();
            var forward = Vector3.ProjectOnPlane(_camera.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(_camera.right, Vector3.up).normalized;
            var direction = Vector3.ClampMagnitude(right * move.x + forward * move.y, 1f);
            var moving = direction.sqrMagnitude > InputDeadZone * InputDeadZone;
            var speed = _input.ReadSprint() ? _tuning.runSpeed : _tuning.walkSpeed;

            _verticalSpeed = _controller.isGrounded ? GroundedPull : _verticalSpeed + Gravity * deltaTime;
            _controller.Move((direction * speed + Vector3.up * _verticalSpeed) * deltaTime);
            if (moving)
            {
                var facing = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, facing, TurnSharpness * deltaTime);
            }

            _animator.SetBool(WalkingParameter, moving);
            _animator.speed = moving ? speed / _tuning.walkSpeed : 1f;
        }

        public void Stop()
        {
            _animator.SetBool(WalkingParameter, false);
            _animator.speed = 1f;
        }

        /// <summary>Moves the standing climber without the controller pushing back.</summary>
        public void PlaceAt(Vector3 feet, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(feet, rotation);
            _controller.enabled = true;
            _verticalSpeed = 0f;
        }

        public void SetPhysicsEnabled(bool isEnabled) => _controller.enabled = isEnabled;
    }
}
