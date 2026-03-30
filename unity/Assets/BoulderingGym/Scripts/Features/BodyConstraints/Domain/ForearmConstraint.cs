using UnityEngine;

namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Forearm (radioulnar) joint constraint — 1 DoF pivot (twist).
    ///
    /// AAOS clinical ROM (degrees):
    /// <code>
    ///   Looking down the forearm axis (elbow → wrist):
    ///
    ///             Supination 80°             Pronation 80°
    ///              (palm up)                  (palm down)
    ///                  ╲                         ╱
    ///                   ╲    thumb up            ╱
    ///                    ╲      │               ╱
    ///                     ╲     │              ╱
    ///      ───────────────(●)───┼────────────────────
    ///                           │  neutral
    ///                           │  (palm medial, thumb up)
    ///                           ↓
    ///
    ///   ┌────────────────────────────────────────┐
    ///   │  -80°           0°            +80°     │
    ///   │    ├─────────────┼─────────────┤        │
    ///   │  supination   neutral     pronation    │
    ///   │  (palm up)               (palm down)   │
    ///   │                                        │
    ///   │  Total axial range: 160°               │
    ///   └────────────────────────────────────────┘
    /// </code>
    ///
    /// Twist-only joint: <see cref="ClampDirection"/> is a pass-through;
    /// use <see cref="ClampTwist"/> for axial rotation enforcement.
    /// </summary>
    public sealed class ForearmConstraint : IJointConstraint
    {
        public string JointName => "Forearm";
        public int DegreesOfFreedom => 1;

        public const float MaxPronationDeg = 80f;
        public const float MaxSupinationDeg = 80f;
        public const float TotalAxialRangeDeg = MaxPronationDeg + MaxSupinationDeg;

        public Vector3 ClampDirection(Vector3 parentAxis, Vector3 childDirection)
        {
            return childDirection.sqrMagnitude > 0.0001f
                ? childDirection.normalized
                : childDirection;
        }

        /// <summary>
        /// Clamp axial twist. Positive = pronation, negative = supination.
        /// </summary>
        public static float ClampTwist(float twistAngleDeg)
        {
            return Mathf.Clamp(twistAngleDeg, -MaxSupinationDeg, MaxPronationDeg);
        }

        public static readonly ForearmConstraint Default = new();
    }
}
