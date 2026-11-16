namespace Project.Features.Climbing.Domain
{
    public sealed class OnWallState : ClimberState
    {
        public OnWallState() : base(ClimberStateId.OnWall)
        {
        }

        public override void OnEnter(ClimberContext context) => context.Effects.EnterWall();

        public override ClimberStateId? HandleTrigger(ClimberTrigger trigger, ClimberContext context)
        {
            return trigger switch
            {
                ClimberTrigger.SteppedDown => ClimberStateId.Ground,
                ClimberTrigger.LostSupport => ClimberStateId.Falling,
                ClimberTrigger.Teleported => ClimberStateId.Ground,
                _ => null,
            };
        }
    }
}
