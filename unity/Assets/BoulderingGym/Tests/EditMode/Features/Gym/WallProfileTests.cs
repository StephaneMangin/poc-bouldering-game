using NUnit.Framework;
using Project.Features.Gym.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features.Gym
{
    [TestFixture]
    public sealed class WallProfileTests
    {
        private static WallProfile KickerThenOverhang()
        {
            return new WallProfile(new[] { new WallSegment(1f, 0f), new WallSegment(3f, 20f) });
        }

        [Test]
        public void OffsetAt_OverhangFootSitsBackFromTheTopEdge()
        {
            var profile = KickerThenOverhang();

            Assert.AreEqual(-3f * Mathf.Tan(20f * Mathf.Deg2Rad), profile.OffsetAt(0f), 1e-4f);
            Assert.AreEqual(-3f * Mathf.Tan(20f * Mathf.Deg2Rad), profile.OffsetAt(1f), 1e-4f);
            Assert.AreEqual(0f, profile.OffsetAt(4f), 1e-4f);
        }

        [Test]
        public void OffsetAt_SlabFootStandsOnThePlanOutline()
        {
            var profile = new WallProfile(new[] { new WallSegment(4f, -15f) });

            Assert.AreEqual(0f, profile.OffsetAt(0f), 1e-4f);
            Assert.AreEqual(-4f * Mathf.Tan(15f * Mathf.Deg2Rad), profile.OffsetAt(4f), 1e-4f);
        }

        [Test]
        public void AngleAt_ReturnsAngleOfSegmentAtHeight()
        {
            var profile = KickerThenOverhang();

            Assert.AreEqual(0f, profile.AngleAt(0.5f));
            Assert.AreEqual(20f, profile.AngleAt(2.5f));
        }

        [Test]
        public void ArcLengthAt_InclinedSegmentIsLongerThanItsHeight()
        {
            var profile = KickerThenOverhang();

            Assert.AreEqual(1f + 3f / Mathf.Cos(20f * Mathf.Deg2Rad), profile.ArcLengthAt(4f), 1e-4f);
        }
    }
}
