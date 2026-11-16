using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    public enum EdgeKind
    {
        Exposed,
        Back,
        Internal,
    }

    public enum VolumeShape
    {
        Triangle,
        Wedge,
        Pyramid,
        Box,
        Dome,
    }

    public readonly struct VolumeRequest
    {
        public VolumeRequest(VolumeShape shape, string colorId, int count, float size)
        {
            Shape = shape;
            ColorId = colorId;
            Count = count;
            Size = size;
        }

        public VolumeShape Shape { get; }

        public string ColorId { get; }

        public int Count { get; }

        /// <summary>Width in meters; zero picks the shape's usual size.</summary>
        public float Size { get; }
    }

    /// <summary>Named climbing sector ("bloc"): one profile and color scheme for all its faces.</summary>
    public sealed class GymSector
    {
        private readonly List<WallFace> _faces = new();

        public GymSector(string id, string name, GymStructure structure, WallProfile profile)
        {
            Id = id;
            Name = name;
            Structure = structure;
            Profile = profile;
        }

        public string Id { get; }

        public string Name { get; }

        public GymStructure Structure { get; }

        public WallProfile Profile { get; }

        public IReadOnlyList<WallFace> Faces => _faces;

        public IReadOnlyList<string> PanelColors { get; set; } = new[] { "grey" };

        public string Pattern { get; set; } = "plain";

        public IReadOnlyList<VolumeRequest> Volumes { get; set; } = new VolumeRequest[0];

        public int Seed { get; set; }

        internal void AddFace(WallFace face) => _faces.Add(face);
    }

    /// <summary>Free-standing or perimeter wall made of adjacent sectors sharing one top height.</summary>
    public sealed class GymStructure
    {
        private readonly List<GymSector> _sectors = new();

        public GymStructure(string id, string name, float height)
        {
            Id = id;
            Name = name;
            Height = height;
        }

        public string Id { get; }

        public string Name { get; }

        public float Height { get; }

        public IReadOnlyList<GymSector> Sectors => _sectors;

        public StructureOutline Outline { get; internal set; }

        internal void AddSector(GymSector sector) => _sectors.Add(sector);
    }

    public sealed class GymLayout
    {
        public GymLayout(string name, IReadOnlyList<GymStructure> structures)
        {
            Name = name;
            Structures = structures;
        }

        public string Name { get; }

        public IReadOnlyList<GymStructure> Structures { get; }

        public Vector2 EnvelopeMin { get; set; }

        public Vector2 EnvelopeMax { get; set; }

        public float CeilingHeight { get; set; }

        public Vector3 SpawnPosition { get; set; }

        public float SpawnYawDeg { get; set; }

        public IReadOnlyDictionary<string, Color> Palette { get; set; } = new Dictionary<string, Color>();

        /// <summary>Island mast foot (on the island top), if the hall has one.</summary>
        public Vector3? MastFoot { get; set; }

        /// <summary>Raised deck along a hall wall, if the hall has one.</summary>
        public MezzanineArea Mezzanine { get; set; }

        public Color ColorOf(string id) => Palette.TryGetValue(id, out var color) ? color : Color.magenta;

        public IEnumerable<GymSector> Sectors
        {
            get
            {
                foreach (var structure in Structures)
                {
                    foreach (var sector in structure.Sectors)
                    {
                        yield return sector;
                    }
                }
            }
        }

        public IEnumerable<WallFace> Faces
        {
            get
            {
                foreach (var sector in Sectors)
                {
                    foreach (var face in sector.Faces)
                    {
                        yield return face;
                    }
                }
            }
        }
    }
}
