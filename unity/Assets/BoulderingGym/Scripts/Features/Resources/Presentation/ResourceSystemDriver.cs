using Project.Core.Domain;
using Project.Core.Events;
using Project.Core.Input;
using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Presentation;
using Project.Features.Physics.Domain;
using Project.Features.Physics.Presentation;
using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using Project.Features.Resources.Domain;
using UnityEngine;
using EventType = Project.Core.Events.EventType;

namespace Project.Features.Resources.Presentation
{
    /// <summary>
    /// MonoBehaviour driver that bridges the three resource domain systems
    /// (Endurance, Grip, LocalFatigue) with the Unity scene. Polls physics
    /// snapshot, FSM state, and hand controller each frame.
    /// </summary>
    public sealed class ResourceSystemDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private PhysicsModelDriver physicsDriver;
        [SerializeField] private PlayerStateMachineDriver playerDriver;
        [SerializeField] private PlayerHandsController playerHands;
        [SerializeField] private InputReader inputReader;

        private EnduranceSystem _endurance;
        private GripSystem _grip;
        private LocalFatigueSystem _fatigue;
        private IEventBus _eventBus;

        private PlayerStateId? _prevState;
        private bool _prevLeftHolding;
        private bool _prevRightHolding;
        private bool _wasEnduranceLow;
        private bool _wasFatigueWarningLeft;
        private bool _wasFatigueWarningRight;
        private bool _wasFatigueCriticalLeft;
        private bool _wasFatigueCriticalRight;
        private bool _wasMicroSlipping;
        private bool _leftFatigueReleased;
        private bool _rightFatigueReleased;

        public EnduranceSystem Endurance => _endurance;
        public GripSystem Grip => _grip;
        public LocalFatigueSystem Fatigue => _fatigue;
        public ResourceSnapshot CurrentSnapshot { get; private set; }

        public void Initialize(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        private void Awake()
        {
            if (tuningConfig != null)
            {
                _endurance = new EnduranceSystem(tuningConfig);
                _grip = new GripSystem(tuningConfig);
                _fatigue = new LocalFatigueSystem(tuningConfig);
            }
        }

        private void Update()
        {
            if (_endurance == null || playerDriver == null) return;

            var dt = Time.deltaTime;
            if (dt <= 0f) return;

            var stateId = playerDriver.StateMachine?.CurrentState?.Id;
            if (!stateId.HasValue) return;

            // Reset on landing (transition to IdleGround from another state)
            if (stateId.Value == PlayerStateId.IdleGround
                && _prevState.HasValue
                && _prevState.Value != PlayerStateId.IdleGround)
            {
                ResetAll();
            }

            _prevState = stateId.Value;

            var physics = physicsDriver != null ? physicsDriver.CurrentSnapshot : default;

            switch (stateId.Value)
            {
                case PlayerStateId.GripStable:
                case PlayerStateId.Reach:
                    UpdateClimbing(dt, physics);
                    break;
                case PlayerStateId.IdleGround:
                    UpdateResting(dt);
                    break;
            }

            DetectGrabs(physics);
            HandleChalkInput();
            PublishThresholdEvents();
            HandleFatigueFailure();
            CurrentSnapshot = BuildSnapshot();
        }

        private void UpdateClimbing(float dt, PhysicsSnapshot physics)
        {
            var gripTexture = GetAverageGripTexture();
            _grip.UpdateDrain(dt, physics.Weight.TotalHandWeight, physics.Multipliers.GripDrainMult, gripTexture);

            var leftActive = playerHands != null && playerHands.IsHolding(HandSide.Left);
            var rightActive = playerHands != null && playerHands.IsHolding(HandSide.Right);
            var leftGripType = GetHeldGripType(HandSide.Left);
            var rightGripType = GetHeldGripType(HandSide.Right);

            _fatigue.Update(dt,
                leftGripType, physics.Weight.WeightOnLeftHand, leftActive,
                rightGripType, physics.Weight.WeightOnRightHand, rightActive,
                physics.Multipliers.FatigueMult);
        }

        private void UpdateResting(float dt)
        {
            _endurance.UpdateRegen(dt);
            _grip.UpdateCooldown(dt);
            // Both arms recover at rest
            _fatigue.Update(dt,
                GripType.Jug, 0f, false,
                GripType.Jug, 0f, false,
                1f);
        }

        private void DetectGrabs(PhysicsSnapshot physics)
        {
            if (playerHands == null) return;

            var leftNow = playerHands.IsHolding(HandSide.Left);
            var rightNow = playerHands.IsHolding(HandSide.Right);

            if (leftNow && !_prevLeftHolding)
            {
                OnGrab(physics);
            }

            if (rightNow && !_prevRightHolding)
            {
                OnGrab(physics);
            }

            _prevLeftHolding = leftNow;
            _prevRightHolding = rightNow;
        }

        private void OnGrab(PhysicsSnapshot physics)
        {
            _endurance.ApplyActionCost(
                ClimbingAction.Grab,
                physics.COG.CogOffsetPenalty,
                physics.Multipliers.EnduranceMult);
            _grip.ApplyBurst();
        }

        private void HandleChalkInput()
        {
            if (inputReader == null) return;
            if (!inputReader.ConsumeChalkPressed()) return;

            if (_grip.TryUseChalk())
            {
                _eventBus?.Publish(new GameEvent(EventType.ChalkUsed));
            }
        }

        private void PublishThresholdEvents()
        {
            if (_eventBus == null) return;

            CheckThreshold(_endurance.Ratio < 0.3f, ref _wasEnduranceLow, EventType.EnduranceLow);
            CheckThreshold(_grip.IsMicroSlipping, ref _wasMicroSlipping, EventType.GripMicroSlip);

            var leftLevel = _fatigue.GetLevel(_fatigue.LeftFatigue);
            var rightLevel = _fatigue.GetLevel(_fatigue.RightFatigue);

            CheckThreshold(leftLevel >= FatigueLevel.Warning, ref _wasFatigueWarningLeft, EventType.FatigueWarning);
            CheckThreshold(leftLevel >= FatigueLevel.Critical, ref _wasFatigueCriticalLeft, EventType.FatigueCritical);
            CheckThreshold(rightLevel >= FatigueLevel.Warning, ref _wasFatigueWarningRight, EventType.FatigueWarning);
            CheckThreshold(rightLevel >= FatigueLevel.Critical, ref _wasFatigueCriticalRight, EventType.FatigueCritical);
        }

        private void CheckThreshold(bool condition, ref bool previousState, EventType eventType)
        {
            if (condition && !previousState)
            {
                _eventBus?.Publish(new GameEvent(eventType));
            }

            previousState = condition;
        }

        private void HandleFatigueFailure()
        {
            if (playerHands == null || playerDriver == null) return;

            var leftLevel = _fatigue.GetLevel(_fatigue.LeftFatigue);
            var rightLevel = _fatigue.GetLevel(_fatigue.RightFatigue);

            if (leftLevel >= FatigueLevel.Failure && !_leftFatigueReleased && playerHands.IsHolding(HandSide.Left))
            {
                playerHands.Release(HandSide.Left);
                _leftFatigueReleased = true;
                _eventBus?.Publish(new GameEvent(EventType.FatigueFailure));
            }

            if (rightLevel >= FatigueLevel.Failure && !_rightFatigueReleased && playerHands.IsHolding(HandSide.Right))
            {
                playerHands.Release(HandSide.Right);
                _rightFatigueReleased = true;
                _eventBus?.Publish(new GameEvent(EventType.FatigueFailure));
            }

            // Reset flags when fatigue recovers below failure
            if (leftLevel < FatigueLevel.Failure) _leftFatigueReleased = false;
            if (rightLevel < FatigueLevel.Failure) _rightFatigueReleased = false;

            // If no support remains after fatigue release, trigger fall
            if (!playerHands.HasAnySupport
                && playerDriver.StateMachine?.CurrentState?.Id is PlayerStateId.GripStable or PlayerStateId.Reach)
            {
                playerDriver.StateMachine.Fire(PlayerTrigger.LostSupport);
            }
        }

        private float GetAverageGripTexture()
        {
            if (playerHands == null) return 1f;

            var sum = 0f;
            var count = 0;

            var left = playerHands.LeftCurrentHold;
            if (left != null)
            {
                var holdTex = left.data.gripTextureHold > 0f ? left.data.gripTextureHold : 1f;
                var wallTex = left.data.gripTextureWall > 0f ? left.data.gripTextureWall : 1f;
                sum += holdTex * wallTex;
                count++;
            }

            var right = playerHands.RightCurrentHold;
            if (right != null)
            {
                var holdTex = right.data.gripTextureHold > 0f ? right.data.gripTextureHold : 1f;
                var wallTex = right.data.gripTextureWall > 0f ? right.data.gripTextureWall : 1f;
                sum += holdTex * wallTex;
                count++;
            }

            return count > 0 ? sum / count : 1f;
        }

        private GripType GetHeldGripType(HandSide side)
        {
            if (playerHands == null) return GripType.Jug;
            var hold = side == HandSide.Left ? playerHands.LeftCurrentHold : playerHands.RightCurrentHold;
            return hold != null ? hold.data.gripType : GripType.Jug;
        }

        private ResourceSnapshot BuildSnapshot()
        {
            return new ResourceSnapshot
            {
                Endurance = _endurance.Current,
                EnduranceMax = _endurance.Max,
                EnduranceRatio = _endurance.Ratio,
                Grip = _grip.Current,
                GripMax = _grip.Max,
                GripRatio = _grip.Ratio,
                LeftArmFatigue = _fatigue.LeftFatigue,
                RightArmFatigue = _fatigue.RightFatigue,
                ChalkAvailable = _grip.IsChalkReady,
                ChalkCooldownRemaining = _grip.ChalkCooldownRemaining,
            };
        }

        public void ResetAll()
        {
            _endurance?.Reset();
            _grip?.Reset();
            _fatigue?.Reset();
            _wasEnduranceLow = false;
            _wasFatigueWarningLeft = false;
            _wasFatigueWarningRight = false;
            _wasFatigueCriticalLeft = false;
            _wasFatigueCriticalRight = false;
            _wasMicroSlipping = false;
            _leftFatigueReleased = false;
            _rightFatigueReleased = false;
        }
    }
}
