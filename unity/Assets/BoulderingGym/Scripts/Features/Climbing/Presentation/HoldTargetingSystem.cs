using Project.Core.Domain;
using Project.Core.Input;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class HoldTargetingSystem : MonoBehaviour
    {
        private const float RaycastMaxDistance = 100f;
        private const float DefaultCursorMagnetRadius = 0.3f;

        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private LayerMask holdLayerMask;

        private HoldComponent _previousTarget;

        public HoldComponent TargetedHold { get; private set; }
        public bool IsTargetValid => TargetedHold != null;

        /// <summary>
        /// Editor/runtime wiring. Prefer this over Inspector-only wiring so dependencies
        /// stay visible to refactoring tools (no string-based reflection in builders).
        /// </summary>
        public void Initialize(TuningConfig config, InputReader input, UnityEngine.Camera camera, LayerMask layerMask)
        {
            tuningConfig = config;
            inputReader = input;
            mainCamera = camera;
            holdLayerMask = layerMask;
        }

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }
        }

        private void Update()
        {
            if (mainCamera == null || inputReader == null)
            {
                return;
            }

            var ray = mainCamera.ScreenPointToRay(inputReader.ReadMousePosition());
            var magnetRadius = tuningConfig != null ? tuningConfig.cursorMagnetRadius : DefaultCursorMagnetRadius;

            HoldComponent hit = null;
            if (UnityEngine.Physics.SphereCast(ray, magnetRadius, out var hitInfo, RaycastMaxDistance, holdLayerMask))
            {
                hit = hitInfo.collider.GetComponentInParent<HoldComponent>();
            }

            UpdateHighlight(hit);
            TargetedHold = hit;
        }

        private void UpdateHighlight(HoldComponent hit)
        {
            if (hit == _previousTarget)
            {
                return;
            }

            if (_previousTarget != null)
            {
                _previousTarget.SetHighlight(false);
            }

            if (hit != null)
            {
                hit.SetHighlight(true);
            }

            _previousTarget = hit;
        }

        private void OnDisable()
        {
            if (_previousTarget != null)
            {
                _previousTarget.SetHighlight(false);
                _previousTarget = null;
            }

            TargetedHold = null;
        }
    }
}
