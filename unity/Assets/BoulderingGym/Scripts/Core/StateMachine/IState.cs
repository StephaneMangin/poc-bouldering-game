using System;

namespace Project.Core.StateMachine
{
    public interface IState<TStateId, TTrigger, TContext>
        where TStateId : struct, Enum
        where TTrigger : struct, Enum
    {
        TStateId Id { get; }
        void OnEnter(TContext context);
        void OnExit(TContext context);
        TStateId? HandleTrigger(TTrigger trigger, TContext context);
    }
}
