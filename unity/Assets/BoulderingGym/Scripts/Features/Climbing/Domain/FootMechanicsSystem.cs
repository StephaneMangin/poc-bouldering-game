using Project.Core.Domain;
using Project.Features.Physics.Domain;

namespace Project.Features.Climbing.Domain
{
    /// <summary>
    /// Pure domain: selects a foot technique and exposes its weight-transfer
    /// share to legs (1 - share = remaining load on hands/body).
    /// </summary>
    /// <remarks>
    /// Selection rule (Sprint 4.0):
    /// - foothold present → Edging
    /// - no foothold + slab → Smearing
    /// - no foothold + steeper → no support (caller treats foot as inactive)
    /// </remarks>
    public sealed class FootMechanicsSystem
    {
        private readonly TuningConfig _config;

        public FootMechanicsSystem(TuningConfig config)
        {
            _config = config;
        }

        public bool CanSupport(bool hasFoothold, WallAngleCategory wallCategory)
        {
            if (hasFoothold) return true;
            return wallCategory == WallAngleCategory.Slab;
        }

        public FootTechnique SelectTechnique(bool hasFoothold, WallAngleCategory wallCategory)
        {
            if (hasFoothold) return FootTechnique.Edging;
            // Smearing only physically makes sense on slab; caller checks CanSupport first.
            return wallCategory == WallAngleCategory.Slab
                ? FootTechnique.Smearing
                : FootTechnique.Edging;
        }

        public float GetWeightShareToFoot(FootTechnique technique)
        {
            return technique switch
            {
                FootTechnique.Edging => _config.footTechniqueEdgingWeightShare,
                FootTechnique.Smearing => _config.footTechniqueSmearingWeightShare,
                _ => _config.footTechniqueEdgingWeightShare,
            };
        }
    }
}
