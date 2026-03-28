namespace Project.Features.Player.Domain.States
{
    public sealed class IdleGroundState : PlayerState
    {
        public IdleGroundState() : base(PlayerStateId.IdleGround)
        {
        }

        public override PlayerStateId? HandleTrigger(PlayerTrigger trigger, PlayerContext context)
        {
            return trigger switch
            {
                PlayerTrigger.GrabRequested => PlayerStateId.Reach,
                _ => null,
            };
        }
    }
}
