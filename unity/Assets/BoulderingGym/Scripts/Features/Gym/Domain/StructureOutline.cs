using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>Boundary edge of a structure, oriented counter-clockwise seen from above.</summary>
    public sealed class OutlineEdge
    {
        public OutlineEdge(Vector3 start, Vector3 end, EdgeKind kind, GymSector sector)
        {
            Start = start;
            End = end;
            Kind = kind;
            Sector = sector;
            var along = (end - start).normalized;
            Outward = new Vector3(along.z, 0f, -along.x);
        }

        public Vector3 Start { get; }

        public Vector3 End { get; }

        public EdgeKind Kind { get; }

        public GymSector Sector { get; }

        public Vector3 Outward { get; }

        public WallFace Face { get; internal set; }

        public float OffsetAt(float y) => Kind == EdgeKind.Exposed ? Sector.Profile.OffsetAt(y) : 0f;
    }

    /// <summary>
    /// Corner shared by the previous edge (incoming) and the next edge (outgoing), per profile level.
    /// Split corners keep both faces' own offsets and are closed by a junction panel.
    /// </summary>
    public sealed class OutlineCorner
    {
        public OutlineCorner(Vector3[] incoming, Vector3[] outgoing, bool isSplit)
        {
            Incoming = incoming;
            Outgoing = outgoing;
            IsSplit = isSplit;
        }

        public IReadOnlyList<Vector3> Incoming { get; }

        public IReadOnlyList<Vector3> Outgoing { get; }

        public bool IsSplit { get; }
    }

    /// <summary>
    /// Closed boundary of a structure with mitered corners at every profile level, so adjacent
    /// faces with different profiles meet without gaps.
    /// </summary>
    public sealed class StructureOutline
    {
        private const float SplitTurnSine = 0.34f;
        private const float OffsetTolerance = 0.01f;
        private const float MaxMiterFactor = 2.5f;

        public StructureOutline(IReadOnlyList<OutlineEdge> orderedEdges, float height)
        {
            Edges = orderedEdges;
            Levels = CollectLevels(orderedEdges, height);
            var corners = new OutlineCorner[orderedEdges.Count];
            for (var i = 0; i < orderedEdges.Count; i++)
            {
                var previous = orderedEdges[(i + orderedEdges.Count - 1) % orderedEdges.Count];
                corners[i] = BuildCorner(previous, orderedEdges[i], Levels);
            }

            Corners = corners;
        }

        public IReadOnlyList<float> Levels { get; }

        public IReadOnlyList<OutlineEdge> Edges { get; }

        /// <summary>Corners[i] sits at Edges[i].Start.</summary>
        public IReadOnlyList<OutlineCorner> Corners { get; }

        public OutlineCorner EndCornerOf(int edgeIndex) => Corners[(edgeIndex + 1) % Corners.Count];

        /// <summary>Chains boundary edges head to tail; the structure boundary must be one simple loop.</summary>
        public static List<OutlineEdge> Chain(IReadOnlyList<OutlineEdge> edges, string structureId)
        {
            var byStart = new Dictionary<Vector3, OutlineEdge>();
            foreach (var edge in edges)
            {
                if (!byStart.TryAdd(edge.Start, edge))
                {
                    throw new InvalidOperationException($"Structure '{structureId}': two boundary edges start at {edge.Start}.");
                }
            }

            var ordered = new List<OutlineEdge> { edges[0] };
            while (ordered.Count < edges.Count)
            {
                if (!byStart.TryGetValue(ordered[ordered.Count - 1].End, out var next) || next == edges[0])
                {
                    throw new InvalidOperationException($"Structure '{structureId}': boundary is not one closed loop.");
                }

                ordered.Add(next);
            }

            if (ordered[ordered.Count - 1].End != ordered[0].Start)
            {
                throw new InvalidOperationException($"Structure '{structureId}': boundary loop does not close.");
            }

            return ordered;
        }

        private static float[] CollectLevels(IReadOnlyList<OutlineEdge> edges, float height)
        {
            var levels = new List<float> { 0f, height };
            foreach (var edge in edges)
            {
                if (edge.Kind != EdgeKind.Exposed)
                {
                    continue;
                }

                foreach (var level in edge.Sector.Profile.Breakpoints)
                {
                    if (level > 0f && level < height && !levels.Exists(l => Mathf.Abs(l - level) < OffsetTolerance))
                    {
                        levels.Add(level);
                    }
                }
            }

            levels.Sort();
            return levels.ToArray();
        }

        private static OutlineCorner BuildCorner(OutlineEdge incoming, OutlineEdge outgoing, IReadOnlyList<float> levels)
        {
            var vertex = outgoing.Start;
            var isSplit = IsNearlyStraight(incoming, outgoing) && OffsetsDiffer(incoming, outgoing, levels);
            var incomingPoints = new Vector3[levels.Count];
            var outgoingPoints = new Vector3[levels.Count];
            for (var k = 0; k < levels.Count; k++)
            {
                var y = levels[k];
                var lift = Vector3.up * y;
                if (isSplit)
                {
                    incomingPoints[k] = vertex + incoming.Outward * incoming.OffsetAt(y) + lift;
                    outgoingPoints[k] = vertex + outgoing.Outward * outgoing.OffsetAt(y) + lift;
                    continue;
                }

                var miter = vertex + Miter(incoming.Outward, incoming.OffsetAt(y), outgoing.Outward, outgoing.OffsetAt(y)) + lift;
                incomingPoints[k] = miter;
                outgoingPoints[k] = miter;
            }

            return new OutlineCorner(incomingPoints, outgoingPoints, isSplit);
        }

        private static bool IsNearlyStraight(OutlineEdge incoming, OutlineEdge outgoing)
        {
            var a = (incoming.End - incoming.Start).normalized;
            var b = (outgoing.End - outgoing.Start).normalized;
            var sine = a.x * b.z - a.z * b.x;
            return Mathf.Abs(sine) < SplitTurnSine && Vector3.Dot(a, b) > 0f;
        }

        private static bool OffsetsDiffer(OutlineEdge incoming, OutlineEdge outgoing, IReadOnlyList<float> levels)
        {
            foreach (var y in levels)
            {
                if (Mathf.Abs(incoming.OffsetAt(y) - outgoing.OffsetAt(y)) > OffsetTolerance)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Horizontal displacement lying at distance offsetA along normalA and offsetB along normalB.</summary>
        private static Vector3 Miter(Vector3 normalA, float offsetA, Vector3 normalB, float offsetB)
        {
            var determinant = normalA.x * normalB.z - normalA.z * normalB.x;
            if (Mathf.Abs(determinant) < 1e-4f)
            {
                return normalA * offsetA;
            }

            var displacement = new Vector3(
                (offsetA * normalB.z - normalA.z * offsetB) / determinant,
                0f,
                (normalA.x * offsetB - offsetA * normalB.x) / determinant);
            var maxLength = MaxMiterFactor * Mathf.Max(Mathf.Abs(offsetA), Mathf.Abs(offsetB));
            return Vector3.ClampMagnitude(displacement, maxLength);
        }
    }
}
