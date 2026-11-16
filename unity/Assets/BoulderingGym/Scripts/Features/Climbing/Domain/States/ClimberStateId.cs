namespace Project.Features.Climbing.Domain
{
    public enum ClimberStateId
    {
        Ground,
        OnWall,
        Falling,
        Landing,
    }

    public enum ClimberTrigger
    {
        /// <summary>A first limb went on a hold from the ground.</summary>
        StartedClimbing,

        /// <summary>The hands let go while the feet still stand on the floor.</summary>
        SteppedDown,

        /// <summary>No hand holds anymore (released, slipped or dropped) with the feet off the floor.</summary>
        LostSupport,

        TouchedFloor,
        LandingFinished,

        /// <summary>Moved to a problem start or the spawn point by the game.</summary>
        Teleported,
    }
}
