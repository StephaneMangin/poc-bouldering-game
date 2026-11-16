using NUnit.Framework;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Climbing
{
    [TestFixture]
    public sealed class PoseSolverTests
    {
        /// <summary>Vertical wall in the plane z = 0, climbed from the negative z side.</summary>
        private sealed class VerticalWall : IClimbingSurface
        {
            public float SignedDistance(Vector3 point) => -point.z;

            public Vector3 NormalAt(float y) => Vector3.back;
        }

        private readonly VerticalWall _wall = new();

        private ContactSet Standing()
        {
            return ContactSet.Empty
                .With(LimbId.LeftFoot, LimbContact.OnFloor(new Vector3(-0.15f, 0f, -0.3f)))
                .With(LimbId.RightFoot, LimbContact.OnFloor(new Vector3(0.15f, 0f, -0.3f)));
        }

        private LimbContact Hold(float x, float y) => LimbContact.OnHold(0, new Vector3(x, y, -0.05f), _wall);

        [Test]
        public void Solve_StandingWithHandsOnShoulderHighHolds_IsValidInFrontOfWall()
        {
            var contacts = Standing().With(LimbId.LeftHand, Hold(-0.25f, 1.8f)).With(LimbId.RightHand, Hold(0.25f, 1.8f));

            var pose = PoseSolver.Solve(contacts, ClimberAnatomy.Default);

            Assert.IsTrue(pose.IsValid);
            Assert.Less(pose.Pelvis.z, -0.19f);
        }

        [Test]
        public void Solve_StandingHandOutOfReach_IsInvalid()
        {
            var contacts = Standing().With(LimbId.RightHand, Hold(0.25f, 2.6f));

            Assert.IsFalse(PoseSolver.Solve(contacts, ClimberAnatomy.Default).IsValid);
        }

        [Test]
        public void Solve_HangingByBothHands_PelvisHangsBelowShoulders()
        {
            var contacts = ContactSet.Empty.With(LimbId.LeftHand, Hold(-0.25f, 3f)).With(LimbId.RightHand, Hold(0.25f, 3f));

            var pose = PoseSolver.Solve(contacts, ClimberAnatomy.Default);

            Assert.IsTrue(pose.IsValid);
            Assert.Less(pose.Pelvis.y, 3f - ClimberAnatomy.Default.TorsoLength);
        }

        [Test]
        public void Solve_FeetFarAboveTheHand_IsInvalid()
        {
            var contacts = ContactSet.Empty
                .With(LimbId.LeftHand, Hold(-0.25f, 2f))
                .With(LimbId.LeftFoot, Hold(-0.2f, 3f))
                .With(LimbId.RightFoot, Hold(0.2f, 3f));

            Assert.IsFalse(PoseSolver.Solve(contacts, ClimberAnatomy.Default).IsValid);
        }
    }
}
