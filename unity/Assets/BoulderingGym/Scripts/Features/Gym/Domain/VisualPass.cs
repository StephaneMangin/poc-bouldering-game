namespace Project.Features.Gym.Domain
{
    /// <summary>
    /// Level design pass the gym is rendered with: whitebox (plain blocks and placeholder holds),
    /// greybox (final shapes, grey walls, colored holds) or art (panel colors and dressing).
    /// </summary>
    public enum VisualPass
    {
        Whitebox,
        Greybox,
        Art,
    }
}
