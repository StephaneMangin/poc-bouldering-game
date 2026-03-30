using Project.Core.Domain;
using Project.Core.Input;
using Project.Core.Utilities;
using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Presentation;
using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using Project.Features.Visual.Presentation;
using UnityEngine;

namespace Project.Features.Locomotion.Presentation
{
    public sealed class HipsMovementController : MonoBehaviour
    {
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private Transform movementTarget;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerStateMachineDriver driver;
        [SerializeField] private Transform wallNormalReference;
        [SerializeField] private float groundApproachMultiplier = 1.15f;
        [SerializeField] private float minDistanceFromWall = 0.4f;
        [SerializeField] private float maxDistanceFromWall = 2.5f;
        [SerializeField] private bool useRootMotionGroundLocomotion = true;

        [Header("Climbing Body Follow")]
        [SerializeField] private float climbBodySnapSpeed = 12f;
        [SerializeField] private Transform pelvisBone;

        private Vector3 _pelvisBoneOffset;
        private ClimbingBodySolver.AnatomyParams _measuredAnatomy;

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
                driver = GetComponentInParent<PlayerStateMachineDriver>();
            }

            if (wallNormalReference == null && movementTarget != null)
            {
                wallNormalReference = movementTarget.Find("WallNormalReference");
            }

            HumanoidRigMapping rig = null;
            if (pelvisBone == null && movementTarget != null)
            {
                rig = movementTarget.GetComponentInChildren<HumanoidRigMapping>();
                if (rig != null)
                {
                    pelvisBone = rig.pelvis;
                }
            }

            if (pelvisBone != null && movementTarget != null)
            {
                _pelvisBoneOffset = pelvisBone.position - movementTarget.position;
            }

            _measuredAnatomy = MeasureAnatomy(rig);
        }

        private static ClimbingBodySolver.AnatomyParams MeasureAnatomy(HumanoidRigMapping rig)
        {
            if (rig == null || rig.pelvis == null)
            {
                return ClimbingBodySolver.AnatomyParams.Default;
            }

            return ClimbingBodySolver.AnatomyParams.FromMeasurements(
                rig.pelvis.position,
                rig.leftUpperArm.position, rig.leftForearm.position, rig.leftHand.position,
                rig.rightUpperArm.position, rig.rightForearm.position, rig.rightHand.position,
                rig.leftUpperLeg.position, rig.leftLowerLeg.position, rig.leftFoot.position,
                rig.rightUpperLeg.position, rig.rightLowerLeg.position, rig.rightFoot.position);
        }

        private void Update()
        {
            if (tuningConfig == null || movementTarget == null)
            {
                return;
            }

            if (IsClimbingState())
            {
                ApplyClimbingBodyFollow();
                return;
            }

            var input = inputReader != null ? inputReader.ReadMove() : Vector2.zero;

            var isGroundLocomotionEnabled = IsGroundLocomotionEnabled();
            if (isGroundLocomotionEnabled && useRootMotionGroundLocomotion)
            {
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
            if (driver == null || driver.StateMachine == null) return false;
            var state = driver.StateMachine.CurrentState.Id;
            return state == PlayerStateId.IdleGround || state == PlayerStateId.Falling;
        }

        private bool IsClimbingState()
        {
            if (driver == null || driver.StateMachine == null) return false;
            var state = driver.StateMachine.CurrentState.Id;
            return state == PlayerStateId.Reach || state == PlayerStateId.GripStable;
        }

        private void ApplyClimbingBodyFollow()
        {
            var hands = driver != null ? driver.PlayerHands : null;
            if (hands == null || !hands.HasAnySupport)
            {
                return;
            }

            var contacts = BuildLimbContacts(hands);
            var wallNormal = WallGeometry.GetWallNormal(wallNormalReference);
            var solution = ClimbingBodySolver.Solve(contacts, _measuredAnatomy, wallNormal);

            if (!solution.IsValid)
            {
                return;
            }

            var rootTarget = ClimbingBodySolver.ComputeRootPosition(solution.PelvisPosition, _pelvisBoneOffset);
            movementTarget.position = Vector3.MoveTowards(
                movementTarget.position, rootTarget, climbBodySnapSpeed * Time.deltaTime);
        }

        private static ClimbingBodySolver.LimbContacts BuildLimbContacts(PlayerHandsController hands)
        {
            return new ClimbingBodySolver.LimbContacts
            {
                LeftHand = GetGripPosition(hands.LeftCurrentHold),
                RightHand = GetGripPosition(hands.RightCurrentHold),
                LeftFoot = GetGripPosition(hands.LeftFootHold),
                RightFoot = GetGripPosition(hands.RightFootHold),
            };
        }

        private static Vector3? GetGripPosition(HoldComponent hold)
        {
            if (hold == null) return null;
            return hold.gripPoint != null ? hold.gripPoint.position : hold.transform.position;
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
