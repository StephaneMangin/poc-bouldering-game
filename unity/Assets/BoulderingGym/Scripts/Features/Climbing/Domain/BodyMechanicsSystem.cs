using Project.Core.Domain;

namespace Project.Features.Climbing.Domain
{
    /// <summary>
    /// Pure domain: chooses the cheapest compatible grip type on a hold,
    /// weighing both fatigue and adherence drain costs from <see cref="TuningConfig"/>.
    /// </summary>
    /// <remarks>
    /// Selection minimises a weighted sum (fatigue + drain) — so a hold
    /// declared as both Jug and Crimp will be climbed as Jug unless tuning
    /// inverts the relation. Falls back to <paramref name="fallback"/> when
    /// no compatible grip is declared.
    /// </remarks>
    public sealed class BodyMechanicsSystem
    {
        private readonly TuningConfig _config;
        private readonly float _fatigueWeight;
        private readonly float _drainWeight;

        public BodyMechanicsSystem(TuningConfig config, float fatigueWeight = 1f, float drainWeight = 1f)
        {
            _config = config;
            _fatigueWeight = fatigueWeight;
            _drainWeight = drainWeight;
        }

        public GripType SelectBestGrip(GripType[] allowed, GripType fallback = GripType.Jug)
        {
            if (allowed == null || allowed.Length == 0)
            {
                return fallback;
            }

            var best = allowed[0];
            var bestCost = ComputeCost(best);
            for (var i = 1; i < allowed.Length; i++)
            {
                var cost = ComputeCost(allowed[i]);
                if (cost < bestCost)
                {
                    best = allowed[i];
                    bestCost = cost;
                }
            }
            return best;
        }

        private float ComputeCost(GripType type)
        {
            return _fatigueWeight * GetFatigueMult(type) + _drainWeight * GetDrainMult(type);
        }

        private float GetFatigueMult(GripType type)
        {
            return type switch
            {
                GripType.Jug => _config.gripTypeFatigueMultJug,
                GripType.Open => _config.gripTypeFatigueMultOpen,
                GripType.Pinch => _config.gripTypeFatigueMultPinch,
                GripType.Crimp => _config.gripTypeFatigueMultCrimp,
                GripType.Gaston => _config.gripTypeFatigueMultGaston,
                GripType.Undercling => _config.gripTypeFatigueMultUndercling,
                GripType.Sloper => _config.gripTypeFatigueMultSloper,
                _ => 1f,
            };
        }

        private float GetDrainMult(GripType type)
        {
            return type switch
            {
                GripType.Jug => _config.gripTypeDrainMultJug,
                GripType.Open => _config.gripTypeDrainMultOpen,
                GripType.Pinch => _config.gripTypeDrainMultPinch,
                GripType.Crimp => _config.gripTypeDrainMultCrimp,
                GripType.Gaston => _config.gripTypeDrainMultGaston,
                GripType.Undercling => _config.gripTypeDrainMultUndercling,
                GripType.Sloper => _config.gripTypeDrainMultSloper,
                _ => 1f,
            };
        }
    }
}
