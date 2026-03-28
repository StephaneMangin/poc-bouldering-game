namespace Project.Features.Player.Domain.States
{
    public sealed class ReachState : PlayerState
    {
        public ReachState() : base(PlayerStateId.Reach)
        {
        }

        public override PlayerStateId? HandleTrigger(PlayerTrigger trigger, PlayerContext context)
        {
            return trigger switch
            {
                PlayerTrigger.GrabSucceeded => PlayerStateId.GripStable,
                PlayerTrigger.GrabFailed => context.PlayerHands != null && context.PlayerHands.HasAnySupport
                    ? PlayerStateId.GripStable
                    : PlayerStateId.IdleGround,
                PlayerTrigger.LostSupport => PlayerStateId.Falling,
                _ => null,
            };
        }
    }
}
