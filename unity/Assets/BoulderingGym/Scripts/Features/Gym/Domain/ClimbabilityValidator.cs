using System.Collections.Generic;
using System.Text;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>
    /// Simulated climber proving a problem can be topped with the game's own reach rules, for every
    /// given body size: start standing with the hands on the start holds, then search (depth first,
    /// with backtracking) sequences where one limb at a time moves up to a hold it can reach while the
    /// other limbs keep the body in place, until both hands match the top hold.
    /// </summary>
    public sealed class ClimbabilityValidator
    {
        private const int MaxExpandedStates = 400;
        private const int CandidatesPerLimb = 2;
        private const float FloorStandOff = 0.3f;
        private const float FootSpread = 0.14f;
        private const float MinProgress = 0.05f;

        private readonly IReadOnlyList<ClimberAnatomy> _anatomies;

        public ClimbabilityValidator(params ClimberAnatomy[] anatomies)
        {
            _anatomies = anatomies;
        }

        public bool CanTop(IReadOnlyList<ProblemHold> holds)
        {
            foreach (var anatomy in _anatomies)
            {
                if (!CanTopWith(holds, anatomy))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Standing in front of the start, each hand on its start hold (one hold: matched); null if unreachable.</summary>
        public static ContactSet StartContacts(IReadOnlyList<ProblemHold> holds, ClimberAnatomy anatomy)
        {
            var starts = new List<ProblemHold>();
            foreach (var hold in holds)
            {
                if (hold.Role == HoldRole.Start)
                {
                    starts.Add(hold);
                }
            }

            if (starts.Count == 0)
            {
                return null;
            }

            starts.Sort((a, b) => a.S.CompareTo(b.S));
            var face = starts[0].Face;
            var startS = starts[0].Problem?.StartS ?? (starts[0].S + starts[starts.Count - 1].S) * 0.5f;
            var contacts = ContactSet.Empty
                .With(LimbId.LeftFoot, LimbContact.OnFloor(FloorPoint(face, startS - FootSpread)))
                .With(LimbId.RightFoot, LimbContact.OnFloor(FloorPoint(face, startS + FootSpread)))
                .With(LimbId.LeftHand, Grip(starts[0]))
                .With(LimbId.RightHand, Grip(starts[starts.Count - 1]));
            return PoseSolver.Solve(contacts, anatomy).IsValid ? contacts : null;
        }

        public static Vector3 FloorPoint(WallFace face, float s)
        {
            return face.FloorPointInFront(s, FloorStandOff);
        }

        public static LimbContact Grip(ProblemHold hold) => LimbContact.OnHold(hold.Id, hold.GripPoint, hold.Face);

        public static LimbContact Step(ProblemHold hold) => LimbContact.OnHold(hold.Id, hold.StepPoint, hold.Face);

        private static bool CanTopWith(IReadOnlyList<ProblemHold> holds, ClimberAnatomy anatomy)
        {
            return Search(holds, anatomy) != null;
        }

        /// <summary>
        /// Limb moves of one way up, starting standing on the floor: each hand to its start hold, then
        /// every move of the search; null when the problem cannot be topped.
        /// </summary>
        public static List<(LimbId limb, ProblemHold hold)> FindRoute(IReadOnlyList<ProblemHold> holds, ClimberAnatomy anatomy)
        {
            var states = Search(holds, anatomy);
            if (states == null)
            {
                return null;
            }

            var byId = new Dictionary<int, ProblemHold>();
            foreach (var hold in holds)
            {
                byId[hold.Id] = hold;
            }

            var route = new List<(LimbId limb, ProblemHold hold)>
            {
                (LimbId.LeftHand, byId[states[0][LimbId.LeftHand].HoldId]),
                (LimbId.RightHand, byId[states[0][LimbId.RightHand].HoldId]),
            };
            for (var i = 1; i < states.Count; i++)
            {
                foreach (var limb in ContactSet.AllLimbs)
                {
                    if (states[i][limb].HoldId != states[i - 1][limb].HoldId)
                    {
                        route.Add((limb, byId[states[i][limb].HoldId]));
                    }
                }
            }

            return route;
        }

        /// <summary>Depth first search of contact states from the start to the matched top; the states of the way found.</summary>
        private static List<ContactSet> Search(IReadOnlyList<ProblemHold> holds, ClimberAnatomy anatomy)
        {
            var start = StartContacts(holds, anatomy);
            var top = FindTop(holds);
            if (start == null || top == null)
            {
                return null;
            }

            var parents = new Dictionary<string, ContactSet>();
            var pending = new Stack<(ContactSet state, ContactSet parent)>();
            pending.Push((start, null));
            while (pending.Count > 0 && parents.Count < MaxExpandedStates)
            {
                var (state, parent) = pending.Pop();
                if (!parents.TryAdd(Key(state), parent))
                {
                    continue;
                }

                if (state.IsOnHold(LimbId.LeftHand, top.Id) && state.IsOnHold(LimbId.RightHand, top.Id))
                {
                    return PathTo(state, parents);
                }

                var successors = Successors(state, holds, anatomy);
                for (var i = successors.Count - 1; i >= 0; i--)
                {
                    pending.Push((successors[i], state));
                }
            }

            return null;
        }

        private static List<ContactSet> PathTo(ContactSet state, Dictionary<string, ContactSet> parents)
        {
            var path = new List<ContactSet>();
            for (var current = state; current != null; current = parents[Key(current)])
            {
                path.Insert(0, current);
            }

            return path;
        }

        /// <summary>
        /// Hand moves first (lower hand, nearest holds above), then foot moves (highest holds first).
        /// Bouldering start rule: the hands stay on the start holds until both feet have left the floor.
        /// </summary>
        private static List<ContactSet> Successors(ContactSet state, IReadOnlyList<ProblemHold> holds, ClimberAnatomy anatomy)
        {
            var successors = new List<ContactSet>();
            var ascending = Ascending(holds);
            foreach (var limb in LowerFirst(state, LimbId.LeftHand, LimbId.RightHand))
            {
                if (state.TouchesFloor)
                {
                    break;
                }

                successors.AddRange(LimbMoves(state, limb, ascending, anatomy));
            }

            var descending = Descending(holds);
            foreach (var limb in LowerFirst(state, LimbId.LeftFoot, LimbId.RightFoot))
            {
                successors.AddRange(LimbMoves(state, limb, descending, anatomy));
            }

            return successors;
        }

        private static IEnumerable<ContactSet> LimbMoves(ContactSet state, LimbId limb, List<ProblemHold> candidates, ClimberAnatomy anatomy)
        {
            var isHand = ContactSet.IsHand(limb);
            var hanging = state.With(limb, LimbContact.Free);
            if ((isHand && hanging.HandsOnHolds == 0) || !PoseSolver.Solve(hanging, anatomy).IsValid)
            {
                yield break;
            }

            var found = 0;
            var currentHeight = state[limb].Position.y;
            foreach (var hold in candidates)
            {
                var contact = isHand ? Grip(hold) : Step(hold);
                if (contact.Position.y < currentHeight + MinProgress || (isHand && !CanGrab(state, limb, hold)))
                {
                    continue;
                }

                var candidate = state.With(limb, contact);
                if (PoseSolver.Solve(candidate, anatomy).IsValid)
                {
                    yield return candidate;
                    if (++found == CandidatesPerLimb)
                    {
                        yield break;
                    }
                }
            }
        }

        /// <summary>Both hands may share only the top hold (the final match).</summary>
        private static bool CanGrab(ContactSet contacts, LimbId hand, ProblemHold hold)
        {
            if (!hold.AcceptsHands)
            {
                return false;
            }

            var other = hand == LimbId.LeftHand ? LimbId.RightHand : LimbId.LeftHand;
            return hold.Role == HoldRole.Top || !contacts.IsOnHold(other, hold.Id);
        }

        private static LimbId[] LowerFirst(ContactSet state, LimbId first, LimbId second)
        {
            return state[second].Position.y < state[first].Position.y ? new[] { second, first } : new[] { first, second };
        }

        private static List<ProblemHold> Ascending(IReadOnlyList<ProblemHold> holds)
        {
            var sorted = new List<ProblemHold>(holds);
            sorted.Sort((a, b) => a.Y.CompareTo(b.Y));
            return sorted;
        }

        private static List<ProblemHold> Descending(IReadOnlyList<ProblemHold> holds)
        {
            var sorted = Ascending(holds);
            sorted.Reverse();
            return sorted;
        }

        private static ProblemHold FindTop(IReadOnlyList<ProblemHold> holds)
        {
            foreach (var hold in holds)
            {
                if (hold.Role == HoldRole.Top)
                {
                    return hold;
                }
            }

            return null;
        }

        private static string Key(ContactSet state)
        {
            var key = new StringBuilder();
            foreach (var limb in ContactSet.AllLimbs)
            {
                var contact = state[limb];
                key.Append(contact.Kind == ContactKind.Hold ? contact.HoldId.ToString() : contact.Kind.ToString()).Append('|');
            }

            return key.ToString();
        }
    }
}
