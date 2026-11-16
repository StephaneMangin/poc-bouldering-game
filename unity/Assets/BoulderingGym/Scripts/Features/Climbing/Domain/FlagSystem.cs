using Project.Core.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Domain
{
    /// <summary>
    /// Pure domain: evaluates the "flag" technique — a free leg extended for
    /// balance on lateral moves. Reduces COG lateral offset penalty.
    /// </summary>
    /// <remarks>
    /// Flagging requires (a) player input asking for it, and (b) at least one
    /// leg not bearing weight (free to flag). The COG offset reduction is a
    /// multiplicative factor applied to the lateral component of the offset.
    /// </remarks>
    public sealed class FlagSystem
    {
        private readonly TuningConfig _config;

        public FlagSystem(TuningConfig config)
        {
            _config = config;
        }

        public bool IsFlagging(bool flagInputHeld, bool hasFreeLeg)
        {
            return flagInputHeld && hasFreeLeg;
        }

        /// <summary>
        /// Returns the multiplier applied to the lateral COG offset when flagging.
        /// 1.0 = no flag effect; lower values reduce barn-door penalty.
        /// </summary>
        public float GetLateralOffsetMultiplier(bool isFlagging)
        {
            if (!isFlagging) return 1f;
            return Mathf.Clamp01(_config.flagLateralOffsetMultiplier);
        }
    }
}
