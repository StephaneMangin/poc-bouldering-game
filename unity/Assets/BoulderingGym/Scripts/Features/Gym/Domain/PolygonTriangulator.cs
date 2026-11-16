using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>Ear clipping for simple polygons in the XZ plane, counter-clockwise seen from above.</summary>
    public static class PolygonTriangulator
    {
        private const float Epsilon = 1e-6f;

        /// <summary>Returns index triples, each counter-clockwise seen from above.</summary>
        public static List<int> Triangulate(IReadOnlyList<Vector3> points)
        {
            var remaining = new List<int>();
            for (var i = 0; i < points.Count; i++)
            {
                remaining.Add(i);
            }

            var triangles = new List<int>();
            while (remaining.Count > 3)
            {
                if (!TryClipEar(points, remaining, triangles))
                {
                    RemoveFlattestVertex(points, remaining);
                }
            }

            if (remaining.Count == 3 && Cross(points[remaining[0]], points[remaining[1]], points[remaining[2]]) > Epsilon)
            {
                triangles.AddRange(remaining);
            }

            return triangles;
        }

        private static bool TryClipEar(IReadOnlyList<Vector3> points, List<int> remaining, List<int> triangles)
        {
            for (var i = 0; i < remaining.Count; i++)
            {
                var previous = remaining[(i + remaining.Count - 1) % remaining.Count];
                var current = remaining[i];
                var next = remaining[(i + 1) % remaining.Count];
                if (!IsEar(points, remaining, previous, current, next))
                {
                    continue;
                }

                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
                remaining.RemoveAt(i);
                return true;
            }

            return false;
        }

        private static bool IsEar(IReadOnlyList<Vector3> points, List<int> remaining, int previous, int current, int next)
        {
            var a = points[previous];
            var b = points[current];
            var c = points[next];
            if (Cross(a, b, c) <= Epsilon)
            {
                return false;
            }

            foreach (var index in remaining)
            {
                if (index != previous && index != current && index != next && IsInside(points[index], a, b, c))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Drops the vertex contributing the least turn; used when only degenerate ears remain.</summary>
        private static void RemoveFlattestVertex(IReadOnlyList<Vector3> points, List<int> remaining)
        {
            var flattest = 0;
            var smallest = float.MaxValue;
            for (var i = 0; i < remaining.Count; i++)
            {
                var previous = points[remaining[(i + remaining.Count - 1) % remaining.Count]];
                var next = points[remaining[(i + 1) % remaining.Count]];
                var turn = Mathf.Abs(Cross(previous, points[remaining[i]], next));
                if (turn < smallest)
                {
                    smallest = turn;
                    flattest = i;
                }
            }

            remaining.RemoveAt(flattest);
        }

        private static float Cross(Vector3 a, Vector3 b, Vector3 c)
        {
            return (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
        }

        private static bool IsInside(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            return Cross(a, b, p) >= -Epsilon && Cross(b, c, p) >= -Epsilon && Cross(c, a, p) >= -Epsilon;
        }
    }
}
