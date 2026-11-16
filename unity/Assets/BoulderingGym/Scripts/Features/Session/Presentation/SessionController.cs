using System;
using Project.Core.Events;
using Project.Core.Input;
using Project.Features.Camera.Presentation;
using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Presentation;
using Project.Features.Gym.Domain;
using Project.Features.Gym.Presentation;
using Project.Features.Session.Domain;
using UnityEngine;
using EventType = Project.Core.Events.EventType;

namespace Project.Features.Session.Presentation
{
    /// <summary>Collaborators of the session, wired by the game bootstrap.</summary>
    public sealed class SessionSetup
    {
        public ClimberController Climber { get; set; }

        public HoldTargeting Targeting { get; set; }

        public OrbitCamera Camera { get; set; }

        public HoldMarkers Markers { get; set; }

        public InputReader Input { get; set; }

        public IEventBus EventBus { get; set; }

        public ClimbRecords Records { get; set; }

        /// <summary>Persists the records after every attempt.</summary>
        public Action<ClimbRecords> Save { get; set; }
    }

    /// <summary>
    /// Runs the selected problem: restricts targeting to its holds, marks its start and top, applies
    /// the bouldering rules every frame, counts moves and keeps personal records.
    /// </summary>
    public sealed class SessionController : MonoBehaviour
    {
        private const float StandBack = 0.9f;
        private const float BannerSeconds = 3f;

        private SessionSetup _setup;
        private float _bannerUntil;
        private string _bannerText;

        public ProblemSession Session { get; } = new();

        public ClimbRecords Records => _setup.Records;

        /// <summary>Large centered announcement ("TOP!"), null when none is showing.</summary>
        public string Banner => Time.time < _bannerUntil ? _bannerText : null;

        /// <summary>Called again when the climber is swapped; the current problem is left.</summary>
        public void Initialize(SessionSetup setup)
        {
            if (_setup == null)
            {
                setup.EventBus.Subscribe(EventType.HoldGrabbed, _ => Session.CountMove());
            }
            else
            {
                Leave();
            }

            _setup = setup;
        }

        public void Select(BoulderProblem problem)
        {
            Session.Select(problem);
            _setup.Targeting.SetFilter(Session.IsAllowed);
            _setup.Markers.ShowProblem(problem);
            GoToStart();
        }

        public void Leave()
        {
            Session.Select(null);
            _setup.Targeting.SetFilter(null);
            _setup.Markers.HideProblem();
        }

        /// <summary>Stands the climber in front of the start holds, facing the wall.</summary>
        public void GoToStart()
        {
            var face = Session.Problem.Face;
            var feet = face.FloorPointInFront(Session.Problem.StartS, StandBack);
            _setup.Climber.Teleport(feet, Quaternion.LookRotation(-face.Outward, Vector3.up));
            _setup.Camera.Snap(-face.Outward);
        }

        private void Update()
        {
            if (Session.Problem == null)
            {
                return;
            }

            if (_setup.Climber.InputEnabled && _setup.Input.ConsumeRestartPressed())
            {
                GoToStart();
            }

            Handle(Session.Update(Time.deltaTime, Snapshot()));
        }

        private ClimbSnapshot Snapshot()
        {
            var climber = _setup.Climber;
            var contacts = climber.Contacts;
            var top = Session.Problem.TopHold;
            var onWall = climber.State == ClimberStateId.OnWall;
            return new ClimbSnapshot(
                onWall && !contacts.TouchesFloor,
                climber.State == ClimberStateId.Ground || climber.State == ClimberStateId.Landing,
                onWall && HandsOnStart(),
                onWall && contacts.IsOnHold(LimbId.LeftHand, top.Id) && contacts.IsOnHold(LimbId.RightHand, top.Id));
        }

        /// <summary>Each hand on a start hold; with a single start hold both hands share it.</summary>
        private bool HandsOnStart()
        {
            var left = _setup.Climber.HoldOf(LimbId.LeftHand);
            var right = _setup.Climber.HoldOf(LimbId.RightHand);
            return left != null && right != null
                && left.Problem == Session.Problem && right.Problem == Session.Problem
                && left.Role == HoldRole.Start && right.Role == HoldRole.Start;
        }

        private void Handle(SessionEvent sessionEvent)
        {
            var problem = Session.Problem;
            if (sessionEvent != SessionEvent.None)
            {
                Debug.Log($"[Session] {problem.Id}: {sessionEvent} (attempt {Session.Attempts}, {Session.AttemptSeconds:0.0}s, {Session.Moves} moves)");
            }

            switch (sessionEvent)
            {
                case SessionEvent.Started:
                    _setup.Records.AddAttempt(problem.Id);
                    ShowBanner("Go!");
                    break;
                case SessionEvent.FalseStart:
                    _setup.Climber.ShowMessage("Start with both hands on the START holds");
                    break;
                case SessionEvent.Topped:
                    var isBest = _setup.Records.AddTop(problem.Id, Session.AttemptSeconds);
                    ShowBanner(isBest ? $"TOP!  {FormatTime(Session.AttemptSeconds)}  new best" : $"TOP!  {FormatTime(Session.AttemptSeconds)}");
                    _setup.Save(_setup.Records);
                    _setup.EventBus.Publish(new GameEvent(EventType.RunCompleted, problem));
                    break;
                case SessionEvent.Failed:
                    _setup.Climber.ShowMessage("Attempt over: back on the mats");
                    _setup.Save(_setup.Records);
                    _setup.EventBus.Publish(new GameEvent(EventType.RunFailed, problem));
                    break;
            }
        }

        private void ShowBanner(string text)
        {
            _bannerText = text;
            _bannerUntil = Time.time + BannerSeconds;
        }

        public static string FormatTime(float seconds)
        {
            var whole = Mathf.FloorToInt(seconds);
            return $"{whole / 60}:{whole % 60:00}.{Mathf.FloorToInt((seconds - whole) * 10f)}";
        }
    }
}
