using System;
using System.Collections.Generic;
using UnityEngine;

namespace UATRecords
{
    /// <summary>
    /// Scenario root — declarative JSON description of a playtest session.
    /// Marshalled by <see cref="ScenarioLoader"/> via <c>JsonUtility</c>.
    /// </summary>
    [Serializable]
    public sealed class Scenario
    {
        public string name = "unnamed";
        public string scene = "VerticalPrototype";
        public float durationSeconds = 10f;
        public int screenshotEveryNFrames = 60;
        // Screen-space ratio (0..1) used by ScriptedInputSource.MousePosition. Default = screen center.
        public float mouseScreenX = 0.5f;
        public float mouseScreenY = 0.5f;
        public List<ScenarioStep> steps = new();
    }

    /// <summary>
    /// One scripted input event. Either a key (down/hold/up) or a mouse button.
    /// Exactly one of <see cref="key"/> / <see cref="mouseButton"/> must be set.
    /// </summary>
    [Serializable]
    public sealed class ScenarioStep
    {
        public float atSeconds;
        public float durationSeconds;
        public string key;
        public int mouseButton = -1;
        // Optional per-step mouse position (screen ratio). Negative = inherit previous / scenario default.
        public float mouseScreenX = -1f;
        public float mouseScreenY = -1f;
        // Optional GameObject name to aim at (resolved at runtime via Camera.WorldToScreenPoint).
        // When set and the step is active, overrides mouseScreenX/Y.
        public string aimAtHold = "";
    }

    /// <summary>Single frame snapshot written to <c>telemetry.jsonl</c>.</summary>
    [Serializable]
    public sealed class TelemetryFrame
    {
        public int frame;
        public float t;
        public Vector3 playerPos;
        public Vector3 playerVel;
        public string fsmState;
        public float fatigueLeft;
        public float fatigueRight;
        public List<string> heldKeys = new();
    }
}
