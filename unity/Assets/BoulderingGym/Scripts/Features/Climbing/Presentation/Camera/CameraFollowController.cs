using Project.Core.Input;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class CameraFollowController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private InputReader inputReader;

        [Header("Follow")]
        [SerializeField] private Vector3 followOffset = new(0f, 2.0f, -4.5f);
        [SerializeField] private float followSmoothTime = 0.6f;

        [Header("Zoom (Mouse Wheel)")]
        [SerializeField] private float zoomSpeed = 15f;
        [SerializeField] private float zoomMin = 2f;
        [SerializeField] private float zoomMax = 15f;

        private float _zoomDistance;

        [Header("Look")]
        [SerializeField] private Vector3 lookOffset = new(0f, 1.2f, 0f);
        [SerializeField] private float recenterDuration = 0.2f;

        [Header("Orbit (Right Mouse)")]
        [SerializeField] private float orbitSensitivity = 3f;
        [SerializeField] private float orbitMinPitch = -30f;
        [SerializeField] private float orbitMaxPitch = 60f;

        private Vector3 _velocity;
        private Quaternion _recenterFrom;
        private Quaternion _recenterTo;
        private float _recenterTimer;
        private bool _isRecenterActive;
        private float _orbitYaw;
        private float _orbitPitch;
        private bool _isOrbiting;

        public float FollowSmoothTime
        {
            get => followSmoothTime;
            set => followSmoothTime = Mathf.Clamp(value, 0.02f, 0.8f);
        }

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = FindFirstObjectByType<InputReader>();
            }

            _zoomDistance = followOffset.magnitude;

            if (target != null)
            {
                SnapToTarget();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleOrbitInput();
            HandleZoomInput();

            if (inputReader != null && inputReader.ConsumeCameraRecenterPressed())
            {
                ResetOrbit();
                StartRecenter();
            }

            var pivotPoint = target.position + lookOffset;

            if (_isOrbiting || HasOrbitOffset())
            {
                ApplyOrbitPosition(pivotPoint);
            }
            else
            {
                var direction = followOffset.normalized;
                var desiredPosition = target.position + direction * _zoomDistance;
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, followSmoothTime);
            }

            if (_isRecenterActive)
            {
                _recenterTimer += Time.deltaTime;
                var t = Mathf.Clamp01(_recenterTimer / Mathf.Max(0.0001f, recenterDuration));
                transform.rotation = Quaternion.Slerp(_recenterFrom, _recenterTo, t);
                if (t >= 1f)
                {
                    _isRecenterActive = false;
                }
            }
            else
            {
                transform.rotation = Quaternion.LookRotation((pivotPoint - transform.position).normalized, Vector3.up);
            }
        }

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            SnapToTarget();
        }

        private void HandleOrbitInput()
        {
            var held = UnityEngine.Input.GetMouseButton(1);
            _isOrbiting = held;

            if (!held)
            {
                return;
            }

            _isRecenterActive = false;
            var deltaX = UnityEngine.Input.GetAxis("Mouse X") * orbitSensitivity;
            var deltaY = UnityEngine.Input.GetAxis("Mouse Y") * orbitSensitivity;
            _orbitYaw += deltaX;
            _orbitPitch = Mathf.Clamp(_orbitPitch - deltaY, orbitMinPitch, orbitMaxPitch);
        }

        private void HandleZoomInput()
        {
            var scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f)
            {
                return;
            }

            _zoomDistance = Mathf.Clamp(_zoomDistance - scroll * zoomSpeed, zoomMin, zoomMax);
        }

        private void ApplyOrbitPosition(Vector3 pivotPoint)
        {
            var orbitDistance = _zoomDistance;
            var rotation = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            var direction = rotation * Vector3.back;
            var desiredPosition = pivotPoint + direction * orbitDistance;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, followSmoothTime * 0.3f);
        }

        private bool HasOrbitOffset()
        {
            return Mathf.Abs(_orbitYaw) > 0.1f || Mathf.Abs(_orbitPitch) > 0.1f;
        }

        private void ResetOrbit()
        {
            _orbitYaw = 0f;
            _orbitPitch = 0f;
            _isOrbiting = false;
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            ResetOrbit();
            _zoomDistance = followOffset.magnitude;
            transform.position = target.position + followOffset;
            var lookPoint = target.position + lookOffset;
            transform.rotation = BuildLookRotation(lookPoint);
            _isRecenterActive = false;
        }

        private void StartRecenter()
        {
            _recenterFrom = transform.rotation;
            _recenterTo = BuildLookRotation(target.position + lookOffset);
            _recenterTimer = 0f;
            _isRecenterActive = true;
            _velocity = Vector3.zero;
        }

        private Quaternion BuildLookRotation(Vector3 lookPoint)
        {
            var direction = (lookPoint - transform.position).normalized;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }

            return Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
