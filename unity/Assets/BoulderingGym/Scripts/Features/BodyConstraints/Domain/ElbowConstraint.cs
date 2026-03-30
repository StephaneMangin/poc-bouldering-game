namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Elbow (humeroulnar) joint constraint — 1 DoF hinge.
    ///
    /// AAOS clinical ROM (degrees):
    /// <code>
    ///         upper arm
    ///            │
    ///            │
    ///            │
    ///            ● olecranon (elbow)
    ///           ╱│╲
    ///          ╱ │ ╲    5° min (hyper-extension guard)
    ///         ╱  │  ╲ ╱
    ///        ╱   │   ●───────────── forearm (nearly straight)
    ///       ╱    │
    ///      ╱     │         forearm at 90°
    ///     ╱      │        ╱
    ///    ╱ 145°  │       ╱
    ///   ╱  max   │      ╱
    ///  ╱  flex   │     ╱
    ///  forearm   │    ╱
    ///  (lock-off)│   ╱
    ///            │  ╱
    ///            │ ╱
    ///            ●───── forearm at 0° (full extension)
    ///            │
    ///         upper arm
    ///
    ///   ┌────────────────────────────────────────┐
    ///   │  0°    5°          90°          145°   │
    ///   │  ║     ├────────────┼────────────┤     │
    ///   │  ║    min        typical        max    │
    ///   │  ║  (guard)     (relaxed)    (lock-off) │
    ///   │  straight arm                          │
    ///   └────────────────────────────────────────┘
    /// </code>
    ///
    /// Climbing: full flexion (lock-off) critical; hyper-extension prevented.
    /// </summary>
    public sealed class ElbowConstraint : SwingConstraint
    {
        public override string JointName => "Elbow";
        public override int DegreesOfFreedom => 1;
        public override float MinSwingAngleDeg => 5f;
        public override float MaxSwingAngleDeg => 145f;

        /// <summary>Static convenience — delegates to Default.ComputeDistanceRange.</summary>
        public static (float minDist, float maxDist) ComputeDistanceRange(
            float upperArmLen, float forearmLen)
            => ((SwingConstraint)Default).ComputeDistanceRange(upperArmLen, forearmLen);

        public static readonly ElbowConstraint Default = new();
    }
}
