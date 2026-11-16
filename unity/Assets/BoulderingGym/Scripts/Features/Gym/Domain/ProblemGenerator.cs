using System.Collections.Generic;
using Project.Features.Climbing.Domain;
using UnityEngine;
using Random = System.Random;

namespace Project.Features.Gym.Domain
{
    /// <summary>
    /// Sets boulder problems on the faces of a sector: one problem per lane, a color circuit
    /// weighted by the wall steepness, holds laid out from the start to the top, and every
    /// problem proven climbable by <see cref="ClimbabilityValidator"/> before it is kept.
    /// </summary>
    public sealed class ProblemGenerator
    {
        private const int AttemptsPerProblem = 8;
        private const float AttemptStepShrink = 0.06f;
        private const float EdgeMargin = 0.3f;
        private const float HoldClearance = 0.07f;
        private const float LaneWidth = 0.45f;
        private const int MaxProblemsPerFace = 14;
        private const float MinLaneWidth = 0.6f;
        private const float TopMargin = 0.35f;
        private const float OverhangAngle = 8f;
        private const float SteepAngle = 25f;
        private const float UsedCircuitPenalty = 3f;
        private const int PlacementTries = 6;

        private readonly IReadOnlyList<Circuit> _circuits;
        private readonly ClimbabilityValidator _validator;
        private int _nextHoldId;

        /// <summary>Every problem must be climbable by each of the validation anatomies (smallest and largest climber).</summary>
        public ProblemGenerator(IReadOnlyList<Circuit> circuits, params ClimberAnatomy[] validationAnatomies)
        {
            _circuits = circuits;
            _validator = new ClimbabilityValidator(validationAnatomies);
        }

        public List<BoulderProblem> Generate(GymSector sector, Random random, IReadOnlyDictionary<WallFace, FaceOccupancy> occupancy)
        {
            var problems = new List<BoulderProblem>();
            foreach (var face in sector.Faces)
            {
                var usedOnFace = new List<Circuit>();
                foreach (var lane in LaneCenters(face))
                {
                    var circuit = PickCircuit(face, random, usedOnFace);
                    var problem = TryCreate(face, lane, circuit, random, occupancy[face]);
                    if (problem == null)
                    {
                        continue;
                    }

                    usedOnFace.Add(circuit);
                    problem.Number = problems.Count + 1;
                    problem.Id = $"{sector.Id}-{problem.Number}";
                    problems.Add(problem);
                }
            }

            return problems;
        }

        private static List<float> LaneCenters(WallFace face)
        {
            var lanes = new List<float>();
            var min = Mathf.Max(face.RangeAt(0.5f).Min, Mathf.Max(face.RangeAt(face.Height * 0.5f).Min, face.RangeAt(face.Height - TopMargin).Min));
            var max = Mathf.Min(face.RangeAt(0.5f).Max, Mathf.Min(face.RangeAt(face.Height * 0.5f).Max, face.RangeAt(face.Height - TopMargin).Max));
            var width = max - min - 2f * EdgeMargin;
            if (width < MinLaneWidth)
            {
                return lanes;
            }

            var count = Mathf.Clamp(Mathf.FloorToInt(width / LaneWidth), 1, MaxProblemsPerFace);
            for (var i = 0; i < count; i++)
            {
                lanes.Add(min + EdgeMargin + width * (i + 0.5f) / count);
            }

            return lanes;
        }

        private Circuit PickCircuit(WallFace face, Random random, List<Circuit> usedOnFace)
        {
            var steepness = MaxAngle(face.Profile);
            var weights = new float[_circuits.Count];
            var total = 0f;
            for (var i = 0; i < _circuits.Count; i++)
            {
                var parameters = _circuits[i].Parameters;
                var weight = steepness <= OverhangAngle ? parameters.weightVertical
                    : steepness <= SteepAngle ? parameters.weightOverhang : parameters.weightSteep;
                weights[i] = usedOnFace.Contains(_circuits[i]) ? weight / UsedCircuitPenalty : weight;
                total += weights[i];
            }

            var pick = random.Range(0f, total);
            for (var i = 0; i < weights.Length; i++)
            {
                pick -= weights[i];
                if (pick <= 0f)
                {
                    return _circuits[i];
                }
            }

            return _circuits[_circuits.Count - 1];
        }

        private static float MaxAngle(WallProfile profile)
        {
            var max = float.MinValue;
            foreach (var level in profile.Breakpoints)
            {
                max = Mathf.Max(max, profile.AngleAt(level));
            }

            return max;
        }

        private BoulderProblem TryCreate(WallFace face, float lane, Circuit circuit, Random random, FaceOccupancy occupancy)
        {
            for (var attempt = 0; attempt < AttemptsPerProblem; attempt++)
            {
                var layout = new ProblemLayout(face, lane, circuit, random, occupancy);
                var holds = layout.Build(1f - AttemptStepShrink * attempt);
                if (holds == null)
                {
                    continue;
                }

                AssignIds(holds);
                if (!_validator.CanTop(holds))
                {
                    continue;
                }

                foreach (var hold in holds)
                {
                    occupancy.Add(hold.S, hold.Y, Footprint(hold));
                }

                return new BoulderProblem(face.Sector, face, circuit, holds) { Grade = GradeOf(circuit, face, layout.MeanStepRatio) };
            }

            return null;
        }

        private void AssignIds(List<ProblemHold> holds)
        {
            foreach (var hold in holds)
            {
                hold.Id = _nextHoldId++;
            }
        }

        private static string GradeOf(Circuit circuit, WallFace face, float meanStepRatio)
        {
            var steepness = Mathf.Clamp01(MaxAngle(face.Profile) / 40f);
            var difficulty = Mathf.Clamp01(0.6f * meanStepRatio + 0.4f * steepness);
            var index = Mathf.Min(circuit.Grades.Count - 1, Mathf.FloorToInt(difficulty * circuit.Grades.Count));
            return circuit.Grades[index];
        }

        private static float Footprint(ProblemHold hold)
        {
            var size = hold.Size;
            return Mathf.Max(size.x, size.y) * 0.5f + HoldClearance;
        }

        /// <summary>Holds of one problem attempt: start holds, hand line, top hold, then feet.</summary>
        private sealed class ProblemLayout
        {
            private readonly WallFace _face;
            private readonly float _lane;
            private readonly Circuit _circuit;
            private readonly Random _random;
            private readonly FaceOccupancy _occupancy;
            private readonly List<ProblemHold> _holds = new();
            private float _stepSum;
            private int _stepCount;

            public ProblemLayout(WallFace face, float lane, Circuit circuit, Random random, FaceOccupancy occupancy)
            {
                _face = face;
                _lane = lane;
                _circuit = circuit;
                _random = random;
                _occupancy = occupancy;
            }

            public float MeanStepRatio { get; private set; }

            public List<ProblemHold> Build(float stepScale)
            {
                var parameters = _circuit.Parameters;
                var startY = _random.Range(1.35f, 1.6f);
                if (!PlaceStart(startY) || !PlaceHandLine(startY, stepScale))
                {
                    return null;
                }

                var topKind = _circuit.Rank <= 1 ? HoldKind.Jug : _random.Pick(parameters.handKinds);
                if (Place(topKind, _lane + _random.Range(-0.12f, 0.12f), _face.Height - TopMargin, HoldRole.Top) == null)
                {
                    return null;
                }

                PlaceFeet(startY);
                var stepRange = parameters.moveStepMax - parameters.moveStepMin;
                MeanStepRatio = _stepCount > 0 && stepRange > 0f
                    ? Mathf.Clamp01((_stepSum / _stepCount - parameters.moveStepMin) / stepRange)
                    : 0f;
                return _holds;
            }

            private bool PlaceStart(float startY)
            {
                var parameters = _circuit.Parameters;
                if (_random.NextDouble() < 0.3)
                {
                    var kind = _circuit.Rank <= 1 ? HoldKind.Jug : _random.Pick(parameters.handKinds);
                    return Place(kind, _lane, startY, HoldRole.Start) != null;
                }

                var spread = _random.Range(0.16f, 0.28f);
                return Place(_random.Pick(parameters.handKinds), _lane - spread, startY + _random.Range(-0.08f, 0.08f), HoldRole.Start) != null
                    && Place(_random.Pick(parameters.handKinds), _lane + spread, startY + _random.Range(-0.08f, 0.08f), HoldRole.Start) != null;
            }

            private bool PlaceHandLine(float startY, float stepScale)
            {
                var parameters = _circuit.Parameters;
                var topY = _face.Height - TopMargin;
                var y = startY;
                var side = _random.Sign();
                while (true)
                {
                    var step = _random.Range(parameters.MoveStep) * stepScale;
                    if (y + step > topY - 0.25f)
                    {
                        return true;
                    }

                    y += step;
                    _stepSum += step / stepScale;
                    _stepCount++;
                    var s = _lane + side * _random.Range(parameters.Lateral);
                    side = -side;
                    if (Place(_random.Pick(parameters.handKinds), s, y, HoldRole.Intermediate) == null)
                    {
                        return false;
                    }
                }
            }

            private void PlaceFeet(float startY)
            {
                var parameters = _circuit.Parameters;
                Place(_random.Pick(parameters.footKinds), _lane - _random.Range(0.12f, 0.3f), _random.Range(0.3f, 0.5f), HoldRole.Intermediate);
                Place(_random.Pick(parameters.footKinds), _lane + _random.Range(0.12f, 0.3f), _random.Range(0.3f, 0.5f), HoldRole.Intermediate);
                foreach (var hand in _holds.ToArray())
                {
                    var y = hand.Y - _random.Range(1.0f, 1.3f);
                    if (hand.Role == HoldRole.Top || y < startY - 0.75f || _random.NextDouble() > parameters.footDensity)
                    {
                        continue;
                    }

                    var towardLane = hand.S < _lane ? 1f : -1f;
                    Place(_random.Pick(parameters.footKinds), hand.S + towardLane * _random.Range(0.05f, 0.25f), y, HoldRole.Intermediate);
                }
            }

            /// <summary>Places a hold near (s, y), jittering it off occupied spots; null when no free spot is found.</summary>
            private ProblemHold Place(HoldKind kind, float s, float y, HoldRole role)
            {
                for (var attempt = 0; attempt < PlacementTries; attempt++)
                {
                    var trialS = attempt == 0 ? s : s + _random.Range(-0.2f, 0.2f);
                    var trialY = attempt == 0 ? y : y + _random.Range(-0.08f, 0.08f);
                    var hold = CreateHold(kind, trialS, trialY, role);
                    if (hold != null && IsFree(hold))
                    {
                        _holds.Add(hold);
                        return hold;
                    }
                }

                return null;
            }

            private ProblemHold CreateHold(HoldKind kind, float s, float y, HoldRole role)
            {
                var range = _face.RangeAt(y);
                if (range.Length < 2f * EdgeMargin)
                {
                    return null;
                }

                var hold = new ProblemHold(_face, kind, Mathf.Clamp(s, range.Min + EdgeMargin, range.Max - EdgeMargin), y)
                {
                    Role = role,
                    Scale = _circuit.Parameters.holdScale * _random.Range(0.9f, 1.1f) * (role == HoldRole.Top ? 1.15f : 1f),
                    RollDeg = kind == HoldKind.Foothold ? _random.Range(0f, 360f) : _random.Range(-25f, 25f),
                };
                return hold;
            }

            private bool IsFree(ProblemHold candidate)
            {
                var radius = Footprint(candidate);
                if (!_occupancy.IsFree(candidate.S, candidate.Y, radius))
                {
                    return false;
                }

                foreach (var hold in _holds)
                {
                    var minDistance = radius + Footprint(hold);
                    if (new Vector2(hold.S - candidate.S, hold.Y - candidate.Y).sqrMagnitude < minDistance * minDistance)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
