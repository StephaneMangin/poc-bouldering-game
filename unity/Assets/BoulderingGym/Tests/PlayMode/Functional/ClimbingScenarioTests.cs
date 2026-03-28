using System.Collections;
using NUnit.Framework;
using Project.Features.Player.Domain;
using Project.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode.Functional
{
    /// <summary>
    /// End-to-end functional tests simulating realistic climbing gameplay sequences.
    /// Each test exercises multiple features together: FSM, hands, physics, posture, camera.
    /// </summary>
    [TestFixture]
    public sealed class ClimbingScenarioTests : TestSceneFixture
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EnsureIdleGround();
        }

        // ─── Scenario: Full climb attempt → grab → hold → release → fall → land ───

        [UnityTest]
        public IEnumerator FullClimbAttempt_GrabHoldReleaseFallLand_ReturnsToIdleUpright()
        {
            // 1. Start idle on ground
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);
            var startPos = Driver.transform.position;

            // 2. Initiate grab → Reach
            Assert.IsTrue(Driver.StateMachine.Fire(PlayerTrigger.GrabRequested));
            yield return WaitForState(PlayerStateId.Reach);

            // 3. Grab succeeds → GripStable
            Assert.IsTrue(Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded));
            yield return WaitForState(PlayerStateId.GripStable);
            yield return WaitFrames(3);

            // 4. Release all holds → lose support → Falling
            if (Hands != null)
            {
                Hands.ReleaseAll();
            }

            Assert.IsTrue(Driver.StateMachine.Fire(PlayerTrigger.LostSupport));
            yield return WaitForState(PlayerStateId.Falling);

            // 5. Land → back to IdleGround
            yield return WaitForState(PlayerStateId.IdleGround, timeout: 10f);
            yield return WaitFrames(15);

            // 6. Verify final state
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);
            Assert.AreEqual(0, Hands.SupportCount, "No holds should be held after landing");

            // 7. Verify player is upright
            var euler = VisualRoot.localRotation.eulerAngles;
            var pitch = NormalizeAngle(euler.x);
            Assert.AreEqual(0f, pitch, 5f, $"Player should be upright after landing, pitch={pitch:F1}°");
        }

        // ─── Scenario: Failed grab should return to IdleGround when no support ───

        [UnityTest]
        public IEnumerator FailedGrab_NoSupport_ReturnsToIdleGround()
        {
            // Start → Reach
            Assert.IsTrue(Driver.StateMachine.Fire(PlayerTrigger.GrabRequested));
            yield return WaitForState(PlayerStateId.Reach);

            // Oracle: GrabFailed with no support → deterministic return to IdleGround
            const PlayerStateId expectedState = PlayerStateId.IdleGround;

            Assert.IsTrue(Driver.StateMachine.Fire(PlayerTrigger.GrabFailed));

            Assert.AreEqual(expectedState, Driver.StateMachine.CurrentState.Id,
                $"After GrabFailed with no support, expected {expectedState}, " +
                $"got {Driver.StateMachine.CurrentState.Id}");

            yield return WaitFrames(3);
        }

        // ─── Scenario: Multiple grab attempts from GripStable stay in GripStable ───

        [UnityTest]
        public IEnumerator MultipleGrabs_FromGripStable_StateRemainsGripStable()
        {
            // Get to GripStable
            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return WaitForState(PlayerStateId.Reach);
            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return WaitForState(PlayerStateId.GripStable);

            // Fire multiple grab triggers — should all keep GripStable
            for (var i = 0; i < 5; i++)
            {
                Assert.IsTrue(Driver.StateMachine.Fire(PlayerTrigger.GrabRequested),
                    $"GrabRequested #{i + 1} should be handled");
                Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);
                yield return null;
            }

            // Succeeded also stays in GripStable
            Assert.IsTrue(Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded));
            Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);

            // Failed also stays in GripStable
            Assert.IsTrue(Driver.StateMachine.Fire(PlayerTrigger.GrabFailed));
            Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);
            yield return null;
        }

        // ─── Scenario: Fall during reach → land → walk-ready ───

        [UnityTest]
        public IEnumerator FallDuringReach_Land_CharacterControllerActive()
        {
            // Reach → Fall
            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return WaitForState(PlayerStateId.Reach);

            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return WaitForState(PlayerStateId.Falling);

            // Wait for landing
            yield return WaitForState(PlayerStateId.IdleGround, timeout: 10f);
            yield return WaitFrames(10);

            // Verify CharacterController is still active
            var cc = Driver.GetComponent<CharacterController>();
            if (cc != null)
            {
                Assert.IsTrue(cc.enabled, "CharacterController should be enabled after landing");
            }

            // Verify player is on the ground (Y close to spawn height)
            Assert.Greater(Driver.transform.position.y, -0.1f,
                "Player should not have fallen through the ground");
            Assert.Less(Driver.transform.position.y, 2f,
                "Player should be near ground level after landing");
        }

        // ─── Scenario: Grab → GripStable → rapid release/grab cycles ───

        [UnityTest]
        public IEnumerator RapidReleaseGrabCycle_MaintainsConsistentState()
        {
            // Get to GripStable
            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return WaitForState(PlayerStateId.Reach);
            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return WaitForState(PlayerStateId.GripStable);

            // Rapid release/grab cycles
            for (var i = 0; i < 3; i++)
            {
                Driver.StateMachine.Fire(PlayerTrigger.ReleaseRequested);
                yield return null;

                var current = Driver.StateMachine.CurrentState.Id;
                Assert.IsTrue(
                    current == PlayerStateId.GripStable || current == PlayerStateId.Reach,
                    $"After release #{i + 1}, expected GripStable or Reach, got {current}");

                if (current == PlayerStateId.Reach)
                {
                    Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
                    yield return WaitForState(PlayerStateId.GripStable);
                }
                else
                {
                    Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
                    Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);
                }

                yield return null;
            }
        }

        // ─── Scenario: Double fall-land cycle without climbing ───

        [UnityTest]
        public IEnumerator DoubleFallLandCycle_PlayerRemainsStable()
        {
            for (var cycle = 0; cycle < 2; cycle++)
            {
                Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id,
                    $"Cycle {cycle}: should start in IdleGround");

                // Go to Reach then Fall
                Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
                yield return WaitForState(PlayerStateId.Reach);

                Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
                yield return WaitForState(PlayerStateId.Falling);

                // Wait for landing
                yield return WaitForState(PlayerStateId.IdleGround, timeout: 10f);
                yield return WaitFrames(10);
            }

            // After two fall-land cycles, should be stable on ground
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);
            Assert.Greater(Driver.transform.position.y, -0.1f, "Player should be above ground");
        }

        // ─── Scenario: Validate FSM cannot skip to invalid state ───

        [UnityTest]
        public IEnumerator InvalidTriggerSequence_FSMRejectsGracefully()
        {
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            // IdleGround does not handle Landed
            Assert.IsFalse(Driver.StateMachine.Fire(PlayerTrigger.Landed));
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            // IdleGround does not handle LostSupport
            Assert.IsFalse(Driver.StateMachine.Fire(PlayerTrigger.LostSupport));
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            // IdleGround does not handle ReleaseRequested
            Assert.IsFalse(Driver.StateMachine.Fire(PlayerTrigger.ReleaseRequested));
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            // IdleGround does not handle GrabSucceeded
            Assert.IsFalse(Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded));
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            yield return null;
        }

        // ─── Scenario: Player position stays coherent through state transitions ───

        [UnityTest]
        public IEnumerator StateTransitions_PositionNeverNaN()
        {
            // Cycle through several states, check position never becomes NaN
            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return WaitFrames(2);
            AssertPositionValid("After GrabRequested");

            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return WaitFrames(2);
            AssertPositionValid("After GrabSucceeded");

            if (Hands != null) Hands.ReleaseAll();
            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return WaitFrames(2);
            AssertPositionValid("After LostSupport");

            yield return WaitForState(PlayerStateId.IdleGround, timeout: 10f);
            yield return WaitFrames(5);
            AssertPositionValid("After landing");
        }

        private void AssertPositionValid(string context)
        {
            var pos = Driver.transform.position;
            Assert.IsFalse(float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z),
                $"{context}: Player position contains NaN ({pos})");
            Assert.IsFalse(float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z),
                $"{context}: Player position contains Infinity ({pos})");
        }
    }
}
