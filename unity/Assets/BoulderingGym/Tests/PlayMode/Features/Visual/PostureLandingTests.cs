using System.Collections;
using NUnit.Framework;
using Project.Features.Player.Domain;
using Project.Tests.PlayMode.Fixtures;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode.Features.Visual
{
    /// <summary>
    /// Tests that climbing posture resets after landing and matches expected pitch per state.
    /// </summary>
    [TestFixture]
    public sealed class PostureLandingTests : TestSceneFixture
    {
        // Oracle: pitch angles per state from ClimberPostureController.ApplyClimbingPosture
        private const float IdleGroundPitch = 0f;
        private const float FallingPitch = 24f;
        private const float GripStablePitch = -2f;
        private const float ReachPitch = -6f;
        private const float PitchTolerance = 3f;
        private const float UprightTolerance = 5f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EnsureIdleGround();
        }

        [UnityTest]
        public IEnumerator ClimbAndFall_LandingPostureIsUpright()
        {
            yield return WaitForState(PlayerStateId.IdleGround);

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return WaitForState(PlayerStateId.Reach);

            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return WaitForState(PlayerStateId.GripStable);

            yield return WaitFrames(5);

            if (Hands != null)
            {
                Hands.ReleaseAll();
            }

            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return WaitForState(PlayerStateId.Falling);

            yield return WaitForState(PlayerStateId.IdleGround, timeout: 10f);
            yield return WaitFrames(15);

            AssertPlayerIsUpright(UprightTolerance);
            Assert.AreEqual(0, Hands.SupportCount, "Hands should not hold anything after landing");
        }

        [UnityTest]
        public IEnumerator DirectFall_LandingPostureIsUpright()
        {
            yield return WaitForState(PlayerStateId.IdleGround);

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return WaitForState(PlayerStateId.Reach);

            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return WaitForState(PlayerStateId.Falling);

            yield return WaitForState(PlayerStateId.IdleGround, timeout: 10f);
            yield return WaitFrames(15);

            AssertPlayerIsUpright(UprightTolerance);
        }

        [UnityTest]
        public IEnumerator PosturePitchByState_MatchesExpected()
        {
            yield return WaitForState(PlayerStateId.IdleGround);

            if (Posture == null)
            {
                Assert.Ignore("ClimberPostureController not found in scene");
            }

            // Disable orchestrator so its LateUpdate doesn't override manual ApplyPosture calls
            if (VisualOrchestrator != null)
                VisualOrchestrator.enabled = false;

            Posture.ApplyPosture(PlayerStateId.IdleGround);
            yield return null;
            var idlePitch = NormalizeAngle(VisualRoot.localRotation.eulerAngles.x);
            Assert.AreEqual(IdleGroundPitch, idlePitch, PitchTolerance,
                $"IdleGround pitch expected ~{IdleGroundPitch}°, got {idlePitch:F1}°");

            Posture.ApplyPosture(PlayerStateId.Falling);
            yield return null;
            var fallingPitch = NormalizeAngle(VisualRoot.localRotation.eulerAngles.x);
            Assert.AreEqual(FallingPitch, fallingPitch, PitchTolerance,
                $"Falling pitch expected ~{FallingPitch}°, got {fallingPitch:F1}°");

            Posture.ApplyPosture(PlayerStateId.GripStable);
            yield return null;
            var gripPitch = NormalizeAngle(VisualRoot.localRotation.eulerAngles.x);
            Assert.AreEqual(GripStablePitch, gripPitch, PitchTolerance,
                $"GripStable pitch expected ~{GripStablePitch}°, got {gripPitch:F1}°");

            Posture.ApplyPosture(PlayerStateId.Reach);
            yield return null;
            var reachPitch = NormalizeAngle(VisualRoot.localRotation.eulerAngles.x);
            Assert.AreEqual(ReachPitch, reachPitch, PitchTolerance,
                $"Reach pitch expected ~{ReachPitch}°, got {reachPitch:F1}°");

            if (VisualOrchestrator != null)
                VisualOrchestrator.enabled = true;
        }

        [UnityTest]
        public IEnumerator PostureReset_AfterMultipleFalls_StaysUpright()
        {
            for (var i = 0; i < 3; i++)
            {
                yield return WaitForState(PlayerStateId.IdleGround);

                Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
                yield return WaitForState(PlayerStateId.Reach);

                Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
                yield return WaitForState(PlayerStateId.Falling);

                yield return WaitForState(PlayerStateId.IdleGround, timeout: 10f);
                yield return WaitFrames(10);
            }

            AssertPlayerIsUpright(UprightTolerance);
        }

        private void AssertPlayerIsUpright(float toleranceDegrees)
        {
            var euler = VisualRoot.localRotation.eulerAngles;
            var pitch = NormalizeAngle(euler.x);
            var roll = NormalizeAngle(euler.z);

            Assert.AreEqual(0f, pitch, toleranceDegrees,
                $"Player pitch should be ~0° (upright), but is {pitch:F1}°");
            Assert.AreEqual(0f, roll, toleranceDegrees,
                $"Player roll should be ~0° (upright), but is {roll:F1}°");
        }
    }
}
