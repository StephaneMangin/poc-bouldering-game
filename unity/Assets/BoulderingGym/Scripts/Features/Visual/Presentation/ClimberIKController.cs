using Project.Core.Utilities;
using Project.Features.BodyConstraints.Domain;
using Project.Features.Climbing.Domain;
using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using Project.Features.Climbing.Presentation;
using UnityEngine;

namespace Project.Features.Visual.Presentation
{
    public sealed class ClimberIKController : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachineDriver driver;
        [SerializeField] private HumanoidRigMapping rigMapping;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator mannequinAnimator;
        [SerializeField] private HoldRegistry holdRegistry;

        [Header("Foot IK Grounding")]
        [SerializeField] private LayerMask footGroundMask = ~0;
        [SerializeField] private float footRaycastStartHeight = 0.55f;
        [SerializeField] private float footRaycastLength = 1.65f;
        [SerializeField] private float footGroundOffset = 0.09f;

        [Header("Animator-driven Ground")]
        [SerializeField] private bool useAnimatorDrivenGroundPose = true;

        [Header("Extremity Offsets (bone → fingertip / toe tip)")]
        [SerializeField] private float fingertipLength = 0.10f;
        [SerializeField] private float toeTipLength = 0.12f;

        private HoldComponent[] _holds = System.Array.Empty<HoldComponent>();

        private HoldComponent _resolvedLeftFootHold;
        private HoldComponent _resolvedRightFootHold;

        // Distance ranges precomputed from constraints + actual bone lengths
        private float _elbowMinDist;
        private float _elbowMaxDist;
        private float _kneeMinDist;
        private float _kneeMaxDist;

        // Foot rest-pose data measured from the rig (for orientation without magic numbers)
        private Quaternion _leftFootRestRotation;
        private Quaternion _rightFootRestRotation;
        private Vector3 _leftFootRestForward;  // foot→ToeBase direction in rest pose
        private Vector3 _rightFootRestForward;

        /// <summary>Anatomy measured from the actual rig at initialization.</summary>
        public ClimbingBodySolver.AnatomyParams MeasuredAnatomy { get; private set; }

        public bool UseAnimatorDrivenGroundPose => useAnimatorDrivenGroundPose;

        public void Initialize(
            PlayerStateMachineDriver driverRef,
            HumanoidRigMapping rig,
            Transform visual,
            Animator animator,
            HoldRegistry registry)
        {
            driver = driverRef;
            rigMapping = rig;
            visualRoot = visual;
            mannequinAnimator = animator;
            holdRegistry = registry;
            RefreshHolds();
            MeasureAnatomyFromRig();
        }

        private void MeasureAnatomyFromRig()
        {
            if (rigMapping == null || rigMapping.pelvis == null)
            {
                MeasuredAnatomy = ClimbingBodySolver.AnatomyParams.Default;
                return;
            }

            MeasuredAnatomy = ClimbingBodySolver.AnatomyParams.FromMeasurements(
                rigMapping.pelvis.position,
                rigMapping.leftUpperArm.position, rigMapping.leftForearm.position, rigMapping.leftHand.position,
                rigMapping.rightUpperArm.position, rigMapping.rightForearm.position, rigMapping.rightHand.position,
                rigMapping.leftUpperLeg.position, rigMapping.leftLowerLeg.position, rigMapping.leftFoot.position,
                rigMapping.rightUpperLeg.position, rigMapping.rightLowerLeg.position, rigMapping.rightFoot.position);

            // Precompute constraint distance ranges from actual bone lengths
            var upperArm = Vector3.Distance(rigMapping.leftUpperArm.position, rigMapping.leftForearm.position);
            var forearm = Vector3.Distance(rigMapping.leftForearm.position, rigMapping.leftHand.position);
            (_elbowMinDist, _elbowMaxDist) = ElbowConstraint.ComputeDistanceRange(upperArm, forearm);

            var upperLeg = Vector3.Distance(rigMapping.leftUpperLeg.position, rigMapping.leftLowerLeg.position);
            var lowerLeg = Vector3.Distance(rigMapping.leftLowerLeg.position, rigMapping.leftFoot.position);
            (_kneeMinDist, _kneeMaxDist) = KneeConstraint.ComputeDistanceRange(upperLeg, lowerLeg);

            // Measure foot rest orientation: the direction foot→ToeBase is the anatomical "forward" of the foot
            _leftFootRestRotation = rigMapping.leftFoot.rotation;
            _rightFootRestRotation = rigMapping.rightFoot.rotation;
            _leftFootRestForward = FindToeForward(rigMapping.leftFoot);
            _rightFootRestForward = FindToeForward(rigMapping.rightFoot);
        }

        /// <summary>
        /// Finds the anatomical forward direction of a foot bone by looking at its first child (ToeBase).
        /// Falls back to the bone's own forward if no child exists.
        /// </summary>
        private static Vector3 FindToeForward(Transform foot)
        {
            if (foot.childCount > 0)
            {
                var toeBase = foot.GetChild(0);
                var dir = (toeBase.position - foot.position).normalized;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    return dir;
                }
            }

            return foot.forward;
        }

        public void RefreshHolds()
        {
            _holds = holdRegistry != null
                ? holdRegistry.GetAllHolds()
                : FindObjectsByType<HoldComponent>(FindObjectsSortMode.None);
        }

        public void ApplyLimbPose(PlayerStateId state)
        {
            if (rigMapping == null)
            {
                return;
            }

            // Arms: IK with elbow bend constraints (min/max distance from shoulder → elbow angle stays valid)
            var leftHandTarget = GetHandTarget(HandSide.Left, state);
            TwoBoneIKSolver.Solve(
                rigMapping.leftUpperArm, rigMapping.leftForearm, rigMapping.leftHand,
                leftHandTarget, GetElbowHint(true, state, leftHandTarget),
                _elbowMinDist, _elbowMaxDist);

            var rightHandTarget = GetHandTarget(HandSide.Right, state);
            TwoBoneIKSolver.Solve(
                rigMapping.rightUpperArm, rigMapping.rightForearm, rigMapping.rightHand,
                rightHandTarget, GetElbowHint(false, state, rightHandTarget),
                _elbowMinDist, _elbowMaxDist);

            // Legs: IK with knee bend constraints
            var leftFootTarget = GetFootTarget(true, state);
            SolveLeg(rigMapping.leftUpperLeg, rigMapping.leftLowerLeg, rigMapping.leftFoot,
                leftFootTarget, GetKneeHint(true, state), GetHoldGripPos(_resolvedLeftFootHold), true);

            var rightFootTarget = GetFootTarget(false, state);
            SolveLeg(rigMapping.rightUpperLeg, rigMapping.rightLowerLeg, rigMapping.rightFoot,
                rightFootTarget, GetKneeHint(false, state), GetHoldGripPos(_resolvedRightFootHold), false);
        }

        public void ApplyFootIKFromAnimator(PlayerStateId state)
        {
            if (mannequinAnimator == null || state != PlayerStateId.IdleGround || !useAnimatorDrivenGroundPose)
            {
                return;
            }

            ApplyFootIK(AvatarIKGoal.LeftFoot);
            ApplyFootIK(AvatarIKGoal.RightFoot);
        }

        private void SolveLeg(Transform upperLeg, Transform lowerLeg, Transform foot,
            Vector3 target, Vector3 bendHint, Vector3? gripPosition = null, bool isLeft = true)
        {
            if (foot == null || lowerLeg == null)
            {
                return;
            }

            // Two-pass approach when placing on a hold:
            // Pass 1: solve IK to grip position to get approximate ankle position
            // → compute constrained foot forward → derive toe-tip offset
            // Pass 2: re-solve IK with corrected target so toe tip lands on grip
            var ikTarget = target;
            if (gripPosition.HasValue && toeTipLength > 0f)
            {
                // Pass 1: approximate solve
                TwoBoneIKSolver.Solve(upperLeg, lowerLeg, foot, gripPosition.Value, bendHint,
                    _kneeMinDist, _kneeMaxDist);

                var shinDir1 = (foot.position - lowerLeg.position).normalized;
                var toGrip1 = gripPosition.Value - foot.position;
                var estForward = toGrip1.sqrMagnitude > 0.0001f
                    ? toGrip1.normalized
                    : visualRoot.forward;
                estForward = AnkleConstraint.ClampFootForward(shinDir1, estForward);

                // Correct target: offset so toe tip (not ankle bone) lands on grip
                ikTarget = gripPosition.Value - estForward * toeTipLength;
            }

            // Final IK solve
            TwoBoneIKSolver.Solve(upperLeg, lowerLeg, foot, ikTarget, bendHint,
                _kneeMinDist, _kneeMaxDist);

            // Shin direction: knee → ankle (post-IK)
            var shinDir = (foot.position - lowerLeg.position).normalized;

            Vector3 desiredForward;
            Vector3 desiredUp;
            if (gripPosition.HasValue)
            {
                // Climbing: toes point toward grip, sole faces the wall
                var toGrip = gripPosition.Value - foot.position;
                desiredForward = toGrip.sqrMagnitude > 0.0001f
                    ? toGrip.normalized
                    : visualRoot.forward;

                // Clamp foot forward within ankle ROM (dorsiflexion/plantarflexion)
                desiredForward = AnkleConstraint.ClampFootForward(shinDir, desiredForward);

                // Sole against the wall: "up" of the foot = wall normal (away from wall = -Z in our convention)
                desiredUp = -visualRoot.forward;
                desiredUp = Vector3.ProjectOnPlane(desiredUp, desiredForward).normalized;
                if (desiredUp.sqrMagnitude < 0.0001f)
                {
                    desiredUp = Vector3.up;
                }
            }
            else
            {
                desiredForward = Vector3.ProjectOnPlane(visualRoot.forward, Vector3.up).normalized;
                desiredUp = Vector3.up;
                if (desiredForward.sqrMagnitude < 0.0001f)
                {
                    return;
                }

                // Ground standing: also clamp to ankle ROM
                desiredForward = AnkleConstraint.ClampFootForward(shinDir, desiredForward);
            }

            // Build desired world rotation from the desired forward/up axes,
            // then compensate for the bone's rest-pose orientation so the mesh aligns correctly.
            var restRot = isLeft ? _leftFootRestRotation : _rightFootRestRotation;
            var restFwd = isLeft ? _leftFootRestForward : _rightFootRestForward;

            // The rest rotation maps restFwd to world — we want desiredForward instead
            var restToDesired = Quaternion.FromToRotation(restFwd, desiredForward);
            var candidateRotation = restToDesired * restRot;

            // Now twist the foot so its "up" (sole normal) aligns with desiredUp
            var restUp = restRot * Vector3.up;
            var candidateUp = restToDesired * restUp;
            var upCorrection = Quaternion.FromToRotation(candidateUp, desiredUp);

            foot.rotation = upCorrection * candidateRotation;
        }

        private Vector3 GetHandTarget(HandSide side, PlayerStateId state)
        {
            var hands = driver != null ? driver.PlayerHands : null;
            if (hands != null && state != PlayerStateId.Falling)
            {
                var hold = side == HandSide.Left ? hands.LeftCurrentHold : hands.RightCurrentHold;
                if (hold != null)
                {
                    var gripPos = hold.gripPoint != null ? hold.gripPoint.position : hold.transform.position;
                    var shoulder = side == HandSide.Left ? rigMapping.leftUpperArm : rigMapping.rightUpperArm;
                    return ApplyExtremityOffset(shoulder, gripPos, fingertipLength);
                }
            }

            var dir = side == HandSide.Left ? -1f : 1f;
            var torso = rigMapping != null && rigMapping.torso != null ? rigMapping.torso : visualRoot;
            return torso.position
                + visualRoot.TransformDirection(new Vector3(0.42f * dir, 0.02f, 0.16f))
                + (state == PlayerStateId.Falling ? Vector3.down * 0.35f : Vector3.up * 0.08f);
        }

        private Vector3 GetFootTarget(bool isLeft, PlayerStateId state)
        {
            if (rigMapping == null || rigMapping.pelvis == null)
            {
                return transform.position + Vector3.down;
            }

            // Reset per-frame foot hold tracking
            if (isLeft) _resolvedLeftFootHold = null;
            else _resolvedRightFootHold = null;

            if (state != PlayerStateId.Falling)
            {
                var hands = driver != null ? driver.PlayerHands : null;
                var explicitHold = isLeft ? hands?.LeftFootHold : hands?.RightFootHold;
                if (explicitHold != null)
                {
                    if (isLeft) _resolvedLeftFootHold = explicitHold;
                    else _resolvedRightFootHold = explicitHold;

                    // Return raw grip position — SolveLeg handles toe-tip offset post-IK
                    return explicitHold.gripPoint != null
                        ? explicitHold.gripPoint.position
                        : explicitHold.transform.position;
                }

                var autoHold = FindBestFoothold(isLeft);
                if (autoHold != null)
                {
                    if (isLeft) _resolvedLeftFootHold = autoHold;
                    else _resolvedRightFootHold = autoHold;

                    // Return raw grip position — SolveLeg handles toe-tip offset post-IK
                    return autoHold.gripPoint != null
                        ? autoHold.gripPoint.position
                        : autoHold.transform.position;
                }
            }

            var side = isLeft ? -1f : 1f;
            return rigMapping.pelvis.position
                + visualRoot.TransformDirection(new Vector3(0.18f * side, -0.65f, 0.12f))
                + (state == PlayerStateId.Falling ? Vector3.down * 0.25f : Vector3.zero);
        }

        private Vector3 GetElbowHint(bool isLeft, PlayerStateId state, Vector3 handTarget)
        {
            var side = isLeft ? -1f : 1f;
            var shoulder = isLeft ? rigMapping.leftUpperArm : rigMapping.rightUpperArm;
            var shoulderPos = shoulder != null ? shoulder.position : transform.position + Vector3.up;

            if (state == PlayerStateId.Falling)
            {
                return shoulderPos + visualRoot.TransformDirection(new Vector3(0.28f * side, -0.15f, -0.10f));
            }

            // Dynamic hint: midpoint between shoulder and target,
            // offset laterally (outward) and slightly behind the arm plane.
            // This ensures the elbow bends AWAY from the body, not folded against it.
            var midpoint = (shoulderPos + handTarget) * 0.5f;
            var lateral = visualRoot.TransformDirection(new Vector3(side, 0f, 0f));
            var behind = visualRoot.TransformDirection(new Vector3(0f, 0f, -1f));
            return midpoint + lateral * 0.35f + behind * 0.10f + Vector3.down * 0.05f;
        }

        private Vector3 GetKneeHint(bool isLeft, PlayerStateId state)
        {
            var side = isLeft ? -1f : 1f;
            var pelvis = rigMapping != null && rigMapping.pelvis != null ? rigMapping.pelvis.position : transform.position;
            var forwardOffset = state == PlayerStateId.Falling ? 0.12f : 0.28f;
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
            if (UnityEngine.Physics.Raycast(origin, Vector3.down, out var hit, footRaycastLength, footGroundMask, QueryTriggerInteraction.Ignore))
            {
                groundedPosition = hit.point + Vector3.up * footGroundOffset;
                normal = hit.normal;
                return true;
            }

            groundedPosition = around;
            normal = Vector3.up;
            return false;
        }

        private void ApplyFootIK(AvatarIKGoal goal)
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

        /// <summary>
        /// Offsets the IK target so the extremity (fingertip / toe tip) lands on gripPos
        /// instead of the bone pivot. Uses chainRoot (shoulder/hip) for direction because
        /// the end-effector bone hasn't been IK-solved yet — its pre-IK position gives
        /// a wrong offset direction. The chain root doesn't move during IK,
        /// and chainRoot→grip is a good approximation of the post-IK forearm→hand direction.
        /// </summary>
        private static Vector3 ApplyExtremityOffset(Transform chainRoot, Vector3 gripPos, float extremityLength)
        {
            if (chainRoot == null || extremityLength <= 0f)
            {
                return gripPos;
            }

            var rootToGrip = gripPos - chainRoot.position;
            if (rootToGrip.sqrMagnitude < 0.0001f)
            {
                return gripPos;
            }

            return gripPos - rootToGrip.normalized * extremityLength;
        }

        private static Vector3? GetHoldGripPos(HoldComponent hold)
        {
            if (hold == null)
            {
                return null;
            }

            return hold.gripPoint != null ? hold.gripPoint.position : hold.transform.position;
        }
    }
}
