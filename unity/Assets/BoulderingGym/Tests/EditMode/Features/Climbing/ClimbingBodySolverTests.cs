using NUnit.Framework;
using Project.Features.Climbing.Domain;
using UnityEngine;
using static Project.Features.Climbing.Domain.ClimbingBodySolver;

namespace Project.Tests.EditMode.Features.Climbing
{
    [TestFixture]
    public sealed class ClimbingBodySolverTests
    {
        private AnatomyParams _anatomy;
        private readonly Vector3 _wallNormal = Vector3.back;

        [SetUp]
        public void SetUp()
        {
            _anatomy = AnatomyParams.Default;
        }

        // ─── Solve: no contacts ───

        [Test]
        public void Solve_NoContacts_ReturnsInvalid()
        {
            var contacts = new LimbContacts();
            var result = ClimbingBodySolver.Solve(contacts, _anatomy, _wallNormal);
            Assert.IsFalse(result.IsValid);
        }

        // ─── Solve: feet only ───

        [Test]
        public void Solve_FeetOnly_PelvisAboveFeet()
        {
            var contacts = new LimbContacts
            {
                LeftFoot = new Vector3(-0.1f, 0.5f, 0f),
                RightFoot = new Vector3(0.1f, 0.5f, 0f),
            };

            var result = ClimbingBodySolver.Solve(contacts, _anatomy, _wallNormal);

            Assert.IsTrue(result.IsValid);
            Assert.Greater(result.PelvisPosition.y, contacts.LeftFoot.Value.y,
                "Pelvis should be above feet when standing");
        }

        [Test]
        public void Solve_SingleFoot_PelvisXCenteredOnFoot()
        {
            var contacts = new LimbContacts
            {
                LeftFoot = new Vector3(1.0f, 0.5f, 0f),
            };

            var result = ClimbingBodySolver.Solve(contacts, _anatomy, _wallNormal);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1.0f, result.PelvisPosition.x, 0.01f,
                "Pelvis X should center on the only foot");
        }

        // ─── Solve: hands only ───

        [Test]
        public void Solve_HandsOnly_PelvisHangsBelowHands()
        {
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(-0.2f, 3.0f, 0f),
                RightHand = new Vector3(0.2f, 3.0f, 0f),
            };

            var result = ClimbingBodySolver.Solve(contacts, _anatomy, _wallNormal);

            Assert.IsTrue(result.IsValid);
            Assert.Less(result.PelvisPosition.y, 3.0f,
                "Pelvis should hang below hands");
        }

        [Test]
        public void Solve_SingleHand_PelvisXCenteredOnHand()
        {
            var contacts = new LimbContacts
            {
                RightHand = new Vector3(0.5f, 3.0f, 0f),
            };

            var result = ClimbingBodySolver.Solve(contacts, _anatomy, _wallNormal);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0.5f, result.PelvisPosition.x, 0.01f,
                "Pelvis X should center on the only hand");
        }

        // ─── Solve: full 4-contact climbing ───

        [Test]
        public void Solve_AllFourContacts_PelvisValid()
        {
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(-0.2f, 2.5f, 0f),
                RightHand = new Vector3(0.2f, 2.5f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            var result = ClimbingBodySolver.Solve(contacts, _anatomy, _wallNormal);

            Assert.IsTrue(result.IsValid);
            Assert.Greater(result.PelvisPosition.y, 1.0f, "Pelvis above feet");
            Assert.Less(result.PelvisPosition.y, 2.5f, "Pelvis below hands");
        }

        [Test]
        public void Solve_AllFourContacts_PelvisXWeightedTowardFeet()
        {
            // Feet at X=0, hands at X=1 → pelvis should be biased toward feet (60%)
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(1.0f, 2.5f, 0f),
                RightHand = new Vector3(1.0f, 2.5f, 0f),
                LeftFoot = new Vector3(0.0f, 1.0f, 0f),
                RightFoot = new Vector3(0.0f, 1.0f, 0f),
            };

            var result = ClimbingBodySolver.Solve(contacts, _anatomy, _wallNormal);

            // Expected: 0.0 * 0.6 + 1.0 * 0.4 = 0.4
            Assert.AreEqual(0.4f, result.PelvisPosition.x, 0.01f,
                "Pelvis X should be 60% feet + 40% hands");
        }

        // ─── Solve: wall offset ───

        [Test]
        public void Solve_WallNormalBack_PelvisOffsetNegativeZ()
        {
            var contacts = new LimbContacts
            {
                LeftHand = new Vector3(0f, 2.0f, 0f),
            };

            var result = ClimbingBodySolver.Solve(contacts, _anatomy, Vector3.back);

            // wallNormal.z = -1, so pelvisZ = avgZ + (-1) * wallOffset = 0 - 0.35 = -0.35
            Assert.AreEqual(-_anatomy.WallOffset, result.PelvisPosition.z, 0.01f,
                "Pelvis should be offset from wall along wall normal");
        }

        // ─── Solve: higher feet → higher pelvis ───

        [Test]
        public void Solve_HigherFeet_HigherPelvis()
        {
            var contactsLow = new LimbContacts
            {
                LeftHand = new Vector3(0f, 3.0f, 0f),
                LeftFoot = new Vector3(0f, 0.5f, 0f),
            };
            var contactsHigh = new LimbContacts
            {
                LeftHand = new Vector3(0f, 3.0f, 0f),
                LeftFoot = new Vector3(0f, 1.5f, 0f),
            };

            var lowResult = ClimbingBodySolver.Solve(contactsLow, _anatomy, _wallNormal);
            var highResult = ClimbingBodySolver.Solve(contactsHigh, _anatomy, _wallNormal);

            Assert.Greater(highResult.PelvisPosition.y, lowResult.PelvisPosition.y,
                "Higher feet should push pelvis higher");
        }

        // ─── CanReach ───

        [Test]
        public void CanReach_WithinArmLength_ReturnsTrue()
        {
            var pelvis = new Vector3(0f, 1.5f, -0.35f);
            // Shoulder is at pelvis + (halfWidth, torsoHeight, 0) = (0.18, 1.98, -0.35)
            // Hold close to shoulder → should be reachable
            var hold = new Vector3(0.18f, 2.2f, -0.35f);

            Assert.IsTrue(ClimbingBodySolver.CanReach(pelvis, hold, true, false, _anatomy));
        }

        [Test]
        public void CanReach_BeyondArmLength_ReturnsFalse()
        {
            var pelvis = new Vector3(0f, 1.5f, -0.35f);
            // Shoulder at (0.18, 1.98, -0.35), hold 2m away → unreachable
            var hold = new Vector3(0.18f, 4.0f, -0.35f);

            Assert.IsFalse(ClimbingBodySolver.CanReach(pelvis, hold, true, false, _anatomy));
        }

        [Test]
        public void CanReach_WithinLegLength_ReturnsTrue()
        {
            var pelvis = new Vector3(0f, 1.5f, -0.35f);
            // Hip at (0.12, 1.5, -0.35), foothold just below
            var hold = new Vector3(0.12f, 0.8f, -0.35f);

            Assert.IsTrue(ClimbingBodySolver.CanReach(pelvis, hold, false, false, _anatomy));
        }

        [Test]
        public void CanReach_BeyondLegLength_ReturnsFalse()
        {
            var pelvis = new Vector3(0f, 1.5f, -0.35f);
            // Hip at (0.12, 1.5, -0.35), foothold way below
            var hold = new Vector3(0.12f, -1.0f, -0.35f);

            Assert.IsFalse(ClimbingBodySolver.CanReach(pelvis, hold, false, false, _anatomy));
        }

        // ─── CanPlaceAndStillReachAll ───

        [Test]
        public void CanPlaceAndStillReachAll_ReasonableSetup_ReturnsTrue()
        {
            var contacts = new LimbContacts
            {
                RightHand = new Vector3(0.2f, 2.0f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            // Place left hand at a reasonable position
            var newHold = new Vector3(-0.2f, 2.0f, 0f);

            Assert.IsTrue(ClimbingBodySolver.CanPlaceAndStillReachAll(
                contacts, newHold, true, true, _anatomy, _wallNormal));
        }

        [Test]
        public void CanPlaceAndStillReachAll_FarHold_ReturnsFalse()
        {
            var contacts = new LimbContacts
            {
                RightHand = new Vector3(0.2f, 2.0f, 0f),
                LeftFoot = new Vector3(-0.1f, 1.0f, 0f),
                RightFoot = new Vector3(0.1f, 1.0f, 0f),
            };

            // Place left hand far away — body recalculation makes existing contacts unreachable
            var newHold = new Vector3(-3.0f, 5.0f, 0f);

            Assert.IsFalse(ClimbingBodySolver.CanPlaceAndStillReachAll(
                contacts, newHold, true, true, _anatomy, _wallNormal));
        }

        // ─── GetShoulderPosition / GetHipPosition ───

        [Test]
        public void GetShoulderPosition_LeftShoulder_NegativeX()
        {
            var pelvis = Vector3.zero;
            var shoulder = ClimbingBodySolver.GetShoulderPosition(pelvis, true, _anatomy);

            Assert.Less(shoulder.x, 0f, "Left shoulder should have negative X offset");
            Assert.AreEqual(_anatomy.TorsoHeight, shoulder.y, 0.001f,
                "Shoulder Y should be TorsoHeight above pelvis");
        }

        [Test]
        public void GetShoulderPosition_RightShoulder_PositiveX()
        {
            var pelvis = Vector3.zero;
            var shoulder = ClimbingBodySolver.GetShoulderPosition(pelvis, false, _anatomy);

            Assert.Greater(shoulder.x, 0f, "Right shoulder should have positive X offset");
        }

        [Test]
        public void GetHipPosition_LeftHip_NegativeX()
        {
            var pelvis = Vector3.zero;
            var hip = ClimbingBodySolver.GetHipPosition(pelvis, true, _anatomy);

            Assert.Less(hip.x, 0f, "Left hip should have negative X offset");
            Assert.AreEqual(0f, hip.y, 0.001f, "Hip should be at pelvis Y");
        }

        // ─── LimbContacts struct ───

        [Test]
        public void LimbContacts_Count_ReflectsPlacedLimbs()
        {
            var contacts = new LimbContacts
            {
                LeftHand = Vector3.zero,
                RightFoot = Vector3.one,
            };

            Assert.AreEqual(2, contacts.Count);
            Assert.IsTrue(contacts.HasAnyHand);
            Assert.IsTrue(contacts.HasAnyFoot);
            Assert.IsTrue(contacts.HasAny);
        }

        [Test]
        public void LimbContacts_Empty_AllFalse()
        {
            var contacts = new LimbContacts();

            Assert.AreEqual(0, contacts.Count);
            Assert.IsFalse(contacts.HasAnyHand);
            Assert.IsFalse(contacts.HasAnyFoot);
            Assert.IsFalse(contacts.HasAny);
        }
    }
}
