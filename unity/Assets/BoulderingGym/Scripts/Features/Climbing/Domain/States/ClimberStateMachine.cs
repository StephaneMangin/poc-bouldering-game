using System.Collections.Generic;
using Project.Core.StateMachine;

namespace Project.Features.Climbing.Domain
{
    public static class ClimberStateMachine
    {
        public static StateMachine<ClimberStateId, ClimberTrigger, ClimberContext> Create(IClimberStateEffects effects)
        {
            var states = new Dictionary<ClimberStateId, IState<ClimberStateId, ClimberTrigger, ClimberContext>>
            {
                [ClimberStateId.Ground] = new GroundState(),
                [ClimberStateId.OnWall] = new OnWallState(),
                [ClimberStateId.Falling] = new FallingState(),
                [ClimberStateId.Landing] = new LandingState(),
            };
            return new StateMachine<ClimberStateId, ClimberTrigger, ClimberContext>(new ClimberContext(effects), states, ClimberStateId.Ground);
        }
    }
}
