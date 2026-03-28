using Project.Core.Domain;
using Project.Core.Input;
using Project.Core.Utilities;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class HipsMovementController : MonoBehaviour
    {
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private Transform movementTarget;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private ClimbingStateMachineDriver driver;
        [SerializeField] private Transform wallNormalReference;
        [SerializeField] private float groundApproachMultiplier = 1.15f;
        [SerializeField] private float minDistanceFromWall = 0.4f;
        [SerializeField] private float maxDistanceFromWall = 2.5f;
        [SerializeField] private bool useRootMotionGroundLocomotion = true;

        private void Awake()
        {
            if (movementTarget == null)
            {
                movementTarget = transform.parent != null ? transform.parent : transform;
            }

            if (inputReader == null)
            {
                inputReader = GetComponentInParent<InputReader>();
            }

            if (driver == null)
            {
                driver = GetComponentInParent<ClimbingStateMachineDriver>();
            }

            if (wallNormalReference == null && movementTarget != null)
            {
                wallNormalReference = movementTarget.Find("WallNormalReference");
            }
        }

        private void Update()
        {
            if (tuningConfig == null || movementTarget == null)
            {
                return;
            }

            var input = inputReader != null ? inputReader.ReadMove() : Vector2.zero;

            var isGroundLocomotionEnabled = IsGroundLocomotionEnabled();
            if (isGroundLocomotionEnabled && useRootMotionGroundLocomotion)
            {
                // Wall distance clamp is handled by GroundRootMotionDriver.
                return;
            }

            var movement = isGroundLocomotionEnabled
                ? BuildGroundMovement(input)
                : new Vector3(input.x, input.y, 0f);

            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            movementTarget.position += movement * (tuningConfig.hipsMoveSpeed * Time.deltaTime);

            if (isGroundLocomotionEnabled)
            {
                ClampDistanceFromWall();
            }
        }

        private bool IsGroundLocomotionEnabled()
        {
            return driver != null
                && driver.StateMachine != null
                && driver.StateMachine.CurrentState.Id == ClimbingStateId.IdleGround;
        }

        private void ClampDistanceFromWall()
        {
            var wallNormal = WallGeometry.GetWallNormal(wallNormalReference);
            WallGeometry.ClampDistanceFromWall(movementTarget, wallNormal, minDistanceFromWall, maxDistanceFromWall);
        }

        private Vector3 BuildGroundMovement(Vector2 input)
        {
            var wallNormal = WallGeometry.GetWallNormal(wallNormalReference);
            var approach = WallGeometry.GetApproachDirection(wallNormal);
            var lateral = WallGeometry.GetLateralDirection(approach);
            return (lateral * input.x) + (approach * (input.y * groundApproachMultiplier));
        }
    }
}
