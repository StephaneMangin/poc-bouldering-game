using UnityEngine;

namespace Project.Features.Camera.Presentation
{
    public sealed class CameraFollowController : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Follow Behind")]
        [SerializeField] private float followBehindHeight = 1.5f;
        [SerializeField] private float followBehindDistance = 3f;
        [SerializeField] private float mouseSensitivity = 2f;

        [Header("Look")]
        [SerializeField] private Vector3 lookOffset = new(0f, 1.2f, 0f);

        [Header("Zoom (Mouse Wheel)")]
        [SerializeField] private float zoomSpeed = 15f;
        [SerializeField] private float zoomMin = 2f;
        [SerializeField] private float zoomMax = 10f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.2f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private float _zoomDistance;
        private float _orbitYaw;

        public float OrbitYaw => _orbitYaw;

        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = value;
        }

        private void Awake()
        {
            _zoomDistance = new Vector3(0f, followBehindHeight, followBehindDistance).magnitude;

            if (target != null)
            {
                _orbitYaw = target.eulerAngles.y;
                SnapBehind();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleMouseYaw();
            HandleZoomInput();
            PositionCamera();
        }

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            if (target != null)
            {
                _orbitYaw = target.eulerAngles.y;
                SnapBehind();
            }
        }

        private void HandleMouseYaw()
        {
            if (!Input.GetMouseButton(1))
            {
                return;
            }

            var mouseX = Input.GetAxis("Mouse X");
            if (Mathf.Abs(mouseX) > 0.001f)
            {
                _orbitYaw += mouseX * mouseSensitivity;
            }
        }

        private void HandleZoomInput()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f)
            {
                return;
            }

            _zoomDistance = Mathf.Clamp(_zoomDistance - scroll * zoomSpeed, zoomMin, zoomMax);
        }

        private void PositionCamera()
        {
            var pivotPoint = target.position + lookOffset;
            var behindRotation = Quaternion.Euler(0f, _orbitYaw, 0f);
            var desiredOffset = behindRotation * new Vector3(0f, followBehindHeight, -_zoomDistance);
            var desiredPosition = target.position + desiredOffset;

            var direction = desiredPosition - pivotPoint;
            var distance = direction.magnitude;

            if (distance > 0.001f && UnityEngine.Physics.SphereCast(
                    pivotPoint, collisionRadius,
                    direction.normalized, out var hit,
                    distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                var safeDistance = Mathf.Max(hit.distance - collisionRadius, 0f);
                transform.position = pivotPoint + direction.normalized * safeDistance;
            }
            else
            {
                transform.position = desiredPosition;
            }

            transform.rotation = Quaternion.LookRotation(
                (pivotPoint - transform.position).normalized, Vector3.up);
        }

        private void SnapBehind()
        {
            if (target == null)
            {
                return;
            }

            _zoomDistance = new Vector3(0f, followBehindHeight, followBehindDistance).magnitude;
            PositionCamera();
        }
    }
}
