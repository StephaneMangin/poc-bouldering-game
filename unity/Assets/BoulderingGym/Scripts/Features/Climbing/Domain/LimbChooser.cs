using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Climbing.Domain
{
    /// <summary>
    /// Orders the limbs a click on a target may move: hands for targets above the hips, feet below
    /// (feet only on the floor or on footholds); free limbs first, then the limb on the target side.
    /// The caller keeps the first candidate that gives a valid pose.
    /// </summary>
    public static class LimbChooser
    {
        private const float HandTargetAboveHips = 0.15f;

        public static List<LimbId> Candidates(ContactSet contacts, PoseSolution pose, Vector3 target, bool handsAllowed)
        {
            var hands = Order(contacts, pose, target, LimbId.LeftHand, LimbId.RightHand);
            var feet = Order(contacts, pose, target, LimbId.LeftFoot, LimbId.RightFoot);
            var candidates = new List<LimbId>();
            if (!handsAllowed)
            {
                candidates.AddRange(feet);
                return candidates;
            }

            var handsFirst = target.y > pose.Pelvis.y + HandTargetAboveHips;
            candidates.AddRange(handsFirst ? hands : feet);
            candidates.AddRange(handsFirst ? feet : hands);
            return candidates;
        }

        private static LimbId[] Order(ContactSet contacts, PoseSolution pose, Vector3 target, LimbId left, LimbId right)
        {
            var leftFree = contacts[left].Kind != ContactKind.Hold;
            var rightFree = contacts[right].Kind != ContactKind.Hold;
            if (leftFree != rightFree)
            {
                return leftFree ? new[] { left, right } : new[] { right, left };
            }

            var targetOnRight = Vector3.Dot(target - pose.Pelvis, pose.Frame.Right) > 0f;
            return targetOnRight ? new[] { right, left } : new[] { left, right };
        }
    }
}
