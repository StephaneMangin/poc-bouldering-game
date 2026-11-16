using System;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    /// <summary>
    /// Contacts and displayed pose of the climber on the wall. Validated contact changes make the
    /// moved limbs travel along an arc away from the wall while the pelvis settles to its new place.
    /// </summary>
    public sealed class ClimbingBody
    {
        private const float ArcClearance = 0.15f;

        private readonly ClimberAnatomy _anatomy;
        private readonly Travel[] _travels = new Travel[4];
        private Vector3 _pelvisVelocity;

        private struct Travel
        {
            public Vector3 From;
            public float Elapsed;
            public float Duration;
        }

        public ClimbingBody(ClimberAnatomy anatomy)
        {
            _anatomy = anatomy;
        }

        public ContactSet Contacts { get; private set; } = ContactSet.Empty;

        public PoseSolution Pose { get; private set; }

        public Vector3 DisplayedPelvis { get; private set; }

        public ClimberAnatomy Anatomy => _anatomy;

        public PoseSolution Evaluate(ContactSet candidate) => PoseSolver.Solve(candidate, _anatomy);

        /// <summary>Adopts validated contacts; each changed limb travels from where it is drawn now.</summary>
        public void Apply(ContactSet contacts, PoseSolution pose, Func<LimbId, Vector3> drawnPosition, float travelSeconds)
        {
            foreach (var limb in ContactSet.AllLimbs)
            {
                var before = Contacts[limb];
                var after = contacts[limb];
                if (after.IsSupporting && (before.Kind != after.Kind || before.HoldId != after.HoldId || before.Position != after.Position))
                {
                    _travels[(int)limb] = new Travel { From = drawnPosition(limb), Duration = travelSeconds };
                }
            }

            Contacts = contacts;
            Pose = pose;
        }

        /// <summary>Starts on the wall from a standing body: the pelvis eases from where it stands.</summary>
        public void Begin(Vector3 standingPelvis)
        {
            Contacts = ContactSet.Empty;
            DisplayedPelvis = standingPelvis;
            _pelvisVelocity = Vector3.zero;
            Array.Clear(_travels, 0, _travels.Length);
        }

        public void Tick(float deltaTime, float settleSeconds)
        {
            for (var i = 0; i < _travels.Length; i++)
            {
                _travels[i].Elapsed += deltaTime;
            }

            DisplayedPelvis = Vector3.SmoothDamp(DisplayedPelvis, Pose.Pelvis, ref _pelvisVelocity, settleSeconds);
        }

        /// <summary>Displayed pelvis moved by the game (falling), contacts dropped.</summary>
        public void Release(Vector3 pelvis)
        {
            Contacts = ContactSet.Empty;
            DisplayedPelvis = pelvis;
        }

        public Vector3 LimbPosition(LimbId limb)
        {
            var target = Contacts[limb].Position;
            var travel = _travels[(int)limb];
            if (travel.Duration <= 0f || travel.Elapsed >= travel.Duration)
            {
                return target;
            }

            var t = Mathf.SmoothStep(0f, 1f, travel.Elapsed / travel.Duration);
            var lift = -Pose.Frame.Forward * (ArcClearance * Mathf.Sin(t * Mathf.PI));
            return Vector3.Lerp(travel.From, target, t) + lift;
        }
    }
}
