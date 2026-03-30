using NUnit.Framework;
using Project.Core.Utilities;
using Project.Features.BodyConstraints.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Core
{
    /// <summary>
    /// Integration tests for TwoBoneIKSolver verifying:
    /// 1. End-effector accuracy — hand bone lands at target position
    /// 2. Elbow direction — bends outward, not folded against body
    /// 3. Full extremity pipeline — fingertip/toe lands on grip
    /// </summary>
    [TestFixture]
    public sealed class IKPipelineTests
    {
        private const float UpperArmLength = 0.28f;
        private const float ForearmLength = 0.24f;
        private const float ChainLength = UpperArmLength + ForearmLength;
        private const float FingertipLength = 0.10f;

        private const float UpperLegLength = 0.42f;
        private const float LowerLegLength = 0.40f;

        private GameObject _root;
        private Transform _shoulder;
        private Transform _forearm;
        private Transform _hand;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("IK_Root");

            var shoulderGO = new GameObject("Shoulder");
            var forearmGO = new GameObject("Forearm");
            var handGO = new GameObject("Hand");

            shoulderGO.transform.SetParent(_root.transform);
            forearmGO.transform.SetParent(shoulderGO.transform);
            handGO.transform.SetParent(forearmGO.transform);

            _shoulder = shoulderGO.transform;
            _forearm = forearmGO.transform;
            _hand = handGO.transform;

            // Rest pose: arm hanging down from shoulder, body close to wall (climbing stance)
            _shoulder.position = new Vector3(0.18f, 2.08f, -0.15f);
            _forearm.position = _shoulder.position + Vector3.down * UpperArmLength;
            _hand.position = _forearm.position + Vector3.down * ForearmLength;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  End-effector accuracy: hand must land at the target
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void Solve_ReachableTarget_HandLandsAtTarget()
        {
            var target = new Vector3(0.20f, 2.50f, -0.05f);
            var hint = _shoulder.position + new Vector3(0.35f, -0.10f, -0.15f);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, target, hint);

            var error = Vector3.Distance(_hand.position, target);
            Assert.Less(error, 0.01f,
                $"Hand must be within 1cm of target. Actual error: {error:F4}m " +
                $"(hand={_hand.position}, target={target})");
        }

        [Test]
        public void Solve_TargetDirectlyAbove_HandReachesTarget()
        {
            // Target straight above shoulder — worst case for bend direction
            var target = _shoulder.position + Vector3.up * (ChainLength * 0.85f);
            var hint = _shoulder.position + new Vector3(0.30f, 0.10f, -0.15f);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, target, hint);

            var error = Vector3.Distance(_hand.position, target);
            Assert.Less(error, 0.01f,
                $"Hand must reach target above shoulder. Error: {error:F4}m");
        }

        [Test]
        public void Solve_TargetAtMaxReach_HandNearTarget()
        {
            // Target at ~95% reach — nearly full extension
            var dir = new Vector3(0.1f, 0.8f, 0.5f).normalized;
            var target = _shoulder.position + dir * (ChainLength * 0.95f);
            var hint = _shoulder.position + new Vector3(0.30f, -0.05f, -0.20f);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, target, hint);

            var error = Vector3.Distance(_hand.position, target);
            Assert.Less(error, 0.02f,
                $"Hand must be within 2cm at near-max reach. Error: {error:F4}m");
        }

        [TestCase(0.20f, 2.50f, 0.00f, TestName = "Hold up-right on wall")]
        [TestCase(-0.10f, 2.40f, 0.00f, TestName = "Hold up-left on wall")]
        [TestCase(0.30f, 2.08f, -0.10f, TestName = "Hold at shoulder height")]
        [TestCase(0.10f, 2.30f, -0.15f, TestName = "Hold slightly off wall")]
        public void Solve_ClimbingTargets_HandWithin1cm(float tx, float ty, float tz)
        {
            var target = new Vector3(tx, ty, tz);
            var hint = _shoulder.position + new Vector3(0.35f, -0.10f, -0.15f);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, target, hint);

            var error = Vector3.Distance(_hand.position, target);
            Assert.Less(error, 0.01f,
                $"Hand at {_hand.position} must be within 1cm of hold at {target}. Error: {error:F4}m");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Elbow direction: must bend outward, not folded against body
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void Solve_RightArmReachingUp_ElbowIsOutward()
        {
            // Right shoulder at X=+0.18, target above — elbow must be MORE to the right
            var target = new Vector3(0.20f, 2.50f, -0.05f);
            var hint = _shoulder.position + new Vector3(0.35f, -0.10f, -0.15f);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, target, hint);

            // Elbow (forearm root) should be OUTSIDE the shoulder-target line
            var shoulderToTarget = (target - _shoulder.position).normalized;
            var shoulderToElbow = _forearm.position - _shoulder.position;
            var elbowLateral = shoulderToElbow - Vector3.Dot(shoulderToElbow, shoulderToTarget) * shoulderToTarget;

            // Lateral component should point outward (positive X for right arm)
            Assert.Greater(elbowLateral.x, 0.01f,
                $"Right elbow must be OUTWARD (positive X lateral). " +
                $"Lateral component: {elbowLateral}");
        }

        [Test]
        public void Solve_LeftArm_ElbowIsOutward()
        {
            // Set up left arm: shoulder on the left side
            _shoulder.position = new Vector3(-0.18f, 2.08f, -0.15f);
            _forearm.position = _shoulder.position + Vector3.down * UpperArmLength;
            _hand.position = _forearm.position + Vector3.down * ForearmLength;

            var target = new Vector3(-0.20f, 2.50f, -0.05f);
            var hint = _shoulder.position + new Vector3(-0.35f, -0.10f, -0.15f);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, target, hint);

            var shoulderToTarget = (target - _shoulder.position).normalized;
            var shoulderToElbow = _forearm.position - _shoulder.position;
            var elbowLateral = shoulderToElbow - Vector3.Dot(shoulderToElbow, shoulderToTarget) * shoulderToTarget;

            // Left arm: lateral should be negative X (outward left)
            Assert.Less(elbowLateral.x, -0.01f,
                $"Left elbow must be OUTWARD (negative X lateral). " +
                $"Lateral component: {elbowLateral}");
        }

        [Test]
        public void Solve_ArmNotFoldedAgainstBody_ElbowAwayFromMidline()
        {
            // Right arm reaching a hold on the wall — elbow must NOT be between shoulder and X=0
            var target = new Vector3(0.20f, 2.50f, 0.00f);
            var hint = _shoulder.position + new Vector3(0.35f, -0.10f, -0.15f);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, target, hint);

            // Body midline at X ≈ 0. Shoulder at X=0.18.
            // A folded arm would have elbow X < shoulder X (closer to body center).
            // A correct arm would have elbow X >= shoulder X (outward).
            Assert.GreaterOrEqual(_forearm.position.x, _shoulder.position.x * 0.5f,
                $"Elbow X ({_forearm.position.x:F3}) should NOT be folded toward body center. " +
                $"Shoulder X: {_shoulder.position.x:F3}");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Full extremity pipeline: with offset, fingertip lands on grip
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void FullPipeline_ExtremityOffset_FingertipLandsOnGrip()
        {
            var gripPosition = new Vector3(0.20f, 2.50f, 0.00f);

            // Compute IK target with extremity offset (same as ClimberIKController does)
            var ikTarget = gripPosition - (_hand.position - gripPosition).normalized * FingertipLength;
            // Alternatively use solver method if hand pre-IK is far away, fallback:
            var boneToGrip = gripPosition - _hand.position;
            ikTarget = gripPosition - boneToGrip.normalized * FingertipLength;

            var hint = _shoulder.position + new Vector3(0.35f, -0.10f, -0.15f);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, ikTarget, hint);

            // After IK: hand bone is at ikTarget.
            // Fingertip = hand + direction(hand→grip) * fingertipLength
            var handToGrip = gripPosition - _hand.position;
            var fingertip = _hand.position + handToGrip.normalized * FingertipLength;

            var tipError = Vector3.Distance(fingertip, gripPosition);
            Assert.Less(tipError, 0.02f,
                $"Fingertip must land on grip. Error: {tipError:F4}m " +
                $"(fingertip={fingertip}, grip={gripPosition})");
        }

        [Test]
        public void FullPipeline_HandBoneIsNotAtGrip_ButOffsetBehind()
        {
            var gripPosition = new Vector3(0.20f, 2.50f, 0.00f);

            var boneToGrip = gripPosition - _hand.position;
            var ikTarget = gripPosition - boneToGrip.normalized * FingertipLength;
            var hint = _shoulder.position + new Vector3(0.35f, -0.10f, -0.15f);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, ikTarget, hint);

            // Hand bone should be FingertipLength AWAY from grip
            var handToGripDist = Vector3.Distance(_hand.position, gripPosition);
            Assert.AreEqual(FingertipLength, handToGripDist, 0.02f,
                $"Hand bone should be {FingertipLength}m from grip, was {handToGripDist:F4}m");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Leg IK: same accuracy for legs
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void Solve_Leg_FootLandsAtTarget()
        {
            // Create leg chain
            var legRoot = new GameObject("LegRoot");
            var hip = new GameObject("Hip");
            var knee = new GameObject("Knee");
            var foot = new GameObject("Foot");

            hip.transform.SetParent(legRoot.transform);
            knee.transform.SetParent(hip.transform);
            foot.transform.SetParent(knee.transform);

            // Hip at pelvis, leg hanging down
            hip.transform.position = new Vector3(0.12f, 1.60f, -0.35f);
            knee.transform.position = hip.transform.position + Vector3.down * UpperLegLength;
            foot.transform.position = knee.transform.position + Vector3.down * LowerLegLength;

            var target = new Vector3(0.10f, 1.00f, 0.00f); // Foothold on wall
            var hint = hip.transform.position + new Vector3(0.14f, -0.30f, 0.25f);

            TwoBoneIKSolver.Solve(hip.transform, knee.transform, foot.transform, target, hint);

            var error = Vector3.Distance(foot.transform.position, target);
            Assert.Less(error, 0.01f,
                $"Foot must land within 1cm of foothold. Error: {error:F4}m");

            Object.DestroyImmediate(legRoot);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Bend constraints: anatomical limits enforced during IK solve
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void BendConstraint_ComputeDistanceRange_ElbowReturnsValidRange()
        {
            var (minDist, maxDist) = ElbowConstraint.ComputeDistanceRange(UpperArmLength, ForearmLength);

            Assert.Greater(minDist, 0f, "Min distance must be positive");
            Assert.Less(minDist, maxDist, "Min distance must be less than max distance");
            Assert.Less(maxDist, ChainLength, "Max distance must be less than full chain length");
            Assert.Greater(minDist, Mathf.Abs(UpperArmLength - ForearmLength),
                "Min distance must exceed geometric minimum (fully folded)");
        }

        [Test]
        public void Solve_WithElbowConstraint_HandStillReachesTarget()
        {
            var target = new Vector3(0.20f, 2.50f, -0.05f);
            var hint = _shoulder.position + new Vector3(0.35f, -0.10f, -0.15f);
            var (minDist, maxDist) = ElbowConstraint.ComputeDistanceRange(UpperArmLength, ForearmLength);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, target, hint, minDist, maxDist);

            var error = Vector3.Distance(_hand.position, target);
            // Within 3cm: constraint may adjust distance slightly
            Assert.Less(error, 0.03f,
                $"Hand must still reach near target with constraints. Error: {error:F4}m");
        }

        [Test]
        public void Solve_TargetTooFar_ConstraintPreventsHyperExtension()
        {
            // Target at near-max extension: without constraint the arm would be nearly straight
            var dir = new Vector3(0.1f, 0.9f, 0.2f).normalized;
            var target = _shoulder.position + dir * (ChainLength * 0.99f);
            var hint = _shoulder.position + new Vector3(0.35f, -0.10f, -0.15f);
            var (minDist, maxDist) = ElbowConstraint.ComputeDistanceRange(UpperArmLength, ForearmLength);

            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, target, hint, minDist, maxDist);

            // With min angle = 5°, arm cannot be fully straight.
            // The hand should NOT reach the full target distance.
            var actualDist = Vector3.Distance(_shoulder.position, _hand.position);
            Assert.LessOrEqual(actualDist, maxDist + 0.01f,
                $"Arm extension {actualDist:F4}m must respect max constraint distance {maxDist:F4}m");
        }

        [Test]
        public void Solve_WithKneeConstraint_LegBendsWithinRange()
        {
            var legRoot = new GameObject("ConstrainedLeg");
            var hip = new GameObject("Hip");
            var knee = new GameObject("Knee");
            var foot = new GameObject("Foot");
            hip.transform.SetParent(legRoot.transform);
            knee.transform.SetParent(hip.transform);
            foot.transform.SetParent(knee.transform);

            hip.transform.position = new Vector3(0.12f, 1.60f, -0.35f);
            knee.transform.position = hip.transform.position + Vector3.down * UpperLegLength;
            foot.transform.position = knee.transform.position + Vector3.down * LowerLegLength;

            var target = new Vector3(0.10f, 1.00f, 0.00f);
            var hint = hip.transform.position + new Vector3(0.14f, -0.30f, 0.25f);
            var (minDist, maxDist) = KneeConstraint.ComputeDistanceRange(UpperLegLength, LowerLegLength);

            TwoBoneIKSolver.Solve(hip.transform, knee.transform, foot.transform, target, hint, minDist, maxDist);

            var actualDist = Vector3.Distance(hip.transform.position, foot.transform.position);
            Assert.GreaterOrEqual(actualDist, minDist - 0.01f,
                $"Leg distance {actualDist:F4}m must respect min constraint {minDist:F4}m");
            Assert.LessOrEqual(actualDist, maxDist + 0.01f,
                $"Leg distance {actualDist:F4}m must respect max constraint {maxDist:F4}m");

            Object.DestroyImmediate(legRoot);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  ApplyExtremityOffset: chainRoot-based direction for stable offset
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void ExtremityOffset_FromChainRoot_FingertipLandsOnGrip()
        {
            // Grip on the wall above and in front of the shoulder
            var gripPosition = new Vector3(0.20f, 2.50f, 0.00f);

            // Compute offset using shoulder (chain root) — NOT the pre-IK hand
            var rootToGrip = gripPosition - _shoulder.position;
            var ikTarget = gripPosition - rootToGrip.normalized * FingertipLength;

            var hint = _shoulder.position + new Vector3(0.35f, -0.10f, -0.15f);
            TwoBoneIKSolver.Solve(_shoulder, _forearm, _hand, ikTarget, hint);

            // After IK, hand bone is at ikTarget. The direction hand→grip is
            // approximately the same as shoulder→grip, so fingertip should land on grip.
            var handToGrip = gripPosition - _hand.position;
            var fingertip = _hand.position + handToGrip.normalized * FingertipLength;

            var tipError = Vector3.Distance(fingertip, gripPosition);
            Assert.Less(tipError, 0.02f,
                $"Fingertip must land on grip within 2cm. Error: {tipError:F4}m " +
                $"(fingertip={fingertip}, grip={gripPosition})");
        }

        [Test]
        public void ExtremityOffset_FromHip_ToeTipLandsOnFoothold()
        {
            // Leg setup
            var legRoot = new GameObject("LegOffset");
            var hip = new GameObject("Hip");
            var knee = new GameObject("Knee");
            var foot = new GameObject("Foot");
            hip.transform.SetParent(legRoot.transform);
            knee.transform.SetParent(hip.transform);
            foot.transform.SetParent(knee.transform);

            hip.transform.position = new Vector3(0.12f, 1.60f, -0.35f);
            knee.transform.position = hip.transform.position + Vector3.down * UpperLegLength;
            foot.transform.position = knee.transform.position + Vector3.down * LowerLegLength;

            var gripPosition = new Vector3(0.10f, 1.00f, 0.00f);
            const float toeTipLen = 0.12f;

            // Offset from hip (chain root), not from pre-IK foot
            var rootToGrip = gripPosition - hip.transform.position;
            var ikTarget = gripPosition - rootToGrip.normalized * toeTipLen;

            var hint = hip.transform.position + new Vector3(0.14f, -0.30f, 0.25f);
            TwoBoneIKSolver.Solve(hip.transform, knee.transform, foot.transform, ikTarget, hint);

            var footToGrip = gripPosition - foot.transform.position;
            var toeTip = foot.transform.position + footToGrip.normalized * toeTipLen;

            var tipError = Vector3.Distance(toeTip, gripPosition);
            Assert.Less(tipError, 0.02f,
                $"Toe tip must land on foothold within 2cm. Error: {tipError:F4}m");

            Object.DestroyImmediate(legRoot);
        }

        [Test]
        public void ExtremityOffset_ChainRootDirection_DiffersFromPreIKBoneDirection()
        {
            // Demonstrate that using the pre-IK hand position gives a WRONG offset
            // while chainRoot (shoulder) gives a CORRECT one
            var gripPosition = new Vector3(0.20f, 2.50f, 0.00f);

            // Pre-IK: hand is at rest pose (hanging down at ~1.56m)
            // Direction hand→grip is mostly UP (~75° from horizontal)
            var preIKDir = (gripPosition - _hand.position).normalized;

            // Chain root: shoulder is at (0.18, 2.08, -0.35)
            // Direction shoulder→grip is more forward/up (~45° from horizontal)
            var chainRootDir = (gripPosition - _shoulder.position).normalized;

            // These directions should be significantly different — proving the pre-IK direction is wrong
            var angleBetween = Vector3.Angle(preIKDir, chainRootDir);
            Assert.Greater(angleBetween, 5f,
                $"Pre-IK hand direction and chainRoot direction should differ significantly. " +
                $"Angle: {angleBetween:F1}°, preIK={preIKDir}, root={chainRootDir}");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  AnkleConstraint: foot forward stays within ankle ROM
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void AnkleConstraint_NeutralAngle_NoClamp()
        {
            // Shin pointing down, foot forward pointing horizontally = 90° = neutral
            var shin = Vector3.down;
            var footForward = Vector3.forward;

            var clamped = AnkleConstraint.ClampFootForward(shin, footForward);

            // Should be unchanged — 90° is within [70°, 140°]
            Assert.Less(Vector3.Angle(clamped, footForward), 0.5f,
                "Neutral ankle (90°) should not be clamped");
        }

        [Test]
        public void AnkleConstraint_MildPlantarflexion_NoClamp()
        {
            // Shin down, foot pointing forward+up = ~120° shin-foot = 30° plantarflexion
            var shin = Vector3.down;
            var footForward = new Vector3(0f, 0.5f, 0.866f).normalized;

            var clamped = AnkleConstraint.ClampFootForward(shin, footForward);
            var angle = Vector3.Angle(shin, clamped);

            // 120° is within [70°, 140°]
            Assert.Less(Vector3.Angle(clamped, footForward), 1f,
                $"30° plantarflexion should not be clamped. Angle: {angle:F1}°");
        }

        [Test]
        public void AnkleConstraint_ExcessiveDorsiflexion_Clamped()
        {
            // Shin down, foot forward pointing downward toward shin = ~40° = too much dorsiflexion
            // angle 40° < 70° min → must be clamped
            var shin = Vector3.down;
            var footForward = new Vector3(0f, -0.766f, 0.643f).normalized;

            var clamped = AnkleConstraint.ClampFootForward(shin, footForward);
            var angle = Vector3.Angle(shin, clamped);

            // Min angle is 70° (90° - 20° dorsiflexion)
            Assert.GreaterOrEqual(angle, 70f - 0.5f,
                $"Dorsiflexion clamped: shin-foot angle must be >= 70°. Got: {angle:F1}°");
        }

        [Test]
        public void AnkleConstraint_ExcessivePlantarflexion_Clamped()
        {
            // Shin down, foot forward pointing steeply upward (opposite shin) = ~160° shin-foot angle
            var shin = Vector3.down;
            var footForward = new Vector3(0f, 0.94f, 0.34f).normalized; // steep upward = extreme plantar

            var clamped = AnkleConstraint.ClampFootForward(shin, footForward);
            var angle = Vector3.Angle(shin, clamped);

            // Max angle is 140° (90° + 50° plantarflexion)
            Assert.LessOrEqual(angle, 140f + 0.5f,
                $"Plantarflexion clamped: shin-foot angle must be <= 140°. Got: {angle:F1}°");
        }

        [Test]
        public void AnkleConstraint_FootPointingBackward_ClampedToMaxPlantar()
        {
            // Extreme case: foot pointing opposite to shin (180°) — physically impossible
            var shin = Vector3.down;
            var footForward = Vector3.down; // same direction as shin = 0° angle → toes toward shin

            var clamped = AnkleConstraint.ClampFootForward(shin, footForward);
            var angle = Vector3.Angle(shin, clamped);

            // Should be clamped to minimum (70°)
            Assert.GreaterOrEqual(angle, 70f - 1f,
                $"Foot parallel to shin must be clamped. Angle: {angle:F1}°");
        }

        [Test]
        public void AnkleConstraint_ReversedFoot_ClampPreventsFlip()
        {
            // Case that causes the "flipped foot" bug: grip is ABOVE the ankle,
            // so foot forward would point upward along the shin → nearly 0° angle
            var shin = Vector3.down;
            var footForward = Vector3.up; // foot tries to point up = sole flipped

            var clamped = AnkleConstraint.ClampFootForward(shin, footForward);
            var angle = Vector3.Angle(shin, clamped);

            // Must be clamped — foot cannot point same direction as negative shin
            // The angle between shin (down) and foot forward (up) is 180°.
            // After clamp it should be within [70°, 140°]
            Assert.GreaterOrEqual(angle, 70f - 1f, $"Reversed foot must be clamped away from shin direction. Angle: {angle:F1}°");
            Assert.LessOrEqual(angle, 140f + 1f, $"Reversed foot must be clamped to max plantar. Angle: {angle:F1}°");
        }

        [TestCase(0f, -1f, 0f, TestName = "Shin straight down")]
        [TestCase(-0.34f, -0.94f, 0f, TestName = "Shin angled 20° forward")]
        [TestCase(0f, -0.87f, 0.5f, TestName = "Shin angled 30° toward wall")]
        public void AnkleConstraint_VariousShinAngles_OutputWithinROM(float sx, float sy, float sz)
        {
            var shin = new Vector3(sx, sy, sz).normalized;
            // Extreme foot forward: pointing opposite to shin
            var footForward = -shin;

            var clamped = AnkleConstraint.ClampFootForward(shin, footForward);
            var angle = Vector3.Angle(shin, clamped);

            Assert.GreaterOrEqual(angle, 70f - 1f,
                $"Clamped angle must be >= 70° (min dorsiflexion). Got: {angle:F1}°");
            Assert.LessOrEqual(angle, 140f + 1f,
                $"Clamped angle must be <= 140° (max plantarflexion). Got: {angle:F1}°");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Foot IK pipeline: toe tip lands on grip, not ankle bone
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void FootPipeline_ToeTipLandsOnGrip_NotAnkleBone()
        {
            // Setup leg chain
            var legRoot = new GameObject("FootPipeline_ToeTip");
            var hip = new GameObject("Hip");
            var knee = new GameObject("Knee");
            var foot = new GameObject("Foot");
            var toeBase = new GameObject("ToeBase");
            hip.transform.SetParent(legRoot.transform);
            knee.transform.SetParent(hip.transform);
            foot.transform.SetParent(knee.transform);
            toeBase.transform.SetParent(foot.transform);

            // Climbing pose: hip near wall
            hip.transform.position = new Vector3(0.12f, 1.00f, -0.20f);
            knee.transform.position = hip.transform.position + Vector3.down * UpperLegLength;
            foot.transform.position = knee.transform.position + Vector3.down * LowerLegLength;
            toeBase.transform.localPosition = new Vector3(0f, 0f, 0.12f); // 12cm forward of ankle

            var gripPos = new Vector3(0.10f, 0.30f, 0.00f); // Foothold on wall
            const float toeTipLen = 0.12f;

            // Two-pass approach (mirrors SolveLeg logic):
            // Pass 1: solve to grip directly
            var hint = hip.transform.position + new Vector3(0.14f, -0.30f, 0.25f);
            TwoBoneIKSolver.Solve(hip.transform, knee.transform, foot.transform, gripPos, hint);

            // Compute constrained foot forward
            var shinDir = (foot.transform.position - knee.transform.position).normalized;
            var toGrip = (gripPos - foot.transform.position);
            var estForward = toGrip.sqrMagnitude > 0.0001f ? toGrip.normalized : Vector3.forward;
            estForward = AnkleConstraint.ClampFootForward(shinDir, estForward);

            // Pass 2: corrected target
            var correctedTarget = gripPos - estForward * toeTipLen;
            TwoBoneIKSolver.Solve(hip.transform, knee.transform, foot.transform, correctedTarget, hint);

            // After pass 2, toe tip should be near grip
            var finalShinDir = (foot.transform.position - knee.transform.position).normalized;
            var finalToGrip = (gripPos - foot.transform.position);
            var finalForward = finalToGrip.sqrMagnitude > 0.0001f ? finalToGrip.normalized : Vector3.forward;
            finalForward = AnkleConstraint.ClampFootForward(finalShinDir, finalForward);
            var toeTipPos = foot.transform.position + finalForward * toeTipLen;

            var tipError = Vector3.Distance(toeTipPos, gripPos);
            Assert.Less(tipError, 0.03f,
                $"Toe tip must land on grip within 3cm. Error: {tipError:F4}m " +
                $"(toeTip={toeTipPos}, grip={gripPos})");

            // Ankle bone must NOT be at grip — it should be offset back
            var ankleToGrip = Vector3.Distance(foot.transform.position, gripPos);
            Assert.Greater(ankleToGrip, toeTipLen * 0.5f,
                $"Ankle bone should be offset from grip by ~{toeTipLen}m, was {ankleToGrip:F4}m");

            Object.DestroyImmediate(legRoot);
        }

        [Test]
        public void FootPipeline_AnkleConstraint_PreventsFootFlip()
        {
            // Setup: grip is ABOVE the hip → foot would need to point upward
            var legRoot = new GameObject("FootPipeline_NoFlip");
            var hip = new GameObject("Hip");
            var knee = new GameObject("Knee");
            var foot = new GameObject("Foot");
            hip.transform.SetParent(legRoot.transform);
            knee.transform.SetParent(hip.transform);
            foot.transform.SetParent(knee.transform);

            hip.transform.position = new Vector3(0.12f, 1.00f, -0.20f);
            knee.transform.position = hip.transform.position + Vector3.down * UpperLegLength;
            foot.transform.position = knee.transform.position + Vector3.down * LowerLegLength;

            // Grip above hip — extreme case that causes foot flip
            var gripPos = new Vector3(0.10f, 1.10f, 0.00f);
            var hint = hip.transform.position + new Vector3(0.14f, -0.30f, 0.25f);

            TwoBoneIKSolver.Solve(hip.transform, knee.transform, foot.transform, gripPos, hint);

            var shinDir = (foot.transform.position - knee.transform.position).normalized;
            var toGrip = (gripPos - foot.transform.position);
            var unconstrained = toGrip.sqrMagnitude > 0.0001f ? toGrip.normalized : Vector3.forward;
            var constrained = AnkleConstraint.ClampFootForward(shinDir, unconstrained);

            // The constrained forward must produce a valid ankle angle
            var shinFootAngle = Vector3.Angle(shinDir, constrained);
            Assert.GreaterOrEqual(shinFootAngle, 70f - 1f,
                $"Shin-foot angle must respect dorsiflexion limit. Got: {shinFootAngle:F1}°");
            Assert.LessOrEqual(shinFootAngle, 140f + 1f,
                $"Shin-foot angle must respect plantarflexion limit. Got: {shinFootAngle:F1}°");

            // The foot forward should NOT be pointing back along the shin (= flipped sole)
            var dotWithShin = Vector3.Dot(constrained, shinDir);
            Assert.Less(dotWithShin, 0.5f,
                $"Foot forward must not be nearly parallel to shin (flipped). Dot: {dotWithShin:F3}");

            Object.DestroyImmediate(legRoot);
        }

        [Test]
        public void FootPipeline_NormalClimbingPose_FootAngleValid()
        {
            // Realistic climbing: hip at wall height, foothold below
            var legRoot = new GameObject("FootPipeline_Normal");
            var hip = new GameObject("Hip");
            var knee = new GameObject("Knee");
            var foot = new GameObject("Foot");
            hip.transform.SetParent(legRoot.transform);
            knee.transform.SetParent(hip.transform);
            foot.transform.SetParent(knee.transform);

            hip.transform.position = new Vector3(0.12f, 1.60f, -0.20f);
            knee.transform.position = hip.transform.position + Vector3.down * UpperLegLength;
            foot.transform.position = knee.transform.position + Vector3.down * LowerLegLength;

            var gripPos = new Vector3(0.10f, 0.50f, 0.00f); // Foothold on wall below pelvis
            var hint = hip.transform.position + new Vector3(0.14f, -0.30f, 0.25f);

            TwoBoneIKSolver.Solve(hip.transform, knee.transform, foot.transform, gripPos, hint);

            var shinDir = (foot.transform.position - knee.transform.position).normalized;
            var toGrip = (gripPos - foot.transform.position);
            var footFwd = toGrip.sqrMagnitude > 0.0001f ? toGrip.normalized : Vector3.forward;
            var constrained = AnkleConstraint.ClampFootForward(shinDir, footFwd);

            var shinFootAngle = Vector3.Angle(shinDir, constrained);
            Assert.GreaterOrEqual(shinFootAngle, 70f - 1f,
                $"Normal climbing: shin-foot angle {shinFootAngle:F1}° must be >= 70°");
            Assert.LessOrEqual(shinFootAngle, 140f + 1f,
                $"Normal climbing: shin-foot angle {shinFootAngle:F1}° must be <= 140°");

            Object.DestroyImmediate(legRoot);
        }
    }
}
