using Project.Core.Domain;
using Project.Core.Utilities;
using Project.Features.Climbing.Domain;
using UnityEngine;

using Project.Features.Player.Domain;

namespace Project.Features.Climbing.Presentation
{
    public sealed class PlayerHandsController : MonoBehaviour, IPlayerHands
    {
        [Header("References")]
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;
        [SerializeField] private Transform leftFootAnchor;
        [SerializeField] private Transform rightFootAnchor;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;

        [Header("Runtime")]
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private Transform wallNormalReference;

        private HoldComponent _leftCurrentHold;
        private HoldComponent _rightCurrentHold;
        private HoldComponent _leftFootHold;
        private HoldComponent _rightFootHold;
        private Vector3 _leftRestLocalPosition;
        private Vector3 _rightRestLocalPosition;
        private Quaternion _leftRestLocalRotation;
        private Quaternion _rightRestLocalRotation;
        private Vector3 _leftFootRestLocalPosition;
        private Vector3 _rightFootRestLocalPosition;
        private Quaternion _leftFootRestLocalRotation;
        private Quaternion _rightFootRestLocalRotation;

        public Transform LeftHandAnchor => leftHandAnchor;

        public Transform RightHandAnchor => rightHandAnchor;

        public Transform LeftFootAnchor => leftFootAnchor;

        public Transform RightFootAnchor => rightFootAnchor;

        public HoldComponent LeftCurrentHold => _leftCurrentHold;

        public HoldComponent RightCurrentHold => _rightCurrentHold;

        public HoldComponent LeftFootHold => _leftFootHold;

        public HoldComponent RightFootHold => _rightFootHold;

        public int SupportCount =>
            (_leftCurrentHold != null ? 1 : 0)
            + (_rightCurrentHold != null ? 1 : 0)
            + (_leftFootHold != null ? 1 : 0)
            + (_rightFootHold != null ? 1 : 0);

        public bool HasAnySupport => SupportCount > 0;

        public bool IsHolding(HandSide handSide)
        {
            return handSide == HandSide.Left ? _leftCurrentHold != null : _rightCurrentHold != null;
        }

        public bool IsLimbHolding(LimbId limb)
        {
            return limb switch
            {
                LimbId.LeftHand => _leftCurrentHold != null,
                LimbId.RightHand => _rightCurrentHold != null,
                LimbId.LeftFoot => _leftFootHold != null,
                LimbId.RightFoot => _rightFootHold != null,
                _ => false,
            };
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

            if (leftFootAnchor != null)
            {
                _leftFootRestLocalPosition = leftFootAnchor.localPosition;
                _leftFootRestLocalRotation = leftFootAnchor.localRotation;
            }

            if (rightFootAnchor != null)
            {
                _rightFootRestLocalPosition = rightFootAnchor.localPosition;
                _rightFootRestLocalRotation = rightFootAnchor.localRotation;
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
        }

        public bool TryPlaceLimb(LimbId limb, HoldComponent targetHold)
        {
            if (targetHold == null || IsLimbHolding(limb))
            {
                return false;
            }

            var anchor = GetAnchor(limb);
            if (anchor == null)
            {
                return false;
            }

            var gripPos = targetHold.gripPoint != null ? targetHold.gripPoint.position : targetHold.transform.position;

            // Validate reach with body solver: check that placing this limb
            // still allows all other held limbs to reach their holds
            var currentContacts = BuildCurrentContacts();
            var wallNormal = WallGeometry.GetWallNormal(wallNormalReference);
            if (!ClimbingBodySolver.CanPlaceAndStillReachAll(
                    currentContacts, gripPos, IsHand(limb), IsLeft(limb),
                    ClimbingBodySolver.AnatomyParams.Default, wallNormal))
            {
                return false;
            }

            SetLimbHold(limb, targetHold);
            targetHold.SetFeedback(true);
            SnapAnchorToHold(anchor, targetHold);
            return true;
        }

        public bool ReleaseLimb(LimbId limb)
        {
            return limb switch
            {
                LimbId.LeftHand => ReleaseHold(ref _leftCurrentHold, leftHandAnchor, _leftRestLocalPosition, _leftRestLocalRotation),
                LimbId.RightHand => ReleaseHold(ref _rightCurrentHold, rightHandAnchor, _rightRestLocalPosition, _rightRestLocalRotation),
                LimbId.LeftFoot => ReleaseHold(ref _leftFootHold, leftFootAnchor, _leftFootRestLocalPosition, _leftFootRestLocalRotation),
                LimbId.RightFoot => ReleaseHold(ref _rightFootHold, rightFootAnchor, _rightFootRestLocalPosition, _rightFootRestLocalRotation),
                _ => false,
            };
        }

        public bool HasAnyHandSupport => _leftCurrentHold != null || _rightCurrentHold != null;

        /// <summary>
        /// True when at least one hand and one foot from the opposite side hold a grip
        /// (left hand + right foot, or right hand + left foot).
        /// </summary>
        public bool HasCrossLimbSupport =>
            (_leftCurrentHold != null && _rightFootHold != null)
            || (_rightCurrentHold != null && _leftFootHold != null);

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
            var releasedLF = ReleaseHold(ref _leftFootHold, leftFootAnchor, _leftFootRestLocalPosition, _leftFootRestLocalRotation);
            var releasedRF = ReleaseHold(ref _rightFootHold, rightFootAnchor, _rightFootRestLocalPosition, _rightFootRestLocalRotation);
            return releasedLeft || releasedRight || releasedLF || releasedRF;
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

        private Transform GetAnchor(LimbId limb)
        {
            return limb switch
            {
                LimbId.LeftHand => leftHandAnchor,
                LimbId.RightHand => rightHandAnchor,
                LimbId.LeftFoot => leftFootAnchor,
                LimbId.RightFoot => rightFootAnchor,
                _ => null,
            };
        }

        private void SetLimbHold(LimbId limb, HoldComponent hold)
        {
            switch (limb)
            {
                case LimbId.LeftHand: _leftCurrentHold = hold; break;
                case LimbId.RightHand: _rightCurrentHold = hold; break;
                case LimbId.LeftFoot: _leftFootHold = hold; break;
                case LimbId.RightFoot: _rightFootHold = hold; break;
            }
        }

        private static bool IsHand(LimbId limb)
        {
            return limb == LimbId.LeftHand || limb == LimbId.RightHand;
        }

        private static bool IsLeft(LimbId limb)
        {
            return limb == LimbId.LeftHand || limb == LimbId.LeftFoot;
        }

        private ClimbingBodySolver.LimbContacts BuildCurrentContacts()
        {
            return new ClimbingBodySolver.LimbContacts
            {
                LeftHand = GetGripPosition(_leftCurrentHold),
                RightHand = GetGripPosition(_rightCurrentHold),
                LeftFoot = GetGripPosition(_leftFootHold),
                RightFoot = GetGripPosition(_rightFootHold),
            };
        }

        private static Vector3? GetGripPosition(HoldComponent hold)
        {
            if (hold == null) return null;
            return hold.gripPoint != null ? hold.gripPoint.position : hold.transform.position;
        }

        private static void SnapAnchorToHold(Transform anchor, HoldComponent hold)
        {
            if (anchor == null || hold == null)
            {
                return;
            }

            var gripPoint = hold.gripPoint != null ? hold.gripPoint : hold.transform;
            anchor.position = gripPoint.position;
            anchor.rotation = gripPoint.rotation;
        }

        private void SnapHandToHold(HandSide handSide, HoldComponent hold)
        {
            var anchor = handSide == HandSide.Left ? leftHandAnchor : rightHandAnchor;
            SnapAnchorToHold(anchor, hold);
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
