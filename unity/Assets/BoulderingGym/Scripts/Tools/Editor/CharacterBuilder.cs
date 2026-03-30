using System.IO;
using Project.Core.Domain;
using Project.Core.Input;
using Project.Features.Camera.Presentation;
using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Presentation;
using Project.Features.Locomotion.Presentation;
using Project.Features.Physics.Domain;
using Project.Features.Physics.Presentation;
using Project.Features.Player.Presentation;
using Project.Features.Resources.Presentation;
using Project.Features.Visual.Presentation;
using Project.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.Tools.Editor
{
    /// <summary>
    /// Instantiates a fully wired Male or Female humanoid character
    /// (Player GO, Mixamo FBX, Animator, all gameplay components).
    /// </summary>
    public static class CharacterBuilder
    {
        private enum HumanoidVariant { Male, Female }

        // ── Asset paths ──
        private const string TuningPath = "Assets/BoulderingGym/Data/Gameplay/Tuning/TuningConfig.asset";
        private const string LocomotionControllerPath = "Assets/BoulderingGym/Data/Gameplay/Animation/HumanoidLocomotion.controller";
        private const string PreferredMaleHumanoidPath = "Assets/BoulderingGym/Art/Characters/John.fbx";
        private const string PreferredFemaleHumanoidPath = "Assets/BoulderingGym/Art/Characters/Elsa.fbx";
        private const string MaleAnimationsDir = "Assets/BoulderingGym/Art/Animations/Male Walk";
        private const string FemaleAnimationsDir = "Assets/BoulderingGym/Art/Animations/Female Walk";

        // ═══════════════════════════════════════════════════════════════
        //  Menu Items
        // ═══════════════════════════════════════════════════════════════

        [MenuItem("Project/Character/Instantiate Male")]
        public static void InstantiateMale() => Instantiate(HumanoidVariant.Male);

        [MenuItem("Project/Character/Instantiate Female")]
        public static void InstantiateFemale() => Instantiate(HumanoidVariant.Female);

        // ═══════════════════════════════════════════════════════════════
        //  Orchestrator
        // ═══════════════════════════════════════════════════════════════

        private static void Instantiate(HumanoidVariant variant)
        {
            EnsureDirectories();
            EnsureMixamoImports(variant);

            var tuning = LoadOrCreateTuningConfig();
            var holdRegistry = EnsureHoldRegistry();
            var player = BuildPlayer(tuning, variant, holdRegistry);
            var camera = EnsureCamera(player.transform);

            WireCameraReferences(player, camera);
            SetupPhysics(player, tuning);
            SetupResources(player, tuning);
            SetupDebugUi(player, tuning, camera);

            holdRegistry.Refresh();
            Selection.activeGameObject = player;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log($"CharacterBuilder: {variant} humanoid instantiated. Save scene with Ctrl+S.");
        }

        // ═══════════════════════════════════════════════════════════════
        //  Player — root GO, components, wiring
        // ═══════════════════════════════════════════════════════════════

        private static GameObject BuildPlayer(TuningConfig tuningConfig, HumanoidVariant variant, HoldRegistry holdRegistry)
        {
            var player = GameObject.Find("Player");
            if (player == null)
                player = new GameObject("Player");

            player.transform.position = new Vector3(0f, 0.3f, 0.8f);

            var hips = EnsureChild(player.transform, "Hips", new Vector3(0f, 1f, 0f));
            var leftHandAnchor = EnsureChild(player.transform, "LeftHandAnchor", new Vector3(-0.35f, 1.45f, 0.65f));
            var rightHandAnchor = EnsureChild(player.transform, "RightHandAnchor", new Vector3(0.35f, 1.45f, 0.65f));
            var leftFootAnchor = EnsureChild(player.transform, "LeftFootAnchor", new Vector3(-0.15f, 0.1f, 0.3f));
            var rightFootAnchor = EnsureChild(player.transform, "RightFootAnchor", new Vector3(0.15f, 0.1f, 0.3f));
            var wallNormalReference = EnsureChild(player.transform, "WallNormalReference", new Vector3(0f, 1.4f, 0f));
            wallNormalReference.forward = Vector3.back;

            var bodyVisual = EnsureHumanoidMannequin(player.transform, variant, out var rigMapping);

            // Remove legacy child ResetAnchor (must NOT be a child — it moves with the player)
            var oldChild = player.transform.Find("ResetAnchor");
            if (oldChild != null)
                Object.DestroyImmediate(oldChild.gameObject);

            // Root-level ResetAnchor at crash-pad surface height
            var resetAnchorGo = GameObject.Find("ResetAnchor") ?? new GameObject("ResetAnchor");
            resetAnchorGo.transform.SetParent(null);
            resetAnchorGo.transform.SetPositionAndRotation(new Vector3(0f, 0.3f, 0.8f), Quaternion.identity);
            var resetAnchor = resetAnchorGo.transform;

            WireRigAnchors(rigMapping, leftHandAnchor, rightHandAnchor, leftFootAnchor, rightFootAnchor);

            var inputReader = player.GetComponent<InputReader>() ?? player.AddComponent<InputReader>();
            var hands = EnsureComponent<PlayerHandsController>(player);

            SetField(hands, "leftHandAnchor", leftHandAnchor);
            SetField(hands, "rightHandAnchor", rightHandAnchor);
            SetField(hands, "leftFootAnchor", leftFootAnchor);
            SetField(hands, "rightFootAnchor", rightFootAnchor);
            SetField(hands, "tuningConfig", tuningConfig);
            SetField(hands, "wallNormalReference", wallNormalReference);
            SetField(hands, "drawDebugGizmos", true);

            var holdTargeting = EnsureComponent<HoldTargetingSystem>(player);
            SetField(holdTargeting, "tuningConfig", tuningConfig);
            SetField(holdTargeting, "inputReader", inputReader);
            var holdsLayerIndex = LayerMask.NameToLayer("Holds");
            if (holdsLayerIndex >= 0)
                SetField(holdTargeting, "holdLayerMask", 1 << holdsLayerIndex);

            var hipsMovement = EnsureComponent<HipsMovementController>(hips.gameObject);
            SetField(hipsMovement, "tuningConfig", tuningConfig);
            SetField(hipsMovement, "inputReader", inputReader);

            var driver = EnsureComponent<PlayerStateMachineDriver>(player);
            SetField(driver, "tuningConfig", tuningConfig);
            SetField(driver, "playerHands", hands);
            SetField(driver, "wallNormalReference", wallNormalReference);
            SetField(driver, "inputReader", inputReader);
            SetField(driver, "holdRegistry", holdRegistry);
            SetField(driver, "holdTargeting", holdTargeting);

            SetField(hipsMovement, "driver", driver);
            SetField(hipsMovement, "wallNormalReference", wallNormalReference);
            SetField(hipsMovement, "pelvisBone", rigMapping.pelvis);

            var hipsSphere = hips.GetComponent<SphereCollider>();
            if (hipsSphere == null)
            {
                hipsSphere = hips.gameObject.AddComponent<SphereCollider>();
                hipsSphere.radius = 0.35f;
            }
            hipsSphere.isTrigger = true;

            // Remove stale Rigidbody BEFORE adding CharacterController (conflict)
            var staleRb = player.GetComponent<Rigidbody>();
            if (staleRb != null)
                Object.DestroyImmediate(staleRb);

            var cc = player.GetComponent<CharacterController>();
            if (cc == null)
                cc = player.AddComponent<CharacterController>();
            cc.height = 1.62f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.81f, 0f);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;

            var fallPhysics = EnsureComponent<FallPhysicsController>(player);
            SetField(fallPhysics, "driver", driver);
            SetField(fallPhysics, "playerHands", hands);
            SetField(fallPhysics, "hipsMovement", hipsMovement);
            SetField(fallPhysics, "characterController", cc);
            SetField(fallPhysics, "resetAnchor", resetAnchor);
            SetField(fallPhysics, "resetBelowY", -0.5f);

            var visualOrchestrator = EnsureComponent<ClimberVisualOrchestrator>(player);

            var animator = FindImportedAnimator(bodyVisual);
            if (animator != null)
            {
                var staleAnimator = bodyVisual.GetComponent<Animator>();
                if (staleAnimator != null && staleAnimator != animator)
                    Object.DestroyImmediate(staleAnimator);
            }
            else
            {
                animator = bodyVisual.GetComponent<Animator>()
                           ?? bodyVisual.gameObject.AddComponent<Animator>();
            }

            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = EnsureLocomotionController(variant);

            // AnimatorIKRelay must live on the same GO as the Animator (OnAnimatorIK)
            var animatorGo = animator.gameObject;
            var ikRelay = EnsureComponent<AnimatorIKRelay>(animatorGo);
            SetField(ikRelay, "orchestrator", visualOrchestrator);

            var bodyRenderer = rigMapping != null && rigMapping.torso != null
                ? rigMapping.torso.GetComponent<Renderer>()
                : FindPrimaryRenderer(bodyVisual);

            SetField(visualOrchestrator, "driver", driver);
            SetField(visualOrchestrator, "mannequinAnimator", animator);
            SetField(visualOrchestrator, "visualRoot", bodyVisual);
            SetField(visualOrchestrator, "visualRenderer", bodyRenderer);
            SetField(visualOrchestrator, "holdRegistry", holdRegistry);

            var groundLocomotion = EnsureComponent<GroundLocomotionController>(player);
            SetField(groundLocomotion, "characterController", cc);
            SetField(groundLocomotion, "inputReader", inputReader);
            SetField(groundLocomotion, "driver", driver);
            SetField(groundLocomotion, "tuningConfig", tuningConfig);
            SetField(visualOrchestrator, "groundLocomotionController", groundLocomotion);

            bodyVisual.localPosition = Vector3.zero;
            return player;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Humanoid FBX — import, mannequin, avatar
        // ═══════════════════════════════════════════════════════════════

        private static void EnsureMixamoImports(HumanoidVariant variant)
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

        private static Transform EnsureHumanoidMannequin(Transform player, HumanoidVariant variant, out HumanoidRigMapping rigMapping)
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

            ClearChildren(body);
            PurgePrimitiveComponents(body.gameObject);

            if (!TryLoadImportedHumanoid(body, rigMapping, variant))
                Debug.LogError("Mixamo FBX not found. Place John.fbx / Elsa.fbx in Assets/BoulderingGym/Art/Characters/.");

            EditorUtility.SetDirty(rigMapping);
            return body;
        }

        private static bool TryLoadImportedHumanoid(Transform body, HumanoidRigMapping rigMapping, HumanoidVariant variant)
        {
            var humanoidAsset = LoadPreferredHumanoidAsset(variant);
            if (humanoidAsset == null) return false;

            ClearChildren(body);

            var importedInstance = PrefabUtility.InstantiatePrefab(humanoidAsset) as GameObject;
            if (importedInstance == null)
                importedInstance = Object.Instantiate(humanoidAsset);

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
            for (var i = 1; i < renderers.Length; i++)
                combinedBounds.Encapsulate(renderers[i].bounds);

            var parentPos = importedRoot.parent != null ? importedRoot.parent.position : Vector3.zero;
            var offset = new Vector3(
                -(combinedBounds.center.x - parentPos.x),
                -(combinedBounds.min.y - parentPos.y),
                -(combinedBounds.center.z - parentPos.z));
            importedRoot.localPosition += offset;
        }

        // ═══════════════════════════════════════════════════════════════
        //  FBX Import — T-pose, avatar copy, clip loop/root settings
        // ═══════════════════════════════════════════════════════════════

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
                return;

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
                clips = importer.defaultClipAnimations;

            var shouldLoop = ShouldClipLoop(clipPath);
            var changed = false;
            for (var i = 0; i < clips.Length; i++)
            {
                if (clips[i].loopTime != shouldLoop)
                {
                    clips[i].loopTime = shouldLoop;
                    changed = true;
                }
                if (!clips[i].lockRootHeightY || clips[i].keepOriginalPositionY || !clips[i].lockRootRotation)
                {
                    clips[i].lockRootHeightY = true;
                    clips[i].keepOriginalPositionY = false;
                    clips[i].lockRootRotation = true;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                Debug.Log($"Set Loop={shouldLoop} + Bake Root Y on clips in {clipPath}.");
            }
        }

        private static bool ShouldClipLoop(string clipPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(clipPath).ToLowerInvariant();
            return fileName is "idle" or "walking";
        }

        // ═══════════════════════════════════════════════════════════════
        //  Animation — AnimatorController, state machine
        // ═══════════════════════════════════════════════════════════════

        private static RuntimeAnimatorController EnsureLocomotionController(HumanoidVariant variant)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(LocomotionControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(LocomotionControllerPath);

            EnsureAnimatorParameter(controller, "is_walking", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(controller, "is_walking_backward", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(controller, "land", AnimatorControllerParameterType.Trigger);
            EnsureAnimatorParameter(controller, "ClimbingState", AnimatorControllerParameterType.Int);
            RemoveAnimatorParameter(controller, "movex");
            RemoveAnimatorParameter(controller, "movez");
            RemoveAnimatorParameter(controller, "speed");
            RemoveAnimatorParameter(controller, "is_grounded");

            var stateMachine = controller.layers[0].stateMachine;
            ConfigureLocomotionStateMachine(stateMachine, controller, variant);

            var layers = controller.layers;
            if (layers.Length > 0 && !layers[0].iKPass)
            {
                layers[0].iKPass = true;
                controller.layers = layers;
            }

            return controller;
        }

        private static void ConfigureLocomotionStateMachine(
            AnimatorStateMachine sm, AnimatorController controller, HumanoidVariant variant)
        {
            foreach (var child in sm.states)
            {
                if (child.state.motion is BlendTree bt)
                    AssetDatabase.RemoveObjectFromAsset(bt);
                sm.RemoveState(child.state);
            }
            sm.anyStateTransitions = System.Array.Empty<AnimatorStateTransition>();

            var animDir = variant == HumanoidVariant.Female ? FemaleAnimationsDir : MaleAnimationsDir;

            var idleClip = LoadMixamoClip(animDir, "idle.fbx");
            var walkClip = LoadMixamoClip(animDir, "walking.fbx");
            var walkBackClip = LoadMixamoClip(animDir, "walking_backward.fbx");
            var landClip = LoadMixamoClip(animDir, "landing.fbx");

            landClip ??= idleClip;
            walkBackClip ??= walkClip;

            if (idleClip == null || walkClip == null)
            {
                Debug.LogError($"Missing idle.fbx or walking.fbx in {animDir}. Cannot build state machine.");
                return;
            }

            var idle = sm.AddState("Idle", new Vector3(0, 0, 0));
            idle.motion = idleClip;
            idle.iKOnFeet = true;

            var walking = sm.AddState("Walking", new Vector3(300, 0, 0));
            walking.motion = walkClip;
            walking.iKOnFeet = true;

            var walkingBackward = sm.AddState("WalkingBackward", new Vector3(300, 150, 0));
            walkingBackward.motion = walkBackClip;
            walkingBackward.iKOnFeet = true;

            var landing = sm.AddState("Landing", new Vector3(0, 150, 0));
            landing.motion = landClip;
            landing.iKOnFeet = true;

            sm.defaultState = idle;

            var tIdleToWalk = idle.AddTransition(walking);
            tIdleToWalk.AddCondition(AnimatorConditionMode.If, 0, "is_walking");
            tIdleToWalk.hasExitTime = false;
            tIdleToWalk.duration = 0.15f;

            var tWalkToIdle = walking.AddTransition(idle);
            tWalkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "is_walking");
            tWalkToIdle.hasExitTime = false;
            tWalkToIdle.duration = 0.15f;

            var tIdleToWalkBack = idle.AddTransition(walkingBackward);
            tIdleToWalkBack.AddCondition(AnimatorConditionMode.If, 0, "is_walking_backward");
            tIdleToWalkBack.hasExitTime = false;
            tIdleToWalkBack.duration = 0.15f;

            var tWalkBackToIdle = walkingBackward.AddTransition(idle);
            tWalkBackToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "is_walking_backward");
            tWalkBackToIdle.hasExitTime = false;
            tWalkBackToIdle.duration = 0.15f;

            var tAnyToLand = sm.AddAnyStateTransition(landing);
            tAnyToLand.AddCondition(AnimatorConditionMode.If, 0, "land");
            tAnyToLand.hasExitTime = false;
            tAnyToLand.duration = 0.1f;

            var tLandToIdle = landing.AddTransition(idle);
            tLandToIdle.hasExitTime = true;
            tLandToIdle.exitTime = 0.9f;
            tLandToIdle.duration = 0.2f;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Camera
        // ═══════════════════════════════════════════════════════════════

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
            var followController = EnsureComponent<CameraFollowController>(camera.gameObject);
            followController.SetTarget(target);
            SetField(followController, "mouseSensitivity", 2f);
        }

        private static void WireCameraReferences(GameObject player, Camera camera)
        {
            var cameraFollow = camera.GetComponent<CameraFollowController>();
            var orchestrator = player.GetComponent<ClimberVisualOrchestrator>();
            if (orchestrator != null && cameraFollow != null)
                SetField(orchestrator, "cameraFollowController", cameraFollow);

            var groundLocomotionCtrl = player.GetComponent<GroundLocomotionController>();
            if (groundLocomotionCtrl != null && cameraFollow != null)
                SetField(groundLocomotionCtrl, "cameraFollowController", cameraFollow);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Physics & Resources
        // ═══════════════════════════════════════════════════════════════

        private static void SetupPhysics(GameObject player, TuningConfig tuningConfig)
        {
            var physicsDriver = EnsureComponent<PhysicsModelDriver>(player);
            var hands = player.GetComponent<PlayerHandsController>();
            var driver = player.GetComponent<PlayerStateMachineDriver>();
            var wallNormalRef = player.transform.Find("WallNormalReference");
            var rigMapping = player.GetComponentInChildren<HumanoidRigMapping>();

            SetField(physicsDriver, "tuningConfig", tuningConfig);
            SetField(physicsDriver, "playerHands", hands);
            SetField(physicsDriver, "driver", driver);

            if (wallNormalRef != null)
                SetField(physicsDriver, "wallNormalReference", wallNormalRef);
            if (rigMapping != null)
            {
                SetField(physicsDriver, "pelvisReference", rigMapping.pelvis);
                SetField(physicsDriver, "leftFootTarget", rigMapping.leftFoot);
                SetField(physicsDriver, "rightFootTarget", rigMapping.rightFoot);
            }

            // Attach WallPanelComponents to gym walls if present
            AttachWallPanelComponents();
        }

        private static void AttachWallPanelComponents()
        {
            var gym = GameObject.Find("BoulderingGym");
            if (gym == null) return;

            var panelNames = new[]
            {
                "Wall_Slab", "Wall_VertLeft", "Wall_VertCenter",
                "Wall_Overhang15", "Wall_Overhang30", "Wall_Overhang45",
            };
            var angles = new[] { 8f, 0f, 0f, -15f, -30f, -45f };

            for (var i = 0; i < panelNames.Length; i++)
            {
                var panelTransform = gym.transform.Find(panelNames[i]);
                if (panelTransform == null) continue;

                var wallPanel = EnsureComponent<WallPanelComponent>(panelTransform.gameObject);
                wallPanel.wallAngleDegrees = angles[i];
                wallPanel.surfaceType = WallSurfaceType.Rough;
                EditorUtility.SetDirty(wallPanel);
            }
        }

        private static void SetupResources(GameObject player, TuningConfig tuningConfig)
        {
            var resourceDriver = EnsureComponent<ResourceSystemDriver>(player);
            SetField(resourceDriver, "tuningConfig", tuningConfig);
            SetField(resourceDriver, "physicsDriver", player.GetComponent<PhysicsModelDriver>());
            SetField(resourceDriver, "playerDriver", player.GetComponent<PlayerStateMachineDriver>());
            SetField(resourceDriver, "playerHands", player.GetComponent<PlayerHandsController>());
            SetField(resourceDriver, "inputReader", player.GetComponent<InputReader>());
        }

        // ═══════════════════════════════════════════════════════════════
        //  Debug UI
        // ═══════════════════════════════════════════════════════════════

        private static void SetupDebugUi(GameObject player, TuningConfig tuningConfig, Camera camera)
        {
            var debugUi = GameObject.Find("DebugUI") ?? new GameObject("DebugUI");
            var driver = player.GetComponent<PlayerStateMachineDriver>();
            var cameraFollow = camera.GetComponent<CameraFollowController>();

            var overlay = EnsureComponent<ClimbingStateOverlay>(debugUi);
            SetField(overlay, "driver", driver);

            var tuningOverlay = EnsureComponent<PlaytestTuningOverlay>(debugUi);
            SetField(tuningOverlay, "tuningConfig", tuningConfig);
            SetField(tuningOverlay, "cameraFollowController", cameraFollow);

            var physicsOverlay = EnsureComponent<PhysicsDebugOverlay>(debugUi);
            SetField(physicsOverlay, "physicsDriver", Object.FindAnyObjectByType<PhysicsModelDriver>());

            var resourceHud = EnsureComponent<ResourceHUD>(debugUi);
            SetField(resourceHud, "resourceDriver", Object.FindAnyObjectByType<ResourceSystemDriver>());

            var limbHud = EnsureComponent<LimbSelectionHUD>(debugUi);
            SetField(limbHud, "driver", driver);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Infrastructure
        // ═══════════════════════════════════════════════════════════════

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory("Assets/BoulderingGym/Data/Gameplay/Tuning");
            Directory.CreateDirectory("Assets/BoulderingGym/Data/Gameplay/Animation");
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

        private static HoldRegistry EnsureHoldRegistry()
        {
            var registryObject = GameObject.Find("HoldRegistry") ?? new GameObject("HoldRegistry");
            return EnsureComponent<HoldRegistry>(registryObject);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════════

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
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

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static void PurgePrimitiveComponents(GameObject go)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) Object.DestroyImmediate(renderer);
            var filter = go.GetComponent<MeshFilter>();
            if (filter != null) Object.DestroyImmediate(filter);
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
        }

        private static void WireRigAnchors(HumanoidRigMapping rigMapping,
            Transform leftHand, Transform rightHand, Transform leftFoot, Transform rightFoot)
        {
            if (rigMapping == null) return;

            if (rigMapping.leftHand != null)
            {
                leftHand.SetParent(rigMapping.leftHand, false);
                leftHand.localPosition = Vector3.zero;
                leftHand.localRotation = Quaternion.identity;
            }
            if (rigMapping.rightHand != null)
            {
                rightHand.SetParent(rigMapping.rightHand, false);
                rightHand.localPosition = Vector3.zero;
                rightHand.localRotation = Quaternion.identity;
            }
            if (rigMapping.leftFoot != null)
            {
                leftFoot.SetParent(rigMapping.leftFoot, false);
                leftFoot.localPosition = Vector3.zero;
                leftFoot.localRotation = Quaternion.identity;
            }
            if (rigMapping.rightFoot != null)
            {
                rightFoot.SetParent(rigMapping.rightFoot, false);
                rightFoot.localPosition = Vector3.zero;
                rightFoot.localRotation = Quaternion.identity;
            }

            if (rigMapping.ValidateRig(out var report))
                Debug.Log("Humanoid rig mapping validated.", rigMapping);
            else
                Debug.LogWarning($"Humanoid rig mapping incomplete:\n{report}", rigMapping);
        }

        private static Animator FindImportedAnimator(Transform parent)
        {
            foreach (var childAnimator in parent.GetComponentsInChildren<Animator>(true))
            {
                if (childAnimator.transform != parent)
                    return childAnimator;
            }
            return null;
        }

        private static Renderer FindPrimaryRenderer(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            return renderers.Length > 0 ? renderers[0] : null;
        }

        private static void EnsureAnimatorParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == name) return;
            }
            controller.AddParameter(name, type);
        }

        private static void RemoveAnimatorParameter(AnimatorController controller, string name)
        {
            var parameters = controller.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                {
                    controller.RemoveParameter(i);
                    return;
                }
            }
        }

        private static AnimationClip LoadMixamoClip(string animDir, string fileName)
        {
            var path = $"{animDir}/{fileName}";
            var objects = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in objects)
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            Debug.LogWarning($"Animation clip not found at {path}");
            return null;
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null) return;
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetField(Object target, string fieldName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null) return;
            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetField(Object target, string fieldName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null) return;
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetField(Object target, string fieldName, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null) return;
            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
