using Project.Core.Domain;
using Project.Core.Events;
using Project.Features.Climbing.Presentation;
using UnityEngine;

namespace Project.Features.Player.Domain
{
    public sealed class PlayerContext
    {
        public TuningConfig TuningConfig;

        public IEventBus EventBus;

        public PlayerHandsController PlayerHands;

        public Transform WallNormalReference;
    }
}
