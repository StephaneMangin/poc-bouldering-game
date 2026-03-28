using Project.Core.Events;

namespace Project.Features.Player.Domain.States
{
    public sealed class FallingState : PlayerState
    {
        public FallingState() : base(PlayerStateId.Falling)
        {
        }

        public override void OnEnter(PlayerContext context)
        {
            context.EventBus?.Publish(new GameEvent(EventType.RunFailed));
            context.PlayerHands?.SetIkEnabled(false);
        }

        public override PlayerStateId? HandleTrigger(PlayerTrigger trigger, PlayerContext context)
        {
            return trigger switch
            {
                PlayerTrigger.Landed => PlayerStateId.IdleGround,
                _ => null,
            };
        }
    }
}
