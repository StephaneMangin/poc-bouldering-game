using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using UnityEngine;

namespace Project.Features.Locomotion.Presentation
{
    public sealed class LocomotionAnimatorController : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachineDriver driver;
        [SerializeField] private Animator mannequinAnimator;

        [Header("Animator Parameters")]
        [SerializeField] private string walkingParameter = "is_walking";
        [SerializeField] private string walkingBackwardParameter = "is_walking_backward";
        [SerializeField] private string landParameter = "land";
        [SerializeField] private string stateParameter = "ClimbingState";

        private int _walkingHash;
        private int _walkingBackwardHash;
        private int _landHash;
        private int _stateHash;
        private bool _hasWalking;
        private bool _hasWalkingBackward;
        private bool _hasLand;
        private bool _hasState;
        private PlayerStateId _previousState;

        public Animator MannequinAnimator => mannequinAnimator;

        public void Initialize(PlayerStateMachineDriver driverRef, Animator animator)
        {
            driver = driverRef;
            mannequinAnimator = animator;
            CacheParameterHashes();
            InitPreviousState();
        }

        private void Awake()
        {
            CacheParameterHashes();
            InitPreviousState();
        }

        private void InitPreviousState()
        {
            _previousState = driver != null && driver.StateMachine != null
                ? driver.StateMachine.CurrentState.Id
                : PlayerStateId.IdleGround;
        }

        private void CacheParameterHashes()
        {
            if (mannequinAnimator == null)
            {
                return;
            }

            _walkingHash = Animator.StringToHash(walkingParameter);
            _walkingBackwardHash = Animator.StringToHash(walkingBackwardParameter);
            _landHash = Animator.StringToHash(landParameter);
            _stateHash = Animator.StringToHash(stateParameter);
            _hasWalking = HasParameter(walkingParameter);
            _hasWalkingBackward = HasParameter(walkingBackwardParameter);
            _hasLand = HasParameter(landParameter);
            _hasState = HasParameter(stateParameter);
        }

        public void UpdateAnimatorState(PlayerStateId state, bool isWalking, bool isWalkingBackward = false)
        {
            if (mannequinAnimator == null)
            {
                return;
            }

            if (!_hasWalking && !_hasLand && !_hasState)
            {
                CacheParameterHashes();
            }

            if (_hasState)
            {
                mannequinAnimator.SetInteger(_stateHash, (int)state);
            }

            if (_hasWalking)
            {
                mannequinAnimator.SetBool(_walkingHash, isWalking);
            }

            if (_hasWalkingBackward)
            {
                mannequinAnimator.SetBool(_walkingBackwardHash, isWalkingBackward);
            }

            if (_hasLand && _previousState == PlayerStateId.Falling && state == PlayerStateId.IdleGround)
            {
                mannequinAnimator.SetTrigger(_landHash);
            }

            _previousState = state;
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
