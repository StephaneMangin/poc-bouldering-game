using System;

namespace Project.Features.Gym.Domain
{
    /// <summary>
    /// Serialized gym layout (Data/Gameplay/GymScenes/blockout_rennes_layout.json).
    /// Plan coordinates in meters: x east, z north, origin at the island center.
    /// </summary>
    [Serializable]
    public sealed class LayoutDefinition
    {
        public string name;
        public string version;
        public EnvelopeDefinition envelope;
        public SpawnDefinition spawn;
        public ColorDefinition[] palette;
        public StructureDefinition[] structures;
        public PropDefinition[] props;
    }

    /// <summary>Decor standing in the hall, such as the island's mast ("mast").</summary>
    [Serializable]
    public sealed class PropDefinition
    {
        public string type;
        public float x;
        public float z;
        public float baseHeight;
    }

    /// <summary>Named sRGB color (components 0..1): panel colors, and hold circuit colors prefixed "hold_".</summary>
    [Serializable]
    public sealed class ColorDefinition
    {
        public string id;
        public float r;
        public float g;
        public float b;
    }

    [Serializable]
    public sealed class EnvelopeDefinition
    {
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
        public float height;
    }

    [Serializable]
    public sealed class SpawnDefinition
    {
        public float x;
        public float z;
        public float yawDeg;
    }

    [Serializable]
    public sealed class PointDefinition
    {
        public float x;
        public float z;
    }

    /// <summary>Sectors sharing walls; their loops index the shared vertex pool, counter-clockwise seen from above.</summary>
    [Serializable]
    public sealed class StructureDefinition
    {
        public string id;
        public string name;
        public float height;
        public PointDefinition[] vertices;
        public SectorDefinition[] sectors;
    }

    /// <summary>
    /// Edge i joins loop[i] to loop[i + 1]; its kind is "exposed" (climbable face toward the room),
    /// "back" (against the building wall) or "internal" (shared with another sector).
    /// </summary>
    [Serializable]
    public sealed class SectorDefinition
    {
        public string id;
        public string name;
        public int[] loop;
        public string[] edges;
        public SegmentDefinition[] profile;
        public string[] panelColors;
        public string pattern;
        public VolumeDefinition[] volumes;
        public int seed;
    }

    [Serializable]
    public sealed class SegmentDefinition
    {
        public float height;
        public float angle;
    }

    /// <summary>Volumes of a sector; size (width in meters) is optional, zero picks the shape's usual size.</summary>
    [Serializable]
    public sealed class VolumeDefinition
    {
        public string shape;
        public string color;
        public int count;
        public float size;
    }
}
