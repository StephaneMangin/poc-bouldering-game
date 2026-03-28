namespace Project.Features.Climbing.Domain.States
{
    public sealed class IdleGroundState : ClimbingState
    {
        public IdleGroundState() : base(ClimbingStateId.IdleGround)
        {
        }

        public override ClimbingStateId? HandleTrigger(ClimbingTrigger trigger, ClimbingContext context)
        {
            return trigger switch
            {
                ClimbingTrigger.GrabRequested => ClimbingStateId.Reach,
                _ => null,
            };
        }
    }
}
