using System.Collections.Generic;
using Project.Core.StateMachine;
using Project.Features.Player.Domain;
using Project.Features.Player.Domain.States;

namespace Project.Features.Player.Application
{
    public sealed class PlayerStateMachine
        : StateMachine<PlayerStateId, PlayerTrigger, PlayerContext>
    {
        public PlayerStateMachine(PlayerContext context)
            : base(
                context,
                new Dictionary<PlayerStateId, IState<PlayerStateId, PlayerTrigger, PlayerContext>>
                {
                    [PlayerStateId.IdleGround] = new IdleGroundState(),
                    [PlayerStateId.Reach] = new ReachState(),
                    [PlayerStateId.GripStable] = new GripStableState(),
                    [PlayerStateId.Falling] = new FallingState(),
                    [PlayerStateId.RunEnd] = new RunEndState(),
                },
                PlayerStateId.IdleGround)
        {
        }
    }
}
