using NUnit.Framework;
using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Climbing
{
    [TestFixture]
    public sealed class BodyOrientationSystemTests
    {
        private TuningConfig _config;
        private BodyOrientationSystem _system;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TuningConfig>();
            _config.hipRotationMaxDeg = 60f;
            _config.torsoTwistMaxDeg = 45f;
            _config.bodyRotationSpeedDegPerS = 120f;
            _system = new BodyOrientationSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initial_State_IsNeutral()
        {
            Assert.AreEqual(0f, _system.HipRotationDeg);
            Assert.AreEqual(0f, _system.TorsoTwistDeg);
        }

        [Test]
        public void Update_ClampsHipToMaxAfterEnoughTime()
        {
            // 120°/s × 1s = 120° step, but clamp to 60°.
            _system.Update(deltaTime: 1f, hipAxis: 1f, torsoAxis: 0f);
            Assert.AreEqual(60f, _system.HipRotationDeg, 1e-3f);
        }

        [Test]
        public void Update_ClampsTorsoToMax()
        {
            _system.Update(1f, 0f, -1f);
            Assert.AreEqual(-45f, _system.TorsoTwistDeg, 1e-3f);
        }

        [Test]
        public void Update_SlewRate_LimitsPerFrameDelta()
        {
            // 120°/s × 0.1s = 12° max step.
            _system.Update(0.1f, 1f, 0f);
            Assert.AreEqual(12f, _system.HipRotationDeg, 1e-3f);
        }

        [Test]
        public void Reset_ReturnsToNeutral()
        {
            _system.Update(1f, 1f, 1f);
            _system.Reset();
            Assert.AreEqual(0f, _system.HipRotationDeg);
            Assert.AreEqual(0f, _system.TorsoTwistDeg);
        }

        [Test]
        public void Update_ZeroDeltaTime_NoChange()
        {
            _system.Update(0f, 1f, 1f);
            Assert.AreEqual(0f, _system.HipRotationDeg);
        }
    }
}
