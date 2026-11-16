using System;
using System.Collections.Generic;
using Project.Core.Input;
using UnityEngine;

namespace UATRecords
{
    /// <summary>
    /// <see cref="IInputSource"/> implementation driven by a <see cref="Scenario"/>.
    /// Maintains the set of currently-held keys based on elapsed time vs. step windows,
    /// and surfaces edge-triggered "down" pulses for one query each.
    /// </summary>
    public sealed class ScriptedInputSource : IInputSource
    {
        private readonly Scenario _scenario;
        private readonly HashSet<KeyCode> _heldKeys = new();
        private readonly HashSet<KeyCode> _pendingKeyDown = new();
        private readonly HashSet<int> _pendingMouseDown = new();
        private readonly Dictionary<string, Transform> _aimCache = new();
        private float _elapsed;

        public ScriptedInputSource(Scenario scenario)
        {
            _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        }

        public Vector3 MousePosition
        {
            get
            {
                if (TryResolveAimAtHold(out var screenPos))
                {
                    return screenPos;
                }
                var (x, y) = ResolveMouseRatio();
                return new Vector3(Screen.width * Mathf.Clamp01(x), Screen.height * Mathf.Clamp01(y), 0f);
            }
        }

        private bool TryResolveAimAtHold(out Vector3 screenPos)
        {
            screenPos = default;
            string aimName = null;
            foreach (var step in _scenario.steps)
            {
                if (step.atSeconds > _elapsed) continue;
                if (!string.IsNullOrEmpty(step.aimAtHold)) aimName = step.aimAtHold;
            }
            if (string.IsNullOrEmpty(aimName)) return false;

            if (!_aimCache.TryGetValue(aimName, out var tr) || tr == null)
            {
                var go = GameObject.Find(aimName);
                if (go == null) return false;
                tr = go.transform;
                _aimCache[aimName] = tr;
            }

            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                // Fallback: any active camera.
                cam = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Camera>();
                if (cam == null) return false;
            }
            screenPos = cam.WorldToScreenPoint(tr.position);
            return true;
        }

        private (float x, float y) ResolveMouseRatio()
        {
            var x = _scenario.mouseScreenX;
            var y = _scenario.mouseScreenY;
            foreach (var step in _scenario.steps)
            {
                if (step.atSeconds > _elapsed) continue;
                if (step.mouseScreenX >= 0f) x = step.mouseScreenX;
                if (step.mouseScreenY >= 0f) y = step.mouseScreenY;
            }
            return (x, y);
        }

        /// <summary>Call once per frame BEFORE consumers read inputs (PlaytestBootstrap drives this).</summary>
        public void Tick(float deltaTime)
        {
            var previous = _elapsed;
            _elapsed += deltaTime;
            RecomputeHeldKeys(previous, _elapsed);
        }

        public bool GetKey(KeyCode key) => _heldKeys.Contains(key);

        public bool GetKeyDown(KeyCode key)
        {
            return _pendingKeyDown.Remove(key);
        }

        public bool GetMouseButtonDown(int button)
        {
            return _pendingMouseDown.Remove(button);
        }

        public bool GetMouseButton(int button) => false;

        public float ScrollDelta => 0f;

        public IReadOnlyCollection<KeyCode> HeldKeysSnapshot => _heldKeys;

        private void RecomputeHeldKeys(float fromT, float toT)
        {
            _heldKeys.Clear();
            foreach (var step in _scenario.steps)
            {
                var start = step.atSeconds;
                var end = step.atSeconds + Mathf.Max(step.durationSeconds, 0f);
                var wasInside = fromT >= start && fromT <= end;
                var isInside = toT >= start && toT <= end;

                if (TryParseKey(step.key, out var keyCode))
                {
                    if (isInside) _heldKeys.Add(keyCode);
                    if (!wasInside && isInside) _pendingKeyDown.Add(keyCode);
                }
                else if (step.mouseButton >= 0 && !wasInside && isInside)
                {
                    _pendingMouseDown.Add(step.mouseButton);
                }
            }
        }

        private static bool TryParseKey(string name, out KeyCode keyCode)
        {
            keyCode = default;
            if (string.IsNullOrEmpty(name)) return false;
            return Enum.TryParse(name, ignoreCase: true, out keyCode);
        }
    }
}
