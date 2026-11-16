using Project.Core.Domain;
using Project.Core.Input;
using UnityEngine;

namespace Project.Features.Camera.Presentation
{
    /// <summary>
    /// Third person camera orbiting the climber's chest: right mouse drag turns it, the wheel zooms,
    /// and it swings behind the climber when a climb starts. It never goes through walls.
    /// </summary>
    public sealed class OrbitCamera : MonoBehaviour
    {
        private const float MinPitch = -15f;
        private const float MaxPitch = 70f;
        private const float DefaultPitch = 12f;
        private const float ZoomStep = 0.6f;
        private const float FollowSharpness = 8f;
        private const float RealignSharpness = 3f;
        private const float CollisionRadius = 0.2f;

        private InputReader _input;
        private TuningConfig _tuning;
        private Transform _target;
        private Vector3 _pivot;
        private Vector3 _lastMouse;
        private float _yaw;
        private float _pitch = DefaultPitch;
        private float _distance;
        private float? _realignYaw;
        private int _collisionMask;

        public void Initialize(InputReader input, TuningConfig tuning, Transform target)
        {
            _input = input;
            _tuning = tuning;
            _target = target;
            _distance = tuning.cameraGroundDistance;
            _collisionMask = UnityEngine.Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Holds");
            _pivot = target.position;
            _lastMouse = input.ReadMousePosition();
        }

        /// <summary>Swings behind the given forward direction at the given distance.</summary>
        public void Frame(Vector3 forward, float distance)
        {
            _realignYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _distance = distance;
        }

        /// <summary>Puts the camera behind a direction at once (spawn, teleport).</summary>
        public void Snap(Vector3 forward)
        {
            _yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _pitch = DefaultPitch;
            _realignYaw = null;
            _pivot = _target.position;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            ReadOrbitAndZoom();
            if (_realignYaw.HasValue)
            {
                _yaw = Mathf.LerpAngle(_yaw, _realignYaw.Value, RealignSharpness * Time.deltaTime);
            }

            _pivot = Vector3.Lerp(_pivot, _target.position + Vector3.up * _tuning.cameraPivotLift, FollowSharpness * Time.deltaTime);
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var desired = _pivot - rotation * Vector3.forward * _distance;
            transform.SetPositionAndRotation(Unblocked(_pivot, desired), rotation);
        }

        private void ReadOrbitAndZoom()
        {
            var mouse = _input.ReadMousePosition();
            if (_input.ReadOrbitHeld())
            {
                var delta = mouse - _lastMouse;
                _yaw += delta.x * _tuning.cameraOrbitDegreesPerPixel;
                _pitch = Mathf.Clamp(_pitch - delta.y * _tuning.cameraOrbitDegreesPerPixel, MinPitch, MaxPitch);
                _realignYaw = null;
            }

            _lastMouse = mouse;
            _distance = Mathf.Clamp(_distance - _input.ReadZoom() * ZoomStep, _tuning.cameraMinDistance, _tuning.cameraMaxDistance);
        }

        private Vector3 Unblocked(Vector3 pivot, Vector3 desired)
        {
            var offset = desired - pivot;
            var distance = offset.magnitude;
            if (distance > 0.001f && UnityEngine.Physics.SphereCast(pivot, CollisionRadius, offset / distance, out var hit, distance, _collisionMask, QueryTriggerInteraction.Ignore))
            {
                return pivot + offset / distance * Mathf.Max(hit.distance, _tuning.cameraMinDistance * 0.5f);
            }

            return desired;
        }
    }
}
