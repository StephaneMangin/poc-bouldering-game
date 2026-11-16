using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Project.Features.Gym.Domain
{
    /// <summary>Large bolt-on shape on a face, centered at surface coordinates (s, y).</summary>
    public sealed class VolumePlacement
    {
        public VolumePlacement(WallFace face, VolumeShape shape, string colorId, Vector2 surfacePosition)
        {
            Face = face;
            Shape = shape;
            ColorId = colorId;
            S = surfacePosition.x;
            Y = surfacePosition.y;
        }

        public WallFace Face { get; }

        public VolumeShape Shape { get; }

        public string ColorId { get; }

        public float S { get; }

        public float Y { get; }

        /// <summary>Width across the wall, meters.</summary>
        public float Size { get; internal set; }

        /// <summary>How far it sticks out of the wall, meters.</summary>
        public float Depth { get; internal set; }

        public float RollDeg { get; internal set; }
    }

    /// <summary>Places a sector's volumes on its faces before problems are set around them.</summary>
    public static class VolumePlanner
    {
        private const int PlacementTries = 12;
        private const float MinFaceWidth = 1.2f;
        private const float BottomClearance = 0.9f;
        private const float TopClearance = 0.5f;
        private const float FootprintFactor = 0.6f;

        public static List<VolumePlacement> Place(GymSector sector, Random random, IReadOnlyDictionary<WallFace, FaceOccupancy> occupancy)
        {
            var placements = new List<VolumePlacement>();
            var faces = WideFaces(sector);
            if (faces.Count == 0)
            {
                return placements;
            }

            foreach (var request in sector.Volumes)
            {
                for (var i = 0; i < request.Count; i++)
                {
                    var face = random.Pick(faces);
                    var placement = TryPlace(face, request, random, occupancy[face]);
                    if (placement != null)
                    {
                        placements.Add(placement);
                    }
                }
            }

            return placements;
        }

        private static List<WallFace> WideFaces(GymSector sector)
        {
            var faces = new List<WallFace>();
            foreach (var face in sector.Faces)
            {
                if (face.Width >= MinFaceWidth)
                {
                    faces.Add(face);
                }
            }

            return faces;
        }

        private static VolumePlacement TryPlace(WallFace face, VolumeRequest request, Random random, FaceOccupancy occupancy)
        {
            var size = random.Range(SizeRange(request.Shape));
            var radius = size * FootprintFactor;
            for (var attempt = 0; attempt < PlacementTries; attempt++)
            {
                var y = random.Range(BottomClearance + radius, face.Height - TopClearance - radius);
                var range = face.RangeAt(y);
                if (range.Length < 2f * radius || CrossesKink(face.Profile, y, radius))
                {
                    continue;
                }

                var s = random.Range(range.Min + radius, range.Max - radius);
                if (!occupancy.IsFree(s, y, radius))
                {
                    continue;
                }

                occupancy.Add(s, y, radius);
                return new VolumePlacement(face, request.Shape, request.ColorId, new Vector2(s, y))
                {
                    Size = size,
                    Depth = size * random.Range(0.35f, 0.5f),
                    RollDeg = random.Range(-30f, 30f),
                };
            }

            return null;
        }

        private static FloatRange SizeRange(VolumeShape shape)
        {
            return shape switch
            {
                VolumeShape.Triangle => new FloatRange(0.6f, 1.1f),
                VolumeShape.Wedge => new FloatRange(0.5f, 0.9f),
                VolumeShape.Pyramid => new FloatRange(0.6f, 1.0f),
                VolumeShape.Box => new FloatRange(0.4f, 0.7f),
                _ => new FloatRange(0.5f, 0.8f),
            };
        }

        /// <summary>A volume straddling a profile break would cut through the wall.</summary>
        private static bool CrossesKink(WallProfile profile, float y, float radius)
        {
            foreach (var level in profile.Breakpoints)
            {
                if (level > 0f && level < profile.Height && Mathf.Abs(level - y) < radius)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
