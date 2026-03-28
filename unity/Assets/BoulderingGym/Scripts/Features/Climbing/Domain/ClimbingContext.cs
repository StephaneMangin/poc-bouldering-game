using Project.Core.Domain;
using Project.Core.Events;
using Project.Features.Climbing.Presentation;
using UnityEngine;

namespace Project.Features.Climbing.Domain
{
    public sealed class ClimbingContext
    {
        public TuningConfig TuningConfig;

        public IEventBus EventBus;

        public PlayerHandsController PlayerHands;

        public Transform WallNormalReference;
    }
}
