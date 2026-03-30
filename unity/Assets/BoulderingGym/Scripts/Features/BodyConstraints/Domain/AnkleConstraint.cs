using UnityEngine;

namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Ankle (talocrural + subtalar) joint constraint — 2 DoF.
    ///
    /// AAOS clinical ROM (degrees):
    /// <code>
    ///   Sagittal view — three key positions:
    ///
    ///     Dorsiflexion 20°      Neutral 90°         Plantarflexion 50°
    ///     (foot tilts up)       (standing)            (tiptoe)
    ///
    ///       shin                 shin                  shin
    ///         │                    │                     │
    ///         │  70°               │  90°                │  140°
    ///         │╱                   │                     │╲
    ///         ●──╱╱╱╱              ●─────────            ●
    ///          ╱╱ foot             foot     ╲             ╲╲
    ///        toes (up)               toe      toe          ╲╲
    ///                                                    foot
    ///                                                   (toes down)
    ///
    ///   Frontal view (looking at heel from behind):
    ///
    ///        shin                     shin
    ///          │                        │
    ///          │    Inversion            │    Eversion
    ///          │      35°               │      15°
    ///          ●                        ●
    ///         ╱                          ╲
    ///        ╱ sole faces                 ╲ sole faces
    ///       ╱  inward (medial)             ╲ outward (lateral)
    ///
    ///   ┌────────────────────────────────────────┐
    ///   │  Shin-to-foot angle:                   │
    ///   │  70°          90°           140°       │
    ///   │   ├────────────┼────────────┤           │
    ///   │ dorsi       neutral      plantar       │
    ///   │ (20° up)                 (50° down)    │
    ///   │                                        │
    ///   │  Cone: 70°–140° (90° ± dorsi/plantar)  │
    ///   └────────────────────────────────────────┘
    /// </code>
    ///
    /// Climbing: precise toe placement, preventing foot flip.
    /// </summary>
    public sealed class AnkleConstraint : SwingConstraint
    {
        public override string JointName => "Ankle";
        public override int DegreesOfFreedom => 2;
        public override float MinSwingAngleDeg => 70f;  // 90° - 20° dorsiflexion
        public override float MaxSwingAngleDeg => 140f; // 90° + 50° plantarflexion

        /// <summary>
        /// Static convenience — same as Default.ClampDirection but with
        /// a more descriptive name for direct callers in the IK pipeline.
        /// </summary>
        public static Vector3 ClampFootForward(Vector3 shinDirection, Vector3 desiredFootForward)
            => Default.ClampDirection(shinDirection, desiredFootForward);

        public static readonly AnkleConstraint Default = new();
    }
}
