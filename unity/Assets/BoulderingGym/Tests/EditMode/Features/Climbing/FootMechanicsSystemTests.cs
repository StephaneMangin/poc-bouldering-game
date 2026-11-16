using NUnit.Framework;
using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using Project.Features.Physics.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Climbing
{
    [TestFixture]
    public sealed class FootMechanicsSystemTests
    {
        private TuningConfig _config;
        private FootMechanicsSystem _system;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TuningConfig>();
            _config.footTechniqueEdgingWeightShare = 0.55f;
            _config.footTechniqueSmearingWeightShare = 0.35f;
            _system = new FootMechanicsSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void CanSupport_FootholdPresent_AlwaysTrue()
        {
            Assert.IsTrue(_system.CanSupport(hasFoothold: true, WallAngleCategory.Roof));
        }

        [Test]
        public void CanSupport_NoFoothold_OnlySlab()
        {
            Assert.IsTrue(_system.CanSupport(false, WallAngleCategory.Slab));
            Assert.IsFalse(_system.CanSupport(false, WallAngleCategory.Vertical));
            Assert.IsFalse(_system.CanSupport(false, WallAngleCategory.Overhang));
            Assert.IsFalse(_system.CanSupport(false, WallAngleCategory.StrongOverhang));
            Assert.IsFalse(_system.CanSupport(false, WallAngleCategory.Roof));
        }

        [Test]
        public void SelectTechnique_WithFoothold_PicksEdging()
        {
            Assert.AreEqual(
                FootTechnique.Edging,
                _system.SelectTechnique(true, WallAngleCategory.Vertical));
        }

        [Test]
        public void SelectTechnique_NoFoothold_OnSlab_PicksSmearing()
        {
            Assert.AreEqual(
                FootTechnique.Smearing,
                _system.SelectTechnique(false, WallAngleCategory.Slab));
        }

        [Test]
        public void GetWeightShareToFoot_EdgingHigherThanSmearing()
        {
            var edging = _system.GetWeightShareToFoot(FootTechnique.Edging);
            var smearing = _system.GetWeightShareToFoot(FootTechnique.Smearing);
            Assert.Greater(edging, smearing);
            Assert.AreEqual(0.55f, edging, 1e-4f);
            Assert.AreEqual(0.35f, smearing, 1e-4f);
        }
    }
}
