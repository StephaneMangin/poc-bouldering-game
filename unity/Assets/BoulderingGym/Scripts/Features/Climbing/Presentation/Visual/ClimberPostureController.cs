using Project.Core.Utilities;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class ClimberPostureController : MonoBehaviour
    {
        [SerializeField] private ClimbingStateMachineDriver driver;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private HumanoidRigMapping rigMapping;

        [Header("Posture Tuning")]
        [SerializeField] private float torsoLeanAngle = 12f;
        [SerializeField] private float headTrackingWeight = 0.85f;

        [Header("Ground Walk")]
        [SerializeField] private float walkRootBob = 2.4f;
        [SerializeField] private float walkRootRoll = 4.2f;

        public void Initialize(ClimbingStateMachineDriver driverRef, Transform visual, HumanoidRigMapping rig)
        {
            driver = driverRef;
            visualRoot = visual;
            rigMapping = rig;
        }

        public void ApplyPosture(ClimbingStateId state, float walkBlend, float walkCycle, Vector3 groundTravelDirection)
        {
            if (visualRoot == null || rigMapping == null || rigMapping.torso == null || rigMapping.pelvis == null)
            {
                return;
            }

            if (state == ClimbingStateId.IdleGround)
            {
                ApplyGroundPosture(walkBlend, walkCycle, groundTravelDirection);
                return;
            }

            ApplyClimbingPosture(state);
        }

        private void ApplyClimbingPosture(ClimbingStateId state)
        {
            var supportCenter = GetSupportCenter();
            var supportOffset = supportCenter - rigMapping.pelvis.position;
            var yaw = Mathf.Clamp(supportOffset.x * 18f, -16f, 16f);
            var pitch = state switch
            {
                ClimbingStateId.Falling => 24f,
                ClimbingStateId.Reach => -6f,
                ClimbingStateId.GripStable => -2f,
                _ => 0f,
            };

            visualRoot.localRotation = Quaternion.Euler(pitch, yaw, 0f);

            var torsoLookTarget = supportCenter + Vector3.up * 0.15f;
            TwoBoneIKSolver.RotateBoneToward(rigMapping.pelvis, torsoLookTarget, torsoLeanAngle);
            TwoBoneIKSolver.RotateBoneToward(rigMapping.torso, torsoLookTarget + Vector3.up * 0.2f, torsoLeanAngle * 1.25f);

            if (rigMapping.head != null)
            {
                TwoBoneIKSolver.RotateBoneToward(rigMapping.head, torsoLookTarget + Vector3.up * 0.3f, 25f * headTrackingWeight);
            }
        }

        private void ApplyGroundPosture(float walkBlend, float walkCycle, Vector3 direction)
        {
            var phase = walkCycle * Mathf.PI * 2f;
            var bob = Mathf.Sin(phase * 2f) * walkRootBob * walkBlend;
            var roll = Mathf.Sin(phase) * walkRootRoll * walkBlend;

            visualRoot.localRotation = Quaternion.Euler(-1.5f + bob, 0f, roll);

            var torsoLookTarget = rigMapping.pelvis.position + (direction * (0.65f + (0.2f * walkBlend))) + Vector3.up * 0.95f;
            TwoBoneIKSolver.RotateBoneToward(rigMapping.pelvis, torsoLookTarget, 10f + (8f * walkBlend));
            TwoBoneIKSolver.RotateBoneToward(rigMapping.torso, torsoLookTarget + Vector3.up * 0.22f, 13f + (10f * walkBlend));

            if (rigMapping.head != null)
            {
                TwoBoneIKSolver.RotateBoneToward(rigMapping.head, torsoLookTarget + Vector3.up * 0.35f, 22f * headTrackingWeight);
            }
        }

        private Vector3 GetSupportCenter()
        {
            var hands = driver != null ? driver.PlayerHands : null;
            var accumulated = Vector3.zero;
            var count = 0;

            if (hands?.LeftHandAnchor != null)
            {
                accumulated += hands.LeftHandAnchor.position;
                count++;
            }

            if (hands?.RightHandAnchor != null)
            {
                accumulated += hands.RightHandAnchor.position;
                count++;
            }

            return count == 0 ? transform.position + Vector3.up * 1.3f : accumulated / count;
        }
    }
}
