using Project.Core.Events;

namespace Project.Features.Player.Domain.States
{
    public sealed class GripStableState : PlayerState
    {
        public GripStableState() : base(PlayerStateId.GripStable)
        {
        }

        public override void OnEnter(PlayerContext context)
        {
            context.EventBus?.Publish(new GameEvent(EventType.HoldGrabbed));
            context.PlayerHands?.SetIkEnabled(true);
        }

        public override void OnExit(PlayerContext context)
        {
            context.PlayerHands?.SetIkEnabled(false);
        }

        public override PlayerStateId? HandleTrigger(PlayerTrigger trigger, PlayerContext context)
        {
            return trigger switch
            {
                PlayerTrigger.GrabRequested => PlayerStateId.GripStable,
                PlayerTrigger.GrabSucceeded => PlayerStateId.GripStable,
                PlayerTrigger.GrabFailed => PlayerStateId.GripStable,
                PlayerTrigger.ReleaseRequested => context.PlayerHands != null && context.PlayerHands.HasCrossLimbSupport
                    ? PlayerStateId.GripStable
                    : PlayerStateId.Reach,
                PlayerTrigger.LostSupport => PlayerStateId.Falling,
                _ => null,
            };
        }
    }
}
