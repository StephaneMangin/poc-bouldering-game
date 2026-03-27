namespace Project.Features.Climbing.Domain
{
    public abstract class ClimbingState
    {
        protected ClimbingState(ClimbingStateId id)
        {
            Id = id;
        }

        public ClimbingStateId Id { get; }

        public virtual void OnEnter(ClimbingContext context)
        {
        }

        public virtual void OnExit(ClimbingContext context)
        {
        }

        public abstract ClimbingStateId? HandleTrigger(ClimbingTrigger trigger, ClimbingContext context);
    }
}
