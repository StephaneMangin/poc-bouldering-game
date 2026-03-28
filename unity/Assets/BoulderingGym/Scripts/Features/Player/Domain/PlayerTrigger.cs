namespace Project.Features.Player.Domain
{
    public enum PlayerTrigger
    {
        GrabRequested,
        GrabSucceeded,
        GrabFailed,
        ReleaseRequested,
        LostSupport,
        Landed
    }
}
