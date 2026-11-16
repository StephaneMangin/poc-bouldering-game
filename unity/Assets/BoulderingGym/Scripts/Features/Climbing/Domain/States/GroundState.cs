namespace Project.Features.Climbing.Domain
{
    public sealed class GroundState : ClimberState
    {
        public GroundState() : base(ClimberStateId.Ground)
        {
        }

        public override void OnEnter(ClimberContext context) => context.Effects.EnterGround();

        public override ClimberStateId? HandleTrigger(ClimberTrigger trigger, ClimberContext context)
        {
            return trigger switch
            {
                ClimberTrigger.StartedClimbing => ClimberStateId.OnWall,
                ClimberTrigger.Teleported => ClimberStateId.Ground,
                _ => null,
            };
        }
    }
}
