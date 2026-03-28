using Project.Core.Domain;
using Project.Core.Input;
using Project.Core.Utilities;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class GroundRootMotionDriver : MonoBehaviour
    {
        [SerializeField] private Animator mannequinAnimator;
        [SerializeField] private ClimbingStateMachineDriver driver;
        [SerializeField] private Transform movementTarget;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private Transform wallNormalReference;
        [SerializeField] private bool applyOnlyInIdleGround = true;
        [SerializeField] private float rootMotionScale = 1f;
        [SerializeField] private bool applyYawFromRootMotion;
        [SerializeField] private float deltaEpsilon = 0.00005f;
        [SerializeField] private float fallbackMoveMultiplier = 1f;
        [SerializeField] private float minDistanceFromWall = 0.4f;
        [SerializeField] private float maxDistanceFromWall = 2.5f;

        private void Awake()
        {
            if (mannequinAnimator == null)
            {
                mannequinAnimator = GetComponent<Animator>();
            }

            if (driver == null)
            {
                driver = GetComponentInParent<ClimbingStateMachineDriver>();
            }

            if (inputReader == null)
            {
                inputReader = GetComponentInParent<InputReader>();
            }

            if (wallNormalReference == null && movementTarget != null)
            {
                wallNormalReference = movementTarget.Find("WallNormalReference");
            }

            if (movementTarget == null)
            {
                movementTarget = transform.parent;
            }

            if (mannequinAnimator != null)
            {
                mannequinAnimator.applyRootMotion = true;
            }
        }

        private void OnAnimatorMove()
        {
            if (mannequinAnimator == null || movementTarget == null || driver == null || driver.StateMachine == null)
            {
                return;
            }

            if (applyOnlyInIdleGround && driver.StateMachine.CurrentState.Id != ClimbingStateId.IdleGround)
            {
                return;
            }

            var delta = Vector3.ProjectOnPlane(mannequinAnimator.deltaPosition, Vector3.up) * rootMotionScale;
            if (delta.sqrMagnitude <= deltaEpsilon)
            {
                delta = BuildFallbackDelta();
            }

            movementTarget.position += delta;
            ClampDistanceFromWall();

            if (!applyYawFromRootMotion)
            {
                return;
            }

            var deltaRotation = mannequinAnimator.deltaRotation;
            var yaw = deltaRotation.eulerAngles.y;
            movementTarget.rotation = Quaternion.Euler(0f, movementTarget.rotation.eulerAngles.y + yaw, 0f);
        }

        private Vector3 BuildFallbackDelta()
        {
            if (inputReader == null || tuningConfig == null)
            {
                return Vector3.zero;
            }

            var input = inputReader.ReadMove();
            if (input.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            var wallNormal = WallGeometry.GetWallNormal(wallNormalReference);
            var direction = WallGeometry.InputToWorldDirection(input, wallNormal) * input.magnitude;
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            return direction * (tuningConfig.hipsMoveSpeed * fallbackMoveMultiplier * Time.deltaTime);
        }

        private void ClampDistanceFromWall()
        {
            if (movementTarget == null)
            {
                return;
            }

            var wallNormal = WallGeometry.GetWallNormal(wallNormalReference);
            WallGeometry.ClampDistanceFromWall(movementTarget, wallNormal, minDistanceFromWall, maxDistanceFromWall);
        }
    }
}
