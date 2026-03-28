using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Project.Core.Events;
using Project.Core.StateMachine;
using Project.Features.Player.Application;
using Project.Features.Player.Domain;
using Project.Features.Player.Domain.States;

namespace Project.Tests.EditMode.Features.Player
{
    /// <summary>
    /// Pure unit tests for the generic StateMachine and PlayerStateMachine.
    /// No scene, no MonoBehaviour — tests FSM logic exhaustively.
    /// </summary>
    [TestFixture]
    public sealed class StateMachineTests
    {
        private PlayerContext _context;
        private PlayerStateMachine _fsm;

        [SetUp]
        public void SetUp()
        {
            _context = new PlayerContext
            {
                EventBus = new StubEventBus(),
            };
            _fsm = new PlayerStateMachine(_context);
        }

        // ─── Initial state ───

        [Test]
        public void InitialState_IsIdleGround()
        {
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
        }

        [Test]
        public void InitialState_LastTriggerIsNull()
        {
            Assert.IsNull(_fsm.LastTrigger);
        }

        // ─── Valid transitions ───

        [Test]
        public void IdleGround_GrabRequested_TransitionsToReach()
        {
            Assert.IsTrue(_fsm.Fire(PlayerTrigger.GrabRequested));
            Assert.AreEqual(PlayerStateId.Reach, _fsm.CurrentState.Id);
        }

        [Test]
        public void Reach_GrabSucceeded_TransitionsToGripStable()
        {
            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.IsTrue(_fsm.Fire(PlayerTrigger.GrabSucceeded));
            Assert.AreEqual(PlayerStateId.GripStable, _fsm.CurrentState.Id);
        }

        [Test]
        public void Reach_GrabFailed_NoSupport_TransitionsToIdleGround()
        {
            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.IsTrue(_fsm.Fire(PlayerTrigger.GrabFailed));
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
        }

        [Test]
        public void Reach_LostSupport_TransitionsToFalling()
        {
            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.IsTrue(_fsm.Fire(PlayerTrigger.LostSupport));
            Assert.AreEqual(PlayerStateId.Falling, _fsm.CurrentState.Id);
        }

        [Test]
        public void GripStable_LostSupport_TransitionsToFalling()
        {
            NavigateTo(PlayerStateId.GripStable);
            Assert.IsTrue(_fsm.Fire(PlayerTrigger.LostSupport));
            Assert.AreEqual(PlayerStateId.Falling, _fsm.CurrentState.Id);
        }

        [Test]
        public void GripStable_GrabRequested_StaysInGripStable()
        {
            NavigateTo(PlayerStateId.GripStable);
            Assert.IsTrue(_fsm.Fire(PlayerTrigger.GrabRequested));
            Assert.AreEqual(PlayerStateId.GripStable, _fsm.CurrentState.Id);
        }

        [Test]
        public void GripStable_ReleaseRequested_NoSupport_TransitionsToReach()
        {
            NavigateTo(PlayerStateId.GripStable);
            Assert.IsTrue(_fsm.Fire(PlayerTrigger.ReleaseRequested));
            Assert.AreEqual(PlayerStateId.Reach, _fsm.CurrentState.Id);
        }

        [Test]
        public void Falling_Landed_TransitionsToIdleGround()
        {
            NavigateTo(PlayerStateId.Falling);
            Assert.IsTrue(_fsm.Fire(PlayerTrigger.Landed));
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
        }

        // ─── Invalid transitions ───

        [Test]
        public void IdleGround_Landed_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.Fire(PlayerTrigger.Landed));
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
        }

        [Test]
        public void IdleGround_LostSupport_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.Fire(PlayerTrigger.LostSupport));
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
        }

        [Test]
        public void IdleGround_GrabSucceeded_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.Fire(PlayerTrigger.GrabSucceeded));
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
        }

        [Test]
        public void IdleGround_GrabFailed_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.Fire(PlayerTrigger.GrabFailed));
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
        }

        [Test]
        public void IdleGround_ReleaseRequested_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.Fire(PlayerTrigger.ReleaseRequested));
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
        }

        [Test]
        public void Falling_GrabRequested_ReturnsFalse()
        {
            NavigateTo(PlayerStateId.Falling);
            Assert.IsFalse(_fsm.Fire(PlayerTrigger.GrabRequested));
            Assert.AreEqual(PlayerStateId.Falling, _fsm.CurrentState.Id);
        }

        [Test]
        public void Falling_GrabSucceeded_ReturnsFalse()
        {
            NavigateTo(PlayerStateId.Falling);
            Assert.IsFalse(_fsm.Fire(PlayerTrigger.GrabSucceeded));
            Assert.AreEqual(PlayerStateId.Falling, _fsm.CurrentState.Id);
        }

        [Test]
        public void Falling_ReleaseRequested_ReturnsFalse()
        {
            NavigateTo(PlayerStateId.Falling);
            Assert.IsFalse(_fsm.Fire(PlayerTrigger.ReleaseRequested));
            Assert.AreEqual(PlayerStateId.Falling, _fsm.CurrentState.Id);
        }

        [Test]
        public void Reach_Landed_ReturnsFalse()
        {
            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.IsFalse(_fsm.Fire(PlayerTrigger.Landed));
            Assert.AreEqual(PlayerStateId.Reach, _fsm.CurrentState.Id);
        }

        // ─── StateChanged event ───

        [Test]
        public void Fire_ValidTransition_RaisesStateChanged()
        {
            PlayerStateId? received = null;
            _fsm.StateChanged += id => received = id;

            _fsm.Fire(PlayerTrigger.GrabRequested);

            Assert.AreEqual(PlayerStateId.Reach, received);
        }

        [Test]
        public void Fire_InvalidTransition_DoesNotRaiseStateChanged()
        {
            var eventFired = false;
            _fsm.StateChanged += _ => eventFired = true;

            _fsm.Fire(PlayerTrigger.Landed);

            Assert.IsFalse(eventFired, "StateChanged should not fire for invalid transitions");
        }

        [Test]
        public void Fire_MultipleTransitions_RaisesForEach()
        {
            var events = new List<PlayerStateId>();
            _fsm.StateChanged += id => events.Add(id);

            _fsm.Fire(PlayerTrigger.GrabRequested);  // → Reach
            _fsm.Fire(PlayerTrigger.GrabSucceeded);   // → GripStable

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual(PlayerStateId.Reach, events[0]);
            Assert.AreEqual(PlayerStateId.GripStable, events[1]);
        }

        // ─── LastTrigger tracking ───

        [Test]
        public void Fire_ValidTransition_UpdatesLastTrigger()
        {
            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.AreEqual(PlayerTrigger.GrabRequested, _fsm.LastTrigger);
        }

        [Test]
        public void Fire_InvalidTransition_StillUpdatesLastTrigger()
        {
            _fsm.Fire(PlayerTrigger.Landed);
            Assert.AreEqual(PlayerTrigger.Landed, _fsm.LastTrigger);
        }

        // ─── Full transition matrix ───

        [Test]
        public void FullCycle_IdleGround_Reach_GripStable_Falling_IdleGround()
        {
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);

            _fsm.Fire(PlayerTrigger.GrabRequested);
            Assert.AreEqual(PlayerStateId.Reach, _fsm.CurrentState.Id);

            _fsm.Fire(PlayerTrigger.GrabSucceeded);
            Assert.AreEqual(PlayerStateId.GripStable, _fsm.CurrentState.Id);

            _fsm.Fire(PlayerTrigger.LostSupport);
            Assert.AreEqual(PlayerStateId.Falling, _fsm.CurrentState.Id);

            _fsm.Fire(PlayerTrigger.Landed);
            Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
        }

        [Test]
        public void FullCycle_CanRepeat()
        {
            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(_fsm.Fire(PlayerTrigger.GrabRequested), $"Cycle {i}: GrabRequested");
                Assert.IsTrue(_fsm.Fire(PlayerTrigger.GrabSucceeded), $"Cycle {i}: GrabSucceeded");
                Assert.IsTrue(_fsm.Fire(PlayerTrigger.LostSupport), $"Cycle {i}: LostSupport");
                Assert.IsTrue(_fsm.Fire(PlayerTrigger.Landed), $"Cycle {i}: Landed");
                Assert.AreEqual(PlayerStateId.IdleGround, _fsm.CurrentState.Id);
            }
        }

        // ─── Constructor validation ───

        [Test]
        public void Constructor_NullContext_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StateMachine<PlayerStateId, PlayerTrigger, PlayerContext>(
                    null,
                    new Dictionary<PlayerStateId, IState<PlayerStateId, PlayerTrigger, PlayerContext>>
                    {
                        [PlayerStateId.IdleGround] = new IdleGroundState(),
                    },
                    PlayerStateId.IdleGround));
        }

        [Test]
        public void Constructor_NullStates_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StateMachine<PlayerStateId, PlayerTrigger, PlayerContext>(
                    new PlayerContext(),
                    null,
                    PlayerStateId.IdleGround));
        }

        // ─── GripStable OnEnter publishes HoldGrabbed ───

        [Test]
        public void GripStable_OnEnter_PublishesHoldGrabbed()
        {
            var stub = (StubEventBus)_context.EventBus;

            // Oracle: entering GripStable should publish exactly 1 HoldGrabbed event
            const int expectedCount = 1;
            const EventType expectedType = EventType.HoldGrabbed;

            NavigateTo(PlayerStateId.GripStable);

            var matchingEvents = stub.PublishedEvents.Where(e => e.Type == expectedType).ToList();
            Assert.AreEqual(expectedCount, matchingEvents.Count,
                $"GripStable OnEnter should publish exactly {expectedCount} {expectedType} event, got {matchingEvents.Count}");
        }

        [Test]
        public void Falling_OnEnter_PublishesRunFailed()
        {
            var stub = (StubEventBus)_context.EventBus;

            // Oracle: entering Falling should publish exactly 1 RunFailed event
            const int expectedCount = 1;
            const EventType expectedType = EventType.RunFailed;

            NavigateTo(PlayerStateId.Falling);

            var matchingEvents = stub.PublishedEvents.Where(e => e.Type == expectedType).ToList();
            Assert.AreEqual(expectedCount, matchingEvents.Count,
                $"Falling OnEnter should publish exactly {expectedCount} {expectedType} event, got {matchingEvents.Count}");
        }

        // ─── Helpers ───

        private void NavigateTo(PlayerStateId target)
        {
            switch (target)
            {
                case PlayerStateId.Reach:
                    _fsm.Fire(PlayerTrigger.GrabRequested);
                    break;
                case PlayerStateId.GripStable:
                    _fsm.Fire(PlayerTrigger.GrabRequested);
                    _fsm.Fire(PlayerTrigger.GrabSucceeded);
                    break;
                case PlayerStateId.Falling:
                    _fsm.Fire(PlayerTrigger.GrabRequested);
                    _fsm.Fire(PlayerTrigger.LostSupport);
                    break;
                case PlayerStateId.RunEnd:
                    // No direct path from IdleGround to RunEnd in current FSM
                    break;
            }

            Assert.AreEqual(target, _fsm.CurrentState.Id,
                $"Failed to navigate to {target}");
        }

        /// <summary>
        /// Minimal IEventBus stub that records published events.
        /// </summary>
        private sealed class StubEventBus : IEventBus
        {
            public readonly List<GameEvent> PublishedEvents = new();

            public void Subscribe(EventType eventType, Action<GameEvent> handler) { }
            public void Unsubscribe(EventType eventType, Action<GameEvent> handler) { }
            public void Publish(GameEvent gameEvent) => PublishedEvents.Add(gameEvent);
        }
    }
}
