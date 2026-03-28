using Project.Core.Utilities;
using Project.Core.Input;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class ClimberIKController : MonoBehaviour
    {
        [SerializeField] private ClimbingStateMachineDriver driver;
        [SerializeField] private HumanoidRigMapping rigMapping;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private Transform wallNormalReference;
        [SerializeField] private Animator mannequinAnimator;
        [SerializeField] private HoldRegistry holdRegistry;

        [Header("Walk Tuning")]
        [SerializeField] private float walkStrideDistance = 0.24f;
        [SerializeField] private float walkFootLift = 0.11f;
        [SerializeField] private float walkFootStance = 0.19f;
        [SerializeField] private float walkArmSwing = 0.08f;
        [SerializeField] private float walkArmLateral = 0.22f;
        [SerializeField] private float walkArmHeight = -0.28f;

        [Header("Foot IK Grounding")]
        [SerializeField] private LayerMask footGroundMask = ~0;
        [SerializeField] private float footRaycastStartHeight = 0.55f;
        [SerializeField] private float footRaycastLength = 1.65f;
        [SerializeField] private float footGroundOffset = 0.03f;

        [Header("Animator-driven Ground")]
        [SerializeField] private bool useAnimatorDrivenGroundPose = true;

        [Header("Joint Limits")]
        [SerializeField] private Vector2 elbowPitchRange = new(5f, 150f);
        [SerializeField] private Vector2 kneePitchRange = new(8f, 160f);
        [SerializeField] private Vector2 shoulderSwingRange = new(-85f, 95f);
        [SerializeField] private Vector2 hipSwingRange = new(-70f, 85f);

        private HoldComponent[] _holds = System.Array.Empty<HoldComponent>();
        private Vector3 _leftFootGroundNormal = Vector3.up;
        private Vector3 _rightFootGroundNormal = Vector3.up;

        public bool UseAnimatorDrivenGroundPose => useAnimatorDrivenGroundPose;

        public void Initialize(
            ClimbingStateMachineDriver driverRef,
            HumanoidRigMapping rig,
            Transform visual,
            InputReader input,
            Transform wallRef,
            Animator animator,
            HoldRegistry registry)
        {
            driver = driverRef;
            rigMapping = rig;
            visualRoot = visual;
            inputReader = input;
            wallNormalReference = wallRef;
            mannequinAnimator = animator;
            holdRegistry = registry;
            RefreshHolds();
        }

        public void RefreshHolds()
        {
            _holds = holdRegistry != null
                ? holdRegistry.GetAllHolds()
                : FindObjectsByType<HoldComponent>(FindObjectsSortMode.None);
        }

        public void ApplyLimbPose(ClimbingStateId state, float walkBlend, float walkCycle, Vector3 groundTravelDirection)
        {
            if (rigMapping == null)
            {
                return;
            }

            var isGroundWalking = state == ClimbingStateId.IdleGround && walkBlend > 0.05f;

            TwoBoneIKSolver.Solve(
                rigMapping.leftUpperArm, rigMapping.leftForearm, rigMapping.leftHand,
                GetHandTarget(HandSide.Left, state, isGroundWalking, walkCycle, groundTravelDirection, walkBlend),
                GetElbowHint(true, state, isGroundWalking, walkCycle, groundTravelDirection, walkBlend));

            TwoBoneIKSolver.Solve(
                rigMapping.rightUpperArm, rigMapping.rightForearm, rigMapping.rightHand,
                GetHandTarget(HandSide.Right, state, isGroundWalking, walkCycle, groundTravelDirection, walkBlend),
                GetElbowHint(false, state, isGroundWalking, walkCycle, groundTravelDirection, walkBlend));

            SolveLeg(rigMapping.leftUpperLeg, rigMapping.leftLowerLeg, rigMapping.leftFoot,
                GetFootTarget(true, state, isGroundWalking, walkCycle, groundTravelDirection, walkBlend),
                GetKneeHint(true, state, isGroundWalking, walkCycle, groundTravelDirection, walkBlend),
                true, state);

            SolveLeg(rigMapping.rightUpperLeg, rigMapping.rightLowerLeg, rigMapping.rightFoot,
                GetFootTarget(false, state, isGroundWalking, walkCycle, groundTravelDirection, walkBlend),
                GetKneeHint(false, state, isGroundWalking, walkCycle, groundTravelDirection, walkBlend),
                false, state);

            ApplyJointLimits();
        }

        public void ApplyFootIKFromAnimator(ClimbingStateId state)
        {
            if (mannequinAnimator == null || state != ClimbingStateId.IdleGround || !useAnimatorDrivenGroundPose)
            {
                return;
            }

            ApplyFootIK(AvatarIKGoal.LeftFoot, true);
            ApplyFootIK(AvatarIKGoal.RightFoot, false);
        }

        private void SolveLeg(Transform upperLeg, Transform lowerLeg, Transform foot, Vector3 target, Vector3 bendHint, bool isLeft, ClimbingStateId state)
        {
            TwoBoneIKSolver.Solve(upperLeg, lowerLeg, foot, target, bendHint);
            if (foot == null)
            {
                return;
            }

            var forward = Vector3.ProjectOnPlane(visualRoot.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (state == ClimbingStateId.IdleGround)
            {
                var groundNormal = isLeft ? _leftFootGroundNormal : _rightFootGroundNormal;
                foot.rotation = Quaternion.LookRotation(forward, groundNormal) * Quaternion.Euler(90f, 0f, 0f);
                return;
            }

            foot.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        }

        private Vector3 GetHandTarget(HandSide side, ClimbingStateId state, bool isGroundWalking, float walkCycle, Vector3 direction, float walkBlend)
        {
            if (isGroundWalking)
            {
                return GetGroundWalkHandTarget(side, walkCycle, direction, walkBlend);
            }

            var hands = driver != null ? driver.PlayerHands : null;
            var anchor = side == HandSide.Left ? hands?.LeftHandAnchor : hands?.RightHandAnchor;
            if (anchor != null && state != ClimbingStateId.Falling)
            {
                return anchor.position;
            }

            var dir = side == HandSide.Left ? -1f : 1f;
            var torso = rigMapping != null && rigMapping.torso != null ? rigMapping.torso : visualRoot;
            return torso.position
                + visualRoot.TransformDirection(new Vector3(0.42f * dir, 0.02f, 0.16f))
                + (state == ClimbingStateId.Falling ? Vector3.down * 0.35f : Vector3.up * 0.08f);
        }

        private Vector3 GetGroundWalkHandTarget(HandSide side, float walkCycle, Vector3 direction, float walkBlend)
        {
            var torso = rigMapping != null && rigMapping.torso != null ? rigMapping.torso : visualRoot;
            if (torso == null)
            {
                return transform.position + Vector3.up;
            }

            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var sideSign = side == HandSide.Left ? -1f : 1f;
            var phase = (walkCycle * Mathf.PI * 2f) + (side == HandSide.Left ? 0f : Mathf.PI);
            var swing = Mathf.Sin(phase) * walkArmSwing * walkBlend;
            var vertical = walkArmHeight + (Mathf.Cos(phase) * 0.015f * walkBlend);

            return torso.position
                + (right * (sideSign * walkArmLateral))
                + (direction * swing)
                + (Vector3.up * vertical);
        }

        private Vector3 GetFootTarget(bool isLeft, ClimbingStateId state, bool isGroundWalking, float walkCycle, Vector3 direction, float walkBlend)
        {
            if (rigMapping == null || rigMapping.pelvis == null)
            {
                return transform.position + Vector3.down;
            }

            if (isGroundWalking)
            {
                return GetGroundWalkFootTarget(isLeft, walkCycle, direction, walkBlend);
            }

            var foothold = FindBestFoothold(isLeft);
            if (state != ClimbingStateId.Falling && foothold != null)
            {
                var gripPoint = foothold.gripPoint != null ? foothold.gripPoint.position : foothold.transform.position;
                return gripPoint + new Vector3(0f, -0.08f, -0.03f);
            }

            var side = isLeft ? -1f : 1f;
            return rigMapping.pelvis.position
                + visualRoot.TransformDirection(new Vector3(0.18f * side, -0.95f, 0.12f))
                + (state == ClimbingStateId.Falling ? Vector3.down * 0.25f : Vector3.zero);
        }

        private Vector3 GetGroundWalkFootTarget(bool isLeft, float walkCycle, Vector3 direction, float walkBlend)
        {
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var sideSign = isLeft ? -1f : 1f;
            var phase = (walkCycle * Mathf.PI * 2f) + (isLeft ? 0f : Mathf.PI);
            var stride = Mathf.Sin(phase) * walkStrideDistance * walkBlend;
            var lift = Mathf.Max(0f, Mathf.Sin(phase)) * walkFootLift * walkBlend;

            var predicted = rigMapping.pelvis.position
                + (right * (sideSign * walkFootStance))
                + (direction * stride)
                + Vector3.up * (-0.96f + lift);

            if (TrySampleGround(predicted, out var groundedPosition, out var normal))
            {
                if (isLeft) { _leftFootGroundNormal = normal; }
                else { _rightFootGroundNormal = normal; }
                return groundedPosition;
            }

            if (isLeft) { _leftFootGroundNormal = Vector3.up; }
            else { _rightFootGroundNormal = Vector3.up; }
            return predicted;
        }

        private Vector3 GetElbowHint(bool isLeft, ClimbingStateId state, bool isGroundWalking, float walkCycle, Vector3 direction, float walkBlend)
        {
            var side = isLeft ? -1f : 1f;
            var torso = rigMapping != null && rigMapping.torso != null ? rigMapping.torso.position : transform.position + Vector3.up;
            if (isGroundWalking)
            {
                var right = Vector3.Cross(Vector3.up, direction).normalized;
                var phase = (walkCycle * Mathf.PI * 2f) + (isLeft ? 0f : Mathf.PI);
                var swing = Mathf.Sin(phase) * 0.12f * walkBlend;
                return torso + (right * (0.26f * side)) + (direction * swing) + Vector3.down * 0.08f;
            }

            var forwardOffset = state == ClimbingStateId.Falling ? -0.1f : 0.18f;
            return torso + visualRoot.TransformDirection(new Vector3(0.28f * side, -0.05f, forwardOffset));
        }

        private Vector3 GetKneeHint(bool isLeft, ClimbingStateId state, bool isGroundWalking, float walkCycle, Vector3 direction, float walkBlend)
        {
            var side = isLeft ? -1f : 1f;
            var pelvis = rigMapping != null && rigMapping.pelvis != null ? rigMapping.pelvis.position : transform.position;
            if (isGroundWalking)
            {
                var right = Vector3.Cross(Vector3.up, direction).normalized;
                var phase = (walkCycle * Mathf.PI * 2f) + (isLeft ? 0f : Mathf.PI);
                var forward = Mathf.Sin(phase) * 0.16f * walkBlend;
                return pelvis + (right * (0.14f * side)) + (direction * forward) + Vector3.down * 0.42f;
            }

            var forwardOffset = state == ClimbingStateId.Falling ? 0.12f : 0.28f;
            return pelvis + visualRoot.TransformDirection(new Vector3(0.14f * side, -0.45f, forwardOffset));
        }

        private HoldComponent FindBestFoothold(bool isLeft)
        {
            if (_holds == null || _holds.Length == 0 || rigMapping == null || rigMapping.pelvis == null)
            {
                return null;
            }

            var pelvisPosition = rigMapping.pelvis.position;
            var side = isLeft ? -1f : 1f;
            HoldComponent bestHold = null;
            var bestScore = float.MaxValue;

            foreach (var hold in _holds)
            {
                if (hold == null)
                {
                    continue;
                }

                var holdPosition = hold.gripPoint != null ? hold.gripPoint.position : hold.transform.position;
                var verticalOffset = pelvisPosition.y - holdPosition.y;
                if (verticalOffset < 0.15f || verticalOffset > 1.45f)
                {
                    continue;
                }

                var lateralOffset = holdPosition.x - pelvisPosition.x;
                if (lateralOffset * side < -0.15f)
                {
                    continue;
                }

                var score = Mathf.Abs(lateralOffset - (0.35f * side))
                    + Mathf.Abs(verticalOffset - 0.95f)
                    + Mathf.Abs(holdPosition.z - pelvisPosition.z) * 0.5f;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestHold = hold;
            }

            return bestHold;
        }

        private bool TrySampleGround(Vector3 around, out Vector3 groundedPosition, out Vector3 normal)
        {
            var origin = around + Vector3.up * footRaycastStartHeight;
            if (Physics.Raycast(origin, Vector3.down, out var hit, footRaycastLength, footGroundMask, QueryTriggerInteraction.Ignore))
            {
                groundedPosition = hit.point + Vector3.up * footGroundOffset;
                normal = hit.normal;
                return true;
            }

            groundedPosition = around;
            normal = Vector3.up;
            return false;
        }

        private void ApplyFootIK(AvatarIKGoal goal, bool isLeft)
        {
            var footBone = goal == AvatarIKGoal.LeftFoot
                ? mannequinAnimator.GetBoneTransform(HumanBodyBones.LeftFoot)
                : mannequinAnimator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (footBone == null)
            {
                return;
            }

            if (!TrySampleGround(footBone.position, out var groundedPosition, out var normal))
            {
                mannequinAnimator.SetIKPositionWeight(goal, 0f);
                mannequinAnimator.SetIKRotationWeight(goal, 0f);
                return;
            }

            mannequinAnimator.SetIKPositionWeight(goal, 1f);
            mannequinAnimator.SetIKPosition(goal, groundedPosition);

            var forward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
            if (forward.sqrMagnitude > 0.0001f)
            {
                mannequinAnimator.SetIKRotationWeight(goal, 0.8f);
                mannequinAnimator.SetIKRotation(goal, Quaternion.LookRotation(forward, normal));
            }
        }

        private void ApplyJointLimits()
        {
            ClampLimbSwing(rigMapping.leftUpperArm, shoulderSwingRange);
            ClampLimbSwing(rigMapping.rightUpperArm, shoulderSwingRange);
            ClampLimbSwing(rigMapping.leftUpperLeg, hipSwingRange);
            ClampLimbSwing(rigMapping.rightUpperLeg, hipSwingRange);

            ClampHingeJoint(rigMapping.leftForearm, elbowPitchRange);
            ClampHingeJoint(rigMapping.rightForearm, elbowPitchRange);
            ClampHingeJoint(rigMapping.leftLowerLeg, kneePitchRange);
            ClampHingeJoint(rigMapping.rightLowerLeg, kneePitchRange);
        }

        private static void ClampLimbSwing(Transform bone, Vector2 range)
        {
            if (bone == null)
            {
                return;
            }

            var euler = NormalizeEuler(bone.localEulerAngles);
            euler.z = Mathf.Clamp(euler.z, range.x, range.y);
            bone.localRotation = Quaternion.Euler(euler);
        }

        private static void ClampHingeJoint(Transform bone, Vector2 range)
        {
            if (bone == null)
            {
                return;
            }

            var euler = NormalizeEuler(bone.localEulerAngles);
            var flexion = Mathf.Clamp(-euler.z, range.x, range.y);
            euler.z = -flexion;
            euler.x = Mathf.Clamp(euler.x, -25f, 25f);
            euler.y = Mathf.Clamp(euler.y, -25f, 25f);
            bone.localRotation = Quaternion.Euler(euler);
        }

        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            euler.x = NormalizeAngle(euler.x);
            euler.y = NormalizeAngle(euler.y);
            euler.z = NormalizeAngle(euler.z);
            return euler;
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f) { angle -= 360f; }
            while (angle < -180f) { angle += 360f; }
            return angle;
        }
    }
}
