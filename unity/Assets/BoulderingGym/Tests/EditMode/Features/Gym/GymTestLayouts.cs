using Project.Features.Gym.Domain;

namespace Project.Tests.EditMode.Features.Gym
{
    /// <summary>Small hand-made layouts for gym domain tests.</summary>
    public static class GymTestLayouts
    {
        public const float WallHeight = 4.2f;

        /// <summary>Free-standing rectangle, every edge climbable, all faces sharing one profile.</summary>
        public static LayoutDefinition Island(float width, float depth, params SegmentDefinition[] profile)
        {
            var structure = new StructureDefinition
            {
                id = "island",
                name = "Island",
                height = WallHeight,
                vertices = new[] { Point(0f, 0f), Point(width, 0f), Point(width, depth), Point(0f, depth) },
                sectors = new[] { Sector("block", new[] { 0, 1, 2, 3 }, new[] { "exposed", "exposed", "exposed", "exposed" }, profile) },
            };
            return Layout(structure);
        }

        /// <summary>Straight perimeter wall of two 3 m sectors side by side, climbable on the south side only.</summary>
        public static LayoutDefinition TwoSectorWall(SegmentDefinition[] westProfile, SegmentDefinition[] eastProfile)
        {
            var structure = new StructureDefinition
            {
                id = "wall",
                name = "Wall",
                height = WallHeight,
                vertices = new[] { Point(0f, 0f), Point(3f, 0f), Point(6f, 0f), Point(6f, 1f), Point(3f, 1f), Point(0f, 1f) },
                sectors = new[]
                {
                    Sector("west", new[] { 0, 1, 4, 5 }, new[] { "exposed", "internal", "back", "back" }, westProfile),
                    Sector("east", new[] { 1, 2, 3, 4 }, new[] { "exposed", "back", "back", "internal" }, eastProfile),
                },
            };
            return Layout(structure);
        }

        public static SegmentDefinition Segment(float height, float angle) => new() { height = height, angle = angle };

        private static LayoutDefinition Layout(StructureDefinition structure)
        {
            return new LayoutDefinition
            {
                name = "Test gym",
                envelope = new EnvelopeDefinition { minX = -10f, maxX = 10f, minZ = -10f, maxZ = 10f, height = 7f },
                spawn = new SpawnDefinition { x = 0f, z = -5f },
                structures = new[] { structure },
            };
        }

        private static SectorDefinition Sector(string id, int[] loop, string[] edges, SegmentDefinition[] profile)
        {
            return new SectorDefinition { id = id, name = id, loop = loop, edges = edges, profile = profile, seed = 42 };
        }

        private static PointDefinition Point(float x, float z) => new() { x = x, z = z };
    }
}
