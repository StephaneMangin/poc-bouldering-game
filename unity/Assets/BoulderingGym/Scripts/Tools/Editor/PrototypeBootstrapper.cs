using System.IO;
using Project.Core.Domain;
using Project.Core.Input;
using Project.Features.Climbing.Domain.PhysicsModel;
using Project.Features.Climbing.Presentation;
using Project.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Tools.Editor
{
    /// <summary>
    /// Unified prototype bootstrapper — sets up the entire VerticalPrototype scene
    /// from a single menu entry. Internals are split into isolated modules (nested
    /// static classes) so each domain can evolve independently without regressions.
    /// </summary>
    public static class PrototypeBootstrapper
    {
        // ── Variant selector ──
        private enum HumanoidVariant { Male, Female }

        // ── Asset paths ──
        private const string ScenePath = "Assets/BoulderingGym/Scenes/VerticalPrototype.unity";
        private const string TuningPath = "Assets/BoulderingGym/Data/Gameplay/Tuning/TuningConfig.asset";
        private const string LocomotionControllerPath = "Assets/BoulderingGym/Data/Gameplay/Animation/HumanoidLocomotion.controller";
        private const string PreferredMaleHumanoidPath = "Assets/BoulderingGym/Art/Characters/John.fbx";
        private const string PreferredFemaleHumanoidPath = "Assets/BoulderingGym/Art/Characters/Elsa.fbx";
        private const string MaleAnimationsDir = "Assets/BoulderingGym/Art/Animations/Male Walk";
        private const string FemaleAnimationsDir = "Assets/BoulderingGym/Art/Animations/Female Walk";

        // ── Wall geometry constants (shared by GymModule, HoldsModule, PhysicsModelModule) ──
        private const float WallBaseZ = 3.0f;
        private const float PanelThickness = 0.25f;

        private static readonly string[] PanelNames =
        {
            "Wall_Slab", "Wall_VertLeft", "Wall_VertCenter",
            "Wall_Overhang15", "Wall_Overhang30", "Wall_Overhang45",
        };

        // ── WallDef: auto-computes center so bottom-front edge sits at (y=0, z=WallBaseZ) ──
        private readonly struct WallDef
        {
            public readonly Vector3 center;
            public readonly float scaleZ;
            public readonly float angleX;
            public readonly float xMin;
            public readonly float xMax;
            public readonly float height;

            public WallDef(float xMin, float xMax, float height, float thickness, float angleX)
            {
                this.xMin = xMin;
                this.xMax = xMax;
                this.height = height;
                this.scaleZ = thickness;
                this.angleX = angleX;

                var halfH = height / 2f;
                var halfD = thickness / 2f;
                var thetaRad = angleX * Mathf.Deg2Rad;
                var cosT = Mathf.Cos(thetaRad);
                var sinT = Mathf.Sin(thetaRad);

                center = new Vector3(
                    (xMin + xMax) / 2f,
                    halfH * cosT - halfD * sinT,
                    WallBaseZ + halfH * sinT + halfD * cosT
                );
            }

            public float Width => xMax - xMin;
        }

        // ── 6 wall profiles ──
        private static readonly WallDef WallSlabDef = new(-14f, -9f, 5.5f, PanelThickness, 8f);
        private static readonly WallDef WallVertLeftDef = new(-9f, -4f, 5.5f, PanelThickness, 0f);
        private static readonly WallDef WallVertCenterDef = new(-4f, 4f, 6f, PanelThickness, 0f);
        private static readonly WallDef WallOverhang15Def = new(4f, 9f, 6.2f, PanelThickness, -15f);
        private static readonly WallDef WallOverhang30Def = new(9f, 14f, 6.5f, PanelThickness, -30f);
        private static readonly WallDef WallOverhang45Def = new(14f, 18f, 7f, PanelThickness, -45f);

        private static readonly WallDef[] AllWalls =
        {
            WallSlabDef, WallVertLeftDef, WallVertCenterDef,
            WallOverhang15Def, WallOverhang30Def, WallOverhang45Def,
        };

        private static WallDef GetWallForX(float x)
        {
            foreach (var w in AllWalls)
            {
                if (x < w.xMax) return w;
            }
            return AllWalls[AllWalls.Length - 1];
        }

        // ═════════════════════════════════════════════════════════════════
        //  Menu Items & Orchestrator
        // ═════════════════════════════════════════════════════════════════

        [MenuItem("Project/Bootstrap/Setup Prototype/Male")]
        public static void SetupPrototypeMale() => SetupPrototype(HumanoidVariant.Male);

        [MenuItem("Project/Bootstrap/Setup Prototype/Female")]
        public static void SetupPrototypeFemale() => SetupPrototype(HumanoidVariant.Female);

        private static void SetupPrototype(HumanoidVariant variant)
        {
            SceneModule.EnsureDirectories();
            HumanoidModule.EnsureMixamoImports(variant);

            var scene = SceneModule.EnsureScene();
            var tuning = SceneModule.LoadOrCreateTuningConfig();
            var holdRegistry = SceneModule.EnsureHoldRegistry();

            var player = PlayerModule.EnsurePlayer(tuning, variant, holdRegistry);
            var camera = CameraModule.EnsureCamera(player.transform);

            GymModule.EnsureWalls();
            HoldsModule.EnsureHolds();
            PhysicsModelModule.Setup(player, tuning);

            var driver = player.GetComponent<ClimbingStateMachineDriver>();
            DebugUiModule.Setup(driver, tuning, camera.GetComponent<CameraFollowController>());

            holdRegistry.Refresh();
            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"Prototype bootstrapped ({variant} humanoid). Save scene with Ctrl+S.");
        }

        // ═════════════════════════════════════════════════════════════════
        //  Helpers — shared utilities for all modules
        // ═════════════════════════════════════════════════════════════════
        private static class Helpers
        {
            public static Transform EnsureChild(Transform parent, string name, Vector3 localPosition)
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

            public static void ClearChildren(Transform root)
            {
                for (var index = root.childCount - 1; index >= 0; index--)
                {
                    Object.DestroyImmediate(root.GetChild(index).gameObject);
                }
            }

            public static void PurgePrimitiveComponents(GameObject go)
            {
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null) Object.DestroyImmediate(renderer);
                var filter = go.GetComponent<MeshFilter>();
                if (filter != null) Object.DestroyImmediate(filter);
                var collider = go.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }

            public static void SetSerializedField(Object target, string fieldName, Object value)
            {
                var serializedObject = new SerializedObject(target);
                var property = serializedObject.FindProperty(fieldName);
                if (property == null) return;

                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }

            public static void SetSerializedField(Object target, string fieldName, bool value)
            {
                var serializedObject = new SerializedObject(target);
                var property = serializedObject.FindProperty(fieldName);
                if (property == null) return;

                property.boolValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }

            public static void SetSerializedField(Object target, string fieldName, float value)
            {
                var serializedObject = new SerializedObject(target);
                var property = serializedObject.FindProperty(fieldName);
                if (property == null) return;

                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  SceneModule — directories, scene, TuningConfig, HoldRegistry
        // ═════════════════════════════════════════════════════════════════
        private static class SceneModule
        {
            public static void EnsureDirectories()
            {
                Directory.CreateDirectory("Assets/BoulderingGym/Scenes");
                Directory.CreateDirectory("Assets/BoulderingGym/Data/Gameplay/Tuning");
                Directory.CreateDirectory("Assets/BoulderingGym/Data/Gameplay/Animation");
                Directory.CreateDirectory("Assets/BoulderingGym/Prefabs/Gameplay");
            }

            /// <summary>
            /// Returns the active scene. Creates a new one only if none is loaded.
            /// Never calls OpenScene to avoid re-serialization regressions.
            /// </summary>
            public static Scene EnsureScene()
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded)
                {
                    return activeScene;
                }

                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                scene.name = "VerticalPrototype";
                EditorSceneManager.SaveScene(scene, ScenePath);
                return scene;
            }

            public static TuningConfig LoadOrCreateTuningConfig()
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

            public static HoldRegistry EnsureHoldRegistry()
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

                return registry;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  CameraModule — Main Camera + CameraFollowController
        // ═════════════════════════════════════════════════════════════════
        private static class CameraModule
        {
            public static Camera EnsureCamera(Transform target)
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
                if (camera == null) return;

                var followController = camera.GetComponent<CameraFollowController>();
                if (followController == null)
                {
                    followController = camera.gameObject.AddComponent<CameraFollowController>();
                }

                followController.SetTarget(target);
                Helpers.SetSerializedField(followController, "followSmoothTime", 0.6f);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  PlayerModule — Player GO, components, wiring
        // ═════════════════════════════════════════════════════════════════
        private static class PlayerModule
        {
            public static GameObject EnsurePlayer(TuningConfig tuningConfig, HumanoidVariant variant, HoldRegistry holdRegistry)
            {
                var player = GameObject.Find("Player");
                if (player == null)
                {
                    player = new GameObject("Player");
                }

                player.transform.position = new Vector3(0f, 0.3f, 0.8f);

                var hips = Helpers.EnsureChild(player.transform, "Hips", new Vector3(0f, 1f, 0f));
                var leftHandAnchor = Helpers.EnsureChild(player.transform, "LeftHandAnchor", new Vector3(-0.35f, 1.45f, 0.65f));
                var rightHandAnchor = Helpers.EnsureChild(player.transform, "RightHandAnchor", new Vector3(0.35f, 1.45f, 0.65f));
                var wallNormalReference = Helpers.EnsureChild(player.transform, "WallNormalReference", new Vector3(0f, 1.4f, 0f));
                var bodyVisual = HumanoidModule.EnsureHumanoidMannequin(player.transform, variant, out var rigMapping);
                EnsureHandMarker(leftHandAnchor, "LeftHandMarker", new Color(0.2f, 0.9f, 1f, 1f));
                EnsureHandMarker(rightHandAnchor, "RightHandMarker", new Color(1f, 0.25f, 0.85f, 1f));
                wallNormalReference.forward = Vector3.back;

                // Remove legacy child ResetAnchor (must NOT be a child — it moves with the player)
                var oldChild = player.transform.Find("ResetAnchor");
                if (oldChild != null)
                {
                    Object.DestroyImmediate(oldChild.gameObject);
                }

                // Root-level ResetAnchor at crash-pad surface height
                var resetAnchorGo = GameObject.Find("ResetAnchor") ?? new GameObject("ResetAnchor");
                resetAnchorGo.transform.SetParent(null);
                resetAnchorGo.transform.SetPositionAndRotation(
                    new Vector3(0f, 0.3f, 0.8f), Quaternion.identity);
                var resetAnchor = resetAnchorGo.transform;

                if (rigMapping != null)
                {
                    if (rigMapping.leftHand != null)
                    {
                        leftHandAnchor.SetParent(rigMapping.leftHand, false);
                        leftHandAnchor.localPosition = Vector3.zero;
                        leftHandAnchor.localRotation = Quaternion.identity;
                    }

                    if (rigMapping.rightHand != null)
                    {
                        rightHandAnchor.SetParent(rigMapping.rightHand, false);
                        rightHandAnchor.localPosition = Vector3.zero;
                        rightHandAnchor.localRotation = Quaternion.identity;
                    }

                    if (rigMapping.ValidateRig(out var report))
                    {
                        Debug.Log("Humanoid rig mapping validated.", rigMapping);
                    }
                    else
                    {
                        Debug.LogWarning($"Humanoid rig mapping incomplete:\n{report}", rigMapping);
                    }
                }

                var hands = player.GetComponent<PlayerHandsController>()
                            ?? player.AddComponent<PlayerHandsController>();
                var inputReader = player.GetComponent<InputReader>()
                                  ?? player.AddComponent<InputReader>();

                Helpers.SetSerializedField(hands, "leftHandAnchor", leftHandAnchor);
                Helpers.SetSerializedField(hands, "rightHandAnchor", rightHandAnchor);
                Helpers.SetSerializedField(hands, "tuningConfig", tuningConfig);
                Helpers.SetSerializedField(hands, "drawDebugGizmos", true);

                var hipsMovement = hips.GetComponent<HipsMovementController>()
                                   ?? hips.gameObject.AddComponent<HipsMovementController>();

                Helpers.SetSerializedField(hipsMovement, "tuningConfig", tuningConfig);
                Helpers.SetSerializedField(hipsMovement, "inputReader", inputReader);

                var driver = player.GetComponent<ClimbingStateMachineDriver>()
                             ?? player.AddComponent<ClimbingStateMachineDriver>();

                Helpers.SetSerializedField(driver, "tuningConfig", tuningConfig);
                Helpers.SetSerializedField(driver, "playerHands", hands);
                Helpers.SetSerializedField(driver, "wallNormalReference", wallNormalReference);
                Helpers.SetSerializedField(driver, "inputReader", inputReader);
                Helpers.SetSerializedField(driver, "holdRegistry", holdRegistry);

                Helpers.SetSerializedField(hipsMovement, "driver", driver);
                Helpers.SetSerializedField(hipsMovement, "wallNormalReference", wallNormalReference);

                var hipsSphere = hips.GetComponent<SphereCollider>();
                if (hipsSphere == null)
                {
                    hipsSphere = hips.gameObject.AddComponent<SphereCollider>();
                    hipsSphere.radius = 0.35f;
                }
                hipsSphere.isTrigger = true;

                var playerRigidbody = player.GetComponent<Rigidbody>()
                                      ?? player.AddComponent<Rigidbody>();
                playerRigidbody.useGravity = false;
                playerRigidbody.isKinematic = true;
                playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

                var fallPhysics = player.GetComponent<FallPhysicsController>()
                                  ?? player.AddComponent<FallPhysicsController>();
                Helpers.SetSerializedField(fallPhysics, "driver", driver);
                Helpers.SetSerializedField(fallPhysics, "playerHands", hands);
                Helpers.SetSerializedField(fallPhysics, "hipsMovement", hipsMovement);
                Helpers.SetSerializedField(fallPhysics, "playerRigidbody", playerRigidbody);
                Helpers.SetSerializedField(fallPhysics, "resetAnchor", resetAnchor);

                var visualOrchestrator = player.GetComponent<ClimberVisualOrchestrator>()
                                         ?? player.AddComponent<ClimberVisualOrchestrator>();

                var animator = HumanoidModule.FindImportedAnimator(bodyVisual);
                if (animator != null)
                {
                    var staleAnimator = bodyVisual.GetComponent<Animator>();
                    if (staleAnimator != null && staleAnimator != animator)
                    {
                        Object.DestroyImmediate(staleAnimator);
                    }
                }
                else
                {
                    animator = bodyVisual.GetComponent<Animator>()
                               ?? bodyVisual.gameObject.AddComponent<Animator>();
                }

                animator.applyRootMotion = true;
                animator.runtimeAnimatorController = AnimationModule.EnsureLocomotionController(variant);

                // GroundRootMotionDriver must live on the same GO as the Animator (OnAnimatorMove)
                var animatorGo = animator.gameObject;
                var rootMotionDriver = animatorGo.GetComponent<GroundRootMotionDriver>();
                if (rootMotionDriver == null)
                {
                    var staleDriver = bodyVisual.GetComponent<GroundRootMotionDriver>();
                    if (staleDriver != null && staleDriver.gameObject != animatorGo)
                    {
                        Object.DestroyImmediate(staleDriver);
                    }
                    rootMotionDriver = animatorGo.AddComponent<GroundRootMotionDriver>();
                }

                // AnimatorIKRelay must live on the same GO as the Animator (OnAnimatorIK)
                var ikRelay = animatorGo.GetComponent<AnimatorIKRelay>()
                              ?? animatorGo.AddComponent<AnimatorIKRelay>();
                Helpers.SetSerializedField(ikRelay, "orchestrator", visualOrchestrator);

                var bodyRenderer = rigMapping != null && rigMapping.torso != null
                    ? rigMapping.torso.GetComponent<Renderer>()
                    : FindPrimaryRenderer(bodyVisual);
                var leftHandRenderer = leftHandAnchor.Find("LeftHandMarker")?.GetComponent<Renderer>();
                var rightHandRenderer = rightHandAnchor.Find("RightHandMarker")?.GetComponent<Renderer>();

                Helpers.SetSerializedField(visualOrchestrator, "driver", driver);
                Helpers.SetSerializedField(visualOrchestrator, "mannequinAnimator", animator);
                Helpers.SetSerializedField(visualOrchestrator, "visualRoot", bodyVisual);
                Helpers.SetSerializedField(visualOrchestrator, "inputReader", inputReader);
                Helpers.SetSerializedField(visualOrchestrator, "wallNormalReference", wallNormalReference);
                Helpers.SetSerializedField(visualOrchestrator, "visualRenderer", bodyRenderer);
                Helpers.SetSerializedField(visualOrchestrator, "leftHandMarker", leftHandRenderer);
                Helpers.SetSerializedField(visualOrchestrator, "rightHandMarker", rightHandRenderer);
                Helpers.SetSerializedField(visualOrchestrator, "holdRegistry", holdRegistry);

                Helpers.SetSerializedField(rootMotionDriver, "mannequinAnimator", animator);
                Helpers.SetSerializedField(rootMotionDriver, "driver", driver);
                Helpers.SetSerializedField(rootMotionDriver, "movementTarget", player.transform);
                Helpers.SetSerializedField(rootMotionDriver, "inputReader", inputReader);
                Helpers.SetSerializedField(rootMotionDriver, "tuningConfig", tuningConfig);
                Helpers.SetSerializedField(rootMotionDriver, "wallNormalReference", wallNormalReference);

                bodyVisual.localPosition = Vector3.zero;

                return player;
            }

            private static void EnsureHandMarker(Transform anchor, string markerName, Color color)
            {
                if (anchor == null) return;

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

            private static Renderer FindPrimaryRenderer(Transform root)
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                return renderers.Length > 0 ? renderers[0] : null;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  HumanoidModule — FBX import, Avatar, RigMapping
        // ═════════════════════════════════════════════════════════════════
        private static class HumanoidModule
        {
            /// <summary>
            /// Configures Mixamo FBX importers before scene manipulation:
            /// T-pose as Humanoid (CreateFromThisModel), clip FBXs as
            /// Humanoid (CopyFromOther) with loop + bake root Y.
            /// </summary>
            public static void EnsureMixamoImports(HumanoidVariant variant)
            {
                var tposePath = variant == HumanoidVariant.Female
                    ? PreferredFemaleHumanoidPath
                    : PreferredMaleHumanoidPath;
                var animDir = variant == HumanoidVariant.Female
                    ? FemaleAnimationsDir
                    : MaleAnimationsDir;

                EnsureTposeHumanoid(tposePath);

                var tposeAvatar = LoadAvatarFromModel(tposePath);
                if (tposeAvatar == null)
                {
                    Debug.LogError($"Cannot find Avatar on T-pose model {tposePath}. Configure it as Humanoid in Inspector first.");
                    return;
                }

                var clipFiles = Directory.GetFiles(animDir, "*.fbx");
                foreach (var clipFile in clipFiles)
                {
                    var assetPath = clipFile.Replace('\\', '/');
                    if (assetPath == tposePath) continue;

                    EnsureClipHumanoidCopy(assetPath, tposeAvatar);
                    EnsureClipLoopAndRootSettings(assetPath);
                }
            }

            public static Transform EnsureHumanoidMannequin(Transform player, HumanoidVariant variant, out HumanoidRigMapping rigMapping)
            {
                var body = player.Find("BodyVisual");
                if (body == null)
                {
                    var bodyObject = new GameObject("BodyVisual");
                    bodyObject.transform.SetParent(player);
                    bodyObject.transform.localPosition = Vector3.zero;
                    bodyObject.transform.localRotation = Quaternion.identity;
                    body = bodyObject.transform;
                }

                rigMapping = body.GetComponent<HumanoidRigMapping>()
                             ?? body.gameObject.AddComponent<HumanoidRigMapping>();

                Helpers.ClearChildren(body);
                Helpers.PurgePrimitiveComponents(body.gameObject);

                if (!TryLoadImportedHumanoid(body, rigMapping, variant))
                {
                    Debug.LogError("Mixamo FBX not found. Place John.fbx / Elsa.fbx in Assets/BoulderingGym/Art/Characters/.");
                }

                EditorUtility.SetDirty(rigMapping);
                return body;
            }

            /// <summary>
            /// Returns the first Animator found on a child of <paramref name="parent"/> (not on parent itself).
            /// Picks up the Animator that the imported FBX brings with its Humanoid Avatar.
            /// </summary>
            public static Animator FindImportedAnimator(Transform parent)
            {
                foreach (var childAnimator in parent.GetComponentsInChildren<Animator>(true))
                {
                    if (childAnimator.transform != parent)
                    {
                        return childAnimator;
                    }
                }
                return null;
            }

            // ── Private helpers ──

            private static bool TryLoadImportedHumanoid(Transform body, HumanoidRigMapping rigMapping, HumanoidVariant variant)
            {
                var humanoidAsset = LoadPreferredHumanoidAsset(variant);
                if (humanoidAsset == null) return false;

                Helpers.ClearChildren(body);

                var importedInstance = PrefabUtility.InstantiatePrefab(humanoidAsset) as GameObject;
                if (importedInstance == null)
                {
                    importedInstance = Object.Instantiate(humanoidAsset);
                }

                importedInstance.name = "ImportedHumanoid";
                importedInstance.transform.SetParent(body, false);
                importedInstance.transform.localPosition = Vector3.zero;
                importedInstance.transform.localRotation = Quaternion.identity;
                importedInstance.transform.localScale = Vector3.one;

                AlignImportedHumanoid(importedInstance.transform);
                rigMapping.AutoAssignFromRoot(importedInstance.transform);
                return true;
            }

            private static GameObject LoadPreferredHumanoidAsset(HumanoidVariant variant)
            {
                var primaryPath = variant == HumanoidVariant.Female
                    ? PreferredFemaleHumanoidPath
                    : PreferredMaleHumanoidPath;
                var fallbackPath = variant == HumanoidVariant.Female
                    ? PreferredMaleHumanoidPath
                    : PreferredFemaleHumanoidPath;

                return AssetDatabase.LoadAssetAtPath<GameObject>(primaryPath)
                       ?? AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPath);
            }

            private static void AlignImportedHumanoid(Transform importedRoot)
            {
                var renderers = importedRoot.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) return;

                var combinedBounds = renderers[0].bounds;
                for (var index = 1; index < renderers.Length; index++)
                {
                    combinedBounds.Encapsulate(renderers[index].bounds);
                }

                var parentPos = importedRoot.parent != null ? importedRoot.parent.position : Vector3.zero;
                var offset = new Vector3(
                    -(combinedBounds.center.x - parentPos.x),
                    -(combinedBounds.min.y - parentPos.y),
                    -(combinedBounds.center.z - parentPos.z));
                importedRoot.localPosition += offset;
            }

            private static void EnsureTposeHumanoid(string modelPath)
            {
                var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogError($"T-pose model not found at {modelPath}");
                    return;
                }

                if (importer.animationType == ModelImporterAnimationType.Human
                    && importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    return;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                Debug.Log($"Configured {modelPath} as Humanoid (Create From This Model).");
            }

            private static Avatar LoadAvatarFromModel(string modelPath)
            {
                var objects = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                foreach (var obj in objects)
                {
                    if (obj is Avatar avatar) return avatar;
                }
                return null;
            }

            private static void EnsureClipHumanoidCopy(string clipPath, Avatar sourceAvatar)
            {
                var importer = AssetImporter.GetAtPath(clipPath) as ModelImporter;
                if (importer == null) return;

                var needsReimport = false;

                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    needsReimport = true;
                }

                if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    needsReimport = true;
                }

                if (importer.sourceAvatar != sourceAvatar)
                {
                    importer.sourceAvatar = sourceAvatar;
                    needsReimport = true;
                }

                if (needsReimport)
                {
                    importer.SaveAndReimport();
                    Debug.Log($"Configured {clipPath} as Humanoid (Copy Avatar from {sourceAvatar.name}).");
                }
            }

            private static void EnsureClipLoopAndRootSettings(string clipPath)
            {
                var importer = AssetImporter.GetAtPath(clipPath) as ModelImporter;
                if (importer == null) return;

                var clips = importer.clipAnimations;
                if (clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                var changed = false;
                for (var i = 0; i < clips.Length; i++)
                {
                    if (!clips[i].loopTime)
                    {
                        clips[i].loopTime = true;
                        changed = true;
                    }

                    if (!clips[i].lockRootHeightY)
                    {
                        clips[i].lockRootHeightY = true;
                        clips[i].keepOriginalPositionY = true;
                        changed = true;
                    }
                }

                if (changed)
                {
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    Debug.Log($"Set Loop Time + Bake Root Y on clips in {clipPath}.");
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  AnimationModule — AnimatorController, BlendTree, clips
        // ═════════════════════════════════════════════════════════════════
        private static class AnimationModule
        {
            public static RuntimeAnimatorController EnsureLocomotionController(HumanoidVariant variant)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(LocomotionControllerPath);
                if (controller == null)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPath(LocomotionControllerPath);
                }

                EnsureAnimatorParameter(controller, "movex", AnimatorControllerParameterType.Float);
                EnsureAnimatorParameter(controller, "movez", AnimatorControllerParameterType.Float);
                EnsureAnimatorParameter(controller, "speed", AnimatorControllerParameterType.Float);
                EnsureAnimatorParameter(controller, "is_grounded", AnimatorControllerParameterType.Bool);
                EnsureAnimatorParameter(controller, "ClimbingState", AnimatorControllerParameterType.Int);

                var stateMachine = controller.layers[0].stateMachine;
                var locomotionState = FindOrCreateState(stateMachine, "Locomotion");
                locomotionState.iKOnFeet = true;
                var blendTree = EnsureFreeformCartesianBlendTree(controller, locomotionState);

                // Idempotency guard: only configure children on first run to prevent GUID corruption
                if (blendTree.children == null || blendTree.children.Length == 0)
                {
                    ConfigureLocomotionChildren(blendTree, variant);
                }

                // Enable IK Pass so OnAnimatorIK fires (Humanoid foot IK grounding)
                var layers = controller.layers;
                if (layers.Length > 0 && !layers[0].iKPass)
                {
                    layers[0].iKPass = true;
                    controller.layers = layers;
                }

                return controller;
            }

            // ── Private helpers ──

            private static void EnsureAnimatorParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
            {
                foreach (var parameter in controller.parameters)
                {
                    if (parameter.name == name) return;
                }
                controller.AddParameter(name, type);
            }

            private static AnimatorState FindOrCreateState(AnimatorStateMachine stateMachine, string stateName)
            {
                foreach (var child in stateMachine.states)
                {
                    if (child.state != null && child.state.name == stateName)
                    {
                        return child.state;
                    }
                }

                var state = stateMachine.AddState(stateName);
                if (stateMachine.defaultState == null)
                {
                    stateMachine.defaultState = state;
                }
                return state;
            }

            private static BlendTree EnsureFreeformCartesianBlendTree(AnimatorController controller, AnimatorState state)
            {
                if (state.motion is BlendTree existingTree)
                {
                    existingTree.blendType = BlendTreeType.FreeformCartesian2D;
                    existingTree.blendParameter = "movex";
                    existingTree.blendParameterY = "movez";
                    existingTree.useAutomaticThresholds = false;
                    return existingTree;
                }

                var blendTree = new BlendTree
                {
                    name = "Locomotion2DFreeform",
                    blendType = BlendTreeType.FreeformCartesian2D,
                    blendParameter = "movex",
                    blendParameterY = "movez",
                    useAutomaticThresholds = false,
                };

                AssetDatabase.AddObjectToAsset(blendTree, controller);
                state.motion = blendTree;
                return blendTree;
            }

            private static void ConfigureLocomotionChildren(BlendTree blendTree, HumanoidVariant variant)
            {
                var animDir = variant == HumanoidVariant.Female
                    ? FemaleAnimationsDir
                    : MaleAnimationsDir;

                var idle = LoadMixamoClip(animDir, "idle.fbx");
                var walkForward = LoadMixamoClip(animDir, "walking.fbx");
                var runForward = LoadMixamoClip(animDir, "standard run.fbx");
                var walkLeft = LoadMixamoClip(animDir, "left strafe walking.fbx");
                var walkRight = LoadMixamoClip(animDir, "right strafe walking.fbx");
                var strafeLeft = LoadMixamoClip(animDir, "left strafe.fbx");
                var strafeRight = LoadMixamoClip(animDir, "right strafe.fbx");

                walkForward ??= idle;
                runForward ??= walkForward;
                walkLeft ??= walkForward;
                walkRight ??= walkForward;
                strafeLeft ??= walkLeft;
                strafeRight ??= walkRight;
                idle ??= walkForward;

                if (idle == null)
                {
                    Debug.LogError($"No animation clips found in {animDir}. Cannot build blend tree.");
                    return;
                }

                blendTree.children = new[]
                {
                    CreateChild(idle, 0f, 0f),
                    CreateChild(walkForward, 0f, 1f),
                    CreateChild(walkLeft, -1f, 0f),
                    CreateChild(walkRight, 1f, 0f),
                    CreateChild(runForward, 0f, 2f),
                    CreateChild(strafeLeft, -2f, 0f),
                    CreateChild(strafeRight, 2f, 0f),
                };
            }

            private static ChildMotion CreateChild(Motion motion, float x, float y)
            {
                return new ChildMotion
                {
                    motion = motion,
                    position = new Vector2(x, y),
                    timeScale = 1f,
                };
            }

            private static AnimationClip LoadMixamoClip(string animDir, string fileName)
            {
                var path = $"{animDir}/{fileName}";
                var objects = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in objects)
                {
                    if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        return clip;
                    }
                }

                Debug.LogWarning($"Animation clip not found at {path}");
                return null;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  GymModule — walls, frames, volumes, crash pads, floor
        // ═════════════════════════════════════════════════════════════════
        private static class GymModule
        {
            // ── Gym surface colors ──
            private static readonly Color WallPanelGrey = new(0.72f, 0.72f, 0.7f, 1f);
            private static readonly Color WallPanelBeige = new(0.85f, 0.82f, 0.75f, 1f);
            private static readonly Color CrashPadColor = new(0.18f, 0.35f, 0.68f, 1f);
            private static readonly Color FrameColor = new(0.3f, 0.3f, 0.32f, 1f);
            private static readonly Color VolumeColor = new(0.6f, 0.6f, 0.58f, 1f);
            private static readonly Color FloorConcreteColor = new(0.45f, 0.45f, 0.42f, 1f);
            private static readonly Color CeilingColor = new(0.2f, 0.2f, 0.22f, 1f);

            public static void EnsureWalls()
            {
                // Purge legacy objects from older bootstrapper versions
                foreach (var rootObj in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (rootObj.name is "Wall_Flat" or "Wall_Overhang" or "Floor" or "BoulderingGym")
                    {
                        Object.DestroyImmediate(rootObj);
                    }
                }

                var gym = new GameObject("BoulderingGym");

                // ── 6 wall panels ──
                var panelColors = new[] { WallPanelBeige, WallPanelGrey, WallPanelBeige, WallPanelGrey, WallPanelBeige, WallPanelGrey };
                for (var i = 0; i < AllWalls.Length; i++)
                {
                    var w = AllWalls[i];
                    EnsureWallPanel(gym.transform, PanelNames[i],
                        w.center, new Vector3(w.Width, w.height, w.scaleZ),
                        Quaternion.Euler(w.angleX, 0f, 0f), panelColors[i]);
                }

                // ── Side return walls ──
                const float returnDepth = 6f;
                const float ceilingH = 7.5f;
                EnsureWallPanel(gym.transform, "Wall_ReturnLeft",
                    new Vector3(WallSlabDef.xMin - 0.1f, ceilingH / 2f, WallBaseZ - returnDepth / 2f),
                    new Vector3(0.2f, ceilingH, returnDepth),
                    Quaternion.identity, WallPanelGrey);
                EnsureWallPanel(gym.transform, "Wall_ReturnRight",
                    new Vector3(WallOverhang45Def.xMax + 0.1f, ceilingH / 2f, WallBaseZ - returnDepth / 2f),
                    new Vector3(0.2f, ceilingH, returnDepth),
                    Quaternion.identity, WallPanelGrey);

                // ── Back wall ──
                var gymWidth = WallOverhang45Def.xMax - WallSlabDef.xMin + 0.4f;
                var gymCenterX = (WallSlabDef.xMin + WallOverhang45Def.xMax) / 2f;
                EnsureWallPanel(gym.transform, "Wall_Back",
                    new Vector3(gymCenterX, ceilingH / 2f, WallBaseZ - returnDepth),
                    new Vector3(gymWidth, ceilingH, 0.2f),
                    Quaternion.identity, new Color(0.25f, 0.25f, 0.27f, 1f));

                // ── Industrial ceiling ──
                EnsureWallPanel(gym.transform, "Ceiling",
                    new Vector3(gymCenterX, ceilingH, WallBaseZ - returnDepth / 2f),
                    new Vector3(gymWidth, 0.15f, returnDepth + 1f),
                    Quaternion.identity, CeilingColor);

                // ── Metal frame: pillars at each panel seam ──
                var seams = new float[AllWalls.Length + 1];
                seams[0] = AllWalls[0].xMin;
                for (var i = 0; i < AllWalls.Length; i++)
                    seams[i + 1] = AllWalls[i].xMax;

                for (var i = 0; i < seams.Length; i++)
                {
                    EnsureFrameBeam(gym.transform, $"Frame_Pillar_{i}",
                        new Vector3(seams[i], ceilingH / 2f, WallBaseZ + 0.15f),
                        new Vector3(0.15f, ceilingH, 0.15f));
                }

                // ── Top beams ──
                for (var i = 0; i < seams.Length - 1; i++)
                {
                    var midX = (seams[i] + seams[i + 1]) / 2f;
                    var spanW = seams[i + 1] - seams[i] + 0.15f;
                    EnsureFrameBeam(gym.transform, $"Frame_TopBeam_{i}",
                        new Vector3(midX, ceilingH - 0.08f, WallBaseZ + 0.08f),
                        new Vector3(spanW, 0.12f, 0.18f));
                }

                // ── Ceiling cross-braces ──
                for (var i = 0; i < 7; i++)
                {
                    var x = WallSlabDef.xMin + 2f + i * 4.5f;
                    EnsureFrameBeam(gym.transform, $"Frame_CeilingCross_{i}",
                        new Vector3(x, ceilingH - 0.1f, WallBaseZ - returnDepth / 2f),
                        new Vector3(0.06f, 0.06f, returnDepth + 1f));
                }

                // ── Volumes (snapped flush to wall surface) ──
                PlaceVolume(gym.transform, "Volume_TriLeft", -6f, 2.5f,
                    new Vector3(1.0f, 1.0f, 0.7f), Quaternion.Euler(25f, 15f, 45f));
                PlaceVolume(gym.transform, "Volume_PyramidCenter", 0f, 1.8f,
                    new Vector3(1.4f, 0.6f, 0.9f), Quaternion.Euler(10f, -5f, 0f));
                PlaceVolume(gym.transform, "Volume_WedgeRight", 7f, 3.5f,
                    new Vector3(0.9f, 1.2f, 0.6f), Quaternion.Euler(-15f, 30f, 20f));
                PlaceVolume(gym.transform, "Volume_SlabBlock", -11f, 2.0f,
                    new Vector3(0.8f, 0.8f, 0.5f), Quaternion.Euler(8f, -20f, 10f));
                PlaceVolume(gym.transform, "Volume_SteepWedge", 12f, 3.0f,
                    new Vector3(1.1f, 0.7f, 0.6f), Quaternion.Euler(-30f, 10f, 15f));
                PlaceVolume(gym.transform, "Volume_ArchLeft", -2f, 4.0f,
                    new Vector3(0.7f, 0.7f, 0.5f), Quaternion.Euler(20f, -10f, 30f));
                PlaceVolume(gym.transform, "Volume_BigWedgeRight", 16f, 2.0f,
                    new Vector3(1.0f, 0.9f, 0.7f), Quaternion.Euler(-25f, 5f, 10f));

                // ── Crash pads ──
                EnsureCrashPad(gym.transform, "CrashPad_Main",
                    new Vector3(gymCenterX, 0.15f, WallBaseZ - 2f),
                    new Vector3(gymWidth - 0.4f, 0.3f, 8f));
                EnsureCrashPad(gym.transform, "CrashPad_Far",
                    new Vector3(gymCenterX, 0.15f, WallBaseZ - returnDepth + 1f),
                    new Vector3(gymWidth - 0.4f, 0.3f, 4f));

                // ── Concrete floor ──
                EnsureFloor(gym.transform);
            }

            // ── Private geometry helpers ──

            private static void EnsureWallPanel(Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rot, Color color)
            {
                var wall = parent.Find(name)?.gameObject;
                if (wall == null)
                {
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = name;
                    wall.transform.SetParent(parent);
                }

                wall.transform.localPosition = pos;
                wall.transform.localRotation = rot;
                wall.transform.localScale = scale;

                var renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Standard")) { color = color };
                    mat.SetFloat("_Glossiness", 0.15f);
                    renderer.sharedMaterial = mat;
                }
            }

            private static void EnsureFrameBeam(Transform parent, string name, Vector3 pos, Vector3 scale)
            {
                var beam = parent.Find(name)?.gameObject;
                if (beam == null)
                {
                    beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    beam.name = name;
                    beam.transform.SetParent(parent);
                }

                beam.transform.localPosition = pos;
                beam.transform.localRotation = Quaternion.identity;
                beam.transform.localScale = scale;

                var renderer = beam.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Standard"))
                    {
                        color = FrameColor,
                    };
                    mat.SetFloat("_Metallic", 0.8f);
                    mat.SetFloat("_Glossiness", 0.5f);
                    renderer.sharedMaterial = mat;
                }
            }

            private static void EnsureVolume(Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rot)
            {
                var vol = parent.Find(name)?.gameObject;
                if (vol == null)
                {
                    vol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    vol.name = name;
                    vol.transform.SetParent(parent);
                }

                vol.transform.localPosition = pos;
                vol.transform.localRotation = rot;
                vol.transform.localScale = scale;

                var renderer = vol.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Standard")) { color = VolumeColor };
                    mat.SetFloat("_Glossiness", 0.2f);
                    renderer.sharedMaterial = mat;
                }
            }

            private static void PlaceVolume(Transform parent, string name, float x, float y, Vector3 scale, Quaternion rot)
            {
                var wall = GetWallForX(x);
                var zSurface = WallBaseZ + y * Mathf.Tan(wall.angleX * Mathf.Deg2Rad);
                var pos = new Vector3(x, y, zSurface - scale.z / 2f);
                EnsureVolume(parent, name, pos, scale, rot);
            }

            private static void EnsureCrashPad(Transform parent, string name, Vector3 pos, Vector3 scale)
            {
                var pad = parent.Find(name)?.gameObject;
                if (pad == null)
                {
                    pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pad.name = name;
                    pad.transform.SetParent(parent);
                }

                pad.transform.localPosition = pos;
                pad.transform.localRotation = Quaternion.identity;
                pad.transform.localScale = scale;

                var renderer = pad.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Standard")) { color = CrashPadColor };
                    mat.SetFloat("_Glossiness", 0.1f);
                    renderer.sharedMaterial = mat;
                }
            }

            private static void EnsureFloor(Transform parent)
            {
                var floor = parent.Find("Floor")?.gameObject;
                if (floor == null)
                {
                    floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    floor.name = "Floor";
                    floor.transform.SetParent(parent);
                }

                var gymCenterX = (WallSlabDef.xMin + WallOverhang45Def.xMax) / 2f;
                var gymWidth = WallOverhang45Def.xMax - WallSlabDef.xMin + 2f;
                floor.transform.localPosition = new Vector3(gymCenterX, -0.25f, WallBaseZ - 3f);
                floor.transform.localRotation = Quaternion.identity;
                floor.transform.localScale = new Vector3(gymWidth, 0.5f, 12f);

                var renderer = floor.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = FloorConcreteColor };
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  HoldsModule — circuit routes, 3D hold meshes
        // ═════════════════════════════════════════════════════════════════
        private static class HoldsModule
        {
            private enum HoldShape { Jug, Crimp, Sloper, Pinch, Volume }

            // ── Circuit colors (Block'Out style) ──
            private static readonly Color CircuitYellow = new(0.95f, 0.85f, 0.15f, 1f);
            private static readonly Color CircuitGreen = new(0.2f, 0.75f, 0.3f, 1f);
            private static readonly Color CircuitCyan = new(0.1f, 0.75f, 0.8f, 1f);
            private static readonly Color CircuitBlue = new(0.15f, 0.4f, 0.85f, 1f);
            private static readonly Color CircuitPurple = new(0.55f, 0.2f, 0.75f, 1f);
            private static readonly Color CircuitOrange = new(0.95f, 0.55f, 0.1f, 1f);
            private static readonly Color CircuitRed = new(0.85f, 0.15f, 0.15f, 1f);
            private static readonly Color CircuitBlack = new(0.15f, 0.15f, 0.15f, 1f);
            private static readonly Color CircuitWhite = new(0.92f, 0.92f, 0.9f, 1f);

            public static void EnsureHolds()
            {
                var root = GameObject.Find("Holds");
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
                root = new GameObject("Holds");

                // ── Bloc 1: Jaune (dalle, initiation) ──
                PlaceRoute(root.transform, "Yellow", CircuitYellow, new (Vector2 pos, HoldShape shape)[]
                {
                    (new Vector2(-12.5f, 0.7f), HoldShape.Jug),
                    (new Vector2(-11.5f, 1.4f), HoldShape.Jug),
                    (new Vector2(-12.2f, 2.1f), HoldShape.Jug),
                    (new Vector2(-11.0f, 2.8f), HoldShape.Jug),
                    (new Vector2(-11.8f, 3.5f), HoldShape.Jug),
                    (new Vector2(-11.3f, 4.2f), HoldShape.Jug),
                }, WallSlabDef);

                // ── Bloc 2: Vert (vertical gauche, débutant) ──
                PlaceRoute(root.transform, "Green", CircuitGreen, new (Vector2 pos, HoldShape shape)[]
                {
                    (new Vector2(-7.8f, 0.8f), HoldShape.Jug),
                    (new Vector2(-7.0f, 1.5f), HoldShape.Jug),
                    (new Vector2(-8.0f, 2.2f), HoldShape.Jug),
                    (new Vector2(-6.5f, 2.9f), HoldShape.Crimp),
                    (new Vector2(-7.5f, 3.6f), HoldShape.Jug),
                    (new Vector2(-6.8f, 4.3f), HoldShape.Jug),
                }, WallVertLeftDef);

                // ── Bloc 3: Cyan (vertical gauche, débutant+) ──
                PlaceRoute(root.transform, "Cyan", CircuitCyan, new (Vector2 pos, HoldShape shape)[]
                {
                    (new Vector2(-5.5f, 0.9f), HoldShape.Crimp),
                    (new Vector2(-4.8f, 1.7f), HoldShape.Jug),
                    (new Vector2(-5.3f, 2.5f), HoldShape.Sloper),
                    (new Vector2(-4.5f, 3.2f), HoldShape.Jug),
                    (new Vector2(-5.0f, 4.0f), HoldShape.Crimp),
                }, WallVertLeftDef);

                // ── Bloc 4: Bleu (vertical centre, intermédiaire) ──
                PlaceRoute(root.transform, "Blue", CircuitBlue, new (Vector2 pos, HoldShape shape)[]
                {
                    (new Vector2(-2.5f, 0.8f), HoldShape.Crimp),
                    (new Vector2(-1.5f, 1.6f), HoldShape.Pinch),
                    (new Vector2(-2.2f, 2.4f), HoldShape.Crimp),
                    (new Vector2(-0.8f, 3.1f), HoldShape.Sloper),
                    (new Vector2(-1.8f, 3.8f), HoldShape.Crimp),
                    (new Vector2(-0.5f, 4.5f), HoldShape.Jug),
                }, WallVertCenterDef);

                // ── Bloc 5: Violet (vertical centre, intermédiaire+) ──
                PlaceRoute(root.transform, "Purple", CircuitPurple, new (Vector2 pos, HoldShape shape)[]
                {
                    (new Vector2(1.0f, 0.9f), HoldShape.Pinch),
                    (new Vector2(2.0f, 1.7f), HoldShape.Crimp),
                    (new Vector2(0.8f, 2.5f), HoldShape.Sloper),
                    (new Vector2(2.5f, 3.3f), HoldShape.Pinch),
                    (new Vector2(1.5f, 4.1f), HoldShape.Crimp),
                    (new Vector2(3.0f, 4.8f), HoldShape.Jug),
                }, WallVertCenterDef);

                // ── Bloc 6: Orange (dévers 15°, avancé) ──
                PlaceRoute(root.transform, "Orange", CircuitOrange, new (Vector2 pos, HoldShape shape)[]
                {
                    (new Vector2(5.5f, 0.9f), HoldShape.Jug),
                    (new Vector2(6.5f, 1.7f), HoldShape.Jug),
                    (new Vector2(5.8f, 2.5f), HoldShape.Pinch),
                    (new Vector2(7.0f, 3.3f), HoldShape.Crimp),
                    (new Vector2(6.2f, 4.1f), HoldShape.Jug),
                    (new Vector2(7.5f, 4.8f), HoldShape.Jug),
                }, WallOverhang15Def);

                // ── Bloc 7: Rouge (dévers 30°, expert) ──
                PlaceRoute(root.transform, "Red", CircuitRed, new (Vector2 pos, HoldShape shape)[]
                {
                    (new Vector2(10.0f, 1.0f), HoldShape.Jug),
                    (new Vector2(11.0f, 1.8f), HoldShape.Jug),
                    (new Vector2(10.5f, 2.6f), HoldShape.Pinch),
                    (new Vector2(12.0f, 3.4f), HoldShape.Crimp),
                    (new Vector2(11.2f, 4.2f), HoldShape.Jug),
                    (new Vector2(12.5f, 5.0f), HoldShape.Jug),
                }, WallOverhang30Def);

                // ── Bloc 8: Noir (dévers 45°, élite) ──
                PlaceRoute(root.transform, "Black", CircuitBlack, new (Vector2 pos, HoldShape shape)[]
                {
                    (new Vector2(15.0f, 1.0f), HoldShape.Jug),
                    (new Vector2(16.0f, 1.8f), HoldShape.Jug),
                    (new Vector2(15.5f, 2.6f), HoldShape.Jug),
                    (new Vector2(16.5f, 3.5f), HoldShape.Pinch),
                    (new Vector2(15.8f, 4.3f), HoldShape.Jug),
                }, WallOverhang45Def);

                // ── Prises blanches dispersées (pieds, repos, déco) ──
                (Vector2 pos, HoldShape shape)[] scatterHolds =
                {
                    (new Vector2(-10.0f, 0.5f), HoldShape.Jug),
                    (new Vector2(-6.0f, 0.5f), HoldShape.Jug),
                    (new Vector2(-2.0f, 0.5f), HoldShape.Jug),
                    (new Vector2(2.0f, 0.5f), HoldShape.Jug),
                    (new Vector2(6.0f, 0.5f), HoldShape.Jug),
                    (new Vector2(10.0f, 0.5f), HoldShape.Jug),
                    (new Vector2(15.0f, 0.5f), HoldShape.Jug),
                    (new Vector2(-7.0f, 2.0f), HoldShape.Crimp),
                    (new Vector2(3.0f, 2.8f), HoldShape.Sloper),
                    (new Vector2(8.0f, 1.5f), HoldShape.Pinch),
                    (new Vector2(13.0f, 2.0f), HoldShape.Crimp),
                };
                for (var i = 0; i < scatterHolds.Length; i++)
                {
                    var h = scatterHolds[i];
                    var wall = GetWallForX(h.pos.x);
                    var snappedPos = SnapToWallSurface(h.pos, wall, h.shape);
                    EnsureHold3D(root.transform, $"Hold_WhiteScatter_{i + 1:00}", snappedPos, h.shape, CircuitWhite, wall);
                }
            }

            // ── Private helpers ──

            private static void PlaceRoute(Transform parent, string routeName, Color color, (Vector2 pos, HoldShape shape)[] holds, WallDef wall)
            {
                for (var i = 0; i < holds.Length; i++)
                {
                    var holdName = $"Hold_{routeName}_{i + 1:00}";
                    var snappedPos = SnapToWallSurface(holds[i].pos, wall, holds[i].shape);
                    EnsureHold3D(parent, holdName, snappedPos, holds[i].shape, color, wall);
                }
            }

            private static void EnsureHold3D(Transform parent, string name, Vector3 position, HoldShape shape, Color color, WallDef wall)
            {
                var hold = parent.Find(name)?.gameObject;
                if (hold != null)
                {
                    Object.DestroyImmediate(hold);
                }

                hold = new GameObject(name);
                hold.transform.SetParent(parent);
                hold.transform.position = position;

                var spinY = ((name.GetHashCode() & 0x7FFFFFFF) % 31) - 15;
                hold.transform.localRotation = Quaternion.Euler(wall.angleX, spinY, 0f);

                var visual = BuildHoldGeometry(shape);
                visual.transform.SetParent(hold.transform, false);
                visual.transform.localPosition = Vector3.zero;

                var holdMat = new Material(Shader.Find("Standard")) { color = color };
                holdMat.SetFloat("_Glossiness", 0.25f);
                var renderers = visual.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                    r.sharedMaterial = holdMat;

                var collider = hold.GetComponent<BoxCollider>();
                if (collider == null) collider = hold.AddComponent<BoxCollider>();
                var combinedBounds = renderers[0].bounds;
                for (var b = 1; b < renderers.Length; b++)
                    combinedBounds.Encapsulate(renderers[b].bounds);
                collider.center = hold.transform.InverseTransformPoint(combinedBounds.center);
                collider.size = hold.transform.InverseTransformVector(combinedBounds.size);

                var gripPoint = new GameObject("GripPoint");
                gripPoint.transform.SetParent(hold.transform);
                gripPoint.transform.localPosition = GetGripOffset(shape);

                var holdComponent = hold.GetComponent<HoldComponent>();
                if (holdComponent == null) holdComponent = hold.AddComponent<HoldComponent>();
                Helpers.SetSerializedField(holdComponent, "gripPoint", gripPoint.transform);
            }

            private static GameObject BuildHoldGeometry(HoldShape shape)
            {
                switch (shape)
                {
                    case HoldShape.Jug:
                    {
                        var root = new GameObject("Hold_Jug");
                        AddBasePlate(root.transform, 0.18f, 0.08f);

                        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        body.name = "Body";
                        body.transform.SetParent(root.transform);
                        body.transform.localScale = new Vector3(0.22f, 0.10f, 0.14f);
                        body.transform.localPosition = new Vector3(0f, 0f, -0.04f);
                        Object.DestroyImmediate(body.GetComponent<Collider>());

                        var lip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        lip.name = "Lip";
                        lip.transform.SetParent(root.transform);
                        lip.transform.localScale = new Vector3(0.18f, 0.025f, 0.02f);
                        lip.transform.localPosition = new Vector3(0f, 0.04f, -0.10f);
                        Object.DestroyImmediate(lip.GetComponent<Collider>());

                        return root;
                    }
                    case HoldShape.Crimp:
                    {
                        var root = new GameObject("Hold_Crimp");
                        AddBasePlate(root.transform, 0.15f, 0.05f);

                        var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        edge.name = "Edge";
                        edge.transform.SetParent(root.transform);
                        edge.transform.localScale = new Vector3(0.18f, 0.035f, 0.05f);
                        edge.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                        Object.DestroyImmediate(edge.GetComponent<Collider>());

                        var bevel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        bevel.name = "Bevel";
                        bevel.transform.SetParent(root.transform);
                        bevel.transform.localScale = new Vector3(0.14f, 0.025f, 0.04f);
                        bevel.transform.localPosition = new Vector3(0f, 0.012f, -0.035f);
                        Object.DestroyImmediate(bevel.GetComponent<Collider>());

                        return root;
                    }
                    case HoldShape.Sloper:
                    {
                        var root = new GameObject("Hold_Sloper");
                        AddBasePlate(root.transform, 0.24f, 0.22f);

                        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        dome.name = "Dome";
                        dome.transform.SetParent(root.transform);
                        dome.transform.localScale = new Vector3(0.30f, 0.10f, 0.22f);
                        dome.transform.localPosition = new Vector3(0f, 0f, -0.04f);
                        Object.DestroyImmediate(dome.GetComponent<Collider>());

                        return root;
                    }
                    case HoldShape.Pinch:
                    {
                        var root = new GameObject("Hold_Pinch");
                        AddBasePlate(root.transform, 0.10f, 0.06f);

                        var spine = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        spine.name = "Spine";
                        spine.transform.SetParent(root.transform);
                        spine.transform.localScale = new Vector3(0.06f, 0.14f, 0.05f);
                        spine.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                        Object.DestroyImmediate(spine.GetComponent<Collider>());

                        var flareL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        flareL.name = "FlareL";
                        flareL.transform.SetParent(root.transform);
                        flareL.transform.localScale = new Vector3(0.055f, 0.08f, 0.04f);
                        flareL.transform.localPosition = new Vector3(-0.035f, 0f, -0.015f);
                        Object.DestroyImmediate(flareL.GetComponent<Collider>());

                        var flareR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        flareR.name = "FlareR";
                        flareR.transform.SetParent(root.transform);
                        flareR.transform.localScale = new Vector3(0.055f, 0.08f, 0.04f);
                        flareR.transform.localPosition = new Vector3(0.035f, 0f, -0.015f);
                        Object.DestroyImmediate(flareR.GetComponent<Collider>());

                        return root;
                    }
                    case HoldShape.Volume:
                    {
                        var root = new GameObject("Hold_Volume");
                        AddBasePlate(root.transform, 0.40f, 0.20f);

                        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        body.name = "Body";
                        body.transform.SetParent(root.transform);
                        body.transform.localScale = new Vector3(0.50f, 0.25f, 0.25f);
                        body.transform.localPosition = new Vector3(0f, 0f, -0.10f);
                        Object.DestroyImmediate(body.GetComponent<Collider>());

                        return root;
                    }
                    default:
                    {
                        var root = new GameObject("Hold_Default");
                        AddBasePlate(root.transform, 0.12f, 0.08f);

                        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        body.name = "Body";
                        body.transform.SetParent(root.transform);
                        body.transform.localScale = new Vector3(0.15f, 0.08f, 0.08f);
                        body.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                        Object.DestroyImmediate(body.GetComponent<Collider>());

                        return root;
                    }
                }
            }

            private static void AddBasePlate(Transform parent, float width, float height)
            {
                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = "BasePlate";
                plate.transform.SetParent(parent);
                plate.transform.localPosition = new Vector3(0f, 0f, 0.003f);
                plate.transform.localScale = new Vector3(width, height, 0.008f);
                Object.DestroyImmediate(plate.GetComponent<Collider>());
            }

            private static Vector3 GetGripOffset(HoldShape shape)
            {
                return shape switch
                {
                    HoldShape.Jug => new Vector3(0f, 0.03f, -0.08f),
                    HoldShape.Crimp => new Vector3(0f, 0.015f, -0.04f),
                    HoldShape.Sloper => new Vector3(0f, 0.02f, -0.06f),
                    HoldShape.Pinch => new Vector3(0f, 0.05f, -0.02f),
                    HoldShape.Volume => new Vector3(0f, 0.08f, -0.12f),
                    _ => new Vector3(0f, 0.02f, -0.04f),
                };
            }

            private static Vector3 SnapToWallSurface(Vector2 holdXY, WallDef wall, HoldShape shape)
            {
                var thetaRad = wall.angleX * Mathf.Deg2Rad;
                var cosT = Mathf.Cos(thetaRad);
                var sinT = Mathf.Sin(thetaRad);
                var embedOffset = GetHoldEmbedOffset(shape);

                var zSurface = WallBaseZ + holdXY.y * Mathf.Tan(thetaRad);

                return new Vector3(
                    holdXY.x,
                    holdXY.y + embedOffset * sinT,
                    zSurface - embedOffset * cosT
                );
            }

            private static float GetHoldEmbedOffset(HoldShape shape)
            {
                return shape switch
                {
                    HoldShape.Jug => 0.01f,
                    HoldShape.Crimp => 0.005f,
                    HoldShape.Sloper => 0.01f,
                    HoldShape.Pinch => 0.01f,
                    HoldShape.Volume => 0.02f,
                    _ => 0.01f,
                };
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  DebugUiModule — all debug overlays
        // ═════════════════════════════════════════════════════════════════
        private static class DebugUiModule
        {
            public static void Setup(
                ClimbingStateMachineDriver driver,
                TuningConfig tuningConfig,
                CameraFollowController cameraFollowController)
            {
                var debugUi = GameObject.Find("DebugUI");
                if (debugUi == null)
                {
                    debugUi = new GameObject("DebugUI");
                }

                // Sprint 1.1: ClimbingStateOverlay
                var overlay = debugUi.GetComponent<ClimbingStateOverlay>()
                              ?? debugUi.AddComponent<ClimbingStateOverlay>();
                Helpers.SetSerializedField(overlay, "driver", driver);

                // Sprint 1.1: PlaytestTuningOverlay
                var tuningOverlay = debugUi.GetComponent<PlaytestTuningOverlay>()
                                    ?? debugUi.AddComponent<PlaytestTuningOverlay>();
                Helpers.SetSerializedField(tuningOverlay, "tuningConfig", tuningConfig);
                Helpers.SetSerializedField(tuningOverlay, "cameraFollowController", cameraFollowController);

                // Sprint 2.0: PhysicsDebugOverlay
                var physicsOverlay = debugUi.GetComponent<PhysicsDebugOverlay>()
                                     ?? debugUi.AddComponent<PhysicsDebugOverlay>();
                var physicsDriver = Object.FindAnyObjectByType<PhysicsModelDriver>();
                Helpers.SetSerializedField(physicsOverlay, "physicsDriver", physicsDriver);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  PhysicsModelModule — PhysicsModelDriver, WallPanelComponents
        // ═════════════════════════════════════════════════════════════════
        private static class PhysicsModelModule
        {
            public static void Setup(GameObject player, TuningConfig tuningConfig)
            {
                AttachPhysicsModelDriver(player, tuningConfig);
                AttachWallPanelComponents();
            }

            private static void AttachPhysicsModelDriver(GameObject player, TuningConfig tuningConfig)
            {
                var physicsDriver = player.GetComponent<PhysicsModelDriver>()
                                    ?? player.AddComponent<PhysicsModelDriver>();

                var hands = player.GetComponent<PlayerHandsController>();
                var driver = player.GetComponent<ClimbingStateMachineDriver>();
                var wallNormalRef = player.transform.Find("WallNormalReference");
                var rigMapping = player.GetComponentInChildren<HumanoidRigMapping>();

                Helpers.SetSerializedField(physicsDriver, "tuningConfig", tuningConfig);
                Helpers.SetSerializedField(physicsDriver, "playerHands", hands);
                Helpers.SetSerializedField(physicsDriver, "driver", driver);

                if (wallNormalRef != null)
                {
                    Helpers.SetSerializedField(physicsDriver, "wallNormalReference", wallNormalRef);
                }

                if (rigMapping != null)
                {
                    Helpers.SetSerializedField(physicsDriver, "pelvisReference", rigMapping.pelvis);
                    Helpers.SetSerializedField(physicsDriver, "leftFootTarget", rigMapping.leftFoot);
                    Helpers.SetSerializedField(physicsDriver, "rightFootTarget", rigMapping.rightFoot);
                }
            }

            private static void AttachWallPanelComponents()
            {
                var gym = GameObject.Find("BoulderingGym");
                if (gym == null)
                {
                    Debug.LogWarning("PhysicsModelModule: BoulderingGym not found — skipping WallPanelComponent setup.");
                    return;
                }

                for (var i = 0; i < PanelNames.Length; i++)
                {
                    var panelTransform = gym.transform.Find(PanelNames[i]);
                    if (panelTransform == null) continue;

                    var wallPanel = panelTransform.GetComponent<WallPanelComponent>()
                                    ?? panelTransform.gameObject.AddComponent<WallPanelComponent>();

                    wallPanel.wallAngleDegrees = AllWalls[i].angleX;
                    wallPanel.surfaceType = WallSurfaceType.Rough;
                    EditorUtility.SetDirty(wallPanel);
                }
            }
        }
    }
}
