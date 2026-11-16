using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>Turns the serialized layout into structures, sectors, faces and mitered outlines.</summary>
    public static class GymLayoutFactory
    {
        public static GymLayout Build(LayoutDefinition definition)
        {
            var structures = new List<GymStructure>();
            foreach (var structureDefinition in definition.structures)
            {
                structures.Add(BuildStructure(structureDefinition));
            }

            return new GymLayout(definition.name, structures)
            {
                EnvelopeMin = new Vector2(definition.envelope.minX, definition.envelope.minZ),
                EnvelopeMax = new Vector2(definition.envelope.maxX, definition.envelope.maxZ),
                CeilingHeight = definition.envelope.height,
                SpawnPosition = new Vector3(definition.spawn.x, 0f, definition.spawn.z),
                SpawnYawDeg = definition.spawn.yawDeg,
                Palette = BuildPalette(definition.palette),
            };
        }

        private static Dictionary<string, Color> BuildPalette(ColorDefinition[] colors)
        {
            var palette = new Dictionary<string, Color>();
            foreach (var color in colors ?? new ColorDefinition[0])
            {
                palette[color.id] = new Color(color.r, color.g, color.b);
            }

            return palette;
        }

        private static GymStructure BuildStructure(StructureDefinition definition)
        {
            var structure = new GymStructure(definition.id, definition.name, definition.height);
            var points = new Vector3[definition.vertices.Length];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = new Vector3(definition.vertices[i].x, 0f, definition.vertices[i].z);
            }

            var boundary = new List<OutlineEdge>();
            foreach (var sectorDefinition in definition.sectors)
            {
                var sector = BuildSector(sectorDefinition, structure);
                structure.AddSector(sector);
                boundary.AddRange(BuildSectorEdges(sectorDefinition, sector, points));
            }

            structure.Outline = new StructureOutline(StructureOutline.Chain(boundary, definition.id), definition.height);
            ApplyFaceLimits(structure.Outline);
            return structure;
        }

        private static GymSector BuildSector(SectorDefinition definition, GymStructure structure)
        {
            return new GymSector(definition.id, definition.name, structure, BuildProfile(definition.profile, structure.Height))
            {
                PanelColors = definition.panelColors is { Length: > 0 } ? definition.panelColors : new[] { "grey" },
                Pattern = string.IsNullOrEmpty(definition.pattern) ? "plain" : definition.pattern,
                Volumes = BuildVolumes(definition.volumes),
                Seed = definition.seed,
            };
        }

        /// <summary>Segment heights are scaled so every sector of a structure tops out at the structure height.</summary>
        private static WallProfile BuildProfile(SegmentDefinition[] segments, float height)
        {
            if (segments == null || segments.Length == 0)
            {
                return new WallProfile(new[] { new WallSegment(height, 0f) });
            }

            var total = 0f;
            foreach (var segment in segments)
            {
                total += segment.height;
            }

            var scaled = new WallSegment[segments.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                scaled[i] = new WallSegment(segments[i].height * height / total, segments[i].angle);
            }

            return new WallProfile(scaled);
        }

        private static IEnumerable<OutlineEdge> BuildSectorEdges(SectorDefinition definition, GymSector sector, Vector3[] points)
        {
            RequireCounterClockwise(definition, points);
            for (var i = 0; i < definition.loop.Length; i++)
            {
                var kind = ParseEdgeKind(definition.edges[i], definition.id);
                if (kind == EdgeKind.Internal)
                {
                    continue;
                }

                var start = points[definition.loop[i]];
                var end = points[definition.loop[(i + 1) % definition.loop.Length]];
                var edge = new OutlineEdge(start, end, kind, sector);
                if (kind == EdgeKind.Exposed)
                {
                    edge.Face = new WallFace($"{sector.Id}-{sector.Faces.Count + 1}", sector, start, end, edge.Outward);
                    sector.AddFace(edge.Face);
                }

                yield return edge;
            }
        }

        private static void ApplyFaceLimits(StructureOutline outline)
        {
            for (var i = 0; i < outline.Edges.Count; i++)
            {
                var face = outline.Edges[i].Face;
                face?.SetCornerLimits(outline.Levels, outline.Corners[i].Outgoing, outline.EndCornerOf(i).Incoming);
            }
        }

        private static void RequireCounterClockwise(SectorDefinition definition, Vector3[] points)
        {
            var twiceArea = 0f;
            for (var i = 0; i < definition.loop.Length; i++)
            {
                var a = points[definition.loop[i]];
                var b = points[definition.loop[(i + 1) % definition.loop.Length]];
                twiceArea += a.x * b.z - b.x * a.z;
            }

            if (twiceArea <= 0f)
            {
                throw new InvalidOperationException($"Sector '{definition.id}': loop must be counter-clockwise seen from above.");
            }
        }

        private static EdgeKind ParseEdgeKind(string kind, string sectorId)
        {
            return kind switch
            {
                "exposed" => EdgeKind.Exposed,
                "back" => EdgeKind.Back,
                "internal" => EdgeKind.Internal,
                _ => throw new InvalidOperationException($"Sector '{sectorId}': unknown edge kind '{kind}'."),
            };
        }

        private static VolumeRequest[] BuildVolumes(VolumeDefinition[] volumes)
        {
            if (volumes == null)
            {
                return new VolumeRequest[0];
            }

            var requests = new VolumeRequest[volumes.Length];
            for (var i = 0; i < volumes.Length; i++)
            {
                var shape = Enum.TryParse<VolumeShape>(volumes[i].shape, true, out var parsed) ? parsed : VolumeShape.Triangle;
                requests[i] = new VolumeRequest(shape, volumes[i].color, volumes[i].count);
            }

            return requests;
        }
    }
}
