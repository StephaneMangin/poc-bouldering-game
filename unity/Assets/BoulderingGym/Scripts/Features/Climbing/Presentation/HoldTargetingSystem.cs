using Project.Core.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class HoldTargetingSystem : MonoBehaviour
    {
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private LayerMask holdLayerMask = ~0;

        private HoldComponent _previousTarget;
        private UnityEngine.Camera _mainCamera;

        public HoldComponent TargetedHold { get; private set; }
        public bool IsTargetValid => TargetedHold != null;

        private void Start()
        {
            _mainCamera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (_mainCamera == null)
            {
                _mainCamera = UnityEngine.Camera.main;
                if (_mainCamera == null) return;
            }

            var ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            var magnetRadius = tuningConfig != null ? tuningConfig.cursorMagnetRadius : 0.3f;

            HoldComponent hit = null;
            if (UnityEngine.Physics.SphereCast(ray, magnetRadius, out var hitInfo, 100f, holdLayerMask))
            {
                hit = hitInfo.collider.GetComponentInParent<HoldComponent>();
            }

            if (hit != _previousTarget)
            {
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

            TargetedHold = hit;
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
