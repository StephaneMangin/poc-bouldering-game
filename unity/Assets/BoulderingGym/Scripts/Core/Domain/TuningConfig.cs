using UnityEngine;

namespace Project.Core.Domain
{
    [CreateAssetMenu(fileName = "TuningConfig", menuName = "Project/Tuning Config")]
    public sealed class TuningConfig : ScriptableObject
    {
        [Header("Version")]
        public string tuningVersion = "0.1.0";

        [Header("Grab Detection")]
        [Min(0.1f)] public float grabRange = 1.6f;
        [Range(5f, 90f)] public float grabAngleMax = 35f;

        [Header("Locomotion")]
        [Min(0.1f)] public float hipsMoveSpeed = 2.0f;

        [Header("Endurance")]
        [Range(10f, 300f)] public float enduranceMax = 100f;
        [Range(0f, 20f)] public float enduranceCostGrab = 2f;
        [Range(0f, 20f)] public float enduranceRegenRestPerS = 6f;
        [Range(0f, 10f)] public float enduranceCostReposition = 4f;
        [Range(0f, 25f)] public float enduranceCostDyno = 12f;

        [Header("Physics Model - COG")]
        [Range(0.2f, 1.2f)] public float cogMaxOffset = 0.6f;
        [Range(0.05f, 0.5f)] public float cogOffsetPenaltyStart = 0.2f;
        [Range(1f, 4f)] public float cogOffsetPenaltyMult = 2.0f;

        [Header("Physics Model - Weight Distribution")]
        [Range(0.1f, 0.6f)] public float weightRatioHandsVertical = 0.35f;
        [Range(0.3f, 0.8f)] public float weightRatioHandsOverhang = 0.55f;
        [Range(0.5f, 0.9f)] public float weightRatioHandsStrongOverhang = 0.70f;
        [Range(0.7f, 1f)] public float weightRatioHandsRoof = 0.90f;
        [Range(0.05f, 0.3f)] public float weightRatioHandsSlab = 0.15f;

        [Header("Wall Angle - Endurance Multipliers")]
        [Range(0.3f, 1f)] public float wallAngleEnduranceMultSlab = 0.6f;
        public float wallAngleEnduranceMultVertical = 1.0f;
        [Range(1f, 2.5f)] public float wallAngleEnduranceMultOverhang = 1.6f;
        [Range(1.5f, 3f)] public float wallAngleEnduranceMultStrongOverhang = 2.0f;
        [Range(2f, 4f)] public float wallAngleEnduranceMultRoof = 2.5f;

        [Header("Wall Angle - Fatigue Multipliers")]
        [Range(0.2f, 1f)] public float wallAngleFatigueMultSlab = 0.5f;
        public float wallAngleFatigueMultVertical = 1.0f;
        [Range(1f, 3f)] public float wallAngleFatigueMultOverhang = 1.8f;
        [Range(1.5f, 3.5f)] public float wallAngleFatigueMultStrongOverhang = 2.5f;
        [Range(2f, 5f)] public float wallAngleFatigueMultRoof = 3.0f;

        [Header("Wall Angle - Grip Drain Multipliers")]
        [Range(0.3f, 1f)] public float wallAngleGripDrainMultSlab = 0.7f;
        public float wallAngleGripDrainMultVertical = 1.0f;
        [Range(1f, 2f)] public float wallAngleGripDrainMultOverhang = 1.4f;
        [Range(1.2f, 2.5f)] public float wallAngleGripDrainMultStrongOverhang = 1.7f;
        [Range(1.5f, 3f)] public float wallAngleGripDrainMultRoof = 2.0f;

        [Header("Body-Wall Contact")]
        [Range(0.1f, 0.8f)] public float slabBodyFriction = 0.4f;
        [Range(0.1f, 0.6f)] public float bodyWallContactThreshold = 0.3f;

        [Header("Grip (Adherence)")]
        [Range(10f, 200f)] public float adherenceMax = 100f;
        [Range(0f, 5f)] public float adherenceLossBasePerS = 1.5f;
        [Range(0f, 15f)] public float adherenceLossBurst = 5f;
        [Range(0f, 60f)] public float chalkRestoreValue = 35f;
        [Range(5f, 60f)] public float chalkCooldownSeconds = 20f;
        [Range(5f, 50f)] public float adherenceMicroSlipThreshold = 30f;

        [Header("Local Fatigue")]
        [Range(1f, 20f)] public float fatigueGainBasePerS = 7f;
        [Range(0f, 20f)] public float fatigueRecoverRestPerS = 10f;
        [Range(40f, 80f)] public float fatigueWarningThreshold = 60f;
        [Range(60f, 95f)] public float fatigueCriticalThreshold = 80f;
        [Range(85f, 100f)] public float fatigueFailureThreshold = 95f;

        [Header("Point & Place")]
        [Min(0.1f)] public float footPlaceRange = 1.0f;
        [Range(0.05f, 1f)] public float cursorMagnetRadius = 0.3f;

        [Header("Climbing Resources Interplay")]
        [Range(1f, 3f)] public float adherenceLowFatigueMult = 1.5f;
        [Range(1f, 4f)] public float adherenceEmptyFatigueMult = 2.5f;
        [Range(1f, 3f)] public float enduranceEmptyFatigueMult = 2.0f;
        [Range(0f, 1f)] public float oneFootHandLoadShare = 0.4f;

        [Header("Climber Motion")]
        [Range(0.5f, 3f)] public float walkSpeed = 1.6f;
        [Range(1f, 6f)] public float runSpeed = 3.2f;
        [Range(0.05f, 1f)] public float limbMoveSeconds = 0.25f;
        [Range(0.05f, 1f)] public float bodySettleSeconds = 0.35f;
        [Range(0.2f, 2f)] public float landingSeconds = 0.8f;
        [Range(0.5f, 4f)] public float maxStartDistance = 2.5f;

        [Header("Camera")]
        [Range(1f, 10f)] public float cameraGroundDistance = 4.2f;
        [Range(1f, 10f)] public float cameraWallDistance = 3.4f;
        [Range(0.5f, 3f)] public float cameraMinDistance = 1.2f;
        [Range(3f, 15f)] public float cameraMaxDistance = 9f;
        [Range(0.05f, 1f)] public float cameraOrbitDegreesPerPixel = 0.25f;
        [Range(-0.5f, 1f)] public float cameraPivotLift = 0.15f;

        [Header("Grip Type - Fatigue Multipliers")]
        [Range(0.3f, 1f)] public float gripTypeFatigueMultJug = 0.6f;
        [Range(0.4f, 1.2f)] public float gripTypeFatigueMultOpen = 0.8f;
        public float gripTypeFatigueMultPinch = 1.0f;
        [Range(0.8f, 1.8f)] public float gripTypeFatigueMultCrimp = 1.3f;
        [Range(1f, 2f)] public float gripTypeFatigueMultGaston = 1.4f;
        [Range(1f, 2.2f)] public float gripTypeFatigueMultUndercling = 1.5f;
        [Range(0.8f, 2f)] public float gripTypeFatigueMultSloper = 1.2f;

        [Header("Grip Type - Drain Multipliers")]
        [Range(0.3f, 1f)] public float gripTypeDrainMultJug = 0.7f;
        [Range(0.4f, 1.2f)] public float gripTypeDrainMultOpen = 0.9f;
        public float gripTypeDrainMultPinch = 1.0f;
        [Range(0.8f, 1.6f)] public float gripTypeDrainMultCrimp = 1.1f;
        [Range(0.8f, 1.6f)] public float gripTypeDrainMultGaston = 1.2f;
        [Range(0.8f, 1.6f)] public float gripTypeDrainMultUndercling = 1.2f;
        [Range(1f, 2f)] public float gripTypeDrainMultSloper = 1.5f;

        [Header("Foot Techniques")]
        [Range(0.3f, 0.8f)] public float footTechniqueEdgingWeightShare = 0.55f;
        [Range(0.2f, 0.5f)] public float footTechniqueSmearingWeightShare = 0.35f;

        [Header("Body Orientation")]
        [Range(15f, 90f)] public float hipRotationMaxDeg = 60f;
        [Range(15f, 60f)] public float torsoTwistMaxDeg = 45f;
        [Range(30f, 360f)] public float bodyRotationSpeedDegPerS = 120f;

        [Header("Flag")]
        [Range(0f, 1f)] public float flagLateralOffsetMultiplier = 0.6f;
    }
}
