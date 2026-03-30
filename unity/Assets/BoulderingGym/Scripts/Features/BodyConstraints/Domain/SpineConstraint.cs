namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Spine (thoracolumbar) joint constraint — 3 DoF.
    ///
    /// AAOS clinical ROM — combined T+L (degrees):
    /// <code>
    ///            head
    ///             ○
    ///             │
    ///      ┌──────┼──────┐
    ///      │     ╱│╲     │  ribcage
    ///      │    ╱ │ ╲    │
    ///      └──────┼──────┘
    ///             │  T/L junction
    ///             ● constraint pivot
    ///            ╱│╲
    ///           ╱ │ ╲
    ///      ┌──────┼──────┐
    ///      │   pelvis     │
    ///      └─────────────┘
    ///
    ///   Sagittal:          Coronal:           Axial (top-down):
    ///
    ///    Flex 80°          Lat 35°  Lat 35°    Rot 45°  Rot 45°
    ///    (AAOS)               ╲   ╱                ╲   ╱
    ///       ↑                  ╲ ╱                  ╲ ╱
    ///       │                   ●                    ●
    ///       ● neutral           │                    │
    ///       │                 spine                 spine
    ///       ↓
    ///    Ext 25°
    ///
    ///   ┌────────────────────────────────────────┐
    ///   │  Climbing cone: 0°–45°                 │
    ///   │  (conservative — AAOS flex is 80° but  │
    ///   │   capped for climbing realism)          │
    ///   └────────────────────────────────────────┘
    /// </code>
    ///
    /// Climbing: trunk rotation for drop-knee, lateral reach.
    /// </summary>
    public sealed class SpineConstraint : SwingConstraint
    {
        public override string JointName => "Spine";
        public override int DegreesOfFreedom => 3;
        public override float MinSwingAngleDeg => 0f;
        public override float MaxSwingAngleDeg => 45f;

        public static readonly SpineConstraint Default = new();
    }
}
