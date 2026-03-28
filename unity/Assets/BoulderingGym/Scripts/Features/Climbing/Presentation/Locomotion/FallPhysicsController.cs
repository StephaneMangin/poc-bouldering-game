using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class FallPhysicsController : MonoBehaviour
    {
        [SerializeField] private ClimbingStateMachineDriver driver;
        [SerializeField] private PlayerHandsController playerHands;
        [SerializeField] private HipsMovementController hipsMovement;
        [SerializeField] private Rigidbody playerRigidbody;
        [SerializeField] private Transform resetAnchor;
        [SerializeField] private float resetBelowY = -2f;

        private bool _isFalling;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private void Awake()
        {
            if (driver == null)
            {
                driver = GetComponent<ClimbingStateMachineDriver>();
            }

            if (playerHands == null)
            {
                playerHands = GetComponent<PlayerHandsController>();
            }

            if (hipsMovement == null)
            {
                hipsMovement = GetComponentInChildren<HipsMovementController>();
            }

            if (playerRigidbody == null)
            {
                playerRigidbody = GetComponent<Rigidbody>();
            }

            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            ConfigureRigidbody(isFalling: false);
        }

        private void Update()
        {
            if (driver == null || driver.StateMachine == null)
            {
                return;
            }

            var isFallingState = driver.StateMachine.CurrentState.Id == ClimbingStateId.Falling;
            if (isFallingState && !_isFalling)
            {
                BeginFall();
            }

            if (!isFallingState && _isFalling)
            {
                EndFallWithoutReset();
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
            if (hipsMovement != null)
            {
                hipsMovement.enabled = false;
            }

            ConfigureRigidbody(isFalling: true);
        }

        private void EndFallWithoutReset()
        {
            _isFalling = false;

            var targetPosition = resetAnchor != null ? resetAnchor.position : _initialPosition;
            var targetRotation = resetAnchor != null ? resetAnchor.rotation : _initialRotation;
            transform.SetPositionAndRotation(targetPosition, targetRotation);

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            ConfigureRigidbody(isFalling: false);
            if (hipsMovement != null)
            {
                hipsMovement.enabled = true;
            }
        }

        private void ResetAfterFall()
        {
            var targetPosition = resetAnchor != null ? resetAnchor.position : _initialPosition;
            var targetRotation = resetAnchor != null ? resetAnchor.rotation : _initialRotation;

            transform.SetPositionAndRotation(targetPosition, targetRotation);
            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            _isFalling = false;
            ConfigureRigidbody(isFalling: false);
            if (hipsMovement != null)
            {
                hipsMovement.enabled = true;
            }

            driver.StateMachine.Fire(ClimbingTrigger.Landed);
        }

        private void ConfigureRigidbody(bool isFalling)
        {
            if (playerRigidbody == null)
            {
                return;
            }

            playerRigidbody.useGravity = isFalling;
            playerRigidbody.isKinematic = !isFalling;
            playerRigidbody.constraints = isFalling
                ? RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ
                : RigidbodyConstraints.FreezeRotation;
        }
    }
}
