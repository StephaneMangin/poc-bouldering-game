using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>
    /// Procedural hold shapes in a local frame: x across, y up the wall, z out of the wall,
    /// base on the wall plane (z = 0). Each shape is a height field over an elliptic base with
    /// a wobbly outline so that no two holds look alike.
    /// </summary>
    public static class HoldMeshFactory
    {
        private const int Rings = 5;
        private const int Segments = 16;
        private const float Wobble = 0.08f;

        public static MeshData Build(HoldKind kind, int variant)
        {
            var size = HoldKindInfo.Size(kind);
            return Blob(size, HeightProfile(kind), variant);
        }

        /// <summary>Whitebox placeholder: a plain block of the hold footprint.</summary>
        public static MeshData Placeholder(HoldKind kind)
        {
            var size = HoldKindInfo.Size(kind);
            return VolumeMeshFactory.Box(size.x, size.y, size.z);
        }

        /// <summary>Relative height at normalized radius r (0 center, 1 rim) and angle (radians) around the base.</summary>
        private static Func<float, float, float> HeightProfile(HoldKind kind)
        {
            return kind switch
            {
                HoldKind.Jug => (r, angle) => Dome(r, 2.5f, 0.5f) * Mathf.Lerp(0.55f, 1f, (Mathf.Sin(angle) + 1f) * 0.5f),
                HoldKind.Edge => (r, angle) => Mathf.Pow(Mathf.Clamp01((1f - r) * 6f), 0.5f),
                HoldKind.Crimp => (r, angle) => Mathf.Pow(Mathf.Clamp01((1f - r) * 5f), 0.5f),
                HoldKind.Sloper => (r, angle) => Dome(r, 2f, 1.5f),
                HoldKind.Pinch => (r, angle) => Dome(r, 1.6f, 0.8f),
                HoldKind.Pocket => (r, angle) => Dome(r, 2f, 0.6f) * (r < 0.5f ? 0.25f + 0.75f * SmoothStep(0.15f, 0.5f, r) : 1f),
                HoldKind.Macro => (r, angle) => Dome(r, 3f, 0.45f) * Mathf.Lerp(0.6f, 1f, (Mathf.Sin(angle) + 1f) * 0.5f),
                _ => (r, angle) => Dome(r, 2f, 0.7f),
            };
        }

        private static float Dome(float r, float power, float exponent)
        {
            return Mathf.Pow(Mathf.Clamp01(1f - Mathf.Pow(r, power)), exponent);
        }

        private static float SmoothStep(float from, float to, float value)
        {
            var t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        private static MeshData Blob(Vector3 size, Func<float, float, float> height, int variant)
        {
            var mesh = new MeshData();
            var phase = variant * 1.618f;
            mesh.Vertices.Add(new Vector3(0f, 0f, size.z * height(0f, 0f)));
            mesh.Uvs.Add(new Vector2(0.5f, 0.5f));
            for (var ring = 1; ring <= Rings; ring++)
            {
                var r = (float)ring / Rings;
                for (var segment = 0; segment < Segments; segment++)
                {
                    var angle = segment * Mathf.PI * 2f / Segments;
                    var outline = 1f + Wobble * Mathf.Sin(3f * angle + phase) + Wobble * 0.6f * Mathf.Cos(5f * angle + phase * 2f);
                    var x = Mathf.Cos(angle) * r * outline;
                    var y = Mathf.Sin(angle) * r * outline;
                    var z = ring == Rings ? 0f : size.z * height(r, angle);
                    mesh.Vertices.Add(new Vector3(x * size.x * 0.5f, y * size.y * 0.5f, z));
                    mesh.Uvs.Add(new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f));
                }
            }

            AddBlobTriangles(mesh.Triangles);
            mesh.Normals.AddRange(SmoothNormals(mesh.Vertices, mesh.Triangles));
            return mesh;
        }

        private static void AddBlobTriangles(List<int> triangles)
        {
            for (var segment = 0; segment < Segments; segment++)
            {
                triangles.AddRange(new[] { 0, RingVertex(1, segment), RingVertex(1, segment + 1) });
            }

            for (var ring = 1; ring < Rings; ring++)
            {
                for (var segment = 0; segment < Segments; segment++)
                {
                    var inner = RingVertex(ring, segment);
                    var outer = RingVertex(ring + 1, segment);
                    var outerNext = RingVertex(ring + 1, segment + 1);
                    var innerNext = RingVertex(ring, segment + 1);
                    triangles.AddRange(new[] { inner, outer, outerNext, inner, outerNext, innerNext });
                }
            }
        }

        private static int RingVertex(int ring, int segment) => 1 + (ring - 1) * Segments + segment % Segments;

        private static Vector3[] SmoothNormals(List<Vector3> vertices, List<int> triangles)
        {
            var normals = new Vector3[vertices.Count];
            for (var t = 0; t < triangles.Count; t += 3)
            {
                var a = vertices[triangles[t]];
                var normal = Vector3.Cross(vertices[triangles[t + 1]] - a, vertices[triangles[t + 2]] - a);
                normals[triangles[t]] += normal;
                normals[triangles[t + 1]] += normal;
                normals[triangles[t + 2]] += normal;
            }

            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = normals[i].sqrMagnitude > 0f ? normals[i].normalized : Vector3.forward;
            }

            return normals;
        }
    }
}
