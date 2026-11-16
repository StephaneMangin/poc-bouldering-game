using Project.App;
using Project.Core.Domain;
using Project.Features.Camera.Presentation;
using Project.Features.Gym.Presentation;
using Project.Features.Session.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Tools.Editor
{
    /// <summary>
    /// Creates the game's template materials, tuning and asset bundle, and the BlockOutRennes scene
    /// (camera, sun, bootstrap), then makes it the only build scene. Safe to run again.
    /// Batch: -executeMethod Project.Tools.Editor.GameSetup.Run -quit
    /// </summary>
    public static class GameSetup
    {
        public const string ScenePath = Root + "/Scenes/BlockOutRennes.unity";

        private const string Root = "Assets/BoulderingGym";
        private const string MaterialsFolder = Root + "/Data/Gameplay/Materials";
        private const string GameFolder = Root + "/Data/Gameplay/Game";
        private const string LayoutPath = Root + "/Data/Gameplay/GymScenes/blockout_rennes_layout.json";
        private const string ProblemsPath = Root + "/Data/Gameplay/GymScenes/blockout_rennes_problems.json";
        private const string TuningPath = Root + "/Data/Gameplay/Tuning/TuningConfig.asset";
        private const string GymTuningPath = Root + "/Data/Gameplay/Tuning/GymTuning.asset";
        private const string GameAssetsPath = GameFolder + "/GameAssets.asset";
        private const string LocomotionPath = Root + "/Data/Gameplay/Animation/HumanoidLocomotion.controller";
        private const string JohnPath = Root + "/Art/Characters/John.fbx";
        private const string ElsaPath = Root + "/Art/Characters/Elsa.fbx";

        [MenuItem("Project/Game/Set Up Scene and Assets")]
        public static void Run()
        {
            EnsureFolder(MaterialsFolder);
            EnsureFolder(GameFolder);
            var assets = LoadOrCreate<GameAssets>(GameAssetsPath);
            assets.layout = Load<TextAsset>(LayoutPath);
            assets.problems = Load<TextAsset>(ProblemsPath);
            assets.tuning = Load<TuningConfig>(TuningPath);
            assets.gymTuning = LoadOrCreate<GymTuning>(GymTuningPath);
            assets.materials = CreateMaterials();
            assets.climbers = new[]
            {
                new ClimberModel { displayName = "John", model = Load<GameObject>(JohnPath) },
                new ClimberModel { displayName = "Elsa", model = Load<GameObject>(ElsaPath) },
            };
            assets.locomotion = Load<RuntimeAnimatorController>(LocomotionPath);
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            CreateScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Block'Out Rennes";
            Debug.Log($"[GameSetup] Scene {ScenePath} ready with {assets.climbers.Length} climbers.");
        }

        private static GymMaterials CreateMaterials()
        {
            var standard = Shader.Find("Standard");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var label = SaveMaterial(new Material(Shader.Find("GUI/Text Shader")) { mainTexture = font.material.mainTexture }, "MAT_Label");
            var marker = new Material(standard) { color = Color.white };
            marker.EnableKeyword("_EMISSION");
            marker.SetColor("_EmissionColor", Color.white);
            marker.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return new GymMaterials
            {
                wall = SaveMaterial(Smooth(new Material(standard), 0.12f), "MAT_Wall"),
                plain = SaveMaterial(Smooth(new Material(standard), 0.2f), "MAT_Plain"),
                hold = SaveMaterial(Smooth(new Material(standard), 0.45f), "MAT_Hold"),
                marker = SaveMaterial(marker, "MAT_Marker"),
                label = label,
                labelFont = font,
            };
        }

        private static Material Smooth(Material material, float smoothness)
        {
            material.SetFloat("_Glossiness", smoothness);
            material.enableInstancing = true;
            return material;
        }

        /// <summary>A new single scene unloads unreferenced assets, so the game assets are loaded again after it.</summary>
        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var assets = Load<GameAssets>(GameAssetsPath);
            var camera = new GameObject("Main Camera") { tag = "MainCamera" };
            var view = camera.AddComponent<UnityEngine.Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.18f, 0.19f, 0.2f);
            view.nearClipPlane = 0.05f;
            view.farClipPlane = 150f;
            camera.AddComponent<AudioListener>();
            camera.AddComponent<OrbitCamera>();
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;

            var game = new GameObject("Game");
            game.AddComponent<SessionController>();
            var bootstrap = game.AddComponent<GameBootstrap>();
            game.AddComponent<GameFlow>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("assets").objectReferenceValue = assets;
            serialized.FindProperty("viewCamera").objectReferenceValue = view;
            serialized.FindProperty("sun").objectReferenceValue = sun;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Material SaveMaterial(Material material, string name)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var existing = Load<Material>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(material, existing);
                return existing;
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = Load<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
