using Project.Core.StateMachine;

namespace Project.Features.Player.Domain
{
    public abstract class PlayerState : IState<PlayerStateId, PlayerTrigger, PlayerContext>
    {
        protected PlayerState(PlayerStateId id)
        {
            Id = id;
        }

        public PlayerStateId Id { get; }

        public virtual void OnEnter(PlayerContext context)
        {
        }

        public virtual void OnExit(PlayerContext context)
        {
        }

        public abstract PlayerStateId? HandleTrigger(PlayerTrigger trigger, PlayerContext context);
    }
}
