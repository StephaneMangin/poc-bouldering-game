using Project.Core.Domain;
using Project.Core.Events;
using Project.Core.Input;
using Project.Features.Climbing.Application;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class ClimbingStateMachineDriver : MonoBehaviour
    {
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private PlayerHandsController playerHands;
        [SerializeField] private Transform wallNormalReference;
        [SerializeField] private HoldRegistry holdRegistry;
        [SerializeField] private InputReader inputReader;

        private HoldComponent[] _holds;

        public ClimbingStateMachine StateMachine { get; private set; }

        public PlayerHandsController PlayerHands => playerHands;

        public string LastActionStatus { get; private set; } = "Ready";

        private void Awake()
        {
            var context = new ClimbingContext
            {
                TuningConfig = tuningConfig,
                EventBus = new InMemoryEventBus(),
                PlayerHands = playerHands,
                WallNormalReference = wallNormalReference,
            };

            StateMachine = new ClimbingStateMachine(context);
            _holds = holdRegistry != null ? holdRegistry.GetAllHolds() : FindObjectsByType<HoldComponent>(FindObjectsSortMode.None);
            if (inputReader == null)
            {
                inputReader = GetComponent<InputReader>();
            }
        }

        private void Update()
        {
            if (StateMachine == null)
            {
                return;
            }

            if (inputReader != null && inputReader.ConsumeLeftGrabPressed())
            {
                TryGrab(HandSide.Left);
            }

            if (inputReader != null && inputReader.ConsumeRightGrabPressed())
            {
                TryGrab(HandSide.Right);
            }

            if (inputReader != null && inputReader.ConsumeReleaseAllPressed())
            {
                ReleaseBothHands();
            }

            if (inputReader != null && inputReader.ConsumeReleaseLeftPressed())
            {
                ReleaseHand(HandSide.Left);
            }

            if (inputReader != null && inputReader.ConsumeReleaseRightPressed())
            {
                ReleaseHand(HandSide.Right);
            }

            if (inputReader != null && inputReader.ConsumeFallPressed())
            {
                StateMachine.Fire(ClimbingTrigger.LostSupport);
            }

            if (inputReader != null && inputReader.ConsumeLandPressed())
            {
                StateMachine.Fire(ClimbingTrigger.Landed);
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

            StateMachine.Fire(ClimbingTrigger.GrabRequested);
            var wallNormal = wallNormalReference != null ? wallNormalReference.forward : Vector3.forward;
            if (playerHands != null && playerHands.TryGrab(handSide, _holds, wallNormal, out var hold))
            {
                StateMachine.Fire(ClimbingTrigger.GrabSucceeded);
                LastActionStatus = $"{handSide} hand grabbed {hold.name} | Layout-safe: ZQSD/WASD/arrows move, J left, E/L right, mouse L/R grab";
                return;
            }

            StateMachine.Fire(ClimbingTrigger.GrabFailed);
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
                StateMachine.Fire(ClimbingTrigger.ReleaseRequested);
                LastActionStatus = $"Released to {playerHands.SupportCount} support(s)";
                return;
            }

            StateMachine.Fire(ClimbingTrigger.LostSupport);
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
                StateMachine.Fire(ClimbingTrigger.ReleaseRequested);
                LastActionStatus = $"Released {handSide} hand | Remaining supports: {playerHands.SupportCount}";
                return;
            }

            StateMachine.Fire(ClimbingTrigger.LostSupport);
            LastActionStatus = $"Released {handSide} hand | No supports left -> falling";
        }
    }
}
