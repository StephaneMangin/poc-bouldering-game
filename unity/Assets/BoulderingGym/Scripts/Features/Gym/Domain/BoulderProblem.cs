using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    public enum HoldRole
    {
        Intermediate,
        Start,
        Top,
    }

    /// <summary>Hold bolted on a face at surface coordinates (s along the base, y height).</summary>
    public sealed class ProblemHold
    {
        public ProblemHold(WallFace face, HoldKind kind, float s, float y)
        {
            Face = face;
            Kind = kind;
            S = s;
            Y = y;
        }

        /// <summary>Unique across the gym; assigned when the problem is accepted.</summary>
        public int Id { get; internal set; } = -1;

        public BoulderProblem Problem { get; internal set; }

        public WallFace Face { get; }

        public HoldKind Kind { get; }

        public float S { get; }

        public float Y { get; }

        public HoldRole Role { get; internal set; }

        public float Scale { get; internal set; } = 1f;

        /// <summary>Rotation around the wall normal, degrees.</summary>
        public float RollDeg { get; internal set; }

        public bool AcceptsHands => HoldKindInfo.AcceptsHands(Kind);

        /// <summary>Stable name, e.g. "quinquin-2:START00" or "quinquin-2:H04", used by scripted playtests.</summary>
        public string Name
        {
            get
            {
                var role = Role == HoldRole.Intermediate ? "H" : Role.ToString().ToUpperInvariant();
                return $"{Problem.Id}:{role}{Problem.IndexOf(this):00}";
            }
        }

        public Vector3 SurfacePoint => Face.SurfacePoint(S, Y);

        public Vector3 Normal => Face.NormalAt(Y);

        public Vector3 Up => Face.UpTangentAt(Y);

        public Vector3 Size => HoldKindInfo.Size(Kind) * Scale;

        /// <summary>Where the fingers close: in front of the hold, toward its top edge.</summary>
        public Vector3 GripPoint => SurfacePoint + Normal * (Size.z * 0.7f) + Up * (Size.y * 0.25f);

        /// <summary>Where the toe rests: on the top of the hold.</summary>
        public Vector3 StepPoint => SurfacePoint + Normal * (Size.z * 0.6f) + Up * (Size.y * 0.5f);
    }

    /// <summary>Boulder problem: holds of one color on one face, from the tagged start holds to the top hold.</summary>
    public sealed class BoulderProblem
    {
        private readonly List<ProblemHold> _holds;

        public BoulderProblem(GymSector sector, WallFace face, Circuit circuit, IEnumerable<ProblemHold> holds)
        {
            Sector = sector;
            Face = face;
            Circuit = circuit;
            _holds = new List<ProblemHold>(holds);
            foreach (var hold in _holds)
            {
                hold.Problem = this;
            }
        }

        public string Id { get; internal set; }

        public int Number { get; internal set; }

        public string Grade { get; internal set; }

        public GymSector Sector { get; }

        public WallFace Face { get; }

        public Circuit Circuit { get; }

        public IReadOnlyList<ProblemHold> Holds => _holds;

        public int IndexOf(ProblemHold hold) => _holds.IndexOf(hold);

        public string DisplayName => $"{Sector.Name} #{Number}";

        public IEnumerable<ProblemHold> StartHolds
        {
            get
            {
                foreach (var hold in _holds)
                {
                    if (hold.Role == HoldRole.Start)
                    {
                        yield return hold;
                    }
                }
            }
        }

        public ProblemHold TopHold => _holds.Find(hold => hold.Role == HoldRole.Top);

        /// <summary>Along-wall coordinate of the start, used to place the climber in front of the problem.</summary>
        public float StartS
        {
            get
            {
                var sum = 0f;
                var count = 0;
                foreach (var hold in StartHolds)
                {
                    sum += hold.S;
                    count++;
                }

                return count > 0 ? sum / count : TopHold.S;
            }
        }
    }
}
