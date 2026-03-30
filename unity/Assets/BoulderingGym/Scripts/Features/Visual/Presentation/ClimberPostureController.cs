using Project.Core.Utilities;
using Project.Features.Climbing.Presentation;
using Project.Features.Physics.Presentation;
using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using UnityEngine;

namespace Project.Features.Visual.Presentation
{
    public sealed class ClimberPostureController : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachineDriver driver;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private HumanoidRigMapping rigMapping;
        [SerializeField] private PhysicsModelDriver physicsDriver;

        [Header("Posture Tuning")]
        [SerializeField] private float torsoLeanAngle = 12f;
        [SerializeField] private float headTrackingWeight = 0.85f;

        public void Initialize(PlayerStateMachineDriver driverRef, Transform visual, HumanoidRigMapping rig,
            PhysicsModelDriver physicsDriverRef = null)
        {
            driver = driverRef;
            visualRoot = visual;
            rigMapping = rig;
            physicsDriver = physicsDriverRef;
        }

        public void ApplyPosture(PlayerStateId state)
        {
            if (visualRoot == null || rigMapping == null || rigMapping.torso == null || rigMapping.pelvis == null)
            {
                return;
            }

            ApplyClimbingPosture(state);
        }

        private void ApplyClimbingPosture(PlayerStateId state)
        {
            var supportCenter = GetSupportCenter();
            var supportOffset = supportCenter - rigMapping.pelvis.position;
            var yaw = Mathf.Clamp(supportOffset.x * 18f, -16f, 16f);
            var pitch = state switch
            {
                PlayerStateId.Falling => 24f,
                PlayerStateId.Reach => -6f,
                PlayerStateId.GripStable => -2f,
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

        private Vector3 GetSupportCenter()
        {
            // Use physics model COG when available (Sprint 2.0+)
            if (physicsDriver != null)
            {
                var snapshot = physicsDriver.CurrentSnapshot;
                if (snapshot.COG.Position.sqrMagnitude > 0.001f)
                {
                    return snapshot.COG.Position;
                }
            }

            // Fallback: average of held grip points (not anchors — anchors move when bones reset)
            var hands = driver != null ? driver.PlayerHands : null;
            var accumulated = Vector3.zero;
            var count = 0;

            AccumulateHoldGrip(hands?.LeftCurrentHold, ref accumulated, ref count);
            AccumulateHoldGrip(hands?.RightCurrentHold, ref accumulated, ref count);
            AccumulateHoldGrip(hands?.LeftFootHold, ref accumulated, ref count);
            AccumulateHoldGrip(hands?.RightFootHold, ref accumulated, ref count);

            return count == 0 ? transform.position + Vector3.up * 1.3f : accumulated / count;
        }

        private static void AccumulateHoldGrip(HoldComponent hold, ref Vector3 sum, ref int count)
        {
            if (hold == null) return;
            sum += hold.gripPoint != null ? hold.gripPoint.position : hold.transform.position;
            count++;
        }
    }
}
