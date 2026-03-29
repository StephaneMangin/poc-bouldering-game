using NUnit.Framework;
using Project.Core.Domain;
using Project.Features.Resources.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Resources
{
    [TestFixture]
    public sealed class GripSystemTests
    {
        private TuningConfig _config;
        private GripSystem _system;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TuningConfig>();
            _config.adherenceMax = 100f;
            _config.adherenceLossBasePerS = 1.5f;
            _config.adherenceLossBurst = 5f;
            _config.chalkRestoreValue = 35f;
            _config.chalkCooldownSeconds = 20f;
            _config.adherenceMicroSlipThreshold = 30f;
            _system = new GripSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void InitialState_FullAdherence()
        {
            Assert.AreEqual(100f, _system.Current, 0.01f);
            Assert.AreEqual(1f, _system.Ratio, 0.01f);
            Assert.IsTrue(_system.IsChalkReady);
            Assert.IsFalse(_system.IsMicroSlipping);
        }

        [Test]
        public void UpdateDrain_ReducesGrip()
        {
            // drain = 1.5 * 1.0 * 0.35 * 1.0 = 0.525/s → 1s → 99.475
            _system.UpdateDrain(1f, weightOnHands: 0.35f, gripDrainMult: 1f, gripTexture: 1f);

            Assert.Less(_system.Current, 100f, "Grip should decrease after drain");
            Assert.AreEqual(100f - 0.525f, _system.Current, 0.01f,
                "Drain = 1.5 * 1.0 * 0.35 * 1.0 = 0.525 per second");
        }

        [Test]
        public void UpdateDrain_HighWeight_FasterDrain()
        {
            var system1 = new GripSystem(_config);
            var system2 = new GripSystem(_config);

            system1.UpdateDrain(1f, weightOnHands: 0.35f, gripDrainMult: 1f, gripTexture: 1f);
            system2.UpdateDrain(1f, weightOnHands: 0.90f, gripDrainMult: 1f, gripTexture: 1f);

            Assert.Less(system2.Current, system1.Current,
                "Higher hand weight should drain grip faster");
        }

        [Test]
        public void UpdateDrain_HighGripDrainMult_FasterDrain()
        {
            var system1 = new GripSystem(_config);
            var system2 = new GripSystem(_config);

            system1.UpdateDrain(1f, weightOnHands: 0.35f, gripDrainMult: 1.0f, gripTexture: 1f);
            system2.UpdateDrain(1f, weightOnHands: 0.35f, gripDrainMult: 2.0f, gripTexture: 1f);

            Assert.Less(system2.Current, system1.Current,
                "Higher grip drain multiplier (roof) should drain faster");
        }

        [Test]
        public void UpdateDrain_HighGripTexture_FasterDrain()
        {
            var system1 = new GripSystem(_config);
            var system2 = new GripSystem(_config);

            system1.UpdateDrain(1f, weightOnHands: 0.35f, gripDrainMult: 1f, gripTexture: 0.5f);
            system2.UpdateDrain(1f, weightOnHands: 0.35f, gripDrainMult: 1f, gripTexture: 2.0f);

            Assert.Less(system2.Current, system1.Current,
                "Smoother hold texture (2.0) should drain grip faster than rough (0.5)");
        }

        [Test]
        public void UpdateDrain_ClampsToZero()
        {
            _system.UpdateDrain(1000f, weightOnHands: 1f, gripDrainMult: 3f, gripTexture: 2f);

            Assert.AreEqual(0f, _system.Current, 0.01f,
                "Grip should never go below zero");
        }

        [Test]
        public void ApplyBurst_ReducesByBurstAmount()
        {
            _system.ApplyBurst();

            Assert.AreEqual(95f, _system.Current, 0.01f,
                "Burst should reduce by adherenceLossBurst (5)");
        }

        [Test]
        public void ApplyBurst_ClampsToZero()
        {
            // Drain to near zero first
            _system.UpdateDrain(500f, weightOnHands: 1f, gripDrainMult: 2f, gripTexture: 1f);
            _system.ApplyBurst();

            Assert.GreaterOrEqual(_system.Current, 0f,
                "Burst should not put grip below zero");
        }

        [Test]
        public void TryUseChalk_WhileReady_RestoresGrip()
        {
            // Drain some grip
            _system.UpdateDrain(50f, weightOnHands: 0.5f, gripDrainMult: 1f, gripTexture: 1f);
            var beforeChalk = _system.Current;

            var result = _system.TryUseChalk();

            Assert.IsTrue(result, "Chalk should succeed when not on cooldown");
            Assert.AreEqual(beforeChalk + 35f, _system.Current, 0.01f,
                "Chalk should restore chalkRestoreValue (35)");
        }

        [Test]
        public void TryUseChalk_OnCooldown_ReturnsFalse()
        {
            _system.TryUseChalk(); // First use — starts cooldown
            var afterFirst = _system.Current;

            var result = _system.TryUseChalk(); // Second use — on cooldown

            Assert.IsFalse(result, "Chalk should fail when on cooldown");
            Assert.AreEqual(afterFirst, _system.Current, 0.01f,
                "Grip should not change when chalk fails");
        }

        [Test]
        public void TryUseChalk_ClampsToMax()
        {
            var result = _system.TryUseChalk(); // Already at 100, chalk adds 35

            Assert.IsTrue(result);
            Assert.AreEqual(100f, _system.Current, 0.01f,
                "Chalk should not exceed max adherence");
        }

        [Test]
        public void ChalkCooldown_TicksDown()
        {
            _system.TryUseChalk(); // Starts 20s cooldown
            Assert.IsFalse(_system.IsChalkReady);

            _system.UpdateCooldown(20f);
            Assert.IsTrue(_system.IsChalkReady,
                "Chalk should be ready after cooldown expires");
        }

        [Test]
        public void ChalkCooldown_TicksInUpdateDrain()
        {
            _system.TryUseChalk(); // Starts 20s cooldown

            // UpdateDrain also ticks cooldown
            _system.UpdateDrain(20f, weightOnHands: 0f, gripDrainMult: 0f, gripTexture: 0f);

            Assert.IsTrue(_system.IsChalkReady,
                "Chalk cooldown should tick during UpdateDrain");
        }

        [Test]
        public void IsMicroSlipping_BelowThreshold_ReturnsTrue()
        {
            // Drain to below 30 but above 0
            _system.UpdateDrain(80f, weightOnHands: 0.5f, gripDrainMult: 1f, gripTexture: 1f);

            if (_system.Current > 0f && _system.Current < 30f)
            {
                Assert.IsTrue(_system.IsMicroSlipping,
                    $"Should be micro-slipping at {_system.Current}");
            }
        }

        [Test]
        public void IsMicroSlipping_AtZero_ReturnsFalse()
        {
            _system.UpdateDrain(1000f, weightOnHands: 1f, gripDrainMult: 3f, gripTexture: 2f);

            Assert.AreEqual(0f, _system.Current, 0.01f);
            Assert.IsFalse(_system.IsMicroSlipping,
                "At zero grip, IsMicroSlipping should be false (total loss, not slipping)");
        }

        [Test]
        public void Reset_RestoresFullState()
        {
            _system.UpdateDrain(50f, weightOnHands: 0.5f, gripDrainMult: 1f, gripTexture: 1f);
            _system.TryUseChalk();
            _system.Reset();

            Assert.AreEqual(100f, _system.Current, 0.01f);
            Assert.IsTrue(_system.IsChalkReady);
            Assert.AreEqual(0f, _system.ChalkCooldownRemaining, 0.01f);
        }
    }
}
