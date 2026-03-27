using System;

namespace Project.Core.Events
{
    public interface IEventBus
    {
        void Subscribe(EventType eventType, Action<GameEvent> handler);

        void Unsubscribe(EventType eventType, Action<GameEvent> handler);

        void Publish(GameEvent gameEvent);
    }
}
