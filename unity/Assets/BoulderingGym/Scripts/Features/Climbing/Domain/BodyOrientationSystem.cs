using Project.Core.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Domain
{
    /// <summary>
    /// Pure domain: tracks hip rotation (±60° default) and torso twist (±45°)
    /// applied from player input. Rotation speed is capped (not instantaneous).
    /// </summary>
    public sealed class BodyOrientationSystem
    {
        private readonly TuningConfig _config;
        private float _hipDeg;
        private float _torsoDeg;

        public BodyOrientationSystem(TuningConfig config)
        {
            _config = config;
        }

        public float HipRotationDeg => _hipDeg;
        public float TorsoTwistDeg => _torsoDeg;

        public void Update(float deltaTime, float hipAxis, float torsoAxis)
        {
            if (deltaTime <= 0f) return;

            var step = _config.bodyRotationSpeedDegPerS * deltaTime;
            var hipTarget = Mathf.Clamp(hipAxis, -1f, 1f) * _config.hipRotationMaxDeg;
            var torsoTarget = Mathf.Clamp(torsoAxis, -1f, 1f) * _config.torsoTwistMaxDeg;

            _hipDeg = Mathf.MoveTowards(_hipDeg, hipTarget, step);
            _torsoDeg = Mathf.MoveTowards(_torsoDeg, torsoTarget, step);
        }

        public void Reset()
        {
            _hipDeg = 0f;
            _torsoDeg = 0f;
        }
    }
}
