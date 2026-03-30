using Project.Core.Domain;
using Project.Core.Events;
using Project.Core.Input;
using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Presentation;
using Project.Features.Player.Application;
using Project.Features.Player.Domain;
using UnityEngine;

namespace Project.Features.Player.Presentation
{
    public sealed class PlayerStateMachineDriver : MonoBehaviour
    {
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private PlayerHandsController playerHands;
        [SerializeField] private Transform wallNormalReference;
        [SerializeField] private HoldRegistry holdRegistry;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private HoldTargetingSystem holdTargeting;

        private LimbSuggestionSystem _limbSuggestion;

        public PlayerStateMachine StateMachine { get; private set; }

        public PlayerHandsController PlayerHands => playerHands;

        public LimbSuggestionSystem LimbSuggestion => _limbSuggestion;

        public LimbId? SuggestedLimb { get; private set; }

        public string LastActionStatus { get; private set; } = "Ready";

        private void Awake()
        {
            var context = new PlayerContext
            {
                TuningConfig = tuningConfig,
                EventBus = new InMemoryEventBus(),
                PlayerHands = playerHands,
                WallNormalReference = wallNormalReference,
            };

            StateMachine = new PlayerStateMachine(context);
            _limbSuggestion = new LimbSuggestionSystem();

            if (inputReader == null)
            {
                inputReader = GetComponent<InputReader>();
            }

            if (holdTargeting == null)
            {
                holdTargeting = GetComponent<HoldTargetingSystem>();
            }
        }

        private void Update()
        {
            if (StateMachine == null || inputReader == null)
            {
                return;
            }

            var state = StateMachine.CurrentState.Id;

            // Context-dependent input routing:
            // IdleGround: limb keys only consumed when a hold is targeted (so WASD keys are free for movement)
            // Reach/GripStable: limb keys always consumed (body follows holds, no WASD movement)
            if (state == PlayerStateId.Reach || state == PlayerStateId.GripStable)
            {
                HandlePointAndPlaceInput();
            }
            else if (state == PlayerStateId.IdleGround
                     && holdTargeting != null && holdTargeting.IsTargetValid)
            {
                HandlePointAndPlaceInput();
            }

            if (inputReader.ConsumeReleaseAllPressed())
            {
                ReleaseAllLimbs();
            }

            if (inputReader.ConsumeFallPressed())
            {
                StateMachine.Fire(PlayerTrigger.LostSupport);
            }

            if (inputReader.ConsumeLandPressed())
            {
                StateMachine.Fire(PlayerTrigger.Landed);
                LastActionStatus = "Landed";
            }
        }

        private void HandlePointAndPlaceInput()
        {
            var hasTarget = holdTargeting != null && holdTargeting.IsTargetValid;

            SuggestedLimb = hasTarget
                ? _limbSuggestion?.SuggestLimb(playerHands, holdTargeting.TargetedHold)
                : null;

            var activeLimb = inputReader.ConsumeActiveLimb();
            if (activeLimb.HasValue)
            {
                if (hasTarget)
                {
                    TryPlaceLimb(activeLimb.Value, holdTargeting.TargetedHold);
                }
                else if (playerHands != null && playerHands.IsLimbHolding(activeLimb.Value))
                {
                    ReleaseLimb(activeLimb.Value);
                }

                return;
            }

            if (inputReader.ConsumeQuickPlacePressed() && SuggestedLimb.HasValue && hasTarget)
            {
                TryPlaceLimb(SuggestedLimb.Value, holdTargeting.TargetedHold);
            }
        }

        private void TryPlaceLimb(LimbId limb, HoldComponent targetHold)
        {
            if (playerHands == null || targetHold == null)
            {
                return;
            }

            if (playerHands.IsLimbHolding(limb))
            {
                playerHands.ReleaseLimb(limb);
            }

            StateMachine.Fire(PlayerTrigger.GrabRequested);
            if (playerHands.TryPlaceLimb(limb, targetHold))
            {
                _limbSuggestion?.RecordPlacement(limb);
                StateMachine.Fire(PlayerTrigger.GrabSucceeded);
                LastActionStatus = $"{limb} placed on {targetHold.name}";
                return;
            }

            StateMachine.Fire(PlayerTrigger.GrabFailed);
            LastActionStatus = $"{limb} cannot reach {targetHold.name}";
        }

        private void ReleaseLimb(LimbId limb)
        {
            if (playerHands == null)
            {
                return;
            }

            playerHands.ReleaseLimb(limb);

            if (playerHands.HasAnySupport)
            {
                StateMachine.Fire(PlayerTrigger.ReleaseRequested);
                LastActionStatus = $"Released {limb} | {playerHands.SupportCount} support(s)";
                return;
            }

            StateMachine.Fire(PlayerTrigger.LostSupport);
            LastActionStatus = $"Released {limb} | No supports -> falling";
        }

        private void ReleaseAllLimbs()
        {
            if (playerHands == null)
            {
                return;
            }

            var released = playerHands.ReleaseAll();
            if (!released)
            {
                LastActionStatus = "Nothing to release";
                return;
            }

            if (playerHands.HasAnySupport)
            {
                StateMachine.Fire(PlayerTrigger.ReleaseRequested);
                LastActionStatus = $"Released to {playerHands.SupportCount} support(s)";
                return;
            }

            StateMachine.Fire(PlayerTrigger.LostSupport);
            LastActionStatus = "Released all -> falling";
        }
    }
}
