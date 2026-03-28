using System.Collections.Generic;
using Project.Features.Camera.Presentation;
using Project.Features.Climbing.Presentation;
using Project.Features.Locomotion.Presentation;
using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using UnityEngine;

namespace Project.Features.Visual.Presentation
{
    public sealed class ClimberVisualOrchestrator : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachineDriver driver;
        [SerializeField] private Animator mannequinAnimator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private HumanoidRigMapping rigMapping;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Renderer leftHandMarker;
        [SerializeField] private Renderer rightHandMarker;
        [SerializeField] private HoldRegistry holdRegistry;
        [SerializeField] private CameraFollowController cameraFollowController;
        [SerializeField] private GroundLocomotionController groundLocomotionController;
        [SerializeField] private bool useAnimatorDrivenGroundPose = true;

        [Header("Idle Head Look")]
        [SerializeField] private float headLookWeight = 0.6f;
        [SerializeField] private float headLookMaxAngle = 70f;

        private LocomotionAnimatorController _animatorController;
        private ClimberPostureController _postureController;
        private ClimberIKController _ikController;
        private ClimberFeedbackController _feedbackController;

        private readonly Dictionary<Transform, Quaternion> _initialLocalRotations = new();

        private void Awake()
        {
            ResolveReferences();
            EnsureSubControllers();
            CacheInitialLocalRotations();
        }

        private void Update()
        {
            if (driver == null || driver.StateMachine == null)
            {
                return;
            }

            var state = driver.StateMachine.CurrentState.Id;
            var isWalking = groundLocomotionController != null && groundLocomotionController.IsWalking;

            _animatorController.UpdateAnimatorState(state, isWalking);
        }

        private void LateUpdate()
        {
            if (driver == null || driver.StateMachine == null)
            {
                return;
            }

            var state = driver.StateMachine.CurrentState.Id;

            if (state == PlayerStateId.IdleGround && useAnimatorDrivenGroundPose && mannequinAnimator != null)
            {
                // Reset visualRoot to neutral so posture from climbing/falling doesn't persist
                if (visualRoot != null)
                {
                    visualRoot.localRotation = Quaternion.identity;
                }

                _feedbackController.ApplyFeedback(state);
                ApplyIdleHeadLookAt();
                return;
            }

            RestoreInitialLocalRotations();
            _postureController.ApplyPosture(state);
            _ikController.ApplyLimbPose(state);
            _feedbackController.ApplyFeedback(state);
        }

        /// <summary>
        /// Called by <see cref="AnimatorIKRelay"/> on the Animator's GameObject.
        /// </summary>
        public void HandleAnimatorIK(int layerIndex)
        {
            if (mannequinAnimator == null || driver == null || driver.StateMachine == null)
            {
                return;
            }

            var state = driver.StateMachine.CurrentState.Id;
            _ikController.ApplyFootIKFromAnimator(state);
        }

        private void ResolveReferences()
        {
            if (driver == null) { driver = GetComponent<PlayerStateMachineDriver>(); }
            if (visualRoot == null)
            {
                var bodyVisual = transform.Find("BodyVisual");
                visualRoot = bodyVisual != null ? bodyVisual : transform;
            }
            if (rigMapping == null && visualRoot != null) { rigMapping = visualRoot.GetComponent<HumanoidRigMapping>(); }
            if (rigMapping != null && !rigMapping.ValidateRig(out _)) { rigMapping.AutoAssignFromRoot(visualRoot); }
            if (visualRenderer == null && visualRoot != null) { visualRenderer = FindPrimaryRenderer(visualRoot); }
            if (leftHandMarker == null) { leftHandMarker = transform.Find("LeftHandAnchor/LeftHandMarker")?.GetComponent<Renderer>(); }
            if (rightHandMarker == null) { rightHandMarker = transform.Find("RightHandAnchor/RightHandMarker")?.GetComponent<Renderer>(); }
            if (holdRegistry == null) { holdRegistry = FindFirstObjectByType<HoldRegistry>(); }
            if (cameraFollowController == null) { cameraFollowController = FindFirstObjectByType<CameraFollowController>(); }
        }

        private void EnsureSubControllers()
        {
            _animatorController = GetOrAddComponent<LocomotionAnimatorController>();
            _animatorController.Initialize(driver, mannequinAnimator);

            _postureController = GetOrAddComponent<ClimberPostureController>();
            _postureController.Initialize(driver, visualRoot, rigMapping);

            _ikController = GetOrAddComponent<ClimberIKController>();
            _ikController.Initialize(driver, rigMapping, visualRoot, mannequinAnimator, holdRegistry);

            _feedbackController = GetOrAddComponent<ClimberFeedbackController>();
            _feedbackController.Initialize(driver, visualRoot, visualRenderer, leftHandMarker, rightHandMarker);
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private void CacheInitialLocalRotations()
        {
            _initialLocalRotations.Clear();
            if (rigMapping == null)
            {
                return;
            }

            foreach (var bone in EnumerateRigBones())
            {
                if (bone != null && !_initialLocalRotations.ContainsKey(bone))
                {
                    _initialLocalRotations.Add(bone, bone.localRotation);
                }
            }
        }

        private void RestoreInitialLocalRotations()
        {
            foreach (var pair in _initialLocalRotations)
            {
                if (pair.Key != null)
                {
                    pair.Key.localRotation = pair.Value;
                }
            }
        }

        private System.Collections.Generic.IEnumerable<Transform> EnumerateRigBones()
        {
            yield return rigMapping.pelvis;
            yield return rigMapping.torso;
            yield return rigMapping.head;
            yield return rigMapping.leftUpperArm;
            yield return rigMapping.leftForearm;
            yield return rigMapping.leftHand;
            yield return rigMapping.rightUpperArm;
            yield return rigMapping.rightForearm;
            yield return rigMapping.rightHand;
            yield return rigMapping.leftUpperLeg;
            yield return rigMapping.leftLowerLeg;
            yield return rigMapping.leftFoot;
            yield return rigMapping.rightUpperLeg;
            yield return rigMapping.rightLowerLeg;
            yield return rigMapping.rightFoot;
        }

        private void ApplyIdleHeadLookAt()
        {
            if (rigMapping == null || rigMapping.head == null || cameraFollowController == null)
            {
                return;
            }

            var characterYaw = transform.eulerAngles.y;
            var deltaYaw = Mathf.DeltaAngle(characterYaw, cameraFollowController.OrbitYaw);
            var headYaw = Mathf.Clamp(deltaYaw, -headLookMaxAngle, headLookMaxAngle) * headLookWeight;

            rigMapping.head.rotation = Quaternion.AngleAxis(headYaw, Vector3.up) * rigMapping.head.rotation;
        }

        private static Renderer FindPrimaryRenderer(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            return renderers.Length > 0 ? renderers[0] : null;
        }
    }
}
