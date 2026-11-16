using Project.Features.Climbing.Domain;
using Project.Features.Gym.Presentation;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    /// <summary>
    /// Shows what the aimed hold means for the climber: a green ring and the limb a click would move,
    /// red when out of reach, grey when the hold belongs to another problem.
    /// </summary>
    public sealed class AimFeedback : MonoBehaviour
    {
        private static readonly Color Reachable = new(0.2f, 1f, 0.35f);
        private static readonly Color Unreachable = new(1f, 0.25f, 0.2f);
        private static readonly Color OtherProblem = new(0.55f, 0.55f, 0.55f);

        private HoldTargeting _targeting;
        private ClimberController _climber;
        private HoldMarkers _markers;

        /// <summary>Short text for the cursor tooltip; null when nothing is aimed.</summary>
        public string Hint { get; private set; }

        public void Initialize(HoldTargeting targeting, ClimberController climber, HoldMarkers markers)
        {
            _targeting = targeting;
            _climber = climber;
            _markers = markers;
        }

        private void LateUpdate()
        {
            var hovered = _targeting.Hovered;
            if (hovered == null)
            {
                _markers.HideTarget();
                Hint = _targeting.Current.IsFloor && _climber.State == ClimberStateId.OnWall ? Describe(_climber.PreviewClick(_targeting.Current)) : null;
                return;
            }

            if (!_targeting.Current.IsValid)
            {
                _markers.ShowTarget(hovered, OtherProblem);
                Hint = "Not part of this problem";
                return;
            }

            var limb = _climber.PreviewClick(_targeting.Current);
            _markers.ShowTarget(hovered, limb.HasValue ? Reachable : Unreachable);
            Hint = Describe(limb);
        }

        private static string Describe(LimbId? limb)
        {
            return limb switch
            {
                LimbId.LeftHand => "Left hand",
                LimbId.RightHand => "Right hand",
                LimbId.LeftFoot => "Left foot",
                LimbId.RightFoot => "Right foot",
                _ => "Out of reach",
            };
        }
    }
}
