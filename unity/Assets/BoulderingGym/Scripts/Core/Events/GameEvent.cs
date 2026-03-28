using System;

namespace Project.Core.Events
{
    public sealed class GameEvent
    {
        public GameEvent(EventType type, object payload = null)
        {
            Type = type;
            Payload = payload;
            TimestampUtc = DateTime.UtcNow;
        }

        public EventType Type { get; }

        public object Payload { get; }

        public DateTime TimestampUtc { get; }
    }
}
