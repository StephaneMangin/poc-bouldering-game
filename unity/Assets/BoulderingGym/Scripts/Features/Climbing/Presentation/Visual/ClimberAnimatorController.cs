using Project.Features.Climbing.Domain;
using Project.Core.Input;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class ClimberAnimatorController : MonoBehaviour
    {
        [SerializeField] private ClimbingStateMachineDriver driver;
        [SerializeField] private Animator mannequinAnimator;
        [SerializeField] private InputReader inputReader;

        [Header("Animator Parameters")]
        [SerializeField] private string stateParameter = "ClimbingState";
        [SerializeField] private string moveXParameter = "movex";
        [SerializeField] private string moveYParameter = "movez";
        [SerializeField] private string speedParameter = "speed";
        [SerializeField] private string groundedParameter = "is_grounded";
        [SerializeField] private float dampTime = 0.1f;
        [SerializeField] private float walkMaxSpeed = 2.2f;
        [SerializeField] private float sprintMultiplier = 2f;

        private bool _hasState;
        private bool _hasMoveX;
        private bool _hasMoveY;
        private bool _hasSpeed;
        private bool _hasGrounded;
        private int _stateHash;
        private int _moveXHash;
        private int _moveYHash;
        private int _speedHash;
        private int _groundedHash;

        public Animator MannequinAnimator => mannequinAnimator;

        public void Initialize(ClimbingStateMachineDriver driverRef, Animator animator, InputReader input)
        {
            driver = driverRef;
            mannequinAnimator = animator;
            inputReader = input;
            CacheParameterHashes();
        }

        private void Awake()
        {
            CacheParameterHashes();
        }

        private void CacheParameterHashes()
        {
            if (mannequinAnimator == null)
            {
                return;
            }

            _stateHash = Animator.StringToHash(stateParameter);
            _moveXHash = Animator.StringToHash(moveXParameter);
            _moveYHash = Animator.StringToHash(moveYParameter);
            _speedHash = Animator.StringToHash(speedParameter);
            _groundedHash = Animator.StringToHash(groundedParameter);
            _hasState = HasParameter(stateParameter);
            _hasMoveX = HasParameter(moveXParameter);
            _hasMoveY = HasParameter(moveYParameter);
            _hasSpeed = HasParameter(speedParameter);
            _hasGrounded = HasParameter(groundedParameter);
        }

        public void UpdateAnimatorState(ClimbingStateId state, Vector3 smoothedVelocity)
        {
            if (mannequinAnimator == null)
            {
                return;
            }

            if (_hasState)
            {
                mannequinAnimator.SetInteger(_stateHash, (int)state);
            }

            var localVelocity = transform.InverseTransformDirection(Vector3.ProjectOnPlane(smoothedVelocity, Vector3.up));
            var normalizedX = Mathf.Clamp(localVelocity.x / Mathf.Max(walkMaxSpeed, 0.01f), -1f, 1f);
            var normalizedY = Mathf.Clamp(localVelocity.z / Mathf.Max(walkMaxSpeed, 0.01f), -1f, 1f);
            var speed01 = Mathf.Clamp01(smoothedVelocity.magnitude / Mathf.Max(walkMaxSpeed, 0.01f));

            if (state == ClimbingStateId.IdleGround && inputReader != null)
            {
                var input = inputReader.ReadMove();
                var multiplier = inputReader.ReadSprint() ? sprintMultiplier : 1f;
                normalizedX = Mathf.Clamp(input.x * multiplier, -2f, 2f);
                normalizedY = Mathf.Clamp(input.y * multiplier, -2f, 2f);
                speed01 = Mathf.Clamp01(input.magnitude * multiplier);
            }

            if (_hasMoveX)
            {
                mannequinAnimator.SetFloat(_moveXHash, normalizedX, dampTime, Time.deltaTime);
            }

            if (_hasMoveY)
            {
                mannequinAnimator.SetFloat(_moveYHash, normalizedY, dampTime, Time.deltaTime);
            }

            if (_hasSpeed)
            {
                mannequinAnimator.SetFloat(_speedHash, speed01, dampTime, Time.deltaTime);
            }

            if (_hasGrounded)
            {
                mannequinAnimator.SetBool(_groundedHash, state == ClimbingStateId.IdleGround);
            }
        }

        private bool HasParameter(string parameterName)
        {
            if (mannequinAnimator == null)
            {
                return false;
            }

            foreach (var parameter in mannequinAnimator.parameters)
            {
                if (parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
