using NUnit.Framework;
using Project.Features.Climbing.Domain;
using UnityEngine;
using static Project.Features.Climbing.Domain.ClimbingBodySolver;

namespace Project.Tests.EditMode.Features.Climbing
{
    /// <summary>
    /// Physical posture tests — TDD: these define CORRECT climbing posture.
    /// Tests verify angles, distances, and extension ratios — not just states.
    /// Written BEFORE the fix to prove the bug and prevent regression.
    /// </summary>
    [TestFixture]
    public sealed class ClimbingPosturePhysicsTests
    {
        private AnatomyParams _anatomy;
        private readonly Vector3 _wallNormal = Vector3.back;

        /// <summary>
        /// Typical pelvis bone offset in a Mixamo humanoid:
        /// the Hips bone sits ~0.95m above the FBX root (feet origin).
        /// </summary>
        private readonly Vector3 _pelvisBoneOffset = new(0f, 0.95f, 0f);

        [SetUp]
        public void SetUp()
        {
            _anatomy = AnatomyParams.Default;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Knee bend: legs must NEVER be at 180° (fully straight)
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void KneeBendRatio_TypicalClimbing_LegsAreBent()
        {
            // Typical setup: hands at 2.5, feet at 1.0
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(-0.2f, 2.5f, 0f),
                RightHand = new Vector3(0.2f, 2.5f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            var solution = Solve(contacts, _anatomy, _wallNormal);
            Assert.IsTrue(solution.IsValid);

            var leftRatio = ComputeLimbExtensionRatio(
                solution.PelvisPosition, contacts.LeftFoot.Value, false, true, _anatomy);
            var rightRatio = ComputeLimbExtensionRatio(
                solution.PelvisPosition, contacts.RightFoot.Value, false, false, _anatomy);

            Assert.Less(leftRatio, 0.95f,
                $"Left leg extension {leftRatio:F2} — knee must be bent (< 0.95), not straight");
            Assert.Less(rightRatio, 0.95f,
                $"Right leg extension {rightRatio:F2} — knee must be bent (< 0.95), not straight");
        }

        [Test]
        public void KneeBendRatio_FeetDirectlyBelowHands_LegsNotHyperExtended()
        {
            // Feet 1.5m below hands — worst case vertical stretch
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(0f, 2.5f, 0f),
                LeftFoot = new Vector3(0f, 1.0f, 0f),
            };

            var solution = Solve(contacts, _anatomy, _wallNormal);
            var ratio = ComputeLimbExtensionRatio(
                solution.PelvisPosition, contacts.LeftFoot.Value, false, true, _anatomy);

            Assert.Less(ratio, 1.0f,
                $"Leg extension ratio {ratio:F2} must be < 1.0 (hyper-extension is impossible)");
        }

        [Test]
        public void KneeBendRatio_NeverExceedsChainLength()
        {
            // Wide spread: feet far apart, hands close together
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(-0.1f, 2.5f, 0f),
                RightHand = new Vector3(0.1f, 2.5f, 0f),
                LeftFoot = new Vector3(-0.4f, 0.8f, 0f),
                RightFoot = new Vector3(0.4f, 0.8f, 0f),
            };

            var solution = Solve(contacts, _anatomy, _wallNormal);

            var leftRatio = ComputeLimbExtensionRatio(
                solution.PelvisPosition, contacts.LeftFoot.Value, false, true, _anatomy);
            var rightRatio = ComputeLimbExtensionRatio(
                solution.PelvisPosition, contacts.RightFoot.Value, false, false, _anatomy);

            Assert.Less(leftRatio, 1.0f,
                $"Left leg ratio {leftRatio:F2} exceeds chain length — physically impossible");
            Assert.Less(rightRatio, 1.0f,
                $"Right leg ratio {rightRatio:F2} exceeds chain length — physically impossible");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Root offset: the KEY bug — solver pelvis ≠ player root
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void RootPosition_WithoutOffset_PelvisEndsTooHigh_LegsHyperExtend()
        {
            // This test PROVES the bug: applying solver pelvis directly to root
            // makes the actual pelvis bone end up too high.
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(0f, 2.5f, 0f),
                LeftFoot = new Vector3(0f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            var solution = Solve(contacts, _anatomy, _wallNormal);

            // BUG: root = solverPelvis → actual pelvis = solverPelvis + boneOffset
            var wrongPelvis = solution.PelvisPosition + _pelvisBoneOffset;
            var wrongLeftRatio = ComputeLimbExtensionRatio(
                wrongPelvis, contacts.LeftFoot.Value, false, true, _anatomy);

            // This SHOULD be >1.0 (proving hyper-extension with the bug)
            Assert.Greater(wrongLeftRatio, 1.0f,
                $"BUG PROOF: without offset fix, leg extension is {wrongLeftRatio:F2} — hyper-extended!");
        }

        [Test]
        public void RootPosition_WithOffset_PelvisCorrect_LegsBent()
        {
            // This test validates the FIX: root = solverPelvis - boneOffset
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(0f, 2.5f, 0f),
                LeftFoot = new Vector3(0f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            var solution = Solve(contacts, _anatomy, _wallNormal);

            // FIX: root = solverPelvis - boneOffset → actual pelvis = solverPelvis
            var correctRoot = ComputeRootPosition(solution.PelvisPosition, _pelvisBoneOffset);
            var actualPelvis = correctRoot + _pelvisBoneOffset; // Should equal solution.PelvisPosition

            Assert.AreEqual(solution.PelvisPosition.x, actualPelvis.x, 0.001f);
            Assert.AreEqual(solution.PelvisPosition.y, actualPelvis.y, 0.001f);
            Assert.AreEqual(solution.PelvisPosition.z, actualPelvis.z, 0.001f);

            var leftRatio = ComputeLimbExtensionRatio(
                actualPelvis, contacts.LeftFoot.Value, false, true, _anatomy);

            Assert.Less(leftRatio, 0.95f,
                $"With offset fix, leg extension {leftRatio:F2} should be < 0.95 (bent knee)");
        }

        [Test]
        public void ComputeRootPosition_SubtractsOffset()
        {
            var pelvisTarget = new Vector3(0f, 1.6f, -0.35f);
            var offset = new Vector3(0f, 0.95f, 0f);

            var root = ComputeRootPosition(pelvisTarget, offset);

            Assert.AreEqual(0f, root.x, 0.001f);
            Assert.AreEqual(0.65f, root.y, 0.001f, "Root Y = pelvis Y - offset Y");
            Assert.AreEqual(-0.35f, root.z, 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Body-wall angle: pelvis must be offset from wall, legs angled
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void BodyWallDistance_PelvisOffsetFromWall()
        {
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(0f, 2.5f, 0f),
                LeftFoot = new Vector3(0f, 1.0f, 0f),
            };

            var solution = Solve(contacts, _anatomy, _wallNormal);

            // Wall is at Z=0, pelvis should be offset backwards (wallNormal = back → Z negative)
            Assert.Less(solution.PelvisPosition.z, 0f,
                "Pelvis must be offset away from the wall (negative Z for back-facing wall)");
            Assert.AreEqual(-_anatomy.WallOffset, solution.PelvisPosition.z, 0.01f,
                "Pelvis Z should be at wall offset distance");
        }

        [Test]
        public void LegAngle_HipToFoot_NotParallelToWall()
        {
            // Foot is ON the wall (Z=0), pelvis is OFFSET (Z=-0.35)
            // The leg must angle FROM the pelvis TOWARD the wall — not be flat
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(0f, 2.5f, 0f),
                LeftFoot = new Vector3(0f, 1.0f, 0f),
            };

            var solution = Solve(contacts, _anatomy, _wallNormal);
            var hip = GetHipPosition(solution.PelvisPosition, true, _anatomy);
            var foot = contacts.LeftFoot.Value;

            var hipToFoot = foot - hip;

            // Foot is closer to wall than hip → Z component should be positive (toward wall)
            Assert.Greater(hipToFoot.z, 0f,
                "Leg should angle TOWARD the wall (foot closer to wall than hip)");

            // The angle between leg direction and vertical should be reasonable
            var angleFromVertical = Vector3.Angle(hipToFoot, Vector3.down);
            Assert.Less(angleFromVertical, 60f,
                $"Leg angle from vertical ({angleFromVertical:F1}°) should be < 60° for normal climbing");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Arm extension: same checks for hands
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void ArmExtensionRatio_TypicalClimbing_ArmsNotOverStretched()
        {
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(-0.2f, 2.5f, 0f),
                RightHand = new Vector3(0.2f, 2.5f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            var solution = Solve(contacts, _anatomy, _wallNormal);

            var leftRatio = ComputeLimbExtensionRatio(
                solution.PelvisPosition, contacts.LeftHand.Value, true, true, _anatomy);
            var rightRatio = ComputeLimbExtensionRatio(
                solution.PelvisPosition, contacts.RightHand.Value, true, false, _anatomy);

            Assert.Less(leftRatio, 1.0f,
                $"Left arm extension {leftRatio:F2} must be < 1.0");
            Assert.Less(rightRatio, 1.0f,
                $"Right arm extension {rightRatio:F2} must be < 1.0");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Full scenario: climb sequence with ascending holds
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void ClimbSequence_AscendingHolds_AllLimbsStayInRange()
        {
            // Simulate climbing: place feet at 1.0m, then hands at 2.0m, then move feet to 1.8m
            var step1 = new LimbContacts
            {
                LeftHand = new Vector3(-0.15f, 2.0f, 0f),
                RightHand = new Vector3(0.15f, 2.0f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            // Move feet higher (climbing progression)
            var step2 = new LimbContacts
            {
                LeftHand = new Vector3(-0.15f, 2.0f, 0f),
                RightHand = new Vector3(0.15f, 2.0f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.6f, 0f),
                RightFoot = new Vector3(0.1f, 1.8f, 0f),
            };

            foreach (var contacts in new[] { step1, step2 })
            {
                var solution = Solve(contacts, _anatomy, _wallNormal);
                Assert.IsTrue(solution.IsValid);

                // Check all placed limbs are within reach
                if (contacts.LeftFoot.HasValue)
                {
                    var ratio = ComputeLimbExtensionRatio(
                        solution.PelvisPosition, contacts.LeftFoot.Value, false, true, _anatomy);
                    Assert.Less(ratio, 1.0f, $"Left leg ratio {ratio:F2} exceeds chain at step");
                }

                if (contacts.RightFoot.HasValue)
                {
                    var ratio = ComputeLimbExtensionRatio(
                        solution.PelvisPosition, contacts.RightFoot.Value, false, false, _anatomy);
                    Assert.Less(ratio, 1.0f, $"Right leg ratio {ratio:F2} exceeds chain at step");
                }

                if (contacts.LeftHand.HasValue)
                {
                    var ratio = ComputeLimbExtensionRatio(
                        solution.PelvisPosition, contacts.LeftHand.Value, true, true, _anatomy);
                    Assert.Less(ratio, 1.0f, $"Left arm ratio {ratio:F2} exceeds chain at step");
                }

                if (contacts.RightHand.HasValue)
                {
                    var ratio = ComputeLimbExtensionRatio(
                        solution.PelvisPosition, contacts.RightHand.Value, true, false, _anatomy);
                    Assert.Less(ratio, 1.0f, $"Right arm ratio {ratio:F2} exceeds chain at step");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Extremity Contact: fingertips / toes must land ON the grip
        // ═══════════════════════════════════════════════════════════════════

        private const float FingertipLength = 0.10f;
        private const float ToeTipLength = 0.12f;

        [Test]
        public void ExtremityOffset_IKTarget_IsExactlyExtremityLengthFromGrip()
        {
            var bonePreIK = new Vector3(0f, 1.5f, -0.3f);
            var gripPos = new Vector3(0.2f, 2.5f, 0f);

            var ikTarget = ComputeExtremityIKTarget(bonePreIK, gripPos, FingertipLength);
            var dist = Vector3.Distance(ikTarget, gripPos);

            Assert.AreEqual(FingertipLength, dist, 0.001f,
                $"IK target must be exactly {FingertipLength}m from grip, got {dist:F4}m");
        }

        [TestCase(0f, 1.5f, -0.3f, 0.2f, 2.5f, 0f, 0.10f, TestName = "Hand reaching up-right")]
        [TestCase(0f, 0.5f, -0.3f, 0.1f, 0.8f, 0f, 0.12f, TestName = "Foot on wall hold")]
        [TestCase(-0.3f, 1.0f, -0.4f, -0.5f, 2.0f, 0f, 0.10f, TestName = "Hand reaching far up-left")]
        [TestCase(0.2f, 0.3f, -0.35f, 0.4f, 0.5f, 0.05f, 0.12f, TestName = "Low foot hold")]
        public void ExtremityTip_WithCorrectOrientation_AlwaysLandsOnGrip(
            float bx, float by, float bz, float gx, float gy, float gz, float length)
        {
            var bone = new Vector3(bx, by, bz);
            var grip = new Vector3(gx, gy, gz);

            var ikTarget = ComputeExtremityIKTarget(bone, grip, length);
            var tip = ReconstructExtremityTip(ikTarget, grip, length);

            Assert.AreEqual(grip.x, tip.x, 0.001f, "Tip X must match grip");
            Assert.AreEqual(grip.y, tip.y, 0.001f, "Tip Y must match grip");
            Assert.AreEqual(grip.z, tip.z, 0.001f, "Tip Z must match grip");
        }

        [Test]
        public void FootOnHold_ToesPointingForward_TipMissesGrip()
        {
            // PROVES THE BUG: SolveLeg orients toes forward (Z+)
            // but the hold is on the wall — toes should point toward the grip
            var bonePreIK = new Vector3(0f, 0.5f, -0.3f);
            var gripPos = new Vector3(0.1f, 0.8f, 0f);

            var ikTarget = ComputeExtremityIKTarget(bonePreIK, gripPos, ToeTipLength);

            // CURRENT BEHAVIOR: toes point forward (Z+) regardless of hold position
            var wrongToeDir = Vector3.forward;
            var wrongTip = ikTarget + wrongToeDir * ToeTipLength;

            var missDistance = Vector3.Distance(wrongTip, gripPos);
            Assert.Greater(missDistance, 0.05f,
                $"BUG PROOF: toes pointing forward miss the grip by {missDistance:F3}m");
        }

        [Test]
        public void FootOnHold_ToesOrientedToGrip_TipLandsOnGrip()
        {
            // VALIDATES THE FIX: orient toes from foot bone toward grip
            var bonePreIK = new Vector3(0f, 0.5f, -0.3f);
            var gripPos = new Vector3(0.1f, 0.8f, 0f);

            var ikTarget = ComputeExtremityIKTarget(bonePreIK, gripPos, ToeTipLength);

            // FIX: toes point from foot bone toward grip
            var correctToeDir = (gripPos - ikTarget).normalized;
            var correctTip = ikTarget + correctToeDir * ToeTipLength;

            Assert.AreEqual(gripPos.x, correctTip.x, 0.001f, "Toe tip X must match grip");
            Assert.AreEqual(gripPos.y, correctTip.y, 0.001f, "Toe tip Y must match grip");
            Assert.AreEqual(gripPos.z, correctTip.z, 0.001f, "Toe tip Z must match grip");
        }

        [Test]
        public void AllLimbs_ReachableHolds_ExtremitiesReachGripPositions()
        {
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(-0.2f, 2.5f, 0f),
                RightHand = new Vector3(0.2f, 2.5f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            var solution = Solve(contacts, _anatomy, _wallNormal);
            Assert.IsTrue(solution.IsValid);

            var limbs = new[]
            {
                (grip: contacts.LeftHand.Value, isHand: true, isLeft: true),
                (grip: contacts.RightHand.Value, isHand: true, isLeft: false),
                (grip: contacts.LeftFoot.Value, isHand: false, isLeft: true),
                (grip: contacts.RightFoot.Value, isHand: false, isLeft: false),
            };

            foreach (var (grip, isHand, isLeft) in limbs)
            {
                var joint = isHand
                    ? GetShoulderPosition(solution.PelvisPosition, isLeft, _anatomy)
                    : GetHipPosition(solution.PelvisPosition, isLeft, _anatomy);
                var chainLength = isHand ? _anatomy.ArmLength : _anatomy.LegLength;
                var extremityLength = isHand ? FingertipLength : ToeTipLength;

                // The joint-to-grip distance must allow the IK target to be reachable
                var ikTarget = ComputeExtremityIKTarget(joint, grip, extremityLength);
                var distToTarget = Vector3.Distance(joint, ikTarget);

                Assert.Less(distToTarget, chainLength,
                    $"{(isLeft ? "Left" : "Right")} {(isHand ? "arm" : "leg")}: " +
                    $"IK target at {distToTarget:F3}m exceeds chain {chainLength:F3}m");

                // When correctly oriented, the extremity tip must land on grip
                var tip = ReconstructExtremityTip(ikTarget, grip, extremityLength);
                Assert.AreEqual(grip.x, tip.x, 0.005f);
                Assert.AreEqual(grip.y, tip.y, 0.005f);
                Assert.AreEqual(grip.z, tip.z, 0.005f);
            }
        }
    }
}
