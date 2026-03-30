namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Shoulder (glenohumeral) joint constraint — 3 DoF ball-and-socket.
    ///
    /// AAOS clinical ROM (degrees):
    /// <code>
    ///                        Flex 180° (arm overhead)
    ///                           │
    ///                       ╱   │   ╲        Abd 180°
    ///                      ╱    │    ╲      ╱ (arm out to side)
    ///                     ╱     │     ╲    ╱
    ///                    ╱      │      ╲  ╱
    ///   Ext 60° ────────●───────┼───────●──────── Flex 180°
    ///   (arm behind)   ╱ ╲ scapula    ╱ (arm forward)
    ///                 ╱   ╲   │      ╱
    ///                ╱     ╲  │     ╱
    ///               ╱       ╲ │    ╱
    ///              ╱         spine
    ///             ╱           │
    ///            Add 45°      │
    ///            (arm across body)
    ///
    ///   Rotations (arm at 90° abduction, elbow bent 90°):
    ///
    ///         palm faces ceiling        palm faces floor
    ///               ╲                        ╱
    ///                ╲        forearm        ╱
    ///       Ext rot   ╲      ╱     ╲       ╱  Int rot
    ///         90°      ╲    ╱       ╲     ╱     70°
    ///                   ╲──●─────────●──╱
    ///                      upper arm
    ///
    ///   ┌────────────────────────────────────────────┐
    ///   │  Current: symmetric cone 0°–180°           │
    ///   │  (effectively unconstrained).               │
    ///   │  Per-plane limits (flex≠ext, abd≠add)       │
    ///   │  would need anatomical reference axes       │
    ///   │  injected into the IK pipeline.             │
    ///   └────────────────────────────────────────────┘
    /// </code>
    ///
    /// Climbing: heavily solicited; full overhead range essential.
    /// </summary>
    public sealed class ShoulderConstraint : SwingConstraint
    {
        public override string JointName => "Shoulder";
        public override int DegreesOfFreedom => 3;
        public override float MinSwingAngleDeg => 0f;
        public override float MaxSwingAngleDeg => 180f;

        public static readonly ShoulderConstraint Default = new();
    }
}
