namespace Project.Features.Player.Domain.States
{
    public sealed class RunEndState : PlayerState
    {
        public RunEndState() : base(PlayerStateId.RunEnd)
        {
        }

        public override PlayerStateId? HandleTrigger(PlayerTrigger trigger, PlayerContext context)
        {
            return trigger switch
            {
                PlayerTrigger.Landed => PlayerStateId.IdleGround,
                PlayerTrigger.GrabRequested => PlayerStateId.IdleGround,
                _ => null,
            };
        }
    }
}
