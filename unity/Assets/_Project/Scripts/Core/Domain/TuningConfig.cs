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
    }
}
