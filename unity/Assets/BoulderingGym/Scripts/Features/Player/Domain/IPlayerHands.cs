namespace Project.Features.Player.Domain
{
    /// <summary>
    /// Abstraction for limb support queries used by FSM state guards.
    /// Allows domain states to check grip status without depending on MonoBehaviour.
    /// </summary>
    public interface IPlayerHands
    {
        bool HasAnySupport { get; }

        bool HasCrossLimbSupport { get; }

        void SetIkEnabled(bool enabled);
    }
}
