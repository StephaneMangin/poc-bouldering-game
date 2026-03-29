using NUnit.Framework;
using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using Project.Features.Resources.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Resources
{
    [TestFixture]
    public sealed class LocalFatigueSystemTests
    {
        private TuningConfig _config;
        private LocalFatigueSystem _system;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TuningConfig>();
            _config.fatigueGainBasePerS = 7f;
            _config.fatigueRecoverRestPerS = 10f;
            _config.fatigueWarningThreshold = 60f;
            _config.fatigueCriticalThreshold = 80f;
            _config.fatigueFailureThreshold = 95f;
            _config.gripTypeFatigueMultJug = 0.6f;
            _config.gripTypeFatigueMultOpen = 0.8f;
            _config.gripTypeFatigueMultPinch = 1.0f;
            _config.gripTypeFatigueMultCrimp = 1.3f;
            _config.gripTypeFatigueMultGaston = 1.4f;
            _config.gripTypeFatigueMultUndercling = 1.5f;
            _config.gripTypeFatigueMultSloper = 1.2f;
            _system = new LocalFatigueSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void InitialState_ZeroFatigue()
        {
            Assert.AreEqual(0f, _system.LeftFatigue, 0.01f);
            Assert.AreEqual(0f, _system.RightFatigue, 0.01f);
        }

        [Test]
        public void Update_ActiveLeftArm_GainsLeftFatigue()
        {
            // gain = 7 * 0.6 (jug) * 0.35 (weight) * 1.0 (vertical) = 1.47/s
            _system.Update(1f,
                GripType.Jug, 0.35f, leftActive: true,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 1f);

            Assert.AreEqual(1.47f, _system.LeftFatigue, 0.01f,
                "Left arm fatigue: 7 * 0.6 * 0.35 * 1.0 = 1.47 per second");
            Assert.AreEqual(0f, _system.RightFatigue, 0.01f,
                "Inactive right arm should not gain fatigue");
        }

        [Test]
        public void Update_InactiveArm_Recovers()
        {
            // Build up some fatigue first
            _system.Update(10f,
                GripType.Jug, 0.5f, leftActive: true,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 1f);
            var fatigueAfterGain = _system.LeftFatigue; // 7*0.6*0.5*1 = 2.1/s * 10s = 21

            // Now rest the left arm
            _system.Update(1f,
                GripType.Jug, 0f, leftActive: false,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 1f);

            Assert.Less(_system.LeftFatigue, fatigueAfterGain,
                "Inactive arm should recover fatigue");
            Assert.AreEqual(fatigueAfterGain - 10f, _system.LeftFatigue, 0.01f,
                "Recovery at 10/s for 1s should reduce by 10");
        }

        [Test]
        public void Update_GripTypeMult_AffectsGainRate()
        {
            var systemJug = new LocalFatigueSystem(_config);
            var systemCrimp = new LocalFatigueSystem(_config);

            systemJug.Update(1f,
                GripType.Jug, 0.35f, leftActive: true,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 1f);

            systemCrimp.Update(1f,
                GripType.Crimp, 0.35f, leftActive: true,
                GripType.Crimp, 0f, rightActive: false,
                wallAngleFatigueMult: 1f);

            Assert.Greater(systemCrimp.LeftFatigue, systemJug.LeftFatigue,
                "Crimp (mult=1.3) should fatigue faster than jug (mult=0.6)");
        }

        [Test]
        public void Update_Weight_AffectsGainRate()
        {
            var systemLight = new LocalFatigueSystem(_config);
            var systemHeavy = new LocalFatigueSystem(_config);

            systemLight.Update(1f,
                GripType.Jug, 0.15f, leftActive: true,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 1f);

            systemHeavy.Update(1f,
                GripType.Jug, 0.90f, leftActive: true,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 1f);

            Assert.Greater(systemHeavy.LeftFatigue, systemLight.LeftFatigue,
                "Higher weight on arm should cause faster fatigue gain");
        }

        [Test]
        public void Update_WallAngleMult_AffectsGainRate()
        {
            var systemVertical = new LocalFatigueSystem(_config);
            var systemRoof = new LocalFatigueSystem(_config);

            systemVertical.Update(1f,
                GripType.Jug, 0.35f, leftActive: true,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 1.0f);

            systemRoof.Update(1f,
                GripType.Jug, 0.35f, leftActive: true,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 3.0f);

            Assert.AreEqual(systemRoof.LeftFatigue, systemVertical.LeftFatigue * 3f, 0.01f,
                "Roof wall angle (3.0) should triple vertical fatigue rate");
        }

        [Test]
        public void Update_ClampsToHundred()
        {
            _system.Update(1000f,
                GripType.Undercling, 1f, leftActive: true,
                GripType.Undercling, 1f, rightActive: true,
                wallAngleFatigueMult: 5f);

            Assert.AreEqual(100f, _system.LeftFatigue, 0.01f,
                "Fatigue should cap at 100");
            Assert.AreEqual(100f, _system.RightFatigue, 0.01f);
        }

        [Test]
        public void Update_RecoveryClampsToZero()
        {
            _system.Update(1000f,
                GripType.Jug, 0f, leftActive: false,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 1f);

            Assert.AreEqual(0f, _system.LeftFatigue, 0.01f,
                "Recovery should not go below zero");
        }

        [TestCase(0f, FatigueLevel.Normal)]
        [TestCase(30f, FatigueLevel.Normal)]
        [TestCase(59f, FatigueLevel.Normal)]
        [TestCase(60f, FatigueLevel.Warning)]
        [TestCase(79f, FatigueLevel.Warning)]
        [TestCase(80f, FatigueLevel.Critical)]
        [TestCase(94f, FatigueLevel.Critical)]
        [TestCase(95f, FatigueLevel.Failure)]
        [TestCase(100f, FatigueLevel.Failure)]
        public void GetLevel_CorrectThresholds(float fatigue, FatigueLevel expected)
        {
            Assert.AreEqual(expected, _system.GetLevel(fatigue),
                $"Fatigue {fatigue} should be {expected}");
        }

        [Test]
        public void Reset_ZeroesBothArms()
        {
            _system.Update(10f,
                GripType.Crimp, 0.5f, leftActive: true,
                GripType.Crimp, 0.5f, rightActive: true,
                wallAngleFatigueMult: 1f);

            _system.Reset();

            Assert.AreEqual(0f, _system.LeftFatigue, 0.01f);
            Assert.AreEqual(0f, _system.RightFatigue, 0.01f);
        }

        [Test]
        public void AlternatingArms_LowerMaxFatigue()
        {
            // Scenario 1: Same arm for 10s
            var sameArm = new LocalFatigueSystem(_config);
            sameArm.Update(10f,
                GripType.Jug, 0.35f, leftActive: true,
                GripType.Jug, 0f, rightActive: false,
                wallAngleFatigueMult: 1f);
            var sameArmFatigueLeft = sameArm.LeftFatigue;

            // Scenario 2: Alternate every 2s (5 switches in 10s)
            var alternating = new LocalFatigueSystem(_config);
            for (var i = 0; i < 5; i++)
            {
                var leftActive = i % 2 == 0;
                alternating.Update(2f,
                    GripType.Jug, leftActive ? 0.35f : 0f, leftActive: leftActive,
                    GripType.Jug, leftActive ? 0f : 0.35f, rightActive: !leftActive,
                    wallAngleFatigueMult: 1f);
            }

            Assert.Less(alternating.LeftFatigue, sameArmFatigueLeft,
                "Alternating arms should result in lower max fatigue per arm");
        }

        [Test]
        public void GetGripTypeFatigueMult_AllTypes_ReturnCorrectValues()
        {
            Assert.AreEqual(0.6f, _system.GetGripTypeFatigueMult(GripType.Jug), 0.001f);
            Assert.AreEqual(0.8f, _system.GetGripTypeFatigueMult(GripType.Open), 0.001f);
            Assert.AreEqual(1.0f, _system.GetGripTypeFatigueMult(GripType.Pinch), 0.001f);
            Assert.AreEqual(1.3f, _system.GetGripTypeFatigueMult(GripType.Crimp), 0.001f);
            Assert.AreEqual(1.4f, _system.GetGripTypeFatigueMult(GripType.Gaston), 0.001f);
            Assert.AreEqual(1.5f, _system.GetGripTypeFatigueMult(GripType.Undercling), 0.001f);
            Assert.AreEqual(1.2f, _system.GetGripTypeFatigueMult(GripType.Sloper), 0.001f);
        }
    }
}
