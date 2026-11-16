using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>One straight section of a wall side profile; positive angle = overhang.</summary>
    public readonly struct WallSegment
    {
        public WallSegment(float height, float angleDeg)
        {
            Height = height;
            AngleDeg = angleDeg;
        }

        public float Height { get; }

        public float AngleDeg { get; }
    }

    /// <summary>
    /// Piecewise-linear side profile of a wall, bottom to top. The offset is the horizontal distance
    /// of the surface from the wall's outermost point, toward the climber; it is never positive.
    /// A floor plan drawn from above shows that outermost point, so an overhanging wall stands with
    /// its foot set back inside the plan outline, and a slab with its foot on it.
    /// </summary>
    public sealed class WallProfile
    {
        private readonly float[] _levels;
        private readonly float[] _offsets;
        private readonly float[] _anglesDeg;

        public WallProfile(IReadOnlyList<WallSegment> segments)
        {
            if (segments == null || segments.Count == 0)
            {
                throw new ArgumentException("A wall profile needs at least one segment.", nameof(segments));
            }

            _levels = new float[segments.Count + 1];
            _offsets = new float[segments.Count + 1];
            _anglesDeg = new float[segments.Count];
            var outermost = 0f;
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                _anglesDeg[i] = segment.AngleDeg;
                _levels[i + 1] = _levels[i] + segment.Height;
                _offsets[i + 1] = _offsets[i] + segment.Height * Mathf.Tan(segment.AngleDeg * Mathf.Deg2Rad);
                outermost = Mathf.Max(outermost, _offsets[i + 1]);
            }

            for (var i = 0; i < _offsets.Length; i++)
            {
                _offsets[i] -= outermost;
            }
        }

        public float Height => _levels[_levels.Length - 1];

        public IReadOnlyList<float> Breakpoints => _levels;

        public float OffsetAt(float height)
        {
            var index = SegmentIndexAt(height);
            var clamped = Mathf.Clamp(height, 0f, Height);
            var bandHeight = _levels[index + 1] - _levels[index];
            var t = bandHeight > 0f ? (clamped - _levels[index]) / bandHeight : 0f;
            return Mathf.Lerp(_offsets[index], _offsets[index + 1], t);
        }

        public float AngleAt(float height)
        {
            return _anglesDeg[SegmentIndexAt(height)];
        }

        /// <summary>Distance measured along the surface from the floor up to a height.</summary>
        public float ArcLengthAt(float height)
        {
            var clamped = Mathf.Clamp(height, 0f, Height);
            var length = 0f;
            for (var i = 0; i < _anglesDeg.Length && _levels[i] < clamped; i++)
            {
                var top = Mathf.Min(_levels[i + 1], clamped);
                length += (top - _levels[i]) / Mathf.Cos(_anglesDeg[i] * Mathf.Deg2Rad);
            }

            return length;
        }

        private int SegmentIndexAt(float height)
        {
            for (var i = 0; i < _anglesDeg.Length - 1; i++)
            {
                if (height < _levels[i + 1])
                {
                    return i;
                }
            }

            return _anglesDeg.Length - 1;
        }
    }
}
