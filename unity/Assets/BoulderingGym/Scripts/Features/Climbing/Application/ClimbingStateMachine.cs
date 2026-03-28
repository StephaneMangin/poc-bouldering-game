using System;
using System.Collections.Generic;
using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Domain.States;
using UnityEngine;

namespace Project.Features.Climbing.Application
{
    public sealed class ClimbingStateMachine
    {
        private readonly ClimbingContext _context;
        private readonly Dictionary<ClimbingStateId, ClimbingState> _states;

        public ClimbingStateMachine(ClimbingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _states = new Dictionary<ClimbingStateId, ClimbingState>
            {
                [ClimbingStateId.IdleGround] = new IdleGroundState(),
                [ClimbingStateId.Reach] = new ReachState(),
                [ClimbingStateId.GripStable] = new GripStableState(),
                [ClimbingStateId.Falling] = new FallingState(),
                [ClimbingStateId.RunEnd] = new RunEndState(),
            };

            CurrentState = _states[ClimbingStateId.IdleGround];
            CurrentState.OnEnter(_context);
        }

        public ClimbingState CurrentState { get; private set; }

        public ClimbingTrigger? LastTrigger { get; private set; }

        public event Action<ClimbingStateId> StateChanged;

        public bool Fire(ClimbingTrigger trigger)
        {
            LastTrigger = trigger;
            var targetStateId = CurrentState.HandleTrigger(trigger, _context);
            if (!targetStateId.HasValue)
            {
                Debug.LogWarning($"Invalid transition: {CurrentState.Id} + {trigger}");
                return false;
            }

            TransitionTo(targetStateId.Value);
            return true;
        }

        private void TransitionTo(ClimbingStateId targetStateId)
        {
            CurrentState.OnExit(_context);
            CurrentState = _states[targetStateId];
            CurrentState.OnEnter(_context);
            StateChanged?.Invoke(CurrentState.Id);
        }
    }
}
