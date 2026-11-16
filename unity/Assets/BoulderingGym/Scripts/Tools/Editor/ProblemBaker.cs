using System.IO;
using Project.Features.Climbing.Domain;
using Project.Features.Gym.Domain;
using Project.Features.Gym.Presentation;
using UnityEditor;
using UnityEngine;

namespace Project.Tools.Editor
{
    /// <summary>
    /// Generates the problems and volumes of the gym from the layout and the gym tuning, and saves
    /// them for the game to load. Run it after changing the layout or the circuit tuning.
    /// Batch: -executeMethod Project.Tools.Editor.ProblemBaker.Bake -quit
    /// </summary>
    public static class ProblemBaker
    {
        private const string LayoutPath = "Assets/BoulderingGym/Data/Gameplay/GymScenes/blockout_rennes_layout.json";
        private const string ProblemsPath = "Assets/BoulderingGym/Data/Gameplay/GymScenes/blockout_rennes_problems.json";
        private const string GymTuningPath = "Assets/BoulderingGym/Data/Gameplay/Tuning/GymTuning.asset";

        [MenuItem("Project/Gym/Bake Problems")]
        public static void Bake()
        {
            var layout = GymLayoutFactory.Build(JsonUtility.FromJson<LayoutDefinition>(File.ReadAllText(LayoutPath)));
            var tuning = AssetDatabase.LoadAssetAtPath<GymTuning>(GymTuningPath);
            var circuits = Circuit.CreateAll(tuning.circuits);
            var content = GymContent.Plan(
                layout,
                circuits,
                ClimberAnatomy.Default.Scaled(tuning.smallestClimberScale),
                ClimberAnatomy.Default.Scaled(tuning.largestClimberScale));
            File.WriteAllText(ProblemsPath, JsonUtility.ToJson(ContentBaking.ToDefinition(content)));
            AssetDatabase.ImportAsset(ProblemsPath);
            Debug.Log($"[ProblemBaker] {content.ProblemCount} problems, {content.HoldCount} holds baked to {ProblemsPath}.");
        }
    }
}
