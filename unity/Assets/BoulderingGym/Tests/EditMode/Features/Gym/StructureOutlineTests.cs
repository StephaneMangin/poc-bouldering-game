using System;
using NUnit.Framework;
using Project.Features.Gym.Domain;
using UnityEngine;
using static Project.Tests.EditMode.Features.Gym.GymTestLayouts;

namespace Project.Tests.EditMode.Features.Gym
{
    [TestFixture]
    public sealed class StructureOutlineTests
    {
        [Test]
        public void IslandCorners_UniformOverhang_MeetAtOneMiterPoint()
        {
            var layout = GymLayoutFactory.Build(Island(3f, 2f, Segment(WallHeight, 20f)));
            var outline = layout.Structures[0].Outline;
            var top = outline.Levels.Count - 1;
            var offset = WallHeight * Mathf.Tan(20f * Mathf.Deg2Rad);

            for (var i = 0; i < outline.Edges.Count; i++)
            {
                var corner = outline.Corners[i];
                var previous = outline.Edges[(i + outline.Edges.Count - 1) % outline.Edges.Count];
                var displacement = corner.Outgoing[top] - outline.Edges[i].Start - Vector3.up * WallHeight;
                Assert.IsFalse(corner.IsSplit);
                Assert.AreEqual(offset, Vector3.Dot(displacement, previous.Outward), 1e-3f);
                Assert.AreEqual(offset, Vector3.Dot(displacement, outline.Edges[i].Outward), 1e-3f);
            }
        }

        [Test]
        public void CollinearSectors_DifferentProfiles_SplitIntoJunction()
        {
            var layout = GymLayoutFactory.Build(TwoSectorWall(new[] { Segment(WallHeight, 0f) }, new[] { Segment(WallHeight, 20f) }));
            var outline = layout.Structures[0].Outline;
            var top = outline.Levels.Count - 1;
            var junction = outline.Corners[1];

            Assert.IsTrue(junction.IsSplit);
            Assert.AreEqual(0f, junction.Incoming[top].z, 1e-3f);
            Assert.AreEqual(-WallHeight * Mathf.Tan(20f * Mathf.Deg2Rad), junction.Outgoing[top].z, 1e-3f);
        }

        [Test]
        public void Build_ClockwiseSectorLoop_Throws()
        {
            var definition = Island(3f, 2f, Segment(WallHeight, 0f));
            Array.Reverse(definition.structures[0].sectors[0].loop);

            Assert.Throws<InvalidOperationException>(() => GymLayoutFactory.Build(definition));
        }
    }
}
