using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>Boxes, cylinders and spheres appended to mesh data, in world coordinates.</summary>
    public static class MeshPrimitives
    {
        private const int CylinderSegments = 16;
        private const int SphereRings = 8;
        private const int SphereSegments = 14;

        /// <summary>Axis-aligned box given by its minimum and maximum corners.</summary>
        public static void Box(MeshData mesh, Vector3 min, Vector3 max)
        {
            var c = new[]
            {
                new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
            };
            var uvs = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.AddQuad(c[0], c[1], c[5], c[4], uvs);
            mesh.AddQuad(c[1], c[2], c[6], c[5], uvs);
            mesh.AddQuad(c[2], c[3], c[7], c[6], uvs);
            mesh.AddQuad(c[3], c[0], c[4], c[7], uvs);
            mesh.AddQuad(c[4], c[5], c[6], c[7], uvs);
            mesh.AddQuad(c[3], c[2], c[1], c[0], uvs);
        }

        /// <summary>Open tube between two points, faces pointing outward.</summary>
        public static void Cylinder(MeshData mesh, Vector3 from, Vector3 to, float radius)
        {
            var axis = (to - from).normalized;
            var side = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var other = Vector3.Cross(axis, side);
            for (var i = 0; i < CylinderSegments; i++)
            {
                var a = Ring(side, other, i) * radius;
                var b = Ring(side, other, i + 1) * radius;
                var u0 = (float)i / CylinderSegments;
                var u1 = (float)(i + 1) / CylinderSegments;
                var uvs = new[] { new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, 1f), new Vector2(u0, 1f) };
                AddOutward(mesh, new[] { from + a, from + b, to + b, to + a }, (a + b) * 0.5f, uvs);
            }
        }

        public static void Sphere(MeshData mesh, Vector3 center, float radius)
        {
            for (var ring = 0; ring < SphereRings; ring++)
            {
                for (var segment = 0; segment < SphereSegments; segment++)
                {
                    var a = SpherePoint(ring, segment);
                    var b = SpherePoint(ring + 1, segment);
                    var c = SpherePoint(ring + 1, segment + 1);
                    var d = SpherePoint(ring, segment + 1);
                    var uvs = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                    AddOutward(mesh, new[] { center + a * radius, center + b * radius, center + c * radius, center + d * radius }, (a + b + c + d) * 0.25f, uvs);
                }
            }
        }

        /// <summary>Ring around an axis: major radius to the tube center, minor radius of the tube.</summary>
        public static void Torus(MeshData mesh, Vector3 center, Vector3 axis, float majorRadius, float minorRadius)
        {
            var side = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var other = Vector3.Cross(axis, side);
            var uvs = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            for (var i = 0; i < CylinderSegments; i++)
            {
                var radialA = Ring(side, other, i);
                var radialB = Ring(side, other, i + 1);
                for (var j = 0; j < CylinderSegments; j++)
                {
                    var a = TubePoint(radialA, j);
                    var b = TubePoint(radialB, j);
                    var c = TubePoint(radialB, j + 1);
                    var d = TubePoint(radialA, j + 1);
                    var outward = (a + b + c + d) * 0.25f - (radialA + radialB) * 0.5f * majorRadius;
                    AddOutward(mesh, new[] { center + a, center + b, center + c, center + d }, outward, uvs);
                }
            }

            Vector3 TubePoint(Vector3 radial, int step)
            {
                var angle = step * Mathf.PI * 2f / CylinderSegments;
                return radial * (majorRadius + minorRadius * Mathf.Cos(angle)) + axis * (minorRadius * Mathf.Sin(angle));
            }
        }

        private static Vector3 Ring(Vector3 side, Vector3 other, int segment)
        {
            var angle = segment * Mathf.PI * 2f / CylinderSegments;
            return side * Mathf.Cos(angle) + other * Mathf.Sin(angle);
        }

        private static Vector3 SpherePoint(int ring, int segment)
        {
            var polar = ring * Mathf.PI / SphereRings;
            var azimuth = segment * Mathf.PI * 2f / SphereSegments;
            return new Vector3(Mathf.Sin(polar) * Mathf.Cos(azimuth), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(azimuth));
        }

        /// <summary>Quad wound so its normal agrees with the outward direction.</summary>
        private static void AddOutward(MeshData mesh, Vector3[] q, Vector3 outward, Vector2[] uvs)
        {
            var normal = Vector3.Cross(q[3] - q[0], q[2] - q[0]);
            if (Vector3.Dot(normal, outward) >= 0f)
            {
                mesh.AddQuad(q[0], q[1], q[2], q[3], uvs);
                return;
            }

            mesh.AddQuad(q[1], q[0], q[3], q[2], new[] { uvs[1], uvs[0], uvs[3], uvs[2] });
        }
    }
}
