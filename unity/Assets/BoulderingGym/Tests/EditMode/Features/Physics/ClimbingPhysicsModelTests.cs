using NUnit.Framework;
using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using Project.Features.Physics.Domain;
using UnityEngine;
using ContactPoint = Project.Features.Physics.Domain.ContactPoint;

namespace Project.Tests.EditMode.Features.Physics
{
    /// <summary>
    /// Pure domain unit tests for ClimbingPhysicsModel.
    /// No scene, no MonoBehaviour — tests math and logic only.
    /// </summary>
    [TestFixture]
    public sealed class ClimbingPhysicsModelTests
    {
        private TuningConfig _config;
        private ClimbingPhysicsModel _model;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TuningConfig>();
            _model = new ClimbingPhysicsModel(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // ─── ComputeCOG ───

        [Test]
        public void ComputeCOG_NoContacts_ReturnsPelvisPosition()
        {
            var pelvis = new Vector3(0f, 1f, 0f);
            var result = _model.ComputeCOG(new ContactPoint[0], pelvis, Vector3.back);

            Assert.AreEqual(pelvis, result.Position);
            Assert.IsFalse(result.IsStable);
            Assert.IsFalse(result.IsInsideSupportPolygon);
        }

        [Test]
        public void ComputeCOG_NullContacts_ReturnsPelvisPosition()
        {
            var pelvis = new Vector3(0f, 1f, 0f);
            var result = _model.ComputeCOG(null, pelvis, Vector3.back);

            Assert.AreEqual(pelvis, result.Position);
            Assert.IsFalse(result.IsStable);
        }

        [Test]
        public void ComputeCOG_SingleHandContact_PositionIsContact()
        {
            var contact = new ContactPoint(new Vector3(0f, 2f, 0f), true, true, HandSide.Left);
            var pelvis = new Vector3(0f, 1f, 0f);

            var result = _model.ComputeCOG(new[] { contact }, pelvis, Vector3.back);

            Assert.AreEqual(contact.Position, result.Position);
        }

        [Test]
        public void ComputeCOG_HandAndFoot_WeightedAverage()
        {
            var hand = new ContactPoint(new Vector3(0f, 2f, 0f), true, true, HandSide.Left);
            var foot = new ContactPoint(new Vector3(0f, 0f, 0f), false, true);
            var pelvis = new Vector3(0f, 1f, 0f);

            // Oracle: ComputeCOG uses 40% hands / 60% feet when both exist
            const float handWeightCog = 0.4f;
            const float footWeightCog = 0.6f;
            var expectedY = hand.Position.y * handWeightCog + foot.Position.y * footWeightCog;

            var result = _model.ComputeCOG(new[] { hand, foot }, pelvis, Vector3.back);

            Assert.AreEqual(expectedY, result.Position.y, 0.01f,
                $"COG.y should be weighted average: hand({hand.Position.y})*{handWeightCog} + foot({foot.Position.y})*{footWeightCog}");
        }

        [Test]
        public void ComputeCOG_InactiveContactsIgnored()
        {
            var active = new ContactPoint(new Vector3(0f, 2f, 0f), true, true, HandSide.Left);
            var inactive = new ContactPoint(new Vector3(10f, 10f, 10f), true, false, HandSide.Right);
            var pelvis = new Vector3(0f, 1f, 0f);

            var result = _model.ComputeCOG(new[] { active, inactive }, pelvis, Vector3.back);

            Assert.AreEqual(active.Position, result.Position,
                "Inactive contacts should not affect COG");
        }

        [Test]
        public void ComputeCOG_TwoHands_EqualWeight()
        {
            var left = new ContactPoint(new Vector3(-1f, 2f, 0f), true, true, HandSide.Left);
            var right = new ContactPoint(new Vector3(1f, 2f, 0f), true, true, HandSide.Right);
            var pelvis = new Vector3(0f, 1f, 0f);

            var result = _model.ComputeCOG(new[] { left, right }, pelvis, Vector3.back);

            // Both are hands, no feet → handWeight=1, each contributes equally
            Assert.AreEqual(0f, result.Position.x, 0.01f, "COG x should be centered between two hands");
            Assert.AreEqual(2f, result.Position.y, 0.01f, "COG y should be at hand height");
        }

        [Test]
        public void ComputeCOG_CogOffsetPenalty_IncreasesWithOffset()
        {
            _config.cogOffsetPenaltyStart = 0.2f;
            _config.cogMaxOffset = 0.6f;
            _config.cogOffsetPenaltyMult = 2.0f;

            // Small offset: hand close to wall plane
            var handClose = new ContactPoint(new Vector3(0f, 2f, 0.1f), true, true, HandSide.Left);
            var pelvis = new Vector3(0f, 1f, 0f);
            var resultClose = _model.ComputeCOG(new[] { handClose }, pelvis, Vector3.back);

            // Large offset: hand far from wall plane
            var handFar = new ContactPoint(new Vector3(0f, 2f, 2f), true, true, HandSide.Left);
            var resultFar = _model.ComputeCOG(new[] { handFar }, pelvis, Vector3.back);

            Assert.Greater(resultFar.CogOffsetPenalty, 0f,
                "Far offset should produce a positive penalty");
            Assert.Greater(resultFar.CogOffsetPenalty, resultClose.CogOffsetPenalty,
                "Penalty should increase with larger offset");
        }

        // ─── IsInsideSupportPolygon ───

        [Test]
        public void IsInsideSupportPolygon_NoPoints_ReturnsFalse()
        {
            var result = ClimbingPhysicsModel.IsInsideSupportPolygon(
                new ContactPoint[0], Vector3.zero);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsInsideSupportPolygon_SinglePoint_CloseEnough_ReturnsTrue()
        {
            var contact = new ContactPoint(new Vector3(1f, 1f, 0f), true, true, HandSide.Left);
            var cog = new Vector3(1.1f, 1.1f, 0f);

            var result = ClimbingPhysicsModel.IsInsideSupportPolygon(
                new[] { contact }, cog);

            Assert.IsTrue(result, "COG very close to single point should be inside");
        }

        [Test]
        public void IsInsideSupportPolygon_SinglePoint_TooFar_ReturnsFalse()
        {
            var contact = new ContactPoint(new Vector3(0f, 0f, 0f), true, true, HandSide.Left);
            var cog = new Vector3(5f, 5f, 0f);

            var result = ClimbingPhysicsModel.IsInsideSupportPolygon(
                new[] { contact }, cog);

            Assert.IsFalse(result, "COG far from single point should be outside");
        }

        [Test]
        public void IsInsideSupportPolygon_TwoPoints_CogOnSegment_ReturnsTrue()
        {
            var a = new ContactPoint(new Vector3(0f, 0f, 0f), true, true, HandSide.Left);
            var b = new ContactPoint(new Vector3(2f, 0f, 0f), true, true, HandSide.Right);
            var cog = new Vector3(1f, 0f, 0f);

            var result = ClimbingPhysicsModel.IsInsideSupportPolygon(
                new[] { a, b }, cog);

            Assert.IsTrue(result, "COG on segment between two points should be inside");
        }

        [Test]
        public void IsInsideSupportPolygon_Triangle_CogInside_ReturnsTrue()
        {
            var contacts = new[]
            {
                new ContactPoint(new Vector3(0f, 0f, 0f), false, true),
                new ContactPoint(new Vector3(2f, 0f, 0f), false, true),
                new ContactPoint(new Vector3(1f, 2f, 0f), true, true, HandSide.Left),
            };
            var cogInside = new Vector3(1f, 0.5f, 0f);

            Assert.IsTrue(
                ClimbingPhysicsModel.IsInsideSupportPolygon(contacts, cogInside),
                "COG inside triangle should return true");
        }

        [Test]
        public void IsInsideSupportPolygon_Triangle_CogOutside_ReturnsFalse()
        {
            var contacts = new[]
            {
                new ContactPoint(new Vector3(0f, 0f, 0f), false, true),
                new ContactPoint(new Vector3(2f, 0f, 0f), false, true),
                new ContactPoint(new Vector3(1f, 2f, 0f), true, true, HandSide.Left),
            };
            var cogOutside = new Vector3(5f, 5f, 0f);

            Assert.IsFalse(
                ClimbingPhysicsModel.IsInsideSupportPolygon(contacts, cogOutside),
                "COG outside triangle should return false");
        }

        [Test]
        public void IsInsideSupportPolygon_Quadrilateral_CogInside_ReturnsTrue()
        {
            var contacts = new[]
            {
                new ContactPoint(new Vector3(-1f, 0f, 0f), false, true),
                new ContactPoint(new Vector3(1f, 0f, 0f), false, true),
                new ContactPoint(new Vector3(1f, 2f, 0f), true, true, HandSide.Right),
                new ContactPoint(new Vector3(-1f, 2f, 0f), true, true, HandSide.Left),
            };
            var cog = new Vector3(0f, 1f, 0f);

            Assert.IsTrue(
                ClimbingPhysicsModel.IsInsideSupportPolygon(contacts, cog),
                "COG inside quadrilateral polygon should return true");
        }

        // ─── ComputeWeight ───

        [Test]
        public void ComputeWeight_NoHands_AllWeightOnFeet()
        {
            var cogPos = new Vector3(0f, 1f, 0f);

            var result = _model.ComputeWeight(new ContactPoint[0], cogPos, WallAngleCategory.Vertical);

            Assert.AreEqual(0f, result.TotalHandWeight, 0.001f);
            Assert.AreEqual(1f, result.WeightOnFeet, 0.001f);
        }

        [Test]
        public void ComputeWeight_SingleLeftHand_FullHandRatio()
        {
            var hand = new ContactPoint(new Vector3(0f, 2f, 0f), true, true, HandSide.Left);
            var cogPos = new Vector3(0f, 1f, 0f);

            // Oracle: single left hand gets full hand ratio for vertical wall (default 0.35)
            const float expectedLeftWeight = 0.35f;
            const float expectedRightWeight = 0f;

            var result = _model.ComputeWeight(new[] { hand }, cogPos, WallAngleCategory.Vertical);

            Assert.AreEqual(expectedLeftWeight, result.WeightOnLeftHand, 0.001f,
                "Single left hand should bear full vertical hand ratio");
            Assert.AreEqual(expectedRightWeight, result.WeightOnRightHand, 0.001f);
        }

        [Test]
        public void ComputeWeight_BothHands_InverseDistanceWeighting()
        {
            // Left hand closer to COG → more weight
            var left = new ContactPoint(new Vector3(0f, 1.5f, 0f), true, true, HandSide.Left);
            var right = new ContactPoint(new Vector3(0f, 3f, 0f), true, true, HandSide.Right);
            var cogPos = new Vector3(0f, 1f, 0f);

            var result = _model.ComputeWeight(new[] { left, right }, cogPos, WallAngleCategory.Vertical);

            Assert.Greater(result.WeightOnLeftHand, result.WeightOnRightHand,
                "Closer hand should bear more weight");
            Assert.Greater(result.TotalHandWeight, 0f);
        }

        [Test]
        public void ComputeWeight_NullContacts_NoHandWeight()
        {
            var result = _model.ComputeWeight(null, Vector3.zero, WallAngleCategory.Slab);

            Assert.AreEqual(0f, result.TotalHandWeight, 0.001f);
            Assert.AreEqual(1f, result.WeightOnFeet, 0.001f);
        }

        // ─── GetMultipliers ───

        [Test]
        public void GetMultipliers_Vertical_BaselineMultipliers()
        {
            var mult = _model.GetMultipliers(WallAngleCategory.Vertical);

            Assert.AreEqual(1.0f, mult.EnduranceMult, 0.001f);
            Assert.AreEqual(1.0f, mult.FatigueMult, 0.001f);
            Assert.AreEqual(1.0f, mult.GripDrainMult, 0.001f);
        }

        [Test]
        public void GetMultipliers_Slab_LowerThanVertical()
        {
            var mult = _model.GetMultipliers(WallAngleCategory.Slab);

            Assert.Less(mult.EnduranceMult, 1.0f, "Slab endurance mult should be < 1");
            Assert.Less(mult.FatigueMult, 1.0f, "Slab fatigue mult should be < 1");
            Assert.Less(mult.GripDrainMult, 1.0f, "Slab grip drain mult should be < 1");
        }

        [Test]
        public void GetMultipliers_Roof_HigherThanVertical()
        {
            var mult = _model.GetMultipliers(WallAngleCategory.Roof);

            Assert.Greater(mult.EnduranceMult, 1.0f, "Roof endurance mult should be > 1");
            Assert.Greater(mult.FatigueMult, 1.0f, "Roof fatigue mult should be > 1");
            Assert.Greater(mult.GripDrainMult, 1.0f, "Roof grip drain mult should be > 1");
        }

        [TestCase(WallAngleCategory.Slab)]
        [TestCase(WallAngleCategory.Vertical)]
        [TestCase(WallAngleCategory.Overhang)]
        [TestCase(WallAngleCategory.StrongOverhang)]
        [TestCase(WallAngleCategory.Roof)]
        public void GetMultipliers_AllCategories_PositiveValues(WallAngleCategory category)
        {
            var mult = _model.GetMultipliers(category);

            Assert.Greater(mult.EnduranceMult, 0f, $"{category} endurance mult should be > 0");
            Assert.Greater(mult.FatigueMult, 0f, $"{category} fatigue mult should be > 0");
            Assert.Greater(mult.GripDrainMult, 0f, $"{category} grip drain mult should be > 0");
        }

        [Test]
        public void GetMultipliers_DifficultyIncreasesWithAngle()
        {
            var slab = _model.GetMultipliers(WallAngleCategory.Slab);
            var vertical = _model.GetMultipliers(WallAngleCategory.Vertical);
            var overhang = _model.GetMultipliers(WallAngleCategory.Overhang);
            var strong = _model.GetMultipliers(WallAngleCategory.StrongOverhang);
            var roof = _model.GetMultipliers(WallAngleCategory.Roof);

            Assert.LessOrEqual(slab.EnduranceMult, vertical.EnduranceMult);
            Assert.LessOrEqual(vertical.EnduranceMult, overhang.EnduranceMult);
            Assert.LessOrEqual(overhang.EnduranceMult, strong.EnduranceMult);
            Assert.LessOrEqual(strong.EnduranceMult, roof.EnduranceMult);
        }

        // ─── ComputeSnapshot integration ───

        [Test]
        public void ComputeSnapshot_VerticalWall_ContainsAllFields()
        {
            // Oracle: vertical wall baseline values
            const WallAngleCategory expectedCategory = WallAngleCategory.Vertical;
            const float expectedAngle = 90f;
            const float expectedEnduranceMult = 1.0f;
            // bodyWallDistance (0.5) > default threshold (0.3) → no body contact
            const bool expectedBodyContact = false;

            var contacts = new[]
            {
                new ContactPoint(new Vector3(-0.3f, 0f, 0f), false, true),
                new ContactPoint(new Vector3(0.3f, 0f, 0f), false, true),
                new ContactPoint(new Vector3(0f, 1.5f, 0f), true, true, HandSide.Left),
            };

            var snapshot = _model.ComputeSnapshot(
                contacts,
                pelvisPosition: new Vector3(0f, 0.8f, 0.2f),
                wallNormal: Vector3.back,
                wallCategory: expectedCategory,
                wallAngleDegrees: expectedAngle,
                bodyWallDistance: 0.5f);

            Assert.AreEqual(expectedCategory, snapshot.WallCategory);
            Assert.AreEqual(expectedAngle, snapshot.WallAngleDegrees, 0.01f);
            Assert.AreEqual(expectedEnduranceMult, snapshot.Multipliers.EnduranceMult, 0.01f);
            Assert.AreEqual(expectedBodyContact, snapshot.HasBodyWallContact,
                "bodyWallDistance > threshold → no body contact");
        }

        [Test]
        public void ComputeSnapshot_Slab_CloseToWall_HasBodyContact()
        {
            // Oracle: bodyWallDistance (0.1) < threshold (0.3) → body contact enabled
            const float bodyWallContactThreshold = 0.3f;
            const float expectedFriction = 0.4f;
            const float bodyWallDistance = 0.1f;

            _config.bodyWallContactThreshold = bodyWallContactThreshold;
            _config.slabBodyFriction = expectedFriction;

            var contacts = new[]
            {
                new ContactPoint(new Vector3(0f, 0f, 0f), false, true),
            };

            var snapshot = _model.ComputeSnapshot(
                contacts,
                pelvisPosition: Vector3.zero,
                wallNormal: Vector3.back,
                wallCategory: WallAngleCategory.Slab,
                wallAngleDegrees: 70f,
                bodyWallDistance: bodyWallDistance);

            Assert.IsTrue(snapshot.HasBodyWallContact,
                $"bodyWallDistance ({bodyWallDistance}) < threshold ({bodyWallContactThreshold}) → body contact");
            Assert.AreEqual(expectedFriction, snapshot.BodyWallFriction, 0.01f);
        }
    }
}
