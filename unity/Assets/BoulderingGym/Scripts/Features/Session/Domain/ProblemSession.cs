using Project.Features.Gym.Domain;

namespace Project.Features.Session.Domain
{
    public enum AttemptPhase
    {
        NoProblem,
        Ready,
        Climbing,
        Topped,
        Failed,
    }

    public enum SessionEvent
    {
        None,
        Started,
        FalseStart,
        Topped,
        Failed,
        Ready,
    }

    /// <summary>Where the climber stands this frame, as needed by the bouldering rules.</summary>
    public readonly struct ClimbSnapshot
    {
        public ClimbSnapshot(bool isOffGround, bool isDown, bool handsOnStart, bool matchedTop)
        {
            IsOffGround = isOffGround;
            IsDown = isDown;
            HandsOnStart = handsOnStart;
            MatchedTop = matchedTop;
        }

        /// <summary>On the wall with no limb on the floor.</summary>
        public bool IsOffGround { get; }

        /// <summary>Standing on the mats (walking or after landing).</summary>
        public bool IsDown { get; }

        /// <summary>Each hand on a start hold of the problem.</summary>
        public bool HandsOnStart { get; }

        /// <summary>Both hands on the top hold.</summary>
        public bool MatchedTop { get; }
    }

    /// <summary>
    /// Bouldering rules for the selected problem: an attempt starts when the climber leaves the floor
    /// with both hands on the start holds, succeeds when both hands match the top hold and fails as
    /// soon as the climber is back on the floor without topping (dab, fall or drop).
    /// </summary>
    public sealed class ProblemSession
    {
        private bool _falseStartReported;

        public BoulderProblem Problem { get; private set; }

        public AttemptPhase Phase { get; private set; } = AttemptPhase.NoProblem;

        public int Attempts { get; private set; }

        public float AttemptSeconds { get; private set; }

        public int Moves { get; private set; }

        public bool IsAllowed(ProblemHold hold) => Problem == null || hold.Problem == Problem;

        public void Select(BoulderProblem problem)
        {
            Problem = problem;
            Phase = problem == null ? AttemptPhase.NoProblem : AttemptPhase.Ready;
            Attempts = 0;
            AttemptSeconds = 0f;
            Moves = 0;
        }

        public void CountMove()
        {
            if (Phase == AttemptPhase.Climbing)
            {
                Moves++;
            }
        }

        public SessionEvent Update(float deltaTime, ClimbSnapshot snapshot)
        {
            switch (Phase)
            {
                case AttemptPhase.Ready:
                    return UpdateReady(snapshot);
                case AttemptPhase.Climbing:
                    AttemptSeconds += deltaTime;
                    return UpdateClimbing(snapshot);
                case AttemptPhase.Topped:
                case AttemptPhase.Failed:
                    return UpdateFinished(snapshot);
                default:
                    return SessionEvent.None;
            }
        }

        private SessionEvent UpdateReady(ClimbSnapshot snapshot)
        {
            if (!snapshot.IsOffGround)
            {
                _falseStartReported = false;
                return SessionEvent.None;
            }

            if (snapshot.HandsOnStart)
            {
                Phase = AttemptPhase.Climbing;
                Attempts++;
                AttemptSeconds = 0f;
                Moves = 0;
                return SessionEvent.Started;
            }

            if (_falseStartReported)
            {
                return SessionEvent.None;
            }

            _falseStartReported = true;
            return SessionEvent.FalseStart;
        }

        private SessionEvent UpdateClimbing(ClimbSnapshot snapshot)
        {
            if (snapshot.MatchedTop)
            {
                Phase = AttemptPhase.Topped;
                return SessionEvent.Topped;
            }

            if (snapshot.IsOffGround)
            {
                return SessionEvent.None;
            }

            Phase = AttemptPhase.Failed;
            return SessionEvent.Failed;
        }

        private SessionEvent UpdateFinished(ClimbSnapshot snapshot)
        {
            if (!snapshot.IsDown)
            {
                return SessionEvent.None;
            }

            Phase = AttemptPhase.Ready;
            return SessionEvent.Ready;
        }
    }
}
