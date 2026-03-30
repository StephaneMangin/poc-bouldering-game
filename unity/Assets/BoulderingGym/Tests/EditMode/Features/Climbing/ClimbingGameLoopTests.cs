using System;
using NUnit.Framework;
using Project.Core.Events;
using Project.Features.Climbing.Domain;
using Project.Features.Player.Application;
using Project.Features.Player.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Climbing
{
    /// <summary>
    /// Functional tests validating the core game loop invariants:
    /// Walk → Approach → Target → Place → Climb → Top/Fall → Land → Repeat.
    /// Combines FSM transitions with ClimbingBodySolver logic.
    /// </summary>
    [TestFixture]
    public sealed class ClimbingGameLoopTests
    {
        private PlayerStateMachine _fsm;
        private ClimbingBodySolver.AnatomyParams _anatomy;
        private readonly Vector3 _wallNormal = Vector3.back;

        [SetUp]
        public void SetUp()
        {
            var context = new PlayerContext
            {
                EventBus = new StubEventBus(),
            };
            _fsm = new PlayerStateMachine(context);
            _anatomy = ClimbingBodySolver.AnatomyParams.Default;
        }

        // ─── Loop step 1-2: Walk → Approach ───

        [Test]
        public void GameLoop_StartsInIdleGround()
        {
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id,
                "Game must start in IdleGround (walking mode)");
        }

        // ─── Loop step 3-4: Target → Place first limb ───

        [Test]
        public void GameLoop_FirstPlacement_TransitionsToReachThenGrip()
        {
            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.AreEqual(PlayerStateId.Reach, _fsm.CurrentState.Id);

            _fsm.Fire(PlayerTrigger.GrabSucceeded);
            Assert.AreEqual(PlayerStateId.GripStable, _fsm.CurrentState.Id,
                "After first successful grab, should be in GripStable");
        }

        // ─── Loop step 5: Climb — body follows placed limbs ───

        [Test]
        public void GameLoop_BodySolverComputesPelvisFromContacts()
        {
            // Simulate climbing with 4 limbs placed
            var contacts = new ClimbingBodySolver.LimbContacts
            {
                LeftHand = new Vector3(-0.2f, 2.5f, 0f),
                RightHand = new Vector3(0.2f, 2.5f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            var result = ClimbingBodySolver.Solve(contacts, _anatomy, _wallNormal);

            Assert.IsTrue(result.IsValid);
            Assert.Greater(result.PelvisPosition.y, 1.0f, "Pelvis must be above feet");
            Assert.Less(result.PelvisPosition.y, 2.5f, "Pelvis must be below hands");
        }

        [Test]
        public void GameLoop_HigherFeetPushBodyHigherForHandReach()
        {
            // Low feet: body low → hands can't reach as high
            var lowFeet = new ClimbingBodySolver.LimbContacts
            {
                LeftHand = new Vector3(0f, 3.0f, 0f),
                LeftFoot = new Vector3(0f, 0.5f, 0f),
            };

            // High feet: body higher → hands can reach higher
            var highFeet = new ClimbingBodySolver.LimbContacts
            {
                LeftHand = new Vector3(0f, 3.0f, 0f),
                LeftFoot = new Vector3(0f, 1.5f, 0f),
            };

            var lowResult = ClimbingBodySolver.Solve(lowFeet, _anatomy, _wallNormal);
            var highResult = ClimbingBodySolver.Solve(highFeet, _anatomy, _wallNormal);

            Assert.Greater(highResult.PelvisPosition.y, lowResult.PelvisPosition.y,
                "Moving feet higher must push the body upward (legs push mechanic)");
        }

        [Test]
        public void GameLoop_PlacementRejectedIfUnreachable()
        {
            var contacts = new ClimbingBodySolver.LimbContacts
            {
                RightHand = new Vector3(0.2f, 2.0f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            // Trying to place left hand 5m away — should fail
            var farHold = new Vector3(-3.0f, 6.0f, 0f);
            Assert.IsFalse(ClimbingBodySolver.CanPlaceAndStillReachAll(
                    contacts, farHold, true, true, _anatomy, _wallNormal),
                "Placement must be rejected if hold is physically unreachable");
        }

        [Test]
        public void GameLoop_PlacementAcceptedIfReasonable()
        {
            var contacts = new ClimbingBodySolver.LimbContacts
            {
                RightHand = new Vector3(0.2f, 2.0f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            // Placing left hand nearby — should succeed
            var nearHold = new Vector3(-0.2f, 2.0f, 0f);
            Assert.IsTrue(ClimbingBodySolver.CanPlaceAndStillReachAll(
                    contacts, nearHold, true, true, _anatomy, _wallNormal),
                "Placement must succeed when hold is within reach");
        }

        // ─── Loop step 6: Fall ───

        [Test]
        public void GameLoop_LostSupport_TransitionsToFalling()
        {
            _fsm.Fire(PlayerTrigger.GrabRequested);
            _fsm.Fire(PlayerTrigger.GrabSucceeded);
            Assert.AreEqual(PlayerStateId.GripStable, _fsm.CurrentState.Id);

            _fsm.Fire(PlayerTrigger.LostSupport);
            Assert.AreEqual(PlayerStateId.Falling, _fsm.CurrentState.Id,
                "Losing all supports must trigger Falling state");
        }

        // ─── Loop step 7-8: Land → Repeat ───

        [Test]
        public void GameLoop_Land_ReturnsToIdleGround()
        {
            _fsm.Fire(PlayerTrigger.GrabRequested);
            _fsm.Fire(PlayerTrigger.GrabSucceeded);
            _fsm.Fire(PlayerTrigger.LostSupport);
            Assert.AreEqual(PlayerStateId.Falling, _fsm.CurrentState.Id);

            _fsm.Fire(PlayerTrigger.Landed);
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id,
                "Landing must return to IdleGround (walking mode) → loop restarts");
        }

        // ─── Full cycle ───

        [Test]
        public void GameLoop_FullCycle_WalkClimbFallLandRepeat()
        {
            // Start walking
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);

            // Grab first hold → Reach
            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.AreEqual(PlayerStateId.Reach, _fsm.CurrentState.Id);

            // Successfully placed → GripStable
            _fsm.Fire(PlayerTrigger.GrabSucceeded);
            Assert.AreEqual(PlayerStateId.GripStable, _fsm.CurrentState.Id);

            // Place second hold (grab again from stable)
            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.AreEqual(PlayerStateId.Reach, _fsm.CurrentState.Id);
            _fsm.Fire(PlayerTrigger.GrabSucceeded);
            Assert.AreEqual(PlayerStateId.GripStable, _fsm.CurrentState.Id);

            // Release all → fall
            _fsm.Fire(PlayerTrigger.LostSupport);
            Assert.AreEqual(PlayerStateId.Falling, _fsm.CurrentState.Id);

            // Land → back to walking
            _fsm.Fire(PlayerTrigger.Landed);
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);

            // Can start climbing again (loop repeats)
            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.AreEqual(PlayerStateId.Reach, _fsm.CurrentState.Id);
        }
    }

    internal sealed class StubEventBus : IEventBus
    {
        public void Publish(GameEvent gameEvent) { }
        public void Subscribe(Project.Core.Events.EventType eventType, Action<GameEvent> handler) { }
        public void Unsubscribe(Project.Core.Events.EventType eventType, Action<GameEvent> handler) { }
    }
}
