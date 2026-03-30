namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Knee (tibiofemoral) joint constraint — 1 DoF hinge.
    ///
    /// AAOS clinical ROM (degrees):
    /// <code>
    ///         thigh
    ///           │
    ///           │
    ///           │
    ///           ● patella (kneecap)
    ///          ╱│╲
    ///         ╱ │ ╲    5° min (hyper-extension guard)
    ///        ╱  │  ╲ ╱
    ///       ╱   │   ●───────────── shin (nearly straight)
    ///      ╱    │
    ///     ╱     │         shin at 90°
    ///    ╱      │        ╱ (sitting)
    ///   ╱ 140°  │       ╱
    ///  ╱  max   │      ╱
    /// ╱  flex   │     ╱
    /// shin      │    ╱
    /// (heel to  │   ╱
    ///  glute)   │  ╱
    ///           │ ╱
    ///           ●───── shin at 0° (leg straight)
    ///           │
    ///         thigh
    ///
    ///   ┌────────────────────────────────────────┐
    ///   │  0°    5°          90°          140°   │
    ///   │  ║     ├────────────┼────────────┤     │
    ///   │  ║    min        sitting        max    │
    ///   │  ║  (guard)                  deep flex │
    ///   │  straight leg             (high-step)  │
    ///   └────────────────────────────────────────┘
    /// </code>
    ///
    /// Climbing: deep knee flexion (high-step, drop-knee) critical.
    /// </summary>
    public sealed class KneeConstraint : SwingConstraint
    {
        public override string JointName => "Knee";
        public override int DegreesOfFreedom => 1;
        public override float MinSwingAngleDeg => 5f;
        public override float MaxSwingAngleDeg => 140f;

        /// <summary>Static convenience — delegates to Default.ComputeDistanceRange.</summary>
        public static (float minDist, float maxDist) ComputeDistanceRange(
            float thighLen, float shinLen)
            => ((SwingConstraint)Default).ComputeDistanceRange(thighLen, shinLen);

        public static readonly KneeConstraint Default = new();
    }
}
