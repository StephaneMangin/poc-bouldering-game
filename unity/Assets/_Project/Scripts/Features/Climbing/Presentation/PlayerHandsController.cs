using Project.Core.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public enum HandSide
    {
        Left,
        Right,
    }

    public sealed class PlayerHandsController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;

        [Header("Runtime")]
        [SerializeField] private TuningConfig tuningConfig;

        private bool _ikEnabled;
        private HoldComponent _leftCurrentHold;
        private HoldComponent _rightCurrentHold;
        private Vector3 _leftRestLocalPosition;
        private Vector3 _rightRestLocalPosition;
        private Quaternion _leftRestLocalRotation;
        private Quaternion _rightRestLocalRotation;

        public Transform LeftHandAnchor => leftHandAnchor;

        public Transform RightHandAnchor => rightHandAnchor;

        public HoldComponent LeftCurrentHold => _leftCurrentHold;

        public HoldComponent RightCurrentHold => _rightCurrentHold;

        public int SupportCount => (_leftCurrentHold != null ? 1 : 0) + (_rightCurrentHold != null ? 1 : 0);

        public bool HasAnySupport => SupportCount > 0;

        public bool IsHolding(HandSide handSide)
        {
            return handSide == HandSide.Left ? _leftCurrentHold != null : _rightCurrentHold != null;
        }

        private void Awake()
        {
            if (leftHandAnchor != null)
            {
                _leftRestLocalPosition = leftHandAnchor.localPosition;
                _leftRestLocalRotation = leftHandAnchor.localRotation;
            }

            if (rightHandAnchor != null)
            {
                _rightRestLocalPosition = rightHandAnchor.localPosition;
                _rightRestLocalRotation = rightHandAnchor.localRotation;
            }
        }

        public bool TryFindBestHoldForLeft(HoldComponent[] candidates, Vector3 wallNormal, out HoldComponent bestHold)
        {
            return TryFindBestHold(leftHandAnchor, candidates, wallNormal, out bestHold);
        }

        public bool TryFindBestHoldForRight(HoldComponent[] candidates, Vector3 wallNormal, out HoldComponent bestHold)
        {
            return TryFindBestHold(rightHandAnchor, candidates, wallNormal, out bestHold);
        }

        public void SetIkEnabled(bool enabled)
        {
            _ikEnabled = enabled;
            // Hook your TwoBoneIKConstraint weight here.
            // Example: leftIk.weight = enabled ? 1f : 0f; rightIk.weight = enabled ? 1f : 0f;
        }

        public bool TryGrab(HandSide handSide, HoldComponent[] candidates, Vector3 wallNormal, out HoldComponent grabbedHold)
        {
            if (IsHolding(handSide))
            {
                grabbedHold = null;
                return false;
            }

            var success = handSide == HandSide.Left
                ? TryFindBestHoldForLeft(candidates, wallNormal, out grabbedHold)
                : TryFindBestHoldForRight(candidates, wallNormal, out grabbedHold);

            if (!success)
            {
                return false;
            }

            if (handSide == HandSide.Left)
            {
                _leftCurrentHold = grabbedHold;
            }
            else
            {
                _rightCurrentHold = grabbedHold;
            }

            grabbedHold.SetFeedback(true);
            SnapHandToHold(handSide, grabbedHold);
            return true;
        }

        public bool Release(HandSide handSide)
        {
            if (handSide == HandSide.Left)
            {
                return ReleaseHold(ref _leftCurrentHold, leftHandAnchor, _leftRestLocalPosition, _leftRestLocalRotation);
            }

            return ReleaseHold(ref _rightCurrentHold, rightHandAnchor, _rightRestLocalPosition, _rightRestLocalRotation);
        }

        public bool ReleaseAll()
        {
            var releasedLeft = Release(HandSide.Left);
            var releasedRight = Release(HandSide.Right);
            return releasedLeft || releasedRight;
        }

        private bool TryFindBestHold(
            Transform hand,
            HoldComponent[] candidates,
            Vector3 wallNormal,
            out HoldComponent bestHold)
        {
            bestHold = null;
            if (hand == null || candidates == null || tuningConfig == null)
            {
                return false;
            }

            var bestDistance = float.MaxValue;
            foreach (var hold in candidates)
            {
                if (hold == null)
                {
                    continue;
                }

                if (hold == _leftCurrentHold || hold == _rightCurrentHold)
                {
                    continue;
                }

                var target = hold.gripPoint != null ? hold.gripPoint.position : hold.transform.position;
                var toHold = target - hand.position;
                var distance = toHold.magnitude;
                if (distance > tuningConfig.grabRange)
                {
                    continue;
                }

                var angle = Vector3.Angle(-wallNormal.normalized, toHold.normalized);
                if (angle > tuningConfig.grabAngleMax)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestHold = hold;
                }
            }

            return bestHold != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos || tuningConfig == null)
            {
                return;
            }

            DrawAnchorGizmo(leftHandAnchor, Color.cyan);
            DrawAnchorGizmo(rightHandAnchor, Color.magenta);
        }

        private void DrawAnchorGizmo(Transform anchor, Color color)
        {
            if (anchor == null)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawWireSphere(anchor.position, tuningConfig.grabRange);
        }

        private void Update()
        {
            if (_ikEnabled)
            {
                // Runtime IK updates are handled by Animation Rigging constraints.
            }
        }

        private void SnapHandToHold(HandSide handSide, HoldComponent hold)
        {
            var anchor = handSide == HandSide.Left ? leftHandAnchor : rightHandAnchor;
            if (anchor == null || hold == null)
            {
                return;
            }

            var gripPoint = hold.gripPoint != null ? hold.gripPoint : hold.transform;
            anchor.position = gripPoint.position;
            anchor.rotation = gripPoint.rotation;
        }

        private static bool ReleaseHold(
            ref HoldComponent hold,
            Transform anchor,
            Vector3 restLocalPosition,
            Quaternion restLocalRotation)
        {
            if (hold == null)
            {
                return false;
            }

            RestoreAnchor(anchor, restLocalPosition, restLocalRotation);
            hold.SetFeedback(false);
            hold = null;
            return true;
        }

        private static void RestoreAnchor(Transform anchor, Vector3 restLocalPosition, Quaternion restLocalRotation)
        {
            if (anchor == null)
            {
                return;
            }

            anchor.localPosition = restLocalPosition;
            anchor.localRotation = restLocalRotation;
        }
    }
}
