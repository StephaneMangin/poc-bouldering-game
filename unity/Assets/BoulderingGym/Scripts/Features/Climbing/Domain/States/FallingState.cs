namespace Project.Features.Climbing.Domain
{
    public sealed class FallingState : ClimberState
    {
        public FallingState() : base(ClimberStateId.Falling)
        {
        }

        public override void OnEnter(ClimberContext context) => context.Effects.EnterFalling();

        public override ClimberStateId? HandleTrigger(ClimberTrigger trigger, ClimberContext context)
        {
            return trigger switch
            {
                ClimberTrigger.TouchedFloor => ClimberStateId.Landing,
                ClimberTrigger.Teleported => ClimberStateId.Ground,
                _ => null,
            };
        }
    }
}
