using Project.Core.Events;

namespace Project.Features.Climbing.Domain.States
{
    public sealed class FallingState : ClimbingState
    {
        public FallingState() : base(ClimbingStateId.Falling)
        {
        }

        public override void OnEnter(ClimbingContext context)
        {
            context.EventBus?.Publish(new GameEvent(EventType.RunFailed));
            context.PlayerHands?.SetIkEnabled(false);
        }

        public override ClimbingStateId? HandleTrigger(ClimbingTrigger trigger, ClimbingContext context)
        {
            return trigger switch
            {
                ClimbingTrigger.Landed => ClimbingStateId.IdleGround,
                _ => null,
            };
        }
    }
}
