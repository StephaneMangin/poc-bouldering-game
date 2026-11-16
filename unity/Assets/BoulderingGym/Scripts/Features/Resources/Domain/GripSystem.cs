using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Resources.Domain
{
    /// <summary>
    /// Pure domain: grip (adherence) drains continuously while holding,
    /// restored only by chalk. Affected by hold texture, hand weight, wall angle.
    /// </summary>
    public sealed class GripSystem
    {
        private readonly TuningConfig _config;
        private float _current;
        private float _chalkCooldownRemaining;

        public GripSystem(TuningConfig config)
        {
            _config = config;
            _current = config.adherenceMax;
        }

        public float Current => _current;
        public float Max => _config.adherenceMax;
        public float Ratio => Max > 0f ? _current / Max : 0f;
        public bool IsChalkReady => _chalkCooldownRemaining <= 0f;
        public float ChalkCooldownRemaining => _chalkCooldownRemaining;
        public bool IsMicroSlipping => _current > 0f && _current < _config.adherenceMicroSlipThreshold;

        public void UpdateDrain(float deltaTime, float weightOnHands, float gripDrainMult, float gripTexture)
        {
            var effectiveTexture = gripTexture > 0f ? gripTexture : 1f;
            var effectiveWeight = Mathf.Max(0.1f, weightOnHands);
            var drain = _config.adherenceLossBasePerS * effectiveTexture * effectiveWeight * gripDrainMult;
            _current = Mathf.Max(0f, _current - drain * deltaTime);
            _chalkCooldownRemaining = Mathf.Max(0f, _chalkCooldownRemaining - deltaTime);
        }

        public void UpdateCooldown(float deltaTime)
        {
            _chalkCooldownRemaining = Mathf.Max(0f, _chalkCooldownRemaining - deltaTime);
        }

        public void ApplyBurst()
        {
            _current = Mathf.Max(0f, _current - _config.adherenceLossBurst);
        }

        public bool TryUseChalk()
        {
            if (_chalkCooldownRemaining > 0f) return false;
            _current = Mathf.Min(Max, _current + _config.chalkRestoreValue);
            _chalkCooldownRemaining = _config.chalkCooldownSeconds;
            return true;
        }

        public void Reset()
        {
            _current = Max;
            _chalkCooldownRemaining = 0f;
        }

        public float GetGripTypeDrainMult(GripType type)
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
