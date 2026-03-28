using System.Collections.Generic;
using Project.Core.Input;
using Project.Core.Utilities;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class ClimberVisualOrchestrator : MonoBehaviour
    {
        [SerializeField] private ClimbingStateMachineDriver driver;
        [SerializeField] private Animator mannequinAnimator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private HumanoidRigMapping rigMapping;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private Transform wallNormalReference;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Renderer leftHandMarker;
        [SerializeField] private Renderer rightHandMarker;
        [SerializeField] private HoldRegistry holdRegistry;
        [SerializeField] private bool useAnimatorDrivenGroundPose = true;

        [Header("Walk Metrics")]
        [SerializeField] private float walkStartSpeed = 0.18f;
        [SerializeField] private float walkMaxSpeed = 2.2f;
        [SerializeField] private float walkCycleFrequency = 1.65f;
        [SerializeField] private float walkBlendResponsiveness = 8f;

        private ClimberAnimatorController _animatorController;
        private ClimberPostureController _postureController;
        private ClimberIKController _ikController;
        private ClimberFeedbackController _feedbackController;

        private readonly Dictionary<Transform, Quaternion> _initialLocalRotations = new();
        private bool _hasMotionSample;
        private bool _hasRootMotionSample;
        private Vector3 _lastPelvisPosition;
        private Vector3 _lastRootPosition;
        private Vector3 _smoothedGroundVelocity;
        private Vector3 _groundTravelDirection = Vector3.forward;
        private float _walkBlend;
        private float _walkCycle;

        private void Awake()
        {
            ResolveReferences();
            EnsureSubControllers();
            CacheInitialLocalRotations();
            InitializeMotionTracking();
        }

        private void LateUpdate()
        {
            if (driver == null || driver.StateMachine == null)
            {
                return;
            }

            var state = driver.StateMachine.CurrentState.Id;
            UpdateGroundWalkMetrics(state);
            _animatorController.UpdateAnimatorState(state, _smoothedGroundVelocity);

            if (state == ClimbingStateId.IdleGround && useAnimatorDrivenGroundPose && mannequinAnimator != null)
            {
                RestoreInitialLocalRotations();
                _feedbackController.ApplyFeedback(state);
                return;
            }

            RestoreInitialLocalRotations();
            _postureController.ApplyPosture(state, _walkBlend, _walkCycle, GetGroundTravelDirection());
            _ikController.ApplyLimbPose(state, _walkBlend, _walkCycle, GetGroundTravelDirection());
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
            if (driver == null) { driver = GetComponent<ClimbingStateMachineDriver>(); }
            if (visualRoot == null)
            {
                var bodyVisual = transform.Find("BodyVisual");
                visualRoot = bodyVisual != null ? bodyVisual : transform;
            }
            if (rigMapping == null && visualRoot != null) { rigMapping = visualRoot.GetComponent<HumanoidRigMapping>(); }
            if (inputReader == null) { inputReader = GetComponent<InputReader>(); }
            if (wallNormalReference == null) { wallNormalReference = transform.Find("WallNormalReference"); }
            if (rigMapping != null && !rigMapping.ValidateRig(out _)) { rigMapping.AutoAssignFromRoot(visualRoot); }
            if (visualRenderer == null && visualRoot != null) { visualRenderer = FindPrimaryRenderer(visualRoot); }
            if (leftHandMarker == null) { leftHandMarker = transform.Find("LeftHandAnchor/LeftHandMarker")?.GetComponent<Renderer>(); }
            if (rightHandMarker == null) { rightHandMarker = transform.Find("RightHandAnchor/RightHandMarker")?.GetComponent<Renderer>(); }
            if (holdRegistry == null) { holdRegistry = FindFirstObjectByType<HoldRegistry>(); }
        }

        private void EnsureSubControllers()
        {
            _animatorController = GetOrAddComponent<ClimberAnimatorController>();
            _animatorController.Initialize(driver, mannequinAnimator, inputReader);

            _postureController = GetOrAddComponent<ClimberPostureController>();
            _postureController.Initialize(driver, visualRoot, rigMapping);

            _ikController = GetOrAddComponent<ClimberIKController>();
            _ikController.Initialize(driver, rigMapping, visualRoot, inputReader, wallNormalReference, mannequinAnimator, holdRegistry);

            _feedbackController = GetOrAddComponent<ClimberFeedbackController>();
            _feedbackController.Initialize(driver, visualRoot, visualRenderer, leftHandMarker, rightHandMarker);
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private void InitializeMotionTracking()
        {
            if (rigMapping != null && rigMapping.pelvis != null)
            {
                _lastPelvisPosition = rigMapping.pelvis.position;
                _hasMotionSample = true;
            }

            _lastRootPosition = transform.position;
            _hasRootMotionSample = true;
        }

        private void UpdateGroundWalkMetrics(ClimbingStateId state)
        {
            if (rigMapping == null || rigMapping.pelvis == null)
            {
                _walkBlend = 0f;
                _smoothedGroundVelocity = Vector3.zero;
                return;
            }

            var pelvisPosition = rigMapping.pelvis.position;
            if (!_hasMotionSample)
            {
                _lastPelvisPosition = pelvisPosition;
                _hasMotionSample = true;
                return;
            }

            var deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            var rawVelocity = (pelvisPosition - _lastPelvisPosition) / deltaTime;
            _lastPelvisPosition = pelvisPosition;

            if (!_hasRootMotionSample)
            {
                _lastRootPosition = transform.position;
                _hasRootMotionSample = true;
            }

            var rootVelocity = (transform.position - _lastRootPosition) / deltaTime;
            _lastRootPosition = transform.position;

            if (state == ClimbingStateId.IdleGround)
            {
                var input = inputReader != null ? inputReader.ReadMove() : Vector2.zero;
                var wallNormal = WallGeometry.GetWallNormal(wallNormalReference);
                var inputDirection = WallGeometry.InputToWorldDirection(input, wallNormal);
                var inputMagnitude = Mathf.Clamp01(input.magnitude);

                var planarVelocity = Vector3.ProjectOnPlane(rootVelocity, Vector3.up);
                if (planarVelocity.sqrMagnitude < 0.0001f)
                {
                    planarVelocity = Vector3.ProjectOnPlane(rawVelocity, Vector3.up);
                }

                var smoothing = 1f - Mathf.Exp(-walkBlendResponsiveness * deltaTime);
                _smoothedGroundVelocity = Vector3.Lerp(_smoothedGroundVelocity, planarVelocity, smoothing);

                var normalizedSpeed = Mathf.InverseLerp(walkStartSpeed, walkMaxSpeed, _smoothedGroundVelocity.magnitude);
                var targetBlend = Mathf.Max(normalizedSpeed, inputMagnitude * 0.95f);
                _walkBlend = Mathf.MoveTowards(_walkBlend, targetBlend, walkBlendResponsiveness * deltaTime);
                _walkCycle = Mathf.Repeat(_walkCycle + (deltaTime * walkCycleFrequency * Mathf.Lerp(0.2f, 1f, _walkBlend)), 1f);

                if (_smoothedGroundVelocity.sqrMagnitude > 0.0004f)
                {
                    _groundTravelDirection = _smoothedGroundVelocity.normalized;
                }
                else if (inputDirection.sqrMagnitude > 0.0001f)
                {
                    _groundTravelDirection = inputDirection;
                }

                return;
            }

            _walkBlend = Mathf.MoveTowards(_walkBlend, 0f, walkBlendResponsiveness * deltaTime);
            _smoothedGroundVelocity = Vector3.Lerp(_smoothedGroundVelocity, Vector3.zero, 1f - Mathf.Exp(-walkBlendResponsiveness * deltaTime));
        }

        private Vector3 GetGroundTravelDirection()
        {
            if (_groundTravelDirection.sqrMagnitude > 0.0001f)
            {
                return _groundTravelDirection.normalized;
            }

            var fallback = Vector3.ProjectOnPlane(visualRoot != null ? visualRoot.forward : transform.forward, Vector3.up);
            return fallback.sqrMagnitude <= 0.0001f ? Vector3.forward : fallback.normalized;
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

        private static Renderer FindPrimaryRenderer(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            return renderers.Length > 0 ? renderers[0] : null;
        }
    }
}
