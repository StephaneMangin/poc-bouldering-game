using System;
using Project.Core.Domain;
using Project.Features.Gym.Presentation;
using UnityEngine;

namespace Project.App
{
    /// <summary>Everything the game loads at start, created by Project/Game/Set Up Scene.</summary>
    [CreateAssetMenu(fileName = "GameAssets", menuName = "Project/Game Assets")]
    public sealed class GameAssets : ScriptableObject
    {
        public TextAsset layout;
        public TextAsset problems;
        public TuningConfig tuning;
        public GymTuning gymTuning;
        public GymMaterials materials;
        public ClimberModel[] climbers;
        public RuntimeAnimatorController locomotion;
    }

    [Serializable]
    public sealed class ClimberModel
    {
        public string displayName;
        public GameObject model;
    }
}
