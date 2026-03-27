using System.IO;
using Project.Core.Domain;
using Project.Core.Input;
using Project.Features.Climbing.Presentation;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Tools.Editor
{
    public static class Sprint1_0PrototypeBootstrapper
    {
        private const string ScenePath = "Assets/_Project/Scenes/VerticalPrototype.unity";
        private const string TuningPath = "Assets/_Project/Data/Gameplay/Tuning/TuningConfig.asset";

        [MenuItem("Project/Bootstrap/Setup Sprint 1.0 Vertical Prototype")]
        public static void SetupSprint1_0Prototype()
        {
            EnsureDirectories();

            var scene = OpenOrCreateScene();
            var tuningConfig = LoadOrCreateTuningConfig();

            var holdRegistry = EnsureHoldRegistry();
            var player = EnsurePlayer(tuningConfig);
            var camera = EnsureCamera(player.transform);
            var driver = player.GetComponent<ClimbingStateMachineDriver>();
            SetSerializedField(driver, "holdRegistry", holdRegistry);
            EnsureDebugUi(driver, tuningConfig, camera.GetComponent<CameraFollowController>());
            EnsureWalls();
            EnsureHolds();

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("VerticalPrototype scene bootstrapped. Remaining manual step: wire Two Bone IK constraints if a rigged character is available.");
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes");
            Directory.CreateDirectory("Assets/_Project/Data/Gameplay/Tuning");
            Directory.CreateDirectory("Assets/_Project/Prefabs/Gameplay");
        }

        private static Scene OpenOrCreateScene()
        {
            if (File.Exists(ScenePath))
            {
                return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "VerticalPrototype";
            return scene;
        }

        private static TuningConfig LoadOrCreateTuningConfig()
        {
            var tuningConfig = AssetDatabase.LoadAssetAtPath<TuningConfig>(TuningPath);
            if (tuningConfig == null)
            {
                tuningConfig = ScriptableObject.CreateInstance<TuningConfig>();
                AssetDatabase.CreateAsset(tuningConfig, TuningPath);
            }

            tuningConfig.tuningVersion = "0.1.0";
            tuningConfig.grabRange = 1.6f;
            tuningConfig.grabAngleMax = 35f;
            tuningConfig.hipsMoveSpeed = 2.0f;
            tuningConfig.enduranceMax = 100f;
            tuningConfig.enduranceCostGrab = 2f;
            EditorUtility.SetDirty(tuningConfig);
            return tuningConfig;
        }

        private static Camera EnsureCamera(Transform target)
        {
            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.tag = "MainCamera";
                EnsureCameraFollow(camera, target);
                return camera;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            EnsureCameraFollow(camera, target);
            return camera;
        }

        private static void EnsureCameraFollow(Camera camera, Transform target)
        {
            if (camera == null)
            {
                return;
            }

            var followController = camera.GetComponent<CameraFollowController>();
            if (followController == null)
            {
                followController = camera.gameObject.AddComponent<CameraFollowController>();
            }

            followController.SetTarget(target);
            SetSerializedField(followController, "followSmoothTime", 0.6f);
        }

        private static GameObject EnsurePlayer(TuningConfig tuningConfig)
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                player = new GameObject("Player");
            }

            player.transform.position = new Vector3(0f, 0f, 0.8f);

            var hips = EnsureChild(player.transform, "Hips", new Vector3(0f, 1f, 0f));
            var leftHandAnchor = EnsureChild(player.transform, "LeftHandAnchor", new Vector3(-0.35f, 1.45f, 0.65f));
            var rightHandAnchor = EnsureChild(player.transform, "RightHandAnchor", new Vector3(0.35f, 1.45f, 0.65f));
            var wallNormalReference = EnsureChild(player.transform, "WallNormalReference", new Vector3(0f, 1.4f, 0f));
            var bodyVisual = EnsureBodyVisual(player.transform);
            EnsureHandMarker(leftHandAnchor, "LeftHandMarker", new Color(0.2f, 0.9f, 1f, 1f));
            EnsureHandMarker(rightHandAnchor, "RightHandMarker", new Color(1f, 0.25f, 0.85f, 1f));
            wallNormalReference.forward = Vector3.back;

            var hands = player.GetComponent<PlayerHandsController>();
            if (hands == null)
            {
                hands = player.AddComponent<PlayerHandsController>();
            }

            var inputReader = player.GetComponent<InputReader>();
            if (inputReader == null)
            {
                inputReader = player.AddComponent<InputReader>();
            }

            SetSerializedField(hands, "leftHandAnchor", leftHandAnchor);
            SetSerializedField(hands, "rightHandAnchor", rightHandAnchor);
            SetSerializedField(hands, "tuningConfig", tuningConfig);
            SetSerializedField(hands, "drawDebugGizmos", true);

            var hipsMovement = hips.GetComponent<HipsMovementController>();
            if (hipsMovement == null)
            {
                hipsMovement = hips.gameObject.AddComponent<HipsMovementController>();
            }

            SetSerializedField(hipsMovement, "tuningConfig", tuningConfig);
            SetSerializedField(hipsMovement, "inputReader", inputReader);

            var driver = player.GetComponent<ClimbingStateMachineDriver>();
            if (driver == null)
            {
                driver = player.AddComponent<ClimbingStateMachineDriver>();
            }

            SetSerializedField(driver, "tuningConfig", tuningConfig);
            SetSerializedField(driver, "playerHands", hands);
            SetSerializedField(driver, "wallNormalReference", wallNormalReference);
            SetSerializedField(driver, "inputReader", inputReader);

            if (hips.GetComponent<SphereCollider>() == null)
            {
                var collider = hips.gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.35f;
            }

            bodyVisual.localPosition = new Vector3(0f, 0.15f, 0f);

            return player;
        }

        private static HoldRegistry EnsureHoldRegistry()
        {
            var registryObject = GameObject.Find("HoldRegistry");
            if (registryObject == null)
            {
                registryObject = new GameObject("HoldRegistry");
            }

            var registry = registryObject.GetComponent<HoldRegistry>();
            if (registry == null)
            {
                registry = registryObject.AddComponent<HoldRegistry>();
            }

            registry.Refresh();
            return registry;
        }

        private static void EnsureDebugUi(
            ClimbingStateMachineDriver driver,
            TuningConfig tuningConfig,
            CameraFollowController cameraFollowController)
        {
            var debugUi = GameObject.Find("DebugUI");
            if (debugUi == null)
            {
                debugUi = new GameObject("DebugUI");
            }

            var overlay = debugUi.GetComponent<ClimbingStateOverlay>();
            if (overlay == null)
            {
                overlay = debugUi.AddComponent<ClimbingStateOverlay>();
            }

            SetSerializedField(overlay, "driver", driver);

            var tuningOverlay = debugUi.GetComponent<PlaytestTuningOverlay>();
            if (tuningOverlay == null)
            {
                tuningOverlay = debugUi.AddComponent<PlaytestTuningOverlay>();
            }

            SetSerializedField(tuningOverlay, "tuningConfig", tuningConfig);
            SetSerializedField(tuningOverlay, "cameraFollowController", cameraFollowController);
        }

        private static void EnsureWalls()
        {
            EnsureWall("Wall_Flat", new Vector3(-2.5f, 3f, 2f), new Vector3(4f, 6f, 0.5f), Quaternion.identity);
            EnsureWall("Wall_Overhang", new Vector3(2.5f, 3.2f, 2f), new Vector3(4f, 4f, 0.5f), Quaternion.Euler(-15f, 0f, 0f));
            EnsureFloor();
        }

        private static void EnsureWall(string name, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            var wall = GameObject.Find(name);
            if (wall == null)
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = name;
            }

            wall.transform.position = position;
            wall.transform.rotation = rotation;
            wall.transform.localScale = scale;
        }

        private static void EnsureFloor()
        {
            var floor = GameObject.Find("Floor");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Floor";
            }

            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(14f, 1f, 8f);
        }

        private static void EnsureHolds()
        {
            var root = GameObject.Find("Holds");
            if (root == null)
            {
                root = new GameObject("Holds");
            }

            var positions = new[]
            {
                new Vector3(-3.3f, 1.2f, 1.7f),
                new Vector3(-2.6f, 2.1f, 1.7f),
                new Vector3(-1.8f, 3.0f, 1.7f),
                new Vector3(-1.0f, 4.1f, 1.7f),
                new Vector3(-0.2f, 5.0f, 1.7f),
                new Vector3(1.5f, 1.5f, 1.55f),
                new Vector3(2.1f, 2.3f, 1.75f),
                new Vector3(2.8f, 3.2f, 1.95f),
                new Vector3(3.3f, 4.0f, 2.12f),
                new Vector3(3.8f, 4.8f, 2.32f),
            };

            for (var index = 0; index < positions.Length; index++)
            {
                EnsureHold(root.transform, index + 1, positions[index]);
            }
        }

        private static void EnsureHold(Transform parent, int index, Vector3 position)
        {
            var name = $"Hold_{index:00}";
            var hold = parent.Find(name)?.gameObject;
            if (hold == null)
            {
                hold = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hold.name = name;
                hold.transform.SetParent(parent);
            }

            hold.transform.position = position;
            hold.transform.localScale = new Vector3(0.3f, 0.18f, 0.2f);

            var gripPoint = EnsureChild(hold.transform, "GripPoint", new Vector3(0f, 0f, -0.18f));
            gripPoint.localPosition = new Vector3(0f, 0f, -0.18f);

            var holdComponent = hold.GetComponent<HoldComponent>();
            if (holdComponent == null)
            {
                holdComponent = hold.AddComponent<HoldComponent>();
            }

            SetSerializedField(holdComponent, "gripPoint", gripPoint);
        }

        private static Transform EnsureChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                child.localPosition = localPosition;
                return child;
            }

            var childObject = new GameObject(name);
            childObject.transform.SetParent(parent);
            childObject.transform.localPosition = localPosition;
            childObject.transform.localRotation = Quaternion.identity;
            return childObject.transform;
        }

        private static Transform EnsureBodyVisual(Transform player)
        {
            var body = player.Find("BodyVisual");
            if (body != null)
            {
                return body;
            }

            var bodyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyObject.name = "BodyVisual";
            bodyObject.transform.SetParent(player);
            bodyObject.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Object.DestroyImmediate(bodyObject.GetComponent<CapsuleCollider>());
            return bodyObject.transform;
        }

        private static void EnsureHandMarker(Transform anchor, string markerName, Color color)
        {
            if (anchor == null)
            {
                return;
            }

            var marker = anchor.Find(markerName);
            if (marker == null)
            {
                var markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markerObject.name = markerName;
                markerObject.transform.SetParent(anchor);
                markerObject.transform.localPosition = Vector3.zero;
                markerObject.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
                Object.DestroyImmediate(markerObject.GetComponent<SphereCollider>());
                marker = markerObject.transform;
            }

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = color;
            }
        }

        private static void SetSerializedField(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerializedField(Object target, string fieldName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerializedField(Object target, string fieldName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                return;
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
