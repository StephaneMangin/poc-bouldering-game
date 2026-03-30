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

        /// <summary>
        /// Dispatch synchronously. Handlers MUST NOT Subscribe/Unsubscribe during dispatch
        /// (no defensive snapshot — iteration is by index for zero per-publish allocation).
        /// </summary>
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

            // Iterate by index — no allocation, contract above forbids mutation during dispatch.
            for (var i = 0; i < list.Count; i++)
            {
                list[i].Invoke(gameEvent);
            }
        }
    }
}
