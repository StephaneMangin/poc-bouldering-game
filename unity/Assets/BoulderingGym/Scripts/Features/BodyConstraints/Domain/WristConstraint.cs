namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Wrist (radiocarpal) joint constraint — 2 DoF condyloid.
    ///
    /// AAOS clinical ROM (degrees):
    /// <code>
    ///   Side view (sagittal):       Top view (coronal):
    ///
    ///      forearm                     forearm
    ///         │                           │
    ///         │                           │
    ///         ● wrist                     ● wrist
    ///        ╱ ╲                         ╱ ╲
    ///       ╱   ╲                       ╱   ╲
    ///      ╱     ╲                     ╱     ╲
    ///    Flex    Ext               Radial   Ulnar
    ///    80°     70°               dev 20°  dev 30°
    ///   (palm   (dorsum             (thumb   (pinky
    ///    down)   down)               side)    side)
    ///
    ///             ╭── dorsum (back of hand)
    ///             │
    ///      ───────●──────── hand aligned (0°)
    ///             │
    ///             ╰── palm
    ///
    ///   ┌────────────────────────────────────────┐
    ///   │  Flex/Ext (sagittal):                  │
    ///   │  Ext 70°       0°          Flex 80°    │
    ///   │    ├────────────┼────────────┤          │
    ///   │                                        │
    ///   │  Deviation (coronal):                  │
    ///   │  Radial 20°    0°         Ulnar 30°    │
    ///   │    ├────────────┼────────────┤          │
    ///   │                                        │
    ///   │  Cone: 0°–80° (max flex envelope)      │
    ///   └────────────────────────────────────────┘
    /// </code>
    ///
    /// Climbing: wrist flex/ext critical for crimps and slopers.
    /// </summary>
    public sealed class WristConstraint : SwingConstraint
    {
        public override string JointName => "Wrist";
        public override int DegreesOfFreedom => 2;
        public override float MinSwingAngleDeg => 0f;
        public override float MaxSwingAngleDeg => 80f;

        public static readonly WristConstraint Default = new();
    }
}
