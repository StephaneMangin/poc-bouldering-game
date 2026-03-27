namespace Project.Features.Climbing.Domain.States
{
    public sealed class RunEndState : ClimbingState
    {
        public RunEndState() : base(ClimbingStateId.RunEnd)
        {
        }

        public override ClimbingStateId? HandleTrigger(ClimbingTrigger trigger, ClimbingContext context)
        {
            return trigger switch
            {
                ClimbingTrigger.GrabRequested => ClimbingStateId.IdleGround,
                _ => null,
            };
        }
    }
}
