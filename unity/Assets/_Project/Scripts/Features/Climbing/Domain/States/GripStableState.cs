using Project.Core.Events;

namespace Project.Features.Climbing.Domain.States
{
    public sealed class GripStableState : ClimbingState
    {
        public GripStableState() : base(ClimbingStateId.GripStable)
        {
        }

        public override void OnEnter(ClimbingContext context)
        {
            context.EventBus?.Publish(new GameEvent(EventType.HoldGrabbed));
            context.PlayerHands?.SetIkEnabled(true);
        }

        public override void OnExit(ClimbingContext context)
        {
            context.PlayerHands?.SetIkEnabled(false);
        }

        public override ClimbingStateId? HandleTrigger(ClimbingTrigger trigger, ClimbingContext context)
        {
            return trigger switch
            {
                ClimbingTrigger.GrabRequested => ClimbingStateId.GripStable,
                ClimbingTrigger.GrabSucceeded => ClimbingStateId.GripStable,
                ClimbingTrigger.GrabFailed => ClimbingStateId.GripStable,
                ClimbingTrigger.ReleaseRequested => context.PlayerHands != null && context.PlayerHands.HasAnySupport
                    ? ClimbingStateId.GripStable
                    : ClimbingStateId.Reach,
                ClimbingTrigger.LostSupport => ClimbingStateId.Falling,
                _ => null,
            };
        }
    }
}
