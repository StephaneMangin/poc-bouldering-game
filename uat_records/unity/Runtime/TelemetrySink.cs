using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Project.Core.Events;
using UnityEngine;
// Disambiguate from UnityEngine.EventType (legacy IMGUI).
using EventType = Project.Core.Events.EventType;

namespace UATRecords
{
    /// <summary>
    /// Append-only telemetry sink — writes one JSON line per frame to
    /// <c>telemetry.jsonl</c> and one line per gameplay event to <c>events.jsonl</c>.
    /// Designed to be cheap (single StreamWriter, line-buffered) so a 30s run
    /// at 60 FPS produces ~1.8k frames without dropping samples.
    /// </summary>
    public sealed class TelemetrySink : IDisposable
    {
        private readonly StreamWriter _frames;
        private readonly StreamWriter _events;
        private readonly IEventBus _bus;
        private readonly Dictionary<EventType, Action<GameEvent>> _handlers = new();
        private bool _disposed;

        public TelemetrySink(string outputDir, IEventBus bus = null)
        {
            Directory.CreateDirectory(outputDir);
            _frames = new StreamWriter(Path.Combine(outputDir, "telemetry.jsonl"), append: false, Encoding.UTF8);
            _events = new StreamWriter(Path.Combine(outputDir, "events.jsonl"), append: false, Encoding.UTF8);
            _bus = bus;
            if (_bus != null)
            {
                SubscribeAllEvents();
            }
        }

        public void WriteFrame(TelemetryFrame frame)
        {
            if (_disposed || frame == null) return;
            _frames.WriteLine(JsonUtility.ToJson(frame));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnsubscribeAllEvents();
            _frames.Flush();
            _frames.Dispose();
            _events.Flush();
            _events.Dispose();
        }

        private void SubscribeAllEvents()
        {
            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                var capturedType = type;
                Action<GameEvent> handler = evt => WriteEvent(capturedType, evt);
                _handlers[capturedType] = handler;
                _bus.Subscribe(capturedType, handler);
            }
        }

        private void UnsubscribeAllEvents()
        {
            if (_bus == null) return;
            foreach (var kv in _handlers)
            {
                _bus.Unsubscribe(kv.Key, kv.Value);
            }
            _handlers.Clear();
        }

        private void WriteEvent(EventType type, GameEvent evt)
        {
            if (_disposed) return;
            // Hand-rolled JSON: payload type is heterogeneous (boxed object), JsonUtility cannot serialise it.
            var payload = evt?.Payload != null ? evt.Payload.ToString() : "null";
            var line = $"{{\"t\":{Time.time.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}," +
                       $"\"type\":\"{type}\"," +
                       $"\"payload\":\"{EscapeJson(payload)}\"}}";
            _events.WriteLine(line);
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
