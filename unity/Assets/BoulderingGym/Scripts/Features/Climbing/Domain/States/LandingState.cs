namespace Project.Features.Climbing.Domain
{
    public sealed class LandingState : ClimberState
    {
        public LandingState() : base(ClimberStateId.Landing)
        {
        }

        public override void OnEnter(ClimberContext context) => context.Effects.EnterLanding();

        public override ClimberStateId? HandleTrigger(ClimberTrigger trigger, ClimberContext context)
        {
            return trigger switch
            {
                ClimberTrigger.LandingFinished => ClimberStateId.Ground,
                ClimberTrigger.Teleported => ClimberStateId.Ground,
                _ => null,
            };
        }
    }
}
