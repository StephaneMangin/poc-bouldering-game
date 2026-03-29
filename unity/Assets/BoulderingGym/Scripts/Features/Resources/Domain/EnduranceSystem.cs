using Project.Core.Domain;
using UnityEngine;

namespace Project.Features.Resources.Domain
{
    /// <summary>
    /// Pure domain: endurance depletes on climbing actions, regens at rest.
    /// Action costs are modified by COG offset penalty and wall angle multiplier.
    /// </summary>
    public sealed class EnduranceSystem
    {
        private readonly TuningConfig _config;
        private float _current;

        public EnduranceSystem(TuningConfig config)
        {
            _config = config;
            _current = config.enduranceMax;
        }

        public float Current => _current;
        public float Max => _config.enduranceMax;
        public float Ratio => Max > 0f ? _current / Max : 0f;

        public void ApplyActionCost(ClimbingAction action, float cogOffsetPenalty, float wallAngleEnduranceMult)
        {
            var baseCost = action switch
            {
                ClimbingAction.Grab => _config.enduranceCostGrab,
                ClimbingAction.Reposition => _config.enduranceCostReposition,
                ClimbingAction.Dyno => _config.enduranceCostDyno,
                _ => 0f,
            };
            var effectiveCost = baseCost * (1f + cogOffsetPenalty) * wallAngleEnduranceMult;
            _current = Mathf.Max(0f, _current - effectiveCost);
        }

        public void UpdateRegen(float deltaTime)
        {
            _current = Mathf.Min(Max, _current + _config.enduranceRegenRestPerS * deltaTime);
        }

        public void Reset()
        {
            _current = Max;
        }
    }
}
