using System.Linq;
using NUnit.Framework;
using Project.Features.Gym.Domain;
using static Project.Tests.EditMode.Features.Gym.GymTestLayouts;

namespace Project.Tests.EditMode.Features.Gym
{
    [TestFixture]
    public sealed class EnvironmentDressingTests
    {
        [Test]
        public void Build_Mezzanine_DeckAtItsHeightAndRailingAboveItsOpenEdge()
        {
            var definition = Island(6f, 3f, Segment(WallHeight, 0f));
            definition.mezzanine = new MezzanineDefinition { minX = -10f, maxX = -6f, minZ = -10f, maxZ = 2f, height = 4f };

            var parts = EnvironmentDressing.Build(GymLayoutFactory.Build(definition));

            var deck = parts[DressingMaterial.Mezzanine].Vertices;
            Assert.AreEqual(4f, deck.Max(v => v.y), 1e-4f, "deck top");
            var railTops = parts[DressingMaterial.Railing].Vertices.Where(v => v.y > 4.9f).ToList();
            Assert.IsNotEmpty(railTops, "no railing above the deck");
            Assert.That(railTops.All(v => v.x >= -10f && v.x <= -6f && v.z >= -10f && v.z <= 2f), "railing outside the deck");
            Assert.That(railTops.Any(v => v.x > -6.2f), "no railing along the open east edge");
        }
    }
}
