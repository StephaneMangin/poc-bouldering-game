using Project.Core.Input;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class CameraFollowController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private InputReader inputReader;

        [Header("Follow")]
        [SerializeField] private Vector3 followOffset = new(0f, 2.4f, -7.8f);
        [SerializeField] private float followSmoothTime = 0.6f;

        [Header("Look")]
        [SerializeField] private Vector3 lookOffset = new(0f, 1.2f, 0f);
        [SerializeField] private float recenterDuration = 0.2f;

        private Vector3 _velocity;
        private Quaternion _recenterFrom;
        private Quaternion _recenterTo;
        private float _recenterTimer;

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

            var desiredPosition = target.position + followOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, followSmoothTime);

            if (inputReader != null && inputReader.ConsumeCameraRecenterPressed())
            {
                StartRecenter();
            }

            if (_recenterTimer < recenterDuration)
            {
                _recenterTimer += Time.deltaTime;
                var t = Mathf.Clamp01(_recenterTimer / Mathf.Max(0.0001f, recenterDuration));
                transform.rotation = Quaternion.Slerp(_recenterFrom, _recenterTo, t);
            }
            else
            {
                transform.rotation = _recenterTo;
            }
        }

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            SnapToTarget();
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + followOffset;
            _recenterTo = BuildTargetRotation();
            transform.rotation = _recenterTo;
            _recenterTimer = recenterDuration;
        }

        private void StartRecenter()
        {
            _recenterFrom = transform.rotation;
            _recenterTo = BuildTargetRotation();
            _recenterTimer = 0f;
        }

        private Quaternion BuildTargetRotation()
        {
            var lookPoint = target.position + lookOffset;
            var direction = (lookPoint - transform.position).normalized;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }

            return Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
