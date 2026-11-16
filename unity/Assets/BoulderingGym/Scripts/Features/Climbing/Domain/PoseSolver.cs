using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Climbing.Domain
{
    /// <summary>Body orientation while climbing: facing the wall, upright.</summary>
    public readonly struct BodyFrame
    {
        public BodyFrame(Vector3 forward)
        {
            Forward = forward;
            Up = Vector3.up;
            Right = Vector3.Cross(Vector3.up, forward).normalized;
        }

        /// <summary>Horizontal direction from the climber into the wall.</summary>
        public Vector3 Forward { get; }

        public Vector3 Up { get; }

        public Vector3 Right { get; }
    }

    public readonly struct PoseSolution
    {
        public PoseSolution(bool isValid, Vector3 pelvis, BodyFrame frame, ClimberAnatomy anatomy)
        {
            IsValid = isValid;
            Pelvis = pelvis;
            Frame = frame;
            var chest = pelvis + frame.Up * anatomy.TorsoLength;
            LeftShoulder = chest - frame.Right * anatomy.ShoulderHalfWidth;
            RightShoulder = chest + frame.Right * anatomy.ShoulderHalfWidth;
            LeftHip = pelvis - frame.Right * anatomy.HipHalfWidth;
            RightHip = pelvis + frame.Right * anatomy.HipHalfWidth;
        }

        public bool IsValid { get; }

        public Vector3 Pelvis { get; }

        public BodyFrame Frame { get; }

        public Vector3 LeftShoulder { get; }

        public Vector3 RightShoulder { get; }

        public Vector3 LeftHip { get; }

        public Vector3 RightHip { get; }

        public static PoseSolution Invalid => new(false, Vector3.zero, new BodyFrame(Vector3.forward), ClimberAnatomy.Default);
    }

    /// <summary>
    /// Finds the pelvis position closest to a natural climbing posture that keeps every supporting
    /// limb within reach. Each limb defines a ball the pelvis must stay in; the wall, the floor and
    /// posture limits are half-spaces. Dykstra's projection gives the nearest point of their
    /// intersection; a remaining violation means the contacts cannot be held together.
    /// </summary>
    public static class PoseSolver
    {
        private const int MaxIterations = 200;
        private const float ConvergedStepSquared = 1e-10f;
        private const float Tolerance = 0.015f;
        private const float HandWeight = 0.6f;
        private const float FootWeight = 0.4f;
        private const float HangingArmBend = 0.85f;
        private const float StandingLegBend = 0.88f;

        /// <summary>How much the legs decide the pelvis height when hands and feet both hold: arms bend more readily.</summary>
        private const float LegsBias = 0.7f;
        private const float StandingOnFloorBias = 0.95f;

        public static PoseSolution Solve(ContactSet contacts, ClimberAnatomy anatomy)
        {
            var primary = PrimarySurface(contacts);
            if (primary == null)
            {
                return PoseSolution.Invalid;
            }

            var preferred = PreferredPelvis(contacts, anatomy, primary);
            var frame = BuildFrame(primary, preferred.y);
            var constraints = BuildConstraints(contacts, anatomy, frame, preferred.y);
            var pelvis = Project(preferred, constraints);
            var isValid = MaxViolation(pelvis, constraints) <= Tolerance;
            return new PoseSolution(isValid, pelvis, frame, anatomy);
        }

        /// <summary>A hand's wall when a hand holds, otherwise a foot's wall; null when nothing is on a hold.</summary>
        private static IClimbingSurface PrimarySurface(ContactSet contacts)
        {
            foreach (var limb in ContactSet.AllLimbs)
            {
                if (contacts[limb].Kind == ContactKind.Hold && contacts[limb].Surface != null)
                {
                    return contacts[limb].Surface;
                }
            }

            return null;
        }

        private static BodyFrame BuildFrame(IClimbingSurface surface, float height)
        {
            var normal = surface.NormalAt(height);
            var horizontal = new Vector3(normal.x, 0f, normal.z).normalized;
            return new BodyFrame(-horizontal);
        }

        private static Vector3 PreferredPelvis(ContactSet contacts, ClimberAnatomy anatomy, IClimbingSurface surface)
        {
            var handCenter = Center(contacts, true, out var handCount);
            var footCenter = Center(contacts, false, out var footCount);
            var hangingY = handCenter.y - anatomy.TorsoLength - HangingArmBend * anatomy.ArmReach;
            var standingY = footCenter.y + StandingLegBend * anatomy.LegReach;
            var legsLead = contacts.TouchesFloor ? StandingOnFloorBias : LegsBias;
            var y = handCount == 0 ? standingY : footCount == 0 ? hangingY : Mathf.Lerp(hangingY, standingY, legsLead);

            var weightSum = handCount * HandWeight + footCount * FootWeight;
            var horizontal = (handCenter * (handCount * HandWeight) + footCenter * (footCount * FootWeight)) / weightSum;
            var pelvis = new Vector3(horizontal.x, Mathf.Max(y, anatomy.MinPelvisHeight), horizontal.z);

            var outward = -BuildFrame(surface, pelvis.y).Forward;
            return pelvis + outward * (anatomy.PreferredWallClearance - surface.SignedDistance(pelvis));
        }

        private static Vector3 Center(ContactSet contacts, bool hands, out int count)
        {
            var sum = Vector3.zero;
            count = 0;
            foreach (var limb in ContactSet.AllLimbs)
            {
                if (ContactSet.IsHand(limb) != hands || !contacts[limb].IsSupporting)
                {
                    continue;
                }

                sum += contacts[limb].Position;
                count++;
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        private static List<IPelvisConstraint> BuildConstraints(ContactSet contacts, ClimberAnatomy anatomy, BodyFrame frame, float height)
        {
            var constraints = new List<IPelvisConstraint> { new HeightRange(anatomy.MinPelvisHeight, float.MaxValue) };
            var surfaces = new List<IClimbingSurface>();
            foreach (var limb in ContactSet.AllLimbs)
            {
                var contact = contacts[limb];
                if (!contact.IsSupporting)
                {
                    continue;
                }

                AddLimbConstraints(constraints, limb, contact.Position, anatomy, frame);
                if (contact.Surface != null && !surfaces.Contains(contact.Surface))
                {
                    surfaces.Add(contact.Surface);
                }
            }

            foreach (var surface in surfaces)
            {
                constraints.Add(new WallClearance(surface, surface.NormalAt(height), anatomy.MinWallClearance, anatomy.MaxWallClearance));
            }

            return constraints;
        }

        private static void AddLimbConstraints(List<IPelvisConstraint> constraints, LimbId limb, Vector3 contact, ClimberAnatomy anatomy, BodyFrame frame)
        {
            var side = ContactSet.IsLeft(limb) ? -1f : 1f;
            if (ContactSet.IsHand(limb))
            {
                var shoulderOffset = frame.Up * anatomy.TorsoLength + frame.Right * (side * anatomy.ShoulderHalfWidth);
                constraints.Add(new Ball(contact - shoulderOffset, anatomy.ArmReach));
                constraints.Add(new HeightRange(float.MinValue, contact.y + anatomy.MaxHandBelowPelvis));
                return;
            }

            var hipOffset = frame.Right * (side * anatomy.HipHalfWidth);
            constraints.Add(new Ball(contact - hipOffset, anatomy.LegReach));
            constraints.Add(new HeightRange(contact.y - anatomy.MaxFootAbovePelvis, float.MaxValue));
        }

        private static Vector3 Project(Vector3 start, List<IPelvisConstraint> constraints)
        {
            var point = start;
            var increments = new Vector3[constraints.Count];
            for (var iteration = 0; iteration < MaxIterations; iteration++)
            {
                var sweepStart = point;
                for (var i = 0; i < constraints.Count; i++)
                {
                    var shifted = point + increments[i];
                    var projected = constraints[i].Project(shifted);
                    increments[i] = shifted - projected;
                    point = projected;
                }

                if ((point - sweepStart).sqrMagnitude < ConvergedStepSquared)
                {
                    break;
                }
            }

            return point;
        }

        private static float MaxViolation(Vector3 point, List<IPelvisConstraint> constraints)
        {
            var worst = 0f;
            foreach (var constraint in constraints)
            {
                worst = Mathf.Max(worst, constraint.Violation(point));
            }

            return worst;
        }

        private interface IPelvisConstraint
        {
            Vector3 Project(Vector3 point);

            float Violation(Vector3 point);
        }

        private sealed class Ball : IPelvisConstraint
        {
            private readonly Vector3 _center;
            private readonly float _radius;

            public Ball(Vector3 center, float radius)
            {
                _center = center;
                _radius = radius;
            }

            public Vector3 Project(Vector3 point)
            {
                var offset = point - _center;
                var distance = offset.magnitude;
                return distance <= _radius ? point : _center + offset * (_radius / distance);
            }

            public float Violation(Vector3 point) => Mathf.Max(0f, Vector3.Distance(point, _center) - _radius);
        }

        private sealed class HeightRange : IPelvisConstraint
        {
            private readonly float _min;
            private readonly float _max;

            public HeightRange(float min, float max)
            {
                _min = min;
                _max = max;
            }

            public Vector3 Project(Vector3 point) => new(point.x, Mathf.Clamp(point.y, _min, _max), point.z);

            public float Violation(Vector3 point) => Mathf.Max(0f, Mathf.Max(_min - point.y, point.y - _max));
        }

        /// <summary>Slab between two planes parallel to the wall at the body height.</summary>
        private sealed class WallClearance : IPelvisConstraint
        {
            private readonly IClimbingSurface _surface;
            private readonly Vector3 _normal;
            private readonly float _min;
            private readonly float _max;

            public WallClearance(IClimbingSurface surface, Vector3 normal, float min, float max)
            {
                _surface = surface;
                _normal = normal;
                _min = min;
                _max = max;
            }

            public Vector3 Project(Vector3 point)
            {
                var distance = _surface.SignedDistance(point);
                return point + _normal * (Mathf.Clamp(distance, _min, _max) - distance);
            }

            public float Violation(Vector3 point)
            {
                var distance = _surface.SignedDistance(point);
                return Mathf.Max(0f, Mathf.Max(_min - distance, distance - _max));
            }
        }
    }
}
