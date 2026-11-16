using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Project.Features.Gym.Domain
{
    public enum DressingMaterial
    {
        Floor,
        Cladding,
        Ceiling,
        Steel,
        Duct,
        Skylight,
        Lamp,
        LightStrip,
        Wood,
        BuoyRed,
        BuoyWhite,
        Flag,
    }

    /// <summary>
    /// Hall shell and dressing as seen on the photos, UVs in meters: grey mats crossed by dark tape seams,
    /// corrugated grey cladding with yellow columns and a yellow girt above the walls, solid yellow
    /// roof beams, white fabric ventilation ducts, skylights and hanging globe lamps. Columns and
    /// lamps keep clear of the climbing walls.
    /// </summary>
    public static class EnvironmentDressing
    {
        private const float Bay = 6f;
        private const float ColumnSize = 0.3f;
        private const float RafterDepth = 0.9f;
        private const float FlangeWidth = 0.25f;
        private const float FlangeThickness = 0.03f;
        private const float WebThickness = 0.015f;
        private const float GirtHeight = 5f;
        private const float GirtSize = 0.25f;
        private const float StripLength = 1.2f;
        private const float StripSpacing = 3f;
        private const float DuctRadius = 0.5f;
        private const float DuctDrop = 2.1f;
        private const float MastRadius = 0.16f;
        private const float BuoyHeight = 6.0f;
        private const float SkylightLength = 3.2f;
        private const float SkylightWidth = 1.4f;
        private const float LampRadius = 0.45f;
        private const float LampHeight = 6.0f;
        private const float LampSpacing = 7f;
        private const float LampClearance = 2.5f;
        private const float CableRadius = 0.01f;

        public static Dictionary<DressingMaterial, MeshData> Build(GymLayout layout)
        {
            var parts = new Dictionary<DressingMaterial, MeshData>();
            foreach (DressingMaterial material in System.Enum.GetValues(typeof(DressingMaterial)))
            {
                parts[material] = new MeshData();
            }

            var walls = Footprints(layout);
            AddShell(parts, layout);
            AddColumns(parts[DressingMaterial.Steel], layout, walls);
            AddGirts(parts[DressingMaterial.Steel], layout);
            AddRoof(parts, layout);
            AddLamps(parts, layout, walls);
            if (layout.MastFoot.HasValue)
            {
                AddMast(parts, layout.MastFoot.Value, layout.CeilingHeight);
            }

            return parts;
        }

        private static void AddShell(Dictionary<DressingMaterial, MeshData> parts, GymLayout layout)
        {
            var min = new Vector3(layout.EnvelopeMin.x, 0f, layout.EnvelopeMin.y);
            var max = new Vector3(layout.EnvelopeMax.x, 0f, layout.EnvelopeMax.y);
            var height = layout.CeilingHeight;
            var corners = new[] { min, new Vector3(max.x, 0f, min.z), max, new Vector3(min.x, 0f, max.z) };
            var size = max - min;
            var floorUvs = new[] { Vector2.zero, new Vector2(size.x, 0f), new Vector2(size.x, size.z), new Vector2(0f, size.z) };
            parts[DressingMaterial.Floor].AddQuad(corners[0], corners[1], corners[2], corners[3], floorUvs);
            for (var i = 0; i < corners.Length; i++)
            {
                var a = corners[i];
                var b = corners[(i + 1) % corners.Length];
                var width = Vector3.Distance(a, b);
                var uvs = new[] { Vector2.zero, new Vector2(width, 0f), new Vector2(width, height), new Vector2(0f, height) };
                parts[DressingMaterial.Cladding].AddQuad(b, a, a + Vector3.up * height, b + Vector3.up * height, uvs);
            }

            var up = Vector3.up * height;
            var ceilingUvs = new[] { Vector2.zero, new Vector2(size.x, 0f), new Vector2(size.x, size.z), new Vector2(0f, size.z) };
            parts[DressingMaterial.Ceiling].AddQuad(corners[3] + up, corners[2] + up, corners[1] + up, corners[0] + up, ceilingUvs);
        }

        private static void AddColumns(MeshData steel, GymLayout layout, List<Vector3[]> walls)
        {
            var min = layout.EnvelopeMin;
            var max = layout.EnvelopeMax;
            var inset = ColumnSize * 0.5f;
            for (var x = min.x + Bay; x < max.x; x += Bay)
            {
                AddColumn(steel, new Vector3(x, 0f, min.y + inset), layout.CeilingHeight, walls);
                AddColumn(steel, new Vector3(x, 0f, max.y - inset), layout.CeilingHeight, walls);
            }

            for (var z = min.y + Bay; z < max.y; z += Bay)
            {
                AddColumn(steel, new Vector3(min.x + inset, 0f, z), layout.CeilingHeight, walls);
                AddColumn(steel, new Vector3(max.x - inset, 0f, z), layout.CeilingHeight, walls);
            }
        }

        private static void AddColumn(MeshData steel, Vector3 foot, float height, List<Vector3[]> walls)
        {
            if (DistanceToWalls(foot, walls) < ColumnSize)
            {
                return;
            }

            var half = new Vector3(ColumnSize, 0f, ColumnSize) * 0.5f;
            MeshPrimitives.Box(steel, foot - half, foot + half + Vector3.up * height);
        }

        private static void AddRoof(Dictionary<DressingMaterial, MeshData> parts, GymLayout layout)
        {
            var min = layout.EnvelopeMin;
            var max = layout.EnvelopeMax;
            var top = layout.CeilingHeight;
            var bay = 0;
            for (var x = min.x + Bay; x < max.x; x += Bay, bay++)
            {
                AddRafter(parts, x, new FloatRange(min.y, max.y), top);
                if (bay % 2 == 0)
                {
                    AddSkylights(parts[DressingMaterial.Skylight], x + Bay * 0.5f, min.y, max.y, top);
                }
            }

            var depth = max.y - min.y;
            for (var k = 1; k <= 3; k++)
            {
                var z = min.y + depth * k / 4f;
                var y = top - DuctDrop;
                MeshPrimitives.Cylinder(parts[DressingMaterial.Duct], new Vector3(min.x + 1f, y, z), new Vector3(max.x - 1f, y, z), DuctRadius);
            }
        }

        /// <summary>Solid I-beam spanning the hall along z, with light strips fixed to the roof mid-bay.</summary>
        private static void AddRafter(Dictionary<DressingMaterial, MeshData> parts, float x, FloatRange span, float top)
        {
            var steel = parts[DressingMaterial.Steel];
            var flange = FlangeWidth * 0.5f;
            var web = WebThickness * 0.5f;
            var bottom = top - RafterDepth;
            MeshPrimitives.Box(steel, new Vector3(x - flange, top - FlangeThickness, span.Min), new Vector3(x + flange, top, span.Max));
            MeshPrimitives.Box(steel, new Vector3(x - web, bottom + FlangeThickness, span.Min), new Vector3(x + web, top - FlangeThickness, span.Max));
            MeshPrimitives.Box(steel, new Vector3(x - flange, bottom, span.Min), new Vector3(x + flange, bottom + FlangeThickness, span.Max));
            var stripX = x + Bay * 0.5f;
            for (var z = span.Min + StripSpacing; z + StripLength <= span.Max; z += StripSpacing)
            {
                var stripCenter = new Vector3(stripX, top - 0.05f, z);
                MeshPrimitives.Box(parts[DressingMaterial.LightStrip], stripCenter - new Vector3(0.06f, 0.03f, StripLength * 0.5f), stripCenter + new Vector3(0.06f, 0.03f, StripLength * 0.5f));
            }
        }

        /// <summary>Horizontal yellow beam along the four hall walls, above the climbing walls.</summary>
        private static void AddGirts(MeshData steel, GymLayout layout)
        {
            var min = layout.EnvelopeMin;
            var max = layout.EnvelopeMax;
            var low = GirtHeight - GirtSize * 0.5f;
            var high = GirtHeight + GirtSize * 0.5f;
            MeshPrimitives.Box(steel, new Vector3(min.x, low, min.y), new Vector3(max.x, high, min.y + GirtSize));
            MeshPrimitives.Box(steel, new Vector3(min.x, low, max.y - GirtSize), new Vector3(max.x, high, max.y));
            MeshPrimitives.Box(steel, new Vector3(min.x, low, min.y), new Vector3(min.x + GirtSize, high, max.y));
            MeshPrimitives.Box(steel, new Vector3(max.x - GirtSize, low, min.y), new Vector3(max.x, high, max.y));
        }

        /// <summary>The island is a ship: a wooden mast to the roof, a lifebuoy and a pirate flag.</summary>
        private static void AddMast(Dictionary<DressingMaterial, MeshData> parts, Vector3 foot, float ceiling)
        {
            MeshPrimitives.Cylinder(parts[DressingMaterial.Wood], foot, new Vector3(foot.x, ceiling, foot.z), MastRadius);
            var buoyCenter = new Vector3(foot.x + MastRadius + 0.3f, BuoyHeight, foot.z);
            MeshPrimitives.Torus(parts[DressingMaterial.BuoyRed], buoyCenter, Vector3.forward, 0.32f, 0.09f);
            for (var i = 0; i < 4; i++)
            {
                var angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var band = buoyCenter + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.32f;
                var tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f) * 0.06f;
                MeshPrimitives.Cylinder(parts[DressingMaterial.BuoyWhite], band - tangent, band + tangent, 0.1f);
            }

            var flagTop = ceiling - RafterDepth - 0.3f;
            MeshPrimitives.Box(parts[DressingMaterial.Flag], new Vector3(foot.x + MastRadius, flagTop - 0.8f, foot.z - 0.01f), new Vector3(foot.x + MastRadius + 1.2f, flagTop, foot.z + 0.01f));
        }

        private static void AddSkylights(MeshData skylight, float x, float minZ, float maxZ, float top)
        {
            var y = top - 0.02f;
            for (var z = minZ + 3f; z + SkylightLength < maxZ - 2f; z += SkylightLength * 2.2f)
            {
                var a = new Vector3(x - SkylightWidth * 0.5f, y, z);
                var b = new Vector3(x + SkylightWidth * 0.5f, y, z);
                var c = new Vector3(x + SkylightWidth * 0.5f, y, z + SkylightLength);
                var d = new Vector3(x - SkylightWidth * 0.5f, y, z + SkylightLength);
                skylight.AddQuad(d, c, b, a, new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            }
        }

        private static void AddLamps(Dictionary<DressingMaterial, MeshData> parts, GymLayout layout, List<Vector3[]> walls)
        {
            var random = new Random(7);
            for (var x = layout.EnvelopeMin.x + LampSpacing * 0.5f; x < layout.EnvelopeMax.x; x += LampSpacing)
            {
                for (var z = layout.EnvelopeMin.y + LampSpacing * 0.5f; z < layout.EnvelopeMax.y; z += LampSpacing)
                {
                    var center = new Vector3(x + (float)random.NextDouble() - 0.5f, LampHeight, z + (float)random.NextDouble() - 0.5f);
                    if (DistanceToWalls(new Vector3(center.x, 0f, center.z), walls) < LampClearance)
                    {
                        continue;
                    }

                    MeshPrimitives.Sphere(parts[DressingMaterial.Lamp], center, LampRadius);
                    MeshPrimitives.Cylinder(parts[DressingMaterial.Steel], center + Vector3.up * LampRadius, new Vector3(center.x, layout.CeilingHeight, center.z), CableRadius);
                }
            }
        }

        /// <summary>Floor outline of every structure, as closed polygons.</summary>
        private static List<Vector3[]> Footprints(GymLayout layout)
        {
            var footprints = new List<Vector3[]>();
            foreach (var structure in layout.Structures)
            {
                var points = new Vector3[structure.Outline.Edges.Count];
                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = structure.Outline.Edges[i].Start;
                }

                footprints.Add(points);
            }

            return footprints;
        }

        /// <summary>Distance on the floor to the nearest structure outline, zero inside a structure.</summary>
        private static float DistanceToWalls(Vector3 point, List<Vector3[]> footprints)
        {
            var nearest = float.MaxValue;
            foreach (var polygon in footprints)
            {
                if (Contains(polygon, point))
                {
                    return 0f;
                }

                for (var i = 0; i < polygon.Length; i++)
                {
                    nearest = Mathf.Min(nearest, SegmentDistance(point, polygon[i], polygon[(i + 1) % polygon.Length]));
                }
            }

            return nearest;
        }

        private static bool Contains(Vector3[] polygon, Vector3 point)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.z > point.z) != (b.z > point.z) && point.x < (b.x - a.x) * (point.z - a.z) / (b.z - a.z) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float SegmentDistance(Vector3 point, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return Vector3.Distance(point, a + ab * t);
        }
    }
}
