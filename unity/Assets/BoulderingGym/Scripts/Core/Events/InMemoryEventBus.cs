using System;
using System.Collections.Generic;

namespace Project.Core.Events
{
    public sealed class InMemoryEventBus : IEventBus
    {
        private readonly Dictionary<EventType, List<Action<GameEvent>>> _handlers = new();

        public void Subscribe(EventType eventType, Action<GameEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!_handlers.TryGetValue(eventType, out var list))
            {
                list = new List<Action<GameEvent>>();
                _handlers[eventType] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        public void Unsubscribe(EventType eventType, Action<GameEvent> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (_handlers.TryGetValue(eventType, out var list))
            {
                list.Remove(handler);
            }
        }

        public void Publish(GameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            if (!_handlers.TryGetValue(gameEvent.Type, out var list))
            {
                return;
            }

            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                handler.Invoke(gameEvent);
            }
        }
    }
}
