using System;
using System.Collections.Generic;
using Project.Core.Domain;
using Project.Core.Events;
using Project.Core.Input;
using Project.Core.StateMachine;
using Project.Features.Climbing.Domain;
using Project.Features.Gym.Domain;
using Project.Features.Resources.Domain;
using UnityEngine;
using EventType = Project.Core.Events.EventType;

namespace Project.Features.Climbing.Presentation
{
    /// <summary>Collaborators of the climber, wired by the game bootstrap.</summary>
    public sealed class ClimberSetup
    {
        public InputReader Input { get; set; }

        public HoldTargeting Targeting { get; set; }

        public ClimberAvatar Avatar { get; set; }

        public ClimberLocomotion Locomotion { get; set; }

        public TuningConfig Tuning { get; set; }

        public IEventBus EventBus { get; set; }

        public ClimberAnatomy Anatomy { get; set; }

        public Func<int, ProblemHold> HoldById { get; set; }
    }

    /// <summary>
    /// Point and place climbing (plan/01_gameplay/game_loop.md): aim a hold, press a limb key (or click
    /// for the suggested limb), the body follows the contacts. Every placement is checked by the pose
    /// solver; arms pump and slip when exhausted; with no hand left the climber falls to the mats.
    /// </summary>
    public sealed class ClimberController : MonoBehaviour, IClimberStateEffects
    {
        private const float FootSpread = 0.14f;
        private const float FallPushAway = 0.6f;
        private const float Gravity = 9.81f;
        private const float MessageSeconds = 2.5f;
        private const float ToeDroop = 0.3f;
        private static readonly int LandParameter = Animator.StringToHash("land");

        private ClimberSetup _setup;
        private StateMachine<ClimberStateId, ClimberTrigger, ClimberContext> _states;
        private ClimbingBody _body;
        private Vector3 _fallVelocity;
        private WallFace _fallFace;
        private float _landingTimeLeft;
        private float _messageUntil;

        public ClimberStateId State => _states.CurrentState.Id;

        public ContactSet Contacts => _body.Contacts;

        public ClimberResources Resources { get; private set; }

        public bool InputEnabled { get; set; } = true;

        public string Message => Time.time < _messageUntil ? MessageText : null;

        private string MessageText { get; set; }

        public Transform CameraTarget => _setup.Avatar.Chest;

        public void Initialize(ClimberSetup setup)
        {
            _setup = setup;
            _body = new ClimbingBody(setup.Anatomy);
            Resources = new ClimberResources(setup.Tuning);
            _states = ClimberStateMachine.Create(this);
        }

        public ProblemHold HoldOf(LimbId limb)
        {
            var contact = Contacts[limb];
            return contact.Kind == ContactKind.Hold ? _setup.HoldById(contact.HoldId) : null;
        }

        /// <summary>The limb a click on the target would move, without moving it; null when none can.</summary>
        public LimbId? PreviewClick(ClimbTarget target)
        {
            if (!target.IsValid)
            {
                return null;
            }

            if (State == ClimberStateId.Ground)
            {
                return target.Hold != null ? FirstStartingLimb(target.Hold, null) : null;
            }

            if (State != ClimberStateId.OnWall)
            {
                return null;
            }

            var handsAllowed = target.Hold != null && target.Hold.AcceptsHands;
            foreach (var limb in LimbChooser.Candidates(Contacts, _body.Pose, target.Point, handsAllowed))
            {
                var alreadyThere = target.Hold != null && Contacts.IsOnHold(limb, target.Hold.Id);
                if (!alreadyThere && CanUse(limb, target) && _body.Evaluate(Contacts.With(limb, ContactFor(limb, target))).IsValid)
                {
                    return limb;
                }
            }

            return null;
        }

        /// <summary>Puts the climber standing at a spot (problem start, spawn), whatever it was doing.</summary>
        public void Teleport(Vector3 feet, Quaternion rotation)
        {
            _states.Fire(ClimberTrigger.Teleported);
            _setup.Locomotion.PlaceAt(feet, rotation);
        }

        public void ShowMessage(string text)
        {
            Debug.Log($"[Climber] {text}");
            MessageText = text;
            _messageUntil = Time.time + MessageSeconds;
        }

        private void Update()
        {
            if (_states == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            switch (State)
            {
                case ClimberStateId.Ground:
                    TickGround(deltaTime);
                    break;
                case ClimberStateId.OnWall:
                    TickWall(deltaTime);
                    break;
                case ClimberStateId.Falling:
                    TickFalling(deltaTime);
                    break;
                default:
                    TickLanding(deltaTime);
                    break;
            }
        }

        // ── Ground ──

        private void TickGround(float deltaTime)
        {
            Resources.UpdateResting(deltaTime);
            if (!InputEnabled)
            {
                _setup.Locomotion.Stop();
                return;
            }

            var target = _setup.Targeting.Current;
            var limb = target.Hold != null ? _setup.Input.ConsumeActiveLimb() : null;
            var clicked = target.Hold != null && _setup.Input.ConsumeQuickPlacePressed();
            if (limb.HasValue || clicked)
            {
                TryStartClimbing(target.Hold, limb);
                return;
            }

            _setup.Locomotion.Tick(deltaTime);
        }

        private void TryStartClimbing(ProblemHold hold, LimbId? requested)
        {
            if (Vector3.Distance(Flat(transform.position), Flat(ClimbabilityValidator.FloorPoint(hold.Face, hold.S))) > _setup.Tuning.maxStartDistance)
            {
                ShowMessage("Too far: walk closer to the wall");
                return;
            }

            var limb = FirstStartingLimb(hold, requested);
            if (!limb.HasValue)
            {
                ShowMessage("Out of reach from the floor");
                return;
            }

            var candidate = StandingAt(hold).With(limb.Value, ContactFor(limb.Value, ClimbTarget.OnHold(hold)));
            _body.Begin(_setup.Avatar.Animator.GetBoneTransform(HumanBodyBones.Hips).position);
            Adopt(candidate, _body.Evaluate(candidate), hold);
            _states.Fire(ClimberTrigger.StartedClimbing);
        }

        /// <summary>First limb able to reach the hold while standing in front of it, or the requested one if it can.</summary>
        private LimbId? FirstStartingLimb(ProblemHold hold, LimbId? requested)
        {
            var standing = StandingAt(hold);
            var feetCenter = ClimbabilityValidator.FloorPoint(hold.Face, hold.S);
            var standingPose = new PoseSolution(true, feetCenter + Vector3.up * _setup.Avatar.StandingHipsHeight, new BodyFrame(-hold.Face.Outward), _setup.Anatomy);
            var limbs = requested.HasValue ? new List<LimbId> { requested.Value } : LimbChooser.Candidates(standing, standingPose, hold.GripPoint, hold.AcceptsHands);
            foreach (var limb in limbs)
            {
                if (CanUse(limb, hold) && _body.Evaluate(standing.With(limb, ContactFor(limb, ClimbTarget.OnHold(hold)))).IsValid)
                {
                    return limb;
                }
            }

            return null;
        }

        /// <summary>Feet on the floor below the hold, or below the middle of the start when it is a start hold.</summary>
        private static ContactSet StandingAt(ProblemHold hold)
        {
            var s = hold.Role == HoldRole.Start ? hold.Problem.StartS : hold.S;
            return ContactSet.Empty
                .With(LimbId.LeftFoot, LimbContact.OnFloor(ClimbabilityValidator.FloorPoint(hold.Face, s - FootSpread)))
                .With(LimbId.RightFoot, LimbContact.OnFloor(ClimbabilityValidator.FloorPoint(hold.Face, s + FootSpread)));
        }

        // ── Wall ──

        private void TickWall(float deltaTime)
        {
            if (InputEnabled)
            {
                HandleWallInput();
            }

            if (State != ClimberStateId.OnWall)
            {
                return;
            }

            var slipped = Resources.UpdateOnWall(deltaTime, CurrentLoad());
            if (slipped.HasValue)
            {
                ShowMessage(slipped == LimbId.LeftHand ? "Left hand slipped!" : "Right hand slipped!");
                _setup.EventBus.Publish(new GameEvent(EventType.HoldSlip, slipped.Value));
                Release(slipped.Value);
                if (State != ClimberStateId.OnWall)
                {
                    return;
                }
            }

            _body.Tick(deltaTime, _setup.Tuning.bodySettleSeconds);
            UpdateAvatar();
        }

        private void HandleWallInput()
        {
            var input = _setup.Input;
            if (input.ConsumeDropPressed())
            {
                LetGo();
                return;
            }

            if (input.ConsumeChalkPressed())
            {
                Chalk();
            }

            var target = _setup.Targeting.Current;
            var limb = input.ConsumeActiveLimb();
            if (limb.HasValue)
            {
                if (target.IsValid)
                {
                    TryPlace(limb.Value, target, true);
                }
                else
                {
                    Release(limb.Value);
                }

                return;
            }

            if (input.ConsumeQuickPlacePressed() && target.IsValid)
            {
                PlaceSuggested(target);
            }
        }

        private void PlaceSuggested(ClimbTarget target)
        {
            var handsAllowed = target.Hold != null && target.Hold.AcceptsHands;
            foreach (var limb in LimbChooser.Candidates(Contacts, _body.Pose, target.Point, handsAllowed))
            {
                var alreadyThere = target.Hold != null && Contacts.IsOnHold(limb, target.Hold.Id);
                if (!alreadyThere && TryPlace(limb, target, false))
                {
                    return;
                }
            }

            ShowMessage("Out of reach");
        }

        private bool TryPlace(LimbId limb, ClimbTarget target, bool explain)
        {
            if (!CanUse(limb, target))
            {
                if (explain)
                {
                    ShowMessage(ContactSet.IsHand(limb) ? "Hands only go on hand holds" : "Feet go on holds or the floor");
                }

                return false;
            }

            var candidate = Contacts.With(limb, ContactFor(limb, target));
            var pose = _body.Evaluate(candidate);
            if (!pose.IsValid)
            {
                if (explain)
                {
                    Debug.Log($"[Climber] {limb} cannot reach {(target.Hold != null ? target.Hold.Name : "the floor")}");
                    ShowMessage("Out of reach");
                }

                return false;
            }

            Adopt(candidate, pose, target.Hold);
            return true;
        }

        private void Adopt(ContactSet contacts, PoseSolution pose, ProblemHold hold)
        {
            _body.Apply(contacts, pose, _setup.Avatar.LimbPosition, _setup.Tuning.limbMoveSeconds);
            Resources.OnLimbPlaced(hold != null ? hold.Face.Profile.AngleAt(hold.Y) : 0f);
            _setup.EventBus.Publish(new GameEvent(EventType.HoldGrabbed, hold));
        }

        private void Release(LimbId limb)
        {
            var remaining = Contacts.With(limb, LimbContact.Free);
            if (remaining.HandsOnHolds == 0)
            {
                LeaveWall(remaining.TouchesFloor);
                return;
            }

            var pose = _body.Evaluate(remaining);
            if (!pose.IsValid)
            {
                LeaveWall(false);
                return;
            }

            _body.Apply(remaining, pose, _setup.Avatar.LimbPosition, _setup.Tuning.limbMoveSeconds);
        }

        private void LetGo() => LeaveWall(Contacts.TouchesFloor);

        private void LeaveWall(bool standing)
        {
            _fallFace = HoldOf(LimbId.LeftHand)?.Face ?? HoldOf(LimbId.RightHand)?.Face;
            _states.Fire(standing ? ClimberTrigger.SteppedDown : ClimberTrigger.LostSupport);
        }

        private void Chalk()
        {
            if (CurrentLoad().HandsHolding == 2)
            {
                ShowMessage("Free a hand to chalk up");
                return;
            }

            if (!Resources.TryChalk(CurrentLoad()))
            {
                ShowMessage("Chalk bag not ready");
                return;
            }

            ShowMessage("Chalked up");
            _setup.EventBus.Publish(new GameEvent(EventType.ChalkUsed));
        }

        private ClimbLoad CurrentLoad()
        {
            var left = HoldOf(LimbId.LeftHand);
            var right = HoldOf(LimbId.RightHand);
            var reference = left ?? right;
            var angle = reference != null ? reference.Face.Profile.AngleAt(reference.Y) : 0f;
            return new ClimbLoad(
                left != null ? HoldKindInfo.Grip(left.Kind) : null,
                right != null ? HoldKindInfo.Grip(right.Kind) : null,
                Contacts.FeetSupporting,
                angle);
        }

        private bool CanUse(LimbId limb, ClimbTarget target)
        {
            if (target.IsFloor)
            {
                return !ContactSet.IsHand(limb);
            }

            return CanUse(limb, target.Hold);
        }

        private static bool CanUse(LimbId limb, ProblemHold hold) => !ContactSet.IsHand(limb) || hold.AcceptsHands;

        private static LimbContact ContactFor(LimbId limb, ClimbTarget target)
        {
            if (target.IsFloor)
            {
                return LimbContact.OnFloor(target.Point);
            }

            return ContactSet.IsHand(limb) ? ClimbabilityValidator.Grip(target.Hold) : ClimbabilityValidator.Step(target.Hold);
        }

        private void UpdateAvatar()
        {
            var pose = _body.Pose;
            var frame = pose.Frame;
            var avatar = _setup.Avatar;
            var bodyRotation = Quaternion.LookRotation(frame.Forward, Vector3.up);
            var drift = _body.DisplayedPelvis - pose.Pelvis;
            transform.SetPositionAndRotation(_body.DisplayedPelvis - Vector3.up * avatar.StandingHipsHeight, bodyRotation);
            avatar.PelvisTarget = _body.DisplayedPelvis;
            avatar.BodyRotation = bodyRotation;
            foreach (var limb in ContactSet.AllLimbs)
            {
                var isHand = ContactSet.IsHand(limb);
                var side = ContactSet.IsLeft(limb) ? -frame.Right : frame.Right;
                var root = isHand ? (ContactSet.IsLeft(limb) ? pose.LeftShoulder : pose.RightShoulder) : (ContactSet.IsLeft(limb) ? pose.LeftHip : pose.RightHip);
                var hint = root + drift + side * 0.2f - frame.Forward * (isHand ? 0.3f : 0.45f) - Vector3.up * (isHand ? 0.3f : 0.05f);
                var aim = isHand ? Vector3.up : (frame.Forward - Vector3.up * ToeDroop).normalized;
                avatar.SetGoal(limb, _body.LimbPosition(limb), aim, Contacts[limb].IsSupporting ? 1f : 0f, hint);
            }
        }

        // ── Falling and landing ──

        private void TickFalling(float deltaTime)
        {
            _fallVelocity += Vector3.down * (Gravity * deltaTime);
            var pelvis = _body.DisplayedPelvis + _fallVelocity * deltaTime;
            if (_fallFace != null)
            {
                var clearance = _setup.Anatomy.MinWallClearance - _fallFace.SignedDistance(pelvis);
                pelvis += _fallFace.NormalAt(pelvis.y) * Mathf.Max(0f, clearance);
            }

            _body.Release(pelvis);
            transform.position = pelvis - Vector3.up * _setup.Avatar.StandingHipsHeight;
            _setup.Avatar.PelvisTarget = pelvis;
            if (transform.position.y <= 0f)
            {
                _states.Fire(ClimberTrigger.TouchedFloor);
            }
        }

        private void TickLanding(float deltaTime)
        {
            Resources.UpdateResting(deltaTime);
            _landingTimeLeft -= deltaTime;
            if (_landingTimeLeft <= 0f)
            {
                _states.Fire(ClimberTrigger.LandingFinished);
            }
        }

        // ── State effects ──

        public void EnterGround()
        {
            _setup.Avatar.IkActive = false;
            var feet = Flat(transform.position);
            _setup.Locomotion.PlaceAt(feet, Quaternion.LookRotation(Flat(transform.forward).normalized, Vector3.up));
        }

        public void EnterWall()
        {
            _setup.Locomotion.Stop();
            _setup.Locomotion.SetPhysicsEnabled(false);
            _setup.Avatar.IkActive = true;
        }

        public void EnterFalling()
        {
            _fallVelocity = -_body.Pose.Frame.Forward * FallPushAway;
            foreach (var limb in ContactSet.AllLimbs)
            {
                _setup.Avatar.SetGoal(limb, Vector3.zero, Vector3.up, 0f, Vector3.zero);
            }
        }

        public void EnterLanding()
        {
            _setup.Avatar.IkActive = false;
            _setup.Avatar.Animator.SetTrigger(LandParameter);
            _setup.Locomotion.PlaceAt(Flat(transform.position), Quaternion.LookRotation(Flat(transform.forward).normalized, Vector3.up));
            _landingTimeLeft = _setup.Tuning.landingSeconds;
        }

        private static Vector3 Flat(Vector3 point) => new(point.x, 0f, point.z);
    }
}
