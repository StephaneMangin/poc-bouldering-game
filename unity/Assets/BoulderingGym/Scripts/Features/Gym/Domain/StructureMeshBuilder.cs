using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>Builds the visible surfaces of a structure from its mitered outline.</summary>
    public static class StructureMeshBuilder
    {
        /// <summary>Climbable face strip; UV in meters: u along the base edge, v along the surface from the floor.</summary>
        public static MeshData BuildFace(StructureOutline outline, int edgeIndex)
        {
            var mesh = new MeshData();
            var face = outline.Edges[edgeIndex].Face;
            var start = outline.Corners[edgeIndex].Outgoing;
            var end = outline.EndCornerOf(edgeIndex).Incoming;
            for (var k = 0; k < outline.Levels.Count - 1; k++)
            {
                var uvs = new[]
                {
                    FaceUv(face, start[k]),
                    FaceUv(face, end[k]),
                    FaceUv(face, end[k + 1]),
                    FaceUv(face, start[k + 1]),
                };
                mesh.AddQuad(start[k], end[k], end[k + 1], start[k + 1], uvs);
            }

            return mesh;
        }

        /// <summary>Back walls, junction panels between collinear faces and the flat top.</summary>
        public static MeshData BuildTrim(StructureOutline outline)
        {
            var mesh = new MeshData();
            for (var i = 0; i < outline.Edges.Count; i++)
            {
                if (outline.Edges[i].Kind == EdgeKind.Back)
                {
                    AddBackStrip(mesh, outline, i);
                }

                if (outline.Corners[i].IsSplit)
                {
                    AddJunction(mesh, outline.Corners[i], outline.Levels.Count);
                }
            }

            AddCap(mesh, outline);
            return mesh;
        }

        private static Vector2 FaceUv(WallFace face, Vector3 point)
        {
            return new Vector2(face.AlongCoordinate(point), face.Profile.ArcLengthAt(point.y));
        }

        private static void AddBackStrip(MeshData mesh, StructureOutline outline, int edgeIndex)
        {
            var start = outline.Corners[edgeIndex].Outgoing;
            var end = outline.EndCornerOf(edgeIndex).Incoming;
            var top = outline.Levels.Count - 1;
            var width = Vector3.Distance(start[0], end[0]);
            var height = outline.Levels[top];
            var uvs = new[] { Vector2.zero, new Vector2(width, 0f), new Vector2(width, height), new Vector2(0f, height) };
            mesh.AddQuad(start[0], end[0], end[top], start[top], uvs);
        }

        private static void AddJunction(MeshData mesh, OutlineCorner corner, int levelCount)
        {
            for (var k = 0; k < levelCount - 1; k++)
            {
                var a = corner.Incoming[k];
                var b = corner.Outgoing[k];
                var c = corner.Outgoing[k + 1];
                var d = corner.Incoming[k + 1];
                var width = Vector3.Distance(a, b);
                var uvs = new[] { new Vector2(0f, a.y), new Vector2(width, b.y), new Vector2(width, c.y), new Vector2(0f, d.y) };
                mesh.AddDoubleSidedQuad(a, b, c, d, uvs);
            }
        }

        private static void AddCap(MeshData mesh, StructureOutline outline)
        {
            var top = outline.Levels.Count - 1;
            var ring = new List<Vector3>();
            foreach (var corner in outline.Corners)
            {
                ring.Add(corner.Incoming[top]);
                if (corner.IsSplit)
                {
                    ring.Add(corner.Outgoing[top]);
                }
            }

            var triangles = PolygonTriangulator.Triangulate(ring);
            for (var t = 0; t < triangles.Count; t += 3)
            {
                var a = ring[triangles[t]];
                var b = ring[triangles[t + 1]];
                var c = ring[triangles[t + 2]];
                mesh.AddTriangle(a, c, b, new Vector2(a.x, a.z), new Vector2(c.x, c.z), new Vector2(b.x, b.z));
            }
        }
    }
}
