namespace Project.Features.Climbing.Domain
{
    /// <summary>
    /// Body dimensions used by the pose solver, in meters. The game measures them on the
    /// character rig; Default matches a 1.75 m climber.
    /// </summary>
    public sealed class ClimberAnatomy
    {
        /// <summary>Pelvis center to shoulder line center.</summary>
        public float TorsoLength { get; set; } = 0.50f;

        public float ShoulderHalfWidth { get; set; } = 0.18f;

        public float HipHalfWidth { get; set; } = 0.10f;

        /// <summary>Shoulder to grip point with the arm straight.</summary>
        public float ArmReach { get; set; } = 0.66f;

        /// <summary>Hip joint to foot contact point with the leg straight.</summary>
        public float LegReach { get; set; } = 0.92f;

        public float MinPelvisHeight { get; set; } = 0.55f;

        /// <summary>Pelvis distance to the wall surface: closest, comfortable and farthest.</summary>
        public float MinWallClearance { get; set; } = 0.20f;

        public float PreferredWallClearance { get; set; } = 0.32f;

        public float MaxWallClearance { get; set; } = 0.80f;

        /// <summary>How far a foot may sit above the pelvis (high step) and a hand below it (mantle).</summary>
        public float MaxFootAbovePelvis { get; set; } = 0.25f;

        public float MaxHandBelowPelvis { get; set; } = 0.35f;

        public static ClimberAnatomy Default => new();

        public ClimberAnatomy Scaled(float factor)
        {
            return new ClimberAnatomy
            {
                TorsoLength = TorsoLength * factor,
                ShoulderHalfWidth = ShoulderHalfWidth * factor,
                HipHalfWidth = HipHalfWidth * factor,
                ArmReach = ArmReach * factor,
                LegReach = LegReach * factor,
                MinPelvisHeight = MinPelvisHeight * factor,
                MinWallClearance = MinWallClearance,
                PreferredWallClearance = PreferredWallClearance,
                MaxWallClearance = MaxWallClearance,
                MaxFootAbovePelvis = MaxFootAbovePelvis * factor,
                MaxHandBelowPelvis = MaxHandBelowPelvis * factor,
            };
        }
    }
}
