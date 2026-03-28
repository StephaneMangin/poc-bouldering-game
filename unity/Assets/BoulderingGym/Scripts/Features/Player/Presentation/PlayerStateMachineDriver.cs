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

        private HoldComponent[] _holds;

        public PlayerStateMachine StateMachine { get; private set; }

        public PlayerHandsController PlayerHands => playerHands;

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
            _holds = holdRegistry != null ? holdRegistry.GetAllHolds() : FindObjectsByType<HoldComponent>(FindObjectsSortMode.None);
            if (inputReader == null)
            {
                inputReader = GetComponent<InputReader>();
            }
        }

        private void Update()
        {
            if (StateMachine == null || inputReader == null)
            {
                return;
            }

            if (inputReader.ConsumeLeftGrabPressed())
            {
                TryGrab(HandSide.Left);
            }

            if (inputReader.ConsumeRightGrabPressed())
            {
                TryGrab(HandSide.Right);
            }

            if (inputReader.ConsumeReleaseAllPressed())
            {
                ReleaseBothHands();
            }

            if (inputReader.ConsumeReleaseLeftPressed())
            {
                ReleaseHand(HandSide.Left);
            }

            if (inputReader.ConsumeReleaseRightPressed())
            {
                ReleaseHand(HandSide.Right);
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

        private void TryGrab(HandSide handSide)
        {
            if (playerHands != null && playerHands.IsHolding(handSide))
            {
                LastActionStatus = $"{handSide} hand is already holding a hold. Release it first.";
                return;
            }

            StateMachine.Fire(PlayerTrigger.GrabRequested);
            var wallNormal = wallNormalReference != null ? wallNormalReference.forward : Vector3.forward;
            if (playerHands != null && playerHands.TryGrab(handSide, _holds, wallNormal, out var hold))
            {
                StateMachine.Fire(PlayerTrigger.GrabSucceeded);
                LastActionStatus = $"{handSide} hand grabbed {hold.name} | Layout-safe: ZQSD/WASD/arrows move, J left, E/L right, mouse L/R grab";
                return;
            }

            StateMachine.Fire(PlayerTrigger.GrabFailed);
            LastActionStatus = $"{handSide} hand grab failed | Layout-safe: ZQSD/WASD/arrows move, J left, E/L right, mouse L/R grab";
        }

        private void ReleaseBothHands()
        {
            if (playerHands == null)
            {
                return;
            }

            var released = playerHands.ReleaseAll();
            if (!released)
            {
                LastActionStatus = "No hands to release";
                return;
            }

            if (playerHands.HasAnySupport)
            {
                StateMachine.Fire(PlayerTrigger.ReleaseRequested);
                LastActionStatus = $"Released to {playerHands.SupportCount} support(s)";
                return;
            }

            StateMachine.Fire(PlayerTrigger.LostSupport);
            LastActionStatus = "Released all supports -> falling";
        }

        private void ReleaseHand(HandSide handSide)
        {
            if (playerHands == null)
            {
                return;
            }

            if (!playerHands.Release(handSide))
            {
                LastActionStatus = $"{handSide} hand had no hold";
                return;
            }

            if (playerHands.HasAnySupport)
            {
                StateMachine.Fire(PlayerTrigger.ReleaseRequested);
                LastActionStatus = $"Released {handSide} hand | Remaining supports: {playerHands.SupportCount}";
                return;
            }

            StateMachine.Fire(PlayerTrigger.LostSupport);
            LastActionStatus = $"Released {handSide} hand | No supports left -> falling";
        }
    }
}
