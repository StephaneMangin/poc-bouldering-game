namespace Project.Features.Climbing.Domain.States
{
    public sealed class ReachState : ClimbingState
    {
        public ReachState() : base(ClimbingStateId.Reach)
        {
        }

        public override ClimbingStateId? HandleTrigger(ClimbingTrigger trigger, ClimbingContext context)
        {
            return trigger switch
            {
                ClimbingTrigger.GrabSucceeded => ClimbingStateId.GripStable,
                ClimbingTrigger.GrabFailed => context.PlayerHands != null && context.PlayerHands.HasAnySupport
                    ? ClimbingStateId.GripStable
                    : ClimbingStateId.IdleGround,
                ClimbingTrigger.LostSupport => ClimbingStateId.Falling,
                _ => null,
            };
        }
    }
}
