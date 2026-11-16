using Project.Core.StateMachine;

namespace Project.Features.Climbing.Domain
{
    public abstract class ClimberState : IState<ClimberStateId, ClimberTrigger, ClimberContext>
    {
        protected ClimberState(ClimberStateId id)
        {
            Id = id;
        }

        public ClimberStateId Id { get; }

        public abstract void OnEnter(ClimberContext context);

        public virtual void OnExit(ClimberContext context)
        {
        }

        public abstract ClimberStateId? HandleTrigger(ClimberTrigger trigger, ClimberContext context);
    }
}
