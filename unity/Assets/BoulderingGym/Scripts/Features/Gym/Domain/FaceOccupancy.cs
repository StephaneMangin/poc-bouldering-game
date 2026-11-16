using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Project.Features.Gym.Domain
{
    /// <summary>Discs already taken on a face (volumes and holds), in surface coordinates.</summary>
    public sealed class FaceOccupancy
    {
        private readonly List<Vector3> _discs = new();

        public bool IsFree(float s, float y, float radius)
        {
            foreach (var disc in _discs)
            {
                var dx = disc.x - s;
                var dy = disc.y - y;
                var minDistance = disc.z + radius;
                if (dx * dx + dy * dy < minDistance * minDistance)
                {
                    return false;
                }
            }

            return true;
        }

        public void Add(float s, float y, float radius) => _discs.Add(new Vector3(s, y, radius));
    }

    internal static class RandomExtensions
    {
        public static float Range(this Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        public static float Range(this Random random, FloatRange range) => random.Range(range.Min, range.Max);

        public static T Pick<T>(this Random random, IReadOnlyList<T> items) => items[random.Next(items.Count)];

        public static float Sign(this Random random) => random.Next(2) == 0 ? -1f : 1f;
    }
}
