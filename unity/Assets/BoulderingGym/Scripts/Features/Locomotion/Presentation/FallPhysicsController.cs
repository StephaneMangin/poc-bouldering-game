using Project.Features.Climbing.Presentation;
using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using UnityEngine;

namespace Project.Features.Locomotion.Presentation
{
    public sealed class FallPhysicsController : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachineDriver driver;
        [SerializeField] private PlayerHandsController playerHands;
        [SerializeField] private HipsMovementController hipsMovement;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform resetAnchor;
        [SerializeField] private float resetBelowY = -2f;

        private bool _isFalling;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private void Awake()
        {
            if (driver == null) driver = GetComponent<PlayerStateMachineDriver>();
            if (playerHands == null) playerHands = GetComponent<PlayerHandsController>();
            if (hipsMovement == null) hipsMovement = GetComponentInChildren<HipsMovementController>();
            if (characterController == null) characterController = GetComponent<CharacterController>();

            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        private void Update()
        {
            if (driver == null || driver.StateMachine == null) return;

            var isFallingState = driver.StateMachine.CurrentState.Id == PlayerStateId.Falling;

            if (isFallingState && !_isFalling)
            {
                BeginFall();
            }

            if (!isFallingState && _isFalling)
            {
                EndFall();
                return;
            }

            if (_isFalling && transform.position.y <= resetBelowY)
            {
                ResetAfterFall();
            }
        }

        private void BeginFall()
        {
            _isFalling = true;
            playerHands?.ReleaseAll();
            if (hipsMovement != null) hipsMovement.enabled = false;
        }

        private void EndFall()
        {
            _isFalling = false;
            if (hipsMovement != null) hipsMovement.enabled = true;
        }

        private void ResetAfterFall()
        {
            var targetPosition = resetAnchor != null ? resetAnchor.position : _initialPosition;
            var targetRotation = resetAnchor != null ? resetAnchor.rotation : _initialRotation;

            if (characterController != null) characterController.enabled = false;
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            if (characterController != null) characterController.enabled = true;

            _isFalling = false;
            if (hipsMovement != null) hipsMovement.enabled = true;
            driver.StateMachine.Fire(PlayerTrigger.Landed);
        }
    }
}
