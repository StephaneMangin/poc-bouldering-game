using System.Collections.Generic;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>Closed interval of along-wall coordinates, in meters from the face base start.</summary>
    public readonly struct FloatRange
    {
        public FloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }

        public float Max { get; }

        public float Length => Max - Min;

        public bool Contains(float value) => value >= Min && value <= Max;
    }

    /// <summary>
    /// Climbable face of a sector: a straight floor edge extruded upward along the sector profile.
    /// Surface coordinates are (s, y): s along the base edge, y the world height.
    /// </summary>
    public sealed class WallFace : IClimbingSurface
    {
        private float[] _limitLevels = { 0f };
        private float[] _limitStarts = { 0f };
        private float[] _limitEnds = { 0f };

        public WallFace(string id, GymSector sector, Vector3 baseStart, Vector3 baseEnd, Vector3 outward)
        {
            Id = id;
            Sector = sector;
            BaseStart = baseStart;
            BaseEnd = baseEnd;
            Width = Vector3.Distance(baseStart, baseEnd);
            Along = (baseEnd - baseStart) / Width;
            Outward = outward.normalized;
            _limitEnds = new[] { Width };
        }

        public string Id { get; }

        public GymSector Sector { get; }

        public WallProfile Profile => Sector.Profile;

        public Vector3 BaseStart { get; }

        public Vector3 BaseEnd { get; }

        public Vector3 Along { get; }

        public Vector3 Outward { get; }

        public float Width { get; }

        public float Height => Profile.Height;

        public Vector3 SurfacePoint(float s, float y)
        {
            return BaseStart + Along * s + Vector3.up * y + Outward * Profile.OffsetAt(y);
        }

        /// <summary>Point on the floor in front of the wall foot, at a distance from it.</summary>
        public Vector3 FloorPointInFront(float s, float distance)
        {
            return SurfacePoint(s, 0f) + Outward * distance;
        }

        public Vector3 NormalAt(float y)
        {
            var angle = Profile.AngleAt(y) * Mathf.Deg2Rad;
            return Outward * Mathf.Cos(angle) - Vector3.up * Mathf.Sin(angle);
        }

        public Vector3 UpTangentAt(float y)
        {
            var angle = Profile.AngleAt(y) * Mathf.Deg2Rad;
            return Vector3.up * Mathf.Cos(angle) + Outward * Mathf.Sin(angle);
        }

        /// <summary>Perpendicular distance from the surface, positive on the climber side.</summary>
        public float SignedDistance(Vector3 point)
        {
            var horizontal = Vector3.Dot(point - BaseStart, Outward) - Profile.OffsetAt(point.y);
            return horizontal * Mathf.Cos(Profile.AngleAt(point.y) * Mathf.Deg2Rad);
        }

        public float AlongCoordinate(Vector3 point)
        {
            return Vector3.Dot(point - BaseStart, Along);
        }

        /// <summary>Along-wall extent actually covered by the face mesh at a height (corners trim or extend it).</summary>
        public FloatRange RangeAt(float y)
        {
            for (var i = 0; i < _limitLevels.Length - 1; i++)
            {
                if (y > _limitLevels[i + 1])
                {
                    continue;
                }

                var band = _limitLevels[i + 1] - _limitLevels[i];
                var t = band > 0f ? Mathf.Clamp01((y - _limitLevels[i]) / band) : 0f;
                return new FloatRange(
                    Mathf.Lerp(_limitStarts[i], _limitStarts[i + 1], t),
                    Mathf.Lerp(_limitEnds[i], _limitEnds[i + 1], t));
            }

            var last = _limitLevels.Length - 1;
            return new FloatRange(_limitStarts[last], _limitEnds[last]);
        }

        internal void SetCornerLimits(IReadOnlyList<float> levels, IReadOnlyList<Vector3> starts, IReadOnlyList<Vector3> ends)
        {
            _limitLevels = new float[levels.Count];
            _limitStarts = new float[levels.Count];
            _limitEnds = new float[levels.Count];
            for (var i = 0; i < levels.Count; i++)
            {
                _limitLevels[i] = levels[i];
                _limitStarts[i] = AlongCoordinate(starts[i]);
                _limitEnds[i] = AlongCoordinate(ends[i]);
            }
        }
    }
}
