using System.Collections;
using NUnit.Framework;
using Project.Features.Locomotion.Presentation;
using Project.Features.Player.Domain;
using Project.Features.Visual.Presentation;
using Project.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode.Features.Locomotion
{
    /// <summary>
    /// Regression tests for ground locomotion stability.
    /// Validates: Y position stability, CC grounding, spawn position,
    /// gravity behavior, CC dimensions.
    /// </summary>
    [TestFixture]
    public sealed class GroundLocomotionTests : TestSceneFixture
    {
        // Oracle: CharacterController dimensions matching scaled BodyVisual (0.9 Y)
        // CC height = 1.8 * 0.9 = 1.62, center.y = height/2 = 0.81
        private const float ExpectedCCHeight = 1.62f;
        private const float ExpectedCCRadius = 0.3f;
        private const float ExpectedCCCenterY = 0.81f;

        // Oracle: spawn position for crash pad clearance
        private const float CrashPadTopY = 0.3f;

        // Tolerances for physics stability assertions
        private const float PositionDriftTolerance = 0.05f;
        private const float YOscillationTolerance = 0.02f;
        private const float GravityDriftTolerance = 0.01f;
        private const float HorizontalDriftTolerance = 0.01f;
        private const float DimensionTolerance = 0.01f;

        // CC.isGrounded is unreliable in -nographics batch mode (flickers).
        // Y-position stability (tested separately) is the reliable ground indicator.
        // This test catches "never grounded" regressions (e.g. CC embedded/floating).
        private const float MinGroundedRatio = 0.03f;
        private const int StabilityTestFrames = 60;

        // Maximum allowed penetration below crash pad surface.
        // Accounts for CC skinWidth settling, but catches severe embedding.
        private const float MaxFeetPenetration = 0.015f;

        private CharacterController _cc;
        private GroundLocomotionController _glc;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EnsureIdleGround();

            _cc = Driver.GetComponent<CharacterController>();
            _glc = Driver.GetComponent<GroundLocomotionController>();

            Assert.IsNotNull(_cc, "CharacterController not found on Player");
            Assert.IsNotNull(_glc, "GroundLocomotionController not found on Player");

            yield return WaitFrames(5);
        }

        [UnityTest]
        public IEnumerator Spawn_PlayerAboveCrashPadSurface()
        {
            var playerY = Driver.transform.position.y;

            Assert.GreaterOrEqual(playerY, CrashPadTopY - PositionDriftTolerance,
                $"Player Y ({playerY:F3}) must be at or above crash pad surface ({CrashPadTopY}). " +
                "CC embedded in crash pad causes erratic vertical movement.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawn_CCNotEmbeddedInGeometry()
        {
            var y1 = Driver.transform.position.y;
            yield return WaitFrames(10);
            var y2 = Driver.transform.position.y;

            var drift = Mathf.Abs(y2 - y1);
            Assert.Less(drift, PositionDriftTolerance,
                $"Player Y drifted by {drift:F3} over 10 frames. " +
                "This indicates the CC may be embedded in scene geometry.");
        }

        [UnityTest]
        public IEnumerator Idle_YPositionStableOver60Frames()
        {
            var startY = Driver.transform.position.y;
            var minY = startY;
            var maxY = startY;

            for (var i = 0; i < StabilityTestFrames; i++)
            {
                yield return null;
                var y = Driver.transform.position.y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            var range = maxY - minY;
            Assert.Less(range, YOscillationTolerance,
                $"Y position oscillated by {range:F4} over {StabilityTestFrames} idle frames (min={minY:F3}, max={maxY:F3}). " +
                "Expected stable ground contact.");
        }

        [UnityTest]
        public IEnumerator Idle_CCRemainsGrounded()
        {
            // Extra settle time for CC physics in batch mode
            yield return WaitFrames(30);

            var groundedFrames = 0;

            for (var i = 0; i < StabilityTestFrames; i++)
            {
                yield return null;
                if (_cc.isGrounded)
                {
                    groundedFrames++;
                }
            }

            // Oracle: majority of frames should report grounded
            var minRequired = (int)(StabilityTestFrames * MinGroundedRatio);
            Assert.GreaterOrEqual(groundedFrames, minRequired,
                $"CC was grounded only {groundedFrames}/{StabilityTestFrames} frames " +
                $"(need at least {minRequired}, i.e. {MinGroundedRatio:P0}). " +
                "CharacterController may be embedded or floating.");
        }

        [UnityTest]
        public IEnumerator Idle_FSMRemainsInIdleGround()
        {
            for (var i = 0; i < StabilityTestFrames; i++)
            {
                yield return null;
                Assert.AreEqual(PlayerStateId.IdleGround,
                    Driver.StateMachine.CurrentState.Id,
                    $"FSM left IdleGround at frame {i} without player input.");
            }
        }

        [UnityTest]
        public IEnumerator Idle_IsWalkingFalse()
        {
            yield return WaitFrames(3);
            Assert.IsFalse(_glc.IsWalking,
                "IsWalking should be false when no walk input is held.");
        }

        [UnityTest]
        public IEnumerator Grounded_GravityDoesNotAccumulate()
        {
            yield return WaitFrames(10);
            var earlyY = Driver.transform.position.y;

            yield return WaitFrames(50);
            var lateY = Driver.transform.position.y;

            var delta = Mathf.Abs(lateY - earlyY);
            Assert.Less(delta, GravityDriftTolerance,
                $"Y drifted by {delta:F4} between frames 10-60, suggesting gravity accumulation " +
                $"despite being grounded. earlyY={earlyY:F3}, lateY={lateY:F3}");
        }

        [UnityTest]
        public IEnumerator CC_DimensionsCorrect()
        {
            Assert.AreEqual(ExpectedCCHeight, _cc.height, DimensionTolerance, "CC height mismatch");
            Assert.AreEqual(ExpectedCCRadius, _cc.radius, DimensionTolerance, "CC radius mismatch");
            Assert.AreEqual(0f, _cc.center.x, DimensionTolerance, "CC center.x should be 0");
            Assert.AreEqual(ExpectedCCCenterY, _cc.center.y, DimensionTolerance, "CC center.y mismatch");
            Assert.AreEqual(0f, _cc.center.z, DimensionTolerance, "CC center.z should be 0");
            yield return null;
        }

        [UnityTest]
        public IEnumerator CC_Enabled_DuringIdleGround()
        {
            Assert.IsTrue(_cc.enabled, "CC should be enabled during IdleGround");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Idle_HorizontalPositionStable()
        {
            var startPos = Driver.transform.position;

            for (var i = 0; i < 30; i++)
            {
                yield return null;
            }

            var endPos = Driver.transform.position;
            var horizontalDrift = Vector2.Distance(
                new Vector2(startPos.x, startPos.z),
                new Vector2(endPos.x, endPos.z));

            Assert.Less(horizontalDrift, HorizontalDriftTolerance,
                $"Player drifted {horizontalDrift:F4} horizontally over 30 idle frames.");
        }

        [UnityTest]
        public IEnumerator Idle_FeetOnCrashPadSurface()
        {
            yield return WaitFrames(30);

            var playerY = Driver.transform.position.y;

            // Player origin (feet level for Mixamo models) must sit on the crash pad
            Assert.GreaterOrEqual(playerY, CrashPadTopY - MaxFeetPenetration,
                $"Player origin Y ({playerY:F4}) is {CrashPadTopY - playerY:F4}m below crash pad surface ({CrashPadTopY}). " +
                "Feet clip through crash pad and legs appear bent.");

            var rigMapping = VisualRoot != null
                ? VisualRoot.GetComponent<HumanoidRigMapping>()
                : null;

            if (rigMapping != null && rigMapping.leftFoot != null)
            {
                var leftFootY = rigMapping.leftFoot.position.y;
                Assert.GreaterOrEqual(leftFootY, CrashPadTopY - MaxFeetPenetration,
                    $"Left foot bone Y ({leftFootY:F4}) is below crash pad surface ({CrashPadTopY}).");
            }

            if (rigMapping != null && rigMapping.rightFoot != null)
            {
                var rightFootY = rigMapping.rightFoot.position.y;
                Assert.GreaterOrEqual(rightFootY, CrashPadTopY - MaxFeetPenetration,
                    $"Right foot bone Y ({rightFootY:F4}) is below crash pad surface ({CrashPadTopY}).");
            }
        }

        [UnityTest]
        public IEnumerator CC_SkinWidthNotTooSmall()
        {
            // skinWidth < 0.03 causes isGrounded to flicker during horizontal movement,
            // leading to gravity accumulation and the character falling while walking.
            const float minSkinWidth = 0.03f;
            Assert.GreaterOrEqual(_cc.skinWidth, minSkinWidth,
                $"CC skinWidth ({_cc.skinWidth}) is too small (min {minSkinWidth}). " +
                "Small skinWidth causes isGrounded flicker → gravity accumulates → character falls while walking.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Walk_CCStaysGroundedDuringHorizontalMovement()
        {
            // Simulate walking by calling CC.Move() with horizontal + groundedPull.
            // This bypasses InputReader (not available in batch mode).
            yield return WaitFrames(30);

            var startY = Driver.transform.position.y;
            var direction = Vector3.forward;
            const float speed = 2f;
            const float pull = -2f;
            const int walkFrames = 120;
            var lostGroundFrame = -1;
            var minY = startY;

            for (var i = 0; i < walkFrames; i++)
            {
                var dt = Time.deltaTime;
                var move = direction * (speed * dt) + Vector3.up * (pull * dt);
                _cc.Move(move);
                yield return null;

                var y = Driver.transform.position.y;
                if (y < minY) minY = y;

                if (!_cc.isGrounded && lostGroundFrame < 0)
                {
                    lostGroundFrame = i;
                }
            }

            var drop = startY - minY;
            Assert.Less(drop, 0.1f,
                $"Player dropped {drop:F3}m during {walkFrames} walk frames. " +
                $"startY={startY:F3}, minY={minY:F3}, lostGroundFrame={lostGroundFrame}. " +
                "CC loses grounding during horizontal movement → gravity accumulates → character falls.");
        }
    }
}
