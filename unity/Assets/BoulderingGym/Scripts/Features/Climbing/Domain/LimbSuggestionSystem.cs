using UnityEngine;

namespace Project.Features.Climbing.Domain
{
    public sealed class LimbSuggestionSystem
    {
        private LimbId _lastPlaced = LimbId.RightHand;

        public LimbId? SuggestLimb(
            Presentation.PlayerHandsController hands,
            Presentation.HoldComponent targetHold)
        {
            if (hands == null || targetHold == null)
            {
                return null;
            }

            var alternate = GetAlternateHand(_lastPlaced);
            if (!hands.IsLimbHolding(alternate))
            {
                return alternate;
            }

            return ClosestFreeLimb(hands, targetHold);
        }

        public void RecordPlacement(LimbId limb)
        {
            _lastPlaced = limb;
        }

        private static LimbId GetAlternateHand(LimbId last)
        {
            return last switch
            {
                LimbId.LeftHand => LimbId.RightHand,
                LimbId.RightHand => LimbId.LeftHand,
                LimbId.LeftFoot => LimbId.RightFoot,
                LimbId.RightFoot => LimbId.LeftFoot,
                _ => LimbId.LeftHand,
            };
        }

        private static LimbId? ClosestFreeLimb(
            Presentation.PlayerHandsController hands,
            Presentation.HoldComponent hold)
        {
            var gripPos = hold.gripPoint != null ? hold.gripPoint.position : hold.transform.position;
            LimbId? best = null;
            var bestDist = float.MaxValue;

            TryCandidate(LimbId.LeftHand, hands.LeftHandAnchor, hands.IsLimbHolding(LimbId.LeftHand), gripPos, ref best, ref bestDist);
            TryCandidate(LimbId.RightHand, hands.RightHandAnchor, hands.IsLimbHolding(LimbId.RightHand), gripPos, ref best, ref bestDist);
            TryCandidate(LimbId.LeftFoot, hands.LeftFootAnchor, hands.IsLimbHolding(LimbId.LeftFoot), gripPos, ref best, ref bestDist);
            TryCandidate(LimbId.RightFoot, hands.RightFootAnchor, hands.IsLimbHolding(LimbId.RightFoot), gripPos, ref best, ref bestDist);

            return best;
        }

        private static void TryCandidate(
            LimbId limb,
            Transform anchor,
            bool isHolding,
            Vector3 gripPos,
            ref LimbId? best,
            ref float bestDist)
        {
            if (anchor == null)
            {
                return;
            }

            var dist = Vector3.Distance(anchor.position, gripPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = limb;
            }
        }
    }
}
