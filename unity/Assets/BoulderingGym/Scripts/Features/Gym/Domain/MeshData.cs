using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>
    /// Engine-agnostic triangle soup with flat normals. Triangles are given clockwise as seen
    /// from their visible side, which is Unity's front-face winding.
    /// </summary>
    public sealed class MeshData
    {
        public List<Vector3> Vertices { get; } = new();

        public List<Vector3> Normals { get; } = new();

        public List<Vector2> Uvs { get; } = new();

        public List<int> Triangles { get; } = new();

        public bool IsEmpty => Triangles.Count == 0;

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-12f)
            {
                return;
            }

            normal.Normalize();
            var first = Vertices.Count;
            Vertices.Add(a);
            Vertices.Add(b);
            Vertices.Add(c);
            Normals.Add(normal);
            Normals.Add(normal);
            Normals.Add(normal);
            Uvs.Add(uvA);
            Uvs.Add(uvB);
            Uvs.Add(uvC);
            Triangles.Add(first);
            Triangles.Add(first + 1);
            Triangles.Add(first + 2);
        }

        /// <summary>Corners as seen from the visible side: bottom-left, bottom-right, top-right, top-left.</summary>
        public void AddQuad(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topRight, Vector3 topLeft, Vector2[] uvs)
        {
            AddTriangle(bottomLeft, topLeft, topRight, uvs[0], uvs[3], uvs[2]);
            AddTriangle(bottomLeft, topRight, bottomRight, uvs[0], uvs[2], uvs[1]);
        }

        public void AddDoubleSidedQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2[] uvs)
        {
            AddQuad(a, b, c, d, uvs);
            AddQuad(b, a, d, c, new[] { uvs[1], uvs[0], uvs[3], uvs[2] });
        }

        /// <summary>
        /// Appends a mesh defined in a local frame (x right, y up, z out of the wall) placed at origin
        /// with the given orthonormal axes and uniform scale.
        /// </summary>
        public void AppendTransformed(MeshData source, Vector3 origin, Vector3 right, Vector3 up, Vector3 forward, float scale)
        {
            var first = Vertices.Count;
            for (var i = 0; i < source.Vertices.Count; i++)
            {
                var local = source.Vertices[i] * scale;
                Vertices.Add(origin + right * local.x + up * local.y + forward * local.z);
                var n = source.Normals[i];
                Normals.Add((right * n.x + up * n.y + forward * n.z).normalized);
                Uvs.Add(source.Uvs[i]);
            }

            foreach (var index in source.Triangles)
            {
                Triangles.Add(first + index);
            }
        }
    }
}
