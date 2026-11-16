using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Project.Features.Gym.Domain
{
    /// <summary>Painted artwork of one face, in the face surface coordinates (meters).</summary>
    public sealed class PanelCanvas
    {
        public PanelCanvas(int width, int height, Color32[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public int Width { get; }

        public int Height { get; }

        /// <summary>Row-major from the bottom row (v = 0) up.</summary>
        public Color32[] Pixels { get; }

        /// <summary>Along-wall coordinate of the left texture edge and covered extent, meters.</summary>
        public float OriginU { get; set; }

        public float MetersWide { get; set; }

        public float MetersHigh { get; set; }
    }

    /// <summary>
    /// Paints the plywood panels of a face: color regions following the sector pattern seen on the
    /// photos (bands, diagonals, triangles, star, frames, split, ermine, Breton flag, lattice), panel joints,
    /// bolt holes on a 20 cm grid and soft chalk stains.
    /// </summary>
    public static class PanelPainter
    {
        private const float BoltSpacing = 0.2f;
        private const float BoltRadius = 0.009f;
        private const float BoltShade = 0.35f;
        private const float StainScale = 0.6f;
        private const float StainStrength = 0.08f;
        private const float PanelSize = 1.22f;
        private const float SpeckleSize = 0.006f;
        private const int LightSpecklePercent = 5;
        private const int DarkSpecklePercent = 2;
        private static readonly Color ChalkWhite = new(0.95f, 0.95f, 0.93f);
        private const float JointWidth = 0.008f;
        private const float JointShade = 0.72f;

        /// <summary>Chalk spots are the surface coordinates (s, y) of the hand holds of the face.</summary>
        public static PanelCanvas Paint(WallFace face, GymLayout layout, int pixelsPerMeter, int maxSize, IReadOnlyList<Vector2> chalkSpots)
        {
            var extent = UExtent(face);
            var metersHigh = face.Profile.ArcLengthAt(face.Height);
            var width = Mathf.Clamp(Mathf.CeilToInt(extent.Length * pixelsPerMeter), 8, maxSize);
            var height = Mathf.Clamp(Mathf.CeilToInt(metersHigh * pixelsPerMeter), 8, maxSize);
            var colors = SectorColors(face.Sector, layout);
            var region = Pattern(face, extent, metersHigh);
            var chalk = new ChalkMap(extent, metersHigh, chalkSpots);
            var pixels = new Color32[width * height];
            var texel = new Vector2(extent.Length / width, metersHigh / height);
            for (var y = 0; y < height; y++)
            {
                var v = (y + 0.5f) / height * metersHigh;
                for (var x = 0; x < width; x++)
                {
                    var u = extent.Min + (x + 0.5f) / width * extent.Length;
                    var color = Supersampled(region, colors, new Vector2(u, v), texel);
                    pixels[y * width + x] = Shade(Color.Lerp(color, ChalkWhite, chalk.Amount(u, v)), u, v);
                }
            }

            return new PanelCanvas(width, height, pixels) { OriginU = extent.Min, MetersWide = extent.Length, MetersHigh = metersHigh };
        }

        /// <summary>Averages four samples inside the texel so color region borders are not jagged.</summary>
        private static Color Supersampled(Func<float, float, int> region, Color[] colors, Vector2 center, Vector2 texel)
        {
            var sum = Color.clear;
            for (var i = 0; i < 4; i++)
            {
                var offset = new Vector2((i % 2 - 0.5f) * 0.5f * texel.x, (i / 2 - 0.5f) * 0.5f * texel.y);
                sum += colors[ColorIndex(region(center.x + offset.x, center.y + offset.y), colors.Length)];
            }

            return sum * 0.25f;
        }

        private static FloatRange UExtent(WallFace face)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var level in face.Profile.Breakpoints)
            {
                var range = face.RangeAt(level);
                min = Mathf.Min(min, range.Min);
                max = Mathf.Max(max, range.Max);
            }

            return new FloatRange(min, max);
        }

        private static Color[] SectorColors(GymSector sector, GymLayout layout)
        {
            var colors = new Color[sector.PanelColors.Count];
            for (var i = 0; i < colors.Length; i++)
            {
                colors[i] = layout.ColorOf(sector.PanelColors[i]);
            }

            return colors;
        }

        private static Color32 Shade(Color color, float u, float v)
        {
            var stain = 1f + StainStrength * (ValueNoise(u / StainScale, v / StainScale) - 0.5f);
            var row = Mathf.FloorToInt(v / BoltSpacing);
            var boltU = u + (row % 2 == 0 ? 0f : BoltSpacing * 0.5f);
            var du = Mathf.Repeat(boltU, BoltSpacing) - BoltSpacing * 0.5f;
            var dv = Mathf.Repeat(v, BoltSpacing) - BoltSpacing * 0.5f;
            var bolt = du * du + dv * dv < BoltRadius * BoltRadius ? BoltShade : 1f;
            var joint = Mathf.Repeat(u, PanelSize) < JointWidth || Mathf.Repeat(v, PanelSize) < JointWidth ? JointShade : 1f;
            var speckle = Hash(Mathf.FloorToInt(u / SpeckleSize), Mathf.FloorToInt(v / SpeckleSize), 5) % 100;
            var grain = speckle < LightSpecklePercent ? 1.3f : speckle < LightSpecklePercent + DarkSpecklePercent ? 0.8f : 1f;
            return (Color32)(color * (stain * bolt * joint * grain));
        }

        private static Func<float, float, int> Pattern(WallFace face, FloatRange extent, float metersHigh)
        {
            var random = new Random(face.Sector.Seed ^ StableHash(face.Id));
            return face.Sector.Pattern switch
            {
                "bands" => Bands(random),
                "hull" => Hull(random, extent),
                "diagonal" => Diagonal(random, extent, metersHigh),
                "triangles" => Triangles(random),
                "star" => Star(random, extent, metersHigh),
                "frames" => Frames(extent, metersHigh),
                "split" => Split(random, extent, metersHigh),
                "ermine" => Ermine(),
                "gwennhadu" => GwennHaDu(extent, metersHigh),
                "lattice" => Lattice(),
                _ => (u, v) => 0,
            };
        }

        /// <summary>Zigzag stripes across the wall, as on the island walls.</summary>
        private static Func<float, float, int> Bands(Random random)
        {
            var bandHeight = Range(random, 0.7f, 1.1f);
            var period = Range(random, 1.6f, 3f);
            var amplitude = Range(random, 0.15f, 0.45f);
            var slope = Range(random, -0.15f, 0.15f);
            return (u, v) =>
            {
                var zigzag = amplitude * (Mathf.Abs(Mathf.Repeat(u / period, 1f) * 2f - 1f) * 2f - 1f);
                return Mathf.FloorToInt((v + zigzag + slope * u) / bandHeight);
            };
        }

        /// <summary>
        /// Island ship hull: grey base sawn by large teal triangles, then wide chevron bands cycling
        /// through the remaining sector colors (photo image_005).
        /// </summary>
        private static Func<float, float, int> Hull(Random random, FloatRange extent)
        {
            var baseHeight = Range(random, 1.1f, 1.6f);
            var toothWidth = Range(random, 1.8f, 2.8f);
            var toothPhase = Range(random, 0f, toothWidth);
            var chevronCenter = extent.Min + extent.Length * Range(random, 0.25f, 0.75f);
            var chevronSlope = Range(random, 0.15f, 0.35f) * (random.Next(2) == 0 ? 1f : -1f);
            var bandHeight = Range(random, 0.55f, 0.85f);
            return (u, v) =>
            {
                if (v < baseHeight)
                {
                    var tooth = Mathf.Abs(Mathf.Repeat(u + toothPhase, toothWidth) / toothWidth * 2f - 1f);
                    return v < baseHeight * (1f - tooth) ? 1 : 0;
                }

                var chevron = v - baseHeight - Mathf.Abs(u - chevronCenter) * chevronSlope;
                return 2 + Mathf.FloorToInt(chevron / bandHeight);
            };
        }

        /// <summary>Background, one diagonal band and a lower corner triangle.</summary>
        private static Func<float, float, int> Diagonal(Random random, FloatRange extent, float metersHigh)
        {
            var angle = Range(random, 25f, 55f) * Mathf.Deg2Rad * (random.Next(2) == 0 ? 1f : -1f);
            var normal = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
            var center = new Vector2(extent.Min + extent.Length * Range(random, 0.3f, 0.7f), metersHigh * Range(random, 0.4f, 0.65f));
            var halfWidth = Range(random, 0.5f, 0.8f);
            var cornerHeight = Range(random, 0.8f, 1.6f);
            return (u, v) =>
            {
                var distance = Vector2.Dot(new Vector2(u, v) - center, normal);
                if (Mathf.Abs(distance) < halfWidth)
                {
                    return 1;
                }

                return v < cornerHeight * (1f - (u - extent.Min) / extent.Length) ? 2 : 0;
            };
        }

        /// <summary>Triangulated facets, each painted with a random sector color.</summary>
        private static Func<float, float, int> Triangles(Random random)
        {
            var cell = Range(random, 0.9f, 1.4f);
            var salt = random.Next();
            return (u, v) =>
            {
                var cu = Mathf.FloorToInt(u / cell);
                var cv = Mathf.FloorToInt(v / cell);
                var upper = Mathf.Repeat(u, cell) + Mathf.Repeat(v, cell) > cell ? 1 : 0;
                return Hash(cu, cv * 2 + upper, salt) % 5 < 2 ? 0 : 1 + Hash(cu, cv * 2 + upper, salt + 1) % 2;
            };
        }

        /// <summary>Wedges radiating from a point above the middle, grey at the foot.</summary>
        private static Func<float, float, int> Star(Random random, FloatRange extent, float metersHigh)
        {
            var center = new Vector2(extent.Min + extent.Length * Range(random, 0.35f, 0.65f), metersHigh * Range(random, 0.55f, 0.75f));
            var wedges = random.Next(7, 11);
            var turn = Range(random, 0f, Mathf.PI);
            var foot = Range(random, 0.6f, 1.1f);
            return (u, v) =>
            {
                if (v < foot)
                {
                    return 2;
                }

                var angle = Mathf.Atan2(v - center.y, u - center.x) + turn;
                return Mathf.FloorToInt(Mathf.Repeat(angle, Mathf.PI * 2f) / (Mathf.PI * 2f) * wedges) % 2;
            };
        }

        /// <summary>Nested frames following the face outline.</summary>
        private static Func<float, float, int> Frames(FloatRange extent, float metersHigh)
        {
            const float frameWidth = 0.45f;
            return (u, v) =>
            {
                var border = Mathf.Min(Mathf.Min(u - extent.Min, extent.Max - u), Mathf.Min(v, metersHigh - v));
                return Mathf.FloorToInt(border / frameWidth);
            };
        }

        /// <summary>Two color halves split by a slanted line, with a top band of the third color.</summary>
        private static Func<float, float, int> Split(Random random, FloatRange extent, float metersHigh)
        {
            var bottom = extent.Min + extent.Length * Range(random, 0.3f, 0.7f);
            var top = bottom + Range(random, -0.8f, 0.8f);
            var band = metersHigh - Range(random, 0.6f, 1.2f);
            return (u, v) =>
            {
                if (v > band)
                {
                    return 2;
                }

                var split = Mathf.Lerp(bottom, top, v / metersHigh);
                return u < split ? 0 : 1;
            };
        }

        /// <summary>White field sown with stylized black ermine spots (Breton flag motif).</summary>
        private static Func<float, float, int> Ermine()
        {
            const float spacing = 0.7f;
            return (u, v) =>
            {
                var row = Mathf.FloorToInt(v / spacing);
                var du = Mathf.Repeat(u + (row % 2) * spacing * 0.5f, spacing) - spacing * 0.5f;
                var dv = Mathf.Repeat(v, spacing) - spacing * 0.5f;
                var body = Mathf.Abs(du) * 2.2f + Mathf.Abs(dv + 0.04f) < 0.13f && dv < 0.06f;
                var crown = dv > 0.08f && dv < 0.14f && (Mathf.Abs(du) < 0.025f || Mathf.Abs(Mathf.Abs(du) - 0.07f) < 0.025f);
                return body || crown ? 1 : 0;
            };
        }

        /// <summary>Breton flag across the face: nine black and white stripes, ermine canton at the top left.</summary>
        private static Func<float, float, int> GwennHaDu(FloatRange extent, float metersHigh)
        {
            var stripe = metersHigh / 9f;
            var ermine = Ermine();
            return (u, v) =>
            {
                var fromTop = metersHigh - v;
                var inCanton = fromTop < stripe * 5f && u - extent.Min < extent.Length * 0.4f;
                if (inCanton)
                {
                    return ermine(u, v);
                }

                return Mathf.FloorToInt(fromTop / stripe) % 2 == 0 ? 1 : 0;
            };
        }

        /// <summary>Wooden slat lattice over the first color.</summary>
        private static Func<float, float, int> Lattice()
        {
            const float spacing = 0.15f;
            const float slat = 0.035f;
            return (u, v) => Mathf.Repeat(u, spacing) < slat || Mathf.Repeat(v, spacing) < slat ? 1 : 0;
        }

        private static float Range(Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        /// <summary>Same value on every run and runtime, unlike string.GetHashCode.</summary>
        private static int StableHash(string text)
        {
            unchecked
            {
                var hash = 17;
                foreach (var character in text)
                {
                    hash = hash * 31 + character;
                }

                return hash;
            }
        }

        private static int Mod(int value, int count) => ((value % count) + count) % count;

        /// <summary>Region indices wrap over the colors; hull bands (2 and up) wrap over the colors after the first two.</summary>
        private static int ColorIndex(int region, int count)
        {
            return region < 2 || count <= 2 ? Mod(region, count) : 2 + Mod(region - 2, count - 2);
        }

        private static int Hash(int x, int y, int salt)
        {
            unchecked
            {
                var h = x * 374761393 + y * 668265263 + salt * 2147483647;
                h = (h ^ (h >> 13)) * 1274126177;
                return (h ^ (h >> 16)) & int.MaxValue;
            }
        }

        private static float ValueNoise(float x, float y)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var tx = SmoothStep(x - x0);
            var ty = SmoothStep(y - y0);
            var a = Lerp(Corner(x0, y0), Corner(x0 + 1, y0), tx);
            var b = Lerp(Corner(x0, y0 + 1), Corner(x0 + 1, y0 + 1), tx);
            return Lerp(a, b, ty);
        }

        private static float Corner(int x, int y) => (Hash(x, y, 17) % 1024) / 1023f;

        private static float SmoothStep(float t) => t * t * (3f - 2f * t);

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>Low resolution map of chalk smudges spread above and around the hand holds.</summary>
        private sealed class ChalkMap
        {
            private const float CellsPerMeter = 16f;
            private const float SpotRadius = 0.13f;
            private const float SpotLift = 0.06f;
            private const float MaxAmount = 0.45f;

            private readonly float[,] _cells;
            private readonly FloatRange _extent;

            public ChalkMap(FloatRange extent, float metersHigh, IReadOnlyList<Vector2> spots)
            {
                _extent = extent;
                _cells = new float[Mathf.CeilToInt(extent.Length * CellsPerMeter) + 2, Mathf.CeilToInt(metersHigh * CellsPerMeter) + 2];
                foreach (var spot in spots ?? new Vector2[0])
                {
                    Splat(spot.x, spot.y + SpotLift);
                }
            }

            public float Amount(float u, float v)
            {
                var x = Mathf.Clamp((u - _extent.Min) * CellsPerMeter, 0f, _cells.GetLength(0) - 1.001f);
                var y = Mathf.Clamp(v * CellsPerMeter, 0f, _cells.GetLength(1) - 1.001f);
                var x0 = (int)x;
                var y0 = (int)y;
                var top = Lerp(_cells[x0, y0 + 1], _cells[x0 + 1, y0 + 1], x - x0);
                var bottom = Lerp(_cells[x0, y0], _cells[x0 + 1, y0], x - x0);
                return Mathf.Min(MaxAmount, Lerp(bottom, top, y - y0));
            }

            private void Splat(float s, float y)
            {
                var radius = Mathf.CeilToInt(SpotRadius * 2f * CellsPerMeter);
                var cx = Mathf.RoundToInt((s - _extent.Min) * CellsPerMeter);
                var cy = Mathf.RoundToInt(y * CellsPerMeter);
                for (var x = Mathf.Max(0, cx - radius); x <= Mathf.Min(_cells.GetLength(0) - 1, cx + radius); x++)
                {
                    for (var z = Mathf.Max(0, cy - radius); z <= Mathf.Min(_cells.GetLength(1) - 1, cy + radius); z++)
                    {
                        var d = new Vector2(x - cx, z - cy) / (SpotRadius * CellsPerMeter);
                        _cells[x, z] += 0.5f * Mathf.Exp(-d.sqrMagnitude);
                    }
                }
            }
        }
    }
}
