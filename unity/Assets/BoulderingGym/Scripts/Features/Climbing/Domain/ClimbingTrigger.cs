namespace Project.Features.Climbing.Domain
{
    public enum ClimbingTrigger
    {
        GrabRequested,
        GrabSucceeded,
        GrabFailed,
        ReleaseRequested,
        LostSupport,
        Landed
    }
}
