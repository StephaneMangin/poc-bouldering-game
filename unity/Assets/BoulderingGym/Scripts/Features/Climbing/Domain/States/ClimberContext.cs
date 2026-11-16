namespace Project.Features.Climbing.Domain
{
    /// <summary>Side effects of entering each climber state, implemented by the presentation controller.</summary>
    public interface IClimberStateEffects
    {
        void EnterGround();

        void EnterWall();

        void EnterFalling();

        void EnterLanding();
    }

    public sealed class ClimberContext
    {
        public ClimberContext(IClimberStateEffects effects)
        {
            Effects = effects;
        }

        public IClimberStateEffects Effects { get; }
    }
}
