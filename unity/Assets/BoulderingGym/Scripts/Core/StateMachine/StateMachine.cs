using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.StateMachine
{
    public class StateMachine<TStateId, TTrigger, TContext>
        where TStateId : struct, Enum
        where TTrigger : struct, Enum
    {
        private readonly TContext _context;
        private readonly Dictionary<TStateId, IState<TStateId, TTrigger, TContext>> _states;

        public StateMachine(
            TContext context,
            Dictionary<TStateId, IState<TStateId, TTrigger, TContext>> states,
            TStateId initialState)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _states = states ?? throw new ArgumentNullException(nameof(states));

            CurrentState = _states[initialState];
            CurrentState.OnEnter(_context);
        }

        public IState<TStateId, TTrigger, TContext> CurrentState { get; private set; }

        public TTrigger? LastTrigger { get; private set; }

        public event Action<TStateId> StateChanged;

        public bool Fire(TTrigger trigger)
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

        private void TransitionTo(TStateId targetStateId)
        {
            CurrentState.OnExit(_context);
            CurrentState = _states[targetStateId];
            CurrentState.OnEnter(_context);
            StateChanged?.Invoke(CurrentState.Id);
        }
    }
}
