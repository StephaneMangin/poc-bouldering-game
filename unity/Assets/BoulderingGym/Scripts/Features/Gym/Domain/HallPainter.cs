using UnityEngine;
using Random = System.Random;

namespace Project.Features.Gym.Domain
{
    /// <summary>Textures of the hall: crash mats with taped seams, corrugated cladding and roof sheeting.</summary>
    public static class HallPainter
    {
        private const int FloorPixelsPerMeter = 24;
        private const float MatWidth = 2f;
        private const float MatLength = 4f;
        private const float SeamWidth = 0.03f;
        private const float CreaseScale = 0.35f;
        private static readonly Color Mat = new(0.74f, 0.75f, 0.74f);
        private static readonly Color Seam = new(0.36f, 0.36f, 0.37f);
        private static readonly Color Tape = new(0.95f, 0.45f, 0.15f);
        private static readonly Color Cladding = new(0.64f, 0.66f, 0.68f);
        private static readonly Color Roof = new(0.47f, 0.49f, 0.50f);

        /// <summary>Whole floor in one texture (UV 0..1 over the hall): staggered mats and orange tape marks.</summary>
        public static PanelCanvas Floor(Vector2 size)
        {
            var width = Mathf.CeilToInt(size.x * FloorPixelsPerMeter);
            var height = Mathf.CeilToInt(size.y * FloorPixelsPerMeter);
            var pixels = new Color32[width * height];
            var random = new Random(11);
            var tapes = TapeMarks(random, size);
            for (var y = 0; y < height; y++)
            {
                var z = (y + 0.5f) / FloorPixelsPerMeter;
                for (var x = 0; x < width; x++)
                {
                    var u = (x + 0.5f) / FloorPixelsPerMeter;
                    pixels[y * width + x] = FloorColor(u, z, tapes);
                }
            }

            return new PanelCanvas(width, height, pixels) { MetersWide = size.x, MetersHigh = size.y };
        }

        /// <summary>One meter of corrugation with vertical ribs, tiled on the hall walls.</summary>
        public static PanelCanvas CorrugatedCladding() => Corrugation(Cladding, 0.15f);

        /// <summary>One meter of roof sheeting, tiled on the ceiling.</summary>
        public static PanelCanvas RoofSheeting() => Corrugation(Roof, 0.2f);

        private static Color32 FloorColor(float u, float z, Vector4[] tapes)
        {
            var row = Mathf.FloorToInt(z / MatLength);
            var shiftedU = u + (row % 2) * MatWidth * 0.5f;
            var onSeam = Mathf.Repeat(shiftedU, MatWidth) < SeamWidth || Mathf.Repeat(z, MatLength) < SeamWidth;
            if (onSeam)
            {
                return Seam;
            }

            foreach (var tape in tapes)
            {
                if (Mathf.Abs(u - tape.x) < tape.z && Mathf.Abs(z - tape.y) < tape.w)
                {
                    return Tape;
                }
            }

            var crease = 0.94f + 0.06f * Mathf.Abs(Mathf.Sin(u / CreaseScale * 3.1f + Mathf.Sin(z / CreaseScale) * 2f));
            return Mat * crease;
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
