using Project.Features.Climbing.Domain.PhysicsModel;
using Project.Features.Climbing.Presentation;
using UnityEditor;
using UnityEngine;

namespace Project.Tools.Editor
{
    public static class PhysicsMaterialAssigner
    {
        private const string BasePath = "Assets/BoulderingGym/Data/PhysicsMaterials";

        [MenuItem("Tools/Physics Materials/Create All Materials")]
        public static void CreateAllMaterials()
        {
            if (!AssetDatabase.IsValidFolder(BasePath))
            {
                AssetDatabase.CreateFolder("Assets/BoulderingGym/Data", "PhysicsMaterials");
            }

            CreateMaterial("PM_Wall_Rough", 0.8f, 0.6f, 0f);
            CreateMaterial("PM_Wall_Smooth", 0.5f, 0.3f, 0f);
            CreateMaterial("PM_Hold_Rubber", 0.9f, 0.7f, 0f);
            CreateMaterial("PM_Hold_Wet", 0.3f, 0.2f, 0.05f);
            CreateMaterial("PM_CrashPad", 0.9f, 0.8f, 0.15f);
            CreateMaterial("PM_Shoe_Rubber", 0.95f, 0.8f, 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PhysicsMaterialAssigner] Created 6 PhysicMaterials in " + BasePath);
        }

        [MenuItem("Tools/Physics Materials/Assign To Scene Objects")]
        public static void AssignToSceneObjects()
        {
            var wallRough = LoadMaterial("PM_Wall_Rough");
            var wallSmooth = LoadMaterial("PM_Wall_Smooth");
            var holdRubber = LoadMaterial("PM_Hold_Rubber");
            var crashPad = LoadMaterial("PM_CrashPad");
            var shoeRubber = LoadMaterial("PM_Shoe_Rubber");

            if (wallRough == null || holdRubber == null)
            {
                Debug.LogError("[PhysicsMaterialAssigner] Materials not found. Run 'Create All Materials' first.");
                return;
            }

            var assignedCount = 0;

            // Wall panels
            var wallPanels = Object.FindObjectsByType<WallPanelComponent>(FindObjectsSortMode.None);
            foreach (var panel in wallPanels)
            {
                var collider = panel.GetComponent<Collider>();
                if (collider == null) continue;

                collider.material = panel.surfaceType == WallSurfaceType.Smooth ? wallSmooth : wallRough;
                EditorUtility.SetDirty(collider);
                assignedCount++;
            }

            // Holds
            var holds = Object.FindObjectsByType<HoldComponent>(FindObjectsSortMode.None);
            foreach (var hold in holds)
            {
                var collider = hold.GetComponent<Collider>();
                if (collider == null) continue;

                collider.material = holdRubber;
                EditorUtility.SetDirty(collider);
                assignedCount++;
            }

            // Crash pads (by name convention)
            var allObjects = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (var col in allObjects)
            {
                if (col.gameObject.name.Contains("CrashPad") || col.gameObject.name.Contains("Mat_Floor"))
                {
                    col.material = crashPad;
                    EditorUtility.SetDirty(col);
                    assignedCount++;
                }
            }

            // Player shoe (capsule collider on player root)
            var drivers = Object.FindObjectsByType<ClimbingStateMachineDriver>(FindObjectsSortMode.None);
            foreach (var d in drivers)
            {
                var capsule = d.GetComponent<CapsuleCollider>();
                if (capsule != null && shoeRubber != null)
                {
                    capsule.material = shoeRubber;
                    EditorUtility.SetDirty(capsule);
                    assignedCount++;
                }
            }

            Debug.Log($"[PhysicsMaterialAssigner] Assigned PhysicMaterials to {assignedCount} colliders.");
        }

        private static void CreateMaterial(string name, float staticFriction, float dynamicFriction, float bounciness)
        {
            var path = $"{BasePath}/{name}.physicMaterial";
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (existing != null)
            {
                existing.staticFriction = staticFriction;
                existing.dynamicFriction = dynamicFriction;
                existing.bounciness = bounciness;
                existing.frictionCombine = PhysicsMaterialCombine.Average;
                existing.bounceCombine = PhysicsMaterialCombine.Average;
                EditorUtility.SetDirty(existing);
                return;
            }

            var mat = new PhysicsMaterial(name)
            {
                staticFriction = staticFriction,
                dynamicFriction = dynamicFriction,
                bounciness = bounciness,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Average,
            };

            AssetDatabase.CreateAsset(mat, path);
        }

        private static PhysicsMaterial LoadMaterial(string name)
        {
            return AssetDatabase.LoadAssetAtPath<PhysicsMaterial>($"{BasePath}/{name}.physicMaterial");
        }
    }
}
