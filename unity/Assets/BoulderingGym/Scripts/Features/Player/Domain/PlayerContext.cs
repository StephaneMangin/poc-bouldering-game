using Project.Core.Domain;
using Project.Core.Events;
using UnityEngine;

namespace Project.Features.Player.Domain
{
    public sealed class PlayerContext
    {
        public TuningConfig TuningConfig;

        public IEventBus EventBus;

        public IPlayerHands PlayerHands;

        public Transform WallNormalReference;
    }
}
