using NUnit.Framework;
using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Climbing
{
    [TestFixture]
    public sealed class BodyMechanicsSystemTests
    {
        private TuningConfig _config;
        private BodyMechanicsSystem _system;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TuningConfig>();
            // Distinct values so cheapest is unambiguous.
            _config.gripTypeFatigueMultJug = 0.6f;
            _config.gripTypeFatigueMultOpen = 0.8f;
            _config.gripTypeFatigueMultPinch = 1.0f;
            _config.gripTypeFatigueMultCrimp = 1.3f;
            _config.gripTypeFatigueMultGaston = 1.4f;
            _config.gripTypeFatigueMultUndercling = 1.5f;
            _config.gripTypeFatigueMultSloper = 1.2f;
            _config.gripTypeDrainMultJug = 0.7f;
            _config.gripTypeDrainMultOpen = 0.9f;
            _config.gripTypeDrainMultPinch = 1.0f;
            _config.gripTypeDrainMultCrimp = 1.1f;
            _config.gripTypeDrainMultGaston = 1.2f;
            _config.gripTypeDrainMultUndercling = 1.2f;
            _config.gripTypeDrainMultSloper = 1.5f;
            _system = new BodyMechanicsSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void SelectBestGrip_EmptyArray_ReturnsFallback()
        {
            var result = _system.SelectBestGrip(System.Array.Empty<GripType>(), GripType.Crimp);
            Assert.AreEqual(GripType.Crimp, result);
        }

        [Test]
        public void SelectBestGrip_NullArray_ReturnsFallback()
        {
            var result = _system.SelectBestGrip(null, GripType.Pinch);
            Assert.AreEqual(GripType.Pinch, result);
        }

        [Test]
        public void SelectBestGrip_SingleEntry_ReturnsEntry()
        {
            var result = _system.SelectBestGrip(new[] { GripType.Crimp }, GripType.Jug);
            Assert.AreEqual(GripType.Crimp, result);
        }

        [Test]
        public void SelectBestGrip_PicksLowestCombinedCost()
        {
            // Jug cost = 0.6 + 0.7 = 1.3 ; Crimp cost = 1.3 + 1.1 = 2.4 ; Sloper cost = 1.2 + 1.5 = 2.7
            var result = _system.SelectBestGrip(new[] { GripType.Crimp, GripType.Jug, GripType.Sloper });
            Assert.AreEqual(GripType.Jug, result);
        }

        [Test]
        public void SelectBestGrip_RespectsFatigueWeightOverride()
        {
            // Heavy drain weight should make Jug (0.7 drain) still win over Crimp (1.1 drain).
            var system = new BodyMechanicsSystem(_config, fatigueWeight: 0f, drainWeight: 1f);
            var result = system.SelectBestGrip(new[] { GripType.Crimp, GripType.Jug, GripType.Sloper });
            Assert.AreEqual(GripType.Jug, result);
        }

        [Test]
        public void SelectBestGrip_UnknownGripFallsBackToOne()
        {
            // Cast an out-of-range enum value: ComputeCost returns 1+1=2, which loses to Jug (1.3).
            var unknown = (GripType)999;
            var result = _system.SelectBestGrip(new[] { unknown, GripType.Jug });
            Assert.AreEqual(GripType.Jug, result);
        }
    }
}
