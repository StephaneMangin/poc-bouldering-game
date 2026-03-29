using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Resources.Domain
{
    /// <summary>
    /// Pure domain: tracks per-arm fatigue that builds while holding
    /// and recovers when the arm is free. Fatigue rate depends on grip type,
    /// weight on arm, and wall angle multiplier.
    /// </summary>
    public sealed class LocalFatigueSystem
    {
        private readonly TuningConfig _config;
        private float _leftFatigue;
        private float _rightFatigue;

        public LocalFatigueSystem(TuningConfig config)
        {
            _config = config;
        }

        public float LeftFatigue => _leftFatigue;
        public float RightFatigue => _rightFatigue;
        public float LeftRatio => _leftFatigue / 100f;
        public float RightRatio => _rightFatigue / 100f;

        public FatigueLevel GetLevel(float fatigue)
        {
            if (fatigue >= _config.fatigueFailureThreshold) return FatigueLevel.Failure;
            if (fatigue >= _config.fatigueCriticalThreshold) return FatigueLevel.Critical;
            if (fatigue >= _config.fatigueWarningThreshold) return FatigueLevel.Warning;
            return FatigueLevel.Normal;
        }

        public void Update(
            float deltaTime,
            GripType leftGripType, float leftWeight, bool leftActive,
            GripType rightGripType, float rightWeight, bool rightActive,
            float wallAngleFatigueMult)
        {
            if (leftActive)
            {
                var mult = GetGripTypeFatigueMult(leftGripType);
                var gain = _config.fatigueGainBasePerS * mult * leftWeight * wallAngleFatigueMult;
                _leftFatigue = Mathf.Min(100f, _leftFatigue + gain * deltaTime);
            }
            else
            {
                _leftFatigue = Mathf.Max(0f, _leftFatigue - _config.fatigueRecoverRestPerS * deltaTime);
            }

            if (rightActive)
            {
                var mult = GetGripTypeFatigueMult(rightGripType);
                var gain = _config.fatigueGainBasePerS * mult * rightWeight * wallAngleFatigueMult;
                _rightFatigue = Mathf.Min(100f, _rightFatigue + gain * deltaTime);
            }
            else
            {
                _rightFatigue = Mathf.Max(0f, _rightFatigue - _config.fatigueRecoverRestPerS * deltaTime);
            }
        }

        public float GetGripTypeFatigueMult(GripType type)
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

        public void Reset()
        {
            _leftFatigue = 0f;
            _rightFatigue = 0f;
        }
    }
}
