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
    }
}
