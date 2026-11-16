using System;
using Project.Core.Input;
using Project.Features.Gym.Domain;
using Project.Features.Gym.Presentation;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    /// <summary>What the mouse points at: a hold, the floor, or nothing.</summary>
    public readonly struct ClimbTarget
    {
        private ClimbTarget(ProblemHold hold, Vector3 point, bool isFloor)
        {
            Hold = hold;
            Point = point;
            IsFloor = isFloor;
        }

        public ProblemHold Hold { get; }

        public Vector3 Point { get; }

        public bool IsFloor { get; }

        public bool IsValid => Hold != null || IsFloor;

        public static ClimbTarget None => default;

        public static ClimbTarget OnHold(ProblemHold hold) => new(hold, hold.GripPoint, false);

        public static ClimbTarget OnFloor(Vector3 point) => new(null, point, true);
    }

    /// <summary>
    /// Raycasts the mouse into the gym, before the climber reads it in the same frame. Holds hidden
    /// behind a wall are ignored; a small sphere cast forgives near misses. Holds outside the current
    /// problem are reported as hovered but not targeted.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class HoldTargeting : MonoBehaviour
    {
        private const float RayLength = 40f;
        private const float AimRadius = 0.05f;
        private const float BehindWallTolerance = 0.15f;
        private const float FloorNormalY = 0.9f;
        private const float FloorHeight = 0.05f;

        private UnityEngine.Camera _camera;
        private InputReader _input;
        private Predicate<ProblemHold> _isAllowed = _ => true;
        private int _holdsMask;

        public ClimbTarget Current { get; private set; }

        /// <summary>Hold under the cursor even when it does not belong to the current problem.</summary>
        public ProblemHold Hovered { get; private set; }

        public void Initialize(UnityEngine.Camera viewCamera, InputReader input)
        {
            _camera = viewCamera;
            _input = input;
            _holdsMask = LayerMask.GetMask(HoldsView.HoldsLayerName);
        }

        public void SetFilter(Predicate<ProblemHold> isAllowed) => _isAllowed = isAllowed ?? (_ => true);

        private void Update()
        {
            if (_camera == null || _input == null)
            {
                return;
            }

            var ray = _camera.ScreenPointToRay(_input.ReadMousePosition());
            var hasHit = UnityEngine.Physics.Raycast(ray, out var hit, RayLength, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Hovered = HoldUnderCursor(ray, hasHit, hit);
            if (Hovered != null)
            {
                Current = _isAllowed(Hovered) ? ClimbTarget.OnHold(Hovered) : ClimbTarget.None;
                return;
            }

            Current = hasHit && hit.normal.y > FloorNormalY && hit.point.y < FloorHeight ? ClimbTarget.OnFloor(hit.point) : ClimbTarget.None;
        }

        private ProblemHold HoldUnderCursor(Ray ray, bool hasHit, RaycastHit hit)
        {
            if (hasHit && hit.collider.TryGetComponent<HoldTarget>(out var direct))
            {
                return direct.Hold;
            }

            var limit = hasHit ? hit.distance + BehindWallTolerance : RayLength;
            if (UnityEngine.Physics.SphereCast(ray, AimRadius, out var near, limit, _holdsMask, QueryTriggerInteraction.Ignore)
                && near.collider.TryGetComponent<HoldTarget>(out var forgiven))
            {
                return forgiven.Hold;
            }

            return null;
        }
    }
}
