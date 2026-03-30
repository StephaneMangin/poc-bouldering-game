namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Hip (coxo-femoral) joint constraint — 3 DoF ball-and-socket.
    ///
    /// AAOS clinical ROM (degrees):
    /// <code>
    ///                  pelvis
    ///         ┌─────────┼─────────┐
    ///         │         │         │
    ///         │      ┌──●──┐      │  acetabulum (hip socket)
    ///         └──────┘  │  └──────┘
    ///                   │
    ///                 thigh (0° = hanging straight down)
    ///                   │
    ///
    ///   Sagittal (side):          Coronal (front):
    ///
    ///      Flex 120°                  Abd 45°
    ///      (high-step)                (leg out to side)
    ///         ╱                          ╱
    ///        ╱                          ╱
    ///       ╱                          ╱
    ///      ●─────── neutral           ●─────── neutral
    ///       ╲      (standing)          ╲      (standing)
    ///        ╲                          ╲
    ///         Ext 30°                    Add 30°
    ///         (leg behind)               (leg across)
    ///
    ///   Rotations (thigh vertical, knee bent 90°):
    ///
    ///       Int rot  35°       45°  Ext rot
    ///             ╲               ╱
    ///              ╲    knee    ╱
    ///               ╲    │    ╱
    ///                ╲   │   ╱
    ///                 ╲──●──╱
    ///                  thigh
    ///                 foot swings in/out
    ///
    ///   ┌────────────────────────────────────────┐
    ///   │  Cone: 0°–150°                         │
    ///   │  (120° flex + 30° ext envelope)         │
    ///   │                                        │
    ///   │  Climbing: high-step  ≈ 120° flex      │
    ///   │            drop-knee ≈ abd + int rot   │
    ///   │            flagging  ≈ ext + abd        │
    ///   └────────────────────────────────────────┘
    /// </code>
    ///
    /// Climbing: high-step, flagging, drop-knee require full ROM.
    /// </summary>
    public sealed class HipConstraint : SwingConstraint
    {
        public override string JointName => "Hip";
        public override int DegreesOfFreedom => 3;
        public override float MinSwingAngleDeg => 0f;
        public override float MaxSwingAngleDeg => 150f;

        public static readonly HipConstraint Default = new();
    }
}
