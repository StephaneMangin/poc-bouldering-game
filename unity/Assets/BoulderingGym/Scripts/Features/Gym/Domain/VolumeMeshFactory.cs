using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>
    /// Faceted bolt-on volumes in the hold local frame (x across, y up the wall, z out of the wall),
    /// base on the wall plane, flat shaded like plywood shapes.
    /// </summary>
    public static class VolumeMeshFactory
    {
        /// <summary>Lies behind every facet of a convex shape standing on the wall plane.</summary>
        private static readonly Vector3 BehindWall = new(0f, 0f, -1f);

        public static MeshData Build(VolumeShape shape, float size, float depth)
        {
            return shape switch
            {
                VolumeShape.Triangle => Triangle(size, depth),
                VolumeShape.Pyramid => Pyramid(size, depth),
                VolumeShape.Wedge => Wedge(size, size * 0.6f, depth),
                VolumeShape.Box => Box(size, size * 0.7f, depth),
                _ => Dome(size, depth),
            };
        }

        /// <summary>Block standing out of the wall: front, top, bottom and both sides.</summary>
        public static MeshData Box(float width, float height, float depth)
        {
            var mesh = new MeshData();
            var x = width * 0.5f;
            var y = height * 0.5f;
            var back = new[] { new Vector3(-x, -y, 0f), new Vector3(x, -y, 0f), new Vector3(x, y, 0f), new Vector3(-x, y, 0f) };
            var front = new[] { back[0] + Vector3.forward * depth, back[1] + Vector3.forward * depth, back[2] + Vector3.forward * depth, back[3] + Vector3.forward * depth };
            AddFacet(mesh, front[1], front[0], front[3], front[2]);
            AddFacet(mesh, back[2], back[3], front[3], front[2]);
            AddFacet(mesh, back[0], back[1], front[1], front[0]);
            AddFacet(mesh, back[3], back[0], front[0], front[3]);
            AddFacet(mesh, back[1], back[2], front[2], front[1]);
            return mesh;
        }

        private static MeshData Triangle(float size, float depth)
        {
            var radius = size / Mathf.Sqrt(3f);
            var apex = new Vector3(0f, -size * 0.05f, depth);
            var mesh = new MeshData();
            for (var i = 0; i < 3; i++)
            {
                var a = Corner(radius, 90f + 120f * i);
                var b = Corner(radius, 90f + 120f * (i + 1));
                AddSide(mesh, a, b, apex);
            }

            return mesh;
        }

        private static MeshData Pyramid(float size, float depth)
        {
            var radius = size * 0.5f * Mathf.Sqrt(2f);
            var apex = new Vector3(size * 0.05f, size * 0.08f, depth);
            var mesh = new MeshData();
            for (var i = 0; i < 4; i++)
            {
                AddSide(mesh, Corner(radius, 45f + 90f * i), Corner(radius, 45f + 90f * (i + 1)), apex);
            }

            return mesh;
        }

        /// <summary>Raised along its top edge: a slanted front facing down and a flat top shelf.</summary>
        private static MeshData Wedge(float width, float height, float depth)
        {
            var mesh = new MeshData();
            var x = width * 0.5f;
            var y = height * 0.5f;
            var bottomLeft = new Vector3(-x, -y, 0f);
            var bottomRight = new Vector3(x, -y, 0f);
            var topRight = new Vector3(x, y, 0f);
            var topLeft = new Vector3(-x, y, 0f);
            var raisedRight = topRight + Vector3.forward * depth;
            var raisedLeft = topLeft + Vector3.forward * depth;
            AddFacet(mesh, bottomRight, bottomLeft, raisedLeft, raisedRight);
            AddFacet(mesh, topRight, topLeft, raisedLeft, raisedRight);
            AddSide(mesh, bottomRight, topRight, raisedRight);
            AddSide(mesh, topLeft, bottomLeft, raisedLeft);
            return mesh;
        }

        private static MeshData Dome(float size, float depth)
        {
            const int rings = 4;
            const int segments = 12;
            var mesh = new MeshData();
            for (var ring = 0; ring < rings; ring++)
            {
                for (var segment = 0; segment < segments; segment++)
                {
                    var a = DomePoint(size, depth, ring, segment, rings, segments);
                    var b = DomePoint(size, depth, ring + 1, segment, rings, segments);
                    var c = DomePoint(size, depth, ring + 1, segment + 1, rings, segments);
                    var d = DomePoint(size, depth, ring, segment + 1, rings, segments);
                    AddFacet(mesh, a, b, c, d);
                }
            }

            return mesh;
        }

        private static Vector3 DomePoint(float size, float depth, int ring, int segment, int rings, int segments)
        {
            var r = (float)ring / rings;
            var angle = segment * Mathf.PI * 2f / segments;
            var height = Mathf.Sqrt(Mathf.Clamp01(1f - r * r));
            return new Vector3(Mathf.Cos(angle) * r * size * 0.5f, Mathf.Sin(angle) * r * size * 0.5f, height * depth);
        }

        private static Vector3 Corner(float radius, float angleDeg)
        {
            var angle = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        /// <summary>Triangle wound so its normal points away from a point behind the wall, i.e. out of the volume.</summary>
        private static void AddSide(MeshData mesh, Vector3 baseA, Vector3 baseB, Vector3 apex)
        {
            var uv = new[] { Vector2.zero, Vector2.right, Vector2.up };
            var center = (baseA + baseB + apex) / 3f;
            var normal = Vector3.Cross(baseB - baseA, apex - baseA);
            if (Vector3.Dot(normal, center - BehindWall) > 0f)
            {
                mesh.AddTriangle(baseA, baseB, apex, uv[0], uv[1], uv[2]);
                return;
            }

            mesh.AddTriangle(baseA, apex, baseB, uv[0], uv[2], uv[1]);
        }

        /// <summary>Quad wound so its normal points away from a point behind the wall, i.e. out of the volume.</summary>
        private static void AddFacet(MeshData mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var center = (a + b + c + d) * 0.25f;
            var outward = center - BehindWall;
            var normal = Vector3.Cross(d - a, c - a);
            var uvs = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            if (Vector3.Dot(normal, outward) > 0f)
            {
                mesh.AddQuad(a, b, c, d, uvs);
                return;
            }

            mesh.AddQuad(b, a, d, c, uvs);
        }
    }
}
