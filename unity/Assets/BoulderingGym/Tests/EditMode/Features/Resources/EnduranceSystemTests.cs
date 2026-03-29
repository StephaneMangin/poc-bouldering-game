using NUnit.Framework;
using Project.Core.Domain;
using Project.Features.Resources.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Resources
{
    [TestFixture]
    public sealed class EnduranceSystemTests
    {
        private TuningConfig _config;
        private EnduranceSystem _system;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TuningConfig>();
            _config.enduranceMax = 100f;
            _config.enduranceCostGrab = 2f;
            _config.enduranceRegenRestPerS = 6f;
            _config.enduranceCostReposition = 4f;
            _config.enduranceCostDyno = 12f;
            _system = new EnduranceSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void InitialState_StartsAtMax()
        {
            Assert.AreEqual(100f, _system.Current, 0.01f);
            Assert.AreEqual(100f, _system.Max, 0.01f);
            Assert.AreEqual(1f, _system.Ratio, 0.01f);
        }

        [Test]
        public void ApplyActionCost_Grab_DeductsBaseCost()
        {
            _system.ApplyActionCost(ClimbingAction.Grab, cogOffsetPenalty: 0f, wallAngleEnduranceMult: 1f);

            Assert.AreEqual(98f, _system.Current, 0.01f,
                "Grab with no penalties should deduct base cost (2)");
        }

        [Test]
        public void ApplyActionCost_Reposition_DeductsBaseCost()
        {
            _system.ApplyActionCost(ClimbingAction.Reposition, cogOffsetPenalty: 0f, wallAngleEnduranceMult: 1f);

            Assert.AreEqual(96f, _system.Current, 0.01f,
                "Reposition with no penalties should deduct base cost (4)");
        }

        [Test]
        public void ApplyActionCost_Dyno_DeductsBaseCost()
        {
            _system.ApplyActionCost(ClimbingAction.Dyno, cogOffsetPenalty: 0f, wallAngleEnduranceMult: 1f);

            Assert.AreEqual(88f, _system.Current, 0.01f,
                "Dyno with no penalties should deduct base cost (12)");
        }

        [Test]
        public void ApplyActionCost_WithCogPenalty_IncreasedCost()
        {
            // cogOffsetPenalty = 1.0 → cost = 2 * (1+1) * 1 = 4
            _system.ApplyActionCost(ClimbingAction.Grab, cogOffsetPenalty: 1f, wallAngleEnduranceMult: 1f);

            Assert.AreEqual(96f, _system.Current, 0.01f,
                "COG penalty of 1.0 should double the grab cost: 2 * (1+1) = 4");
        }

        [Test]
        public void ApplyActionCost_WithWallAngleMult_IncreasedCost()
        {
            // wallAngle = 2.5 → cost = 2 * 1 * 2.5 = 5
            _system.ApplyActionCost(ClimbingAction.Grab, cogOffsetPenalty: 0f, wallAngleEnduranceMult: 2.5f);

            Assert.AreEqual(95f, _system.Current, 0.01f,
                "Roof multiplier (2.5) should make grab cost 2*1*2.5 = 5");
        }

        [Test]
        public void ApplyActionCost_CombinedModifiers_CorrectCost()
        {
            // cost = 2 * (1+1.5) * 2.0 = 2 * 2.5 * 2.0 = 10
            _system.ApplyActionCost(ClimbingAction.Grab, cogOffsetPenalty: 1.5f, wallAngleEnduranceMult: 2.0f);

            Assert.AreEqual(90f, _system.Current, 0.01f,
                "Combined: 2 * (1+1.5) * 2.0 = 10");
        }

        [Test]
        public void ApplyActionCost_ClampsToZero()
        {
            // Drain enough to go below zero
            for (var i = 0; i < 20; i++)
            {
                _system.ApplyActionCost(ClimbingAction.Dyno, cogOffsetPenalty: 0f, wallAngleEnduranceMult: 1f);
            }

            Assert.AreEqual(0f, _system.Current, 0.01f,
                "Endurance should never go below zero");
        }

        [Test]
        public void UpdateRegen_IncreasesEndurance()
        {
            _system.ApplyActionCost(ClimbingAction.Dyno, cogOffsetPenalty: 0f, wallAngleEnduranceMult: 1f);
            var afterCost = _system.Current; // 88

            _system.UpdateRegen(1f);

            // Oracle: 88 + 6*1 = 94
            Assert.AreEqual(afterCost + 6f, _system.Current, 0.01f,
                "1 second of regen at 6/s should add 6");
        }

        [Test]
        public void UpdateRegen_ClampsToMax()
        {
            _system.UpdateRegen(100f);

            Assert.AreEqual(100f, _system.Current, 0.01f,
                "Regen should not exceed max endurance");
        }

        [Test]
        public void Reset_RestoresToMax()
        {
            _system.ApplyActionCost(ClimbingAction.Dyno, cogOffsetPenalty: 0f, wallAngleEnduranceMult: 1f);
            _system.Reset();

            Assert.AreEqual(100f, _system.Current, 0.01f,
                "Reset should restore to max");
        }

        [Test]
        public void Ratio_ReturnsCorrectProportion()
        {
            // Drain to 50
            for (var i = 0; i < 25; i++)
            {
                _system.ApplyActionCost(ClimbingAction.Grab, cogOffsetPenalty: 0f, wallAngleEnduranceMult: 1f);
            }

            Assert.AreEqual(0.5f, _system.Ratio, 0.01f,
                "After 25 grabs (50 cost), ratio should be 0.5");
        }
    }
}
