using UnityEngine;
using Random = System.Random;

namespace Project.Features.Gym.Domain
{
    /// <summary>Textures of the hall: crash mats with taped seams, corrugated cladding and roof sheeting.</summary>
    public static class HallPainter
    {
        private const int FloorPixelsPerMeter = 24;
        private const float MatCover = 7f;
        private const float SeamWidth = 0.05f;
        private const float CoverShadeSpread = 0.06f;
        private static readonly Color Mat = new(0.62f, 0.63f, 0.64f);
        private static readonly Color Seam = new(0.27f, 0.27f, 0.29f);
        private static readonly Color Tape = new(0.95f, 0.45f, 0.15f);
        private static readonly Color Cladding = new(0.64f, 0.66f, 0.68f);
        private static readonly Color Roof = new(0.47f, 0.49f, 0.50f);

        /// <summary>Whole floor in one texture (UV 0..1 over the hall): large mat covers with dark irregular seams and orange tape marks.</summary>
        public static PanelCanvas Floor(Vector2 size)
        {
            var width = Mathf.CeilToInt(size.x * FloorPixelsPerMeter);
            var height = Mathf.CeilToInt(size.y * FloorPixelsPerMeter);
            var pixels = new Color32[width * height];
            var random = new Random(11);
            var tapes = TapeMarks(random, size);
            var covers = new MatCovers(size);
            for (var y = 0; y < height; y++)
            {
                var z = (y + 0.5f) / FloorPixelsPerMeter;
                for (var x = 0; x < width; x++)
                {
                    var u = (x + 0.5f) / FloorPixelsPerMeter;
                    pixels[y * width + x] = FloorColor(new Vector2(u, z), tapes, covers);
                }
            }

            return new PanelCanvas(width, height, pixels) { MetersWide = size.x, MetersHigh = size.y };
        }

        /// <summary>One meter of corrugation with vertical ribs, tiled on the hall walls.</summary>
        public static PanelCanvas CorrugatedCladding() => Corrugation(Cladding, 0.15f);

        /// <summary>One meter of roof sheeting, tiled on the ceiling.</summary>
        public static PanelCanvas RoofSheeting() => Corrugation(Roof, 0.2f);

        private static Color32 FloorColor(Vector2 point, Vector4[] tapes, MatCovers covers)
        {
            var (shade, seamDistance) = covers.At(point);
            var color = Mat * shade;
            foreach (var tape in tapes)
            {
                if (Mathf.Abs(point.x - tape.x) < tape.z && Mathf.Abs(point.y - tape.y) < tape.w)
                {
                    color = Tape;
                }
            }

            // Seam edge blended over one texel so the lines do not step
            const float texel = 1f / FloorPixelsPerMeter;
            var seam = Mathf.InverseLerp(SeamWidth * 0.5f + texel * 0.5f, SeamWidth * 0.5f - texel * 0.5f, seamDistance);
            return Color.Lerp(color, Seam, seam);
        }

        /// <summary>Large mat covers, one per jittered grid cell, each with its own shade.</summary>
        private sealed class MatCovers
        {
            private readonly Vector2[,] _seeds;
            private readonly float[,] _shades;

            public MatCovers(Vector2 size)
            {
                // One spare cell on every side so points on the hall edge find all their neighbors
                var columns = Mathf.CeilToInt(size.x / MatCover) + 2;
                var rows = Mathf.CeilToInt(size.y / MatCover) + 2;
                _seeds = new Vector2[columns, rows];
                _shades = new float[columns, rows];
                var random = new Random(23);
                for (var i = 0; i < columns; i++)
                {
                    for (var j = 0; j < rows; j++)
                    {
                        var jitter = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
                        _seeds[i, j] = (new Vector2(i - 1, j - 1) + new Vector2(0.15f, 0.15f) + jitter * 0.7f) * MatCover;
                        _shades[i, j] = 1f - CoverShadeSpread * (float)random.NextDouble();
                    }
                }
            }

            /// <summary>Shade of the cover under a floor point and the distance to its nearest seam.</summary>
            public (float shade, float seamDistance) At(Vector2 point)
            {
                var column = Mathf.FloorToInt(point.x / MatCover) + 1;
                var row = Mathf.FloorToInt(point.y / MatCover) + 1;
                var nearest = (cell: new Vector2Int(column, row), distance: float.MaxValue);
                var second = nearest;
                for (var i = column - 1; i <= column + 1; i++)
                {
                    for (var j = row - 1; j <= row + 1; j++)
                    {
                        var distance = Vector2.Distance(point, _seeds[i, j]);
                        if (distance < nearest.distance)
                        {
                            second = nearest;
                            nearest = (new Vector2Int(i, j), distance);
                        }
                        else if (distance < second.distance)
                        {
                            second = (new Vector2Int(i, j), distance);
                        }
                    }
                }

                var near = _seeds[nearest.cell.x, nearest.cell.y];
                var far = _seeds[second.cell.x, second.cell.y];
                var seam = Vector2.Dot((near + far) * 0.5f - point, (far - near).normalized);
                return (_shades[nearest.cell.x, nearest.cell.y], seam);
            }
        }

        /// <summary>Short tape strips: center (x, y) and half extents (z, w), meters.</summary>
        private static Vector4[] TapeMarks(Random random, Vector2 size)
        {
            var tapes = new Vector4[24];
            for (var i = 0; i < tapes.Length; i++)
            {
                var horizontal = random.Next(2) == 0;
                tapes[i] = new Vector4(
                    (float)random.NextDouble() * size.x,
                    (float)random.NextDouble() * size.y,
                    horizontal ? 0.25f : 0.025f,
                    horizontal ? 0.025f : 0.25f);
            }

            return tapes;
        }

        private static PanelCanvas Corrugation(Color baseColor, float period)
        {
            const int size = 128;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var t = x / (float)size;
                    var wave = 0.88f + 0.12f * Mathf.Sin(t / period * Mathf.PI * 2f);
                    pixels[y * size + x] = baseColor * wave;
                }
            }

            return new PanelCanvas(size, size, pixels) { MetersWide = 1f, MetersHigh = 1f };
        }
    }
}
