using NUnit.Framework;
using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Climbing
{
    [TestFixture]
    public sealed class FlagSystemTests
    {
        private TuningConfig _config;
        private FlagSystem _system;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TuningConfig>();
            _config.flagLateralOffsetMultiplier = 0.6f;
            _system = new FlagSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void IsFlagging_RequiresInputAndFreeLeg()
        {
            Assert.IsTrue(_system.IsFlagging(true, true));
            Assert.IsFalse(_system.IsFlagging(false, true));
            Assert.IsFalse(_system.IsFlagging(true, false));
            Assert.IsFalse(_system.IsFlagging(false, false));
        }

        [Test]
        public void GetLateralOffsetMultiplier_FlaggingReducesPenalty()
        {
            Assert.AreEqual(1f, _system.GetLateralOffsetMultiplier(false));
            Assert.AreEqual(0.6f, _system.GetLateralOffsetMultiplier(true), 1e-4f);
        }

        [Test]
        public void GetLateralOffsetMultiplier_ClampsConfigToUnitInterval()
        {
            _config.flagLateralOffsetMultiplier = 1.5f;
            Assert.AreEqual(1f, _system.GetLateralOffsetMultiplier(true));
            _config.flagLateralOffsetMultiplier = -0.2f;
            Assert.AreEqual(0f, _system.GetLateralOffsetMultiplier(true));
        }
    }
}
