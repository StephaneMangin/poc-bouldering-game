using System;
using System.Globalization;
using System.IO;
using Project.Core.Events;
using Project.Core.Input;
using UnityEngine;

namespace UATRecords
{
    /// <summary>
    /// Single MonoBehaviour entry point: loads a <see cref="Scenario"/>, swaps the
    /// scene's <see cref="InputReader"/> source with a <see cref="ScriptedInputSource"/>,
    /// streams telemetry + screenshots into an artefacts directory, then quits.
    /// </summary>
    /// <remarks>
    /// Read CLI args via <see cref="Configure"/> before adding to a GameObject — the
    /// component itself is engine-agnostic about how the path/directory are obtained.
    /// </remarks>
    [DefaultExecutionOrder(-10000)]
    public sealed class PlaytestBootstrap : MonoBehaviour
    {
        // Serialized so values survive the domain reload triggered by
        // EditorApplication.EnterPlaymode when launched from PlaytestCLI.
        [SerializeField] private Scenario _scenario;
        [SerializeField] private string _artefactsDir;
        private ScriptedInputSource _input;
        private ScreenshotCapture _screenshots;
        private TelemetrySink _sink;
        private float _elapsed;
        private bool _finished;

        public void Configure(Scenario scenario, string artefactsDir)
        {
            _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            _artefactsDir = artefactsDir ?? throw new ArgumentNullException(nameof(artefactsDir));
        }

        private void Start()
        {
            if (_scenario == null)
            {
                Debug.LogError("[UATRecords] PlaytestBootstrap.Start: scenario not configured.");
                enabled = false;
                return;
            }

            Directory.CreateDirectory(_artefactsDir);
            _input = new ScriptedInputSource(_scenario);
            _screenshots = new ScreenshotCapture(_artefactsDir, _scenario.screenshotEveryNFrames);
            _sink = new TelemetrySink(_artefactsDir, FindSceneEventBus());
            InstallScriptedInputOnAllReaders();
            Debug.Log($"[UATRecords] Scenario '{_scenario.name}' started → {_artefactsDir}");
        }

        private void Update()
        {
            if (_finished || _scenario == null) return;
            _input.Tick(Time.deltaTime);
            _elapsed += Time.deltaTime;
            EmitFrame();
            if (_elapsed >= _scenario.durationSeconds)
            {
                Finish();
            }
        }

        private void LateUpdate()
        {
            if (_finished) return;
            _screenshots?.TickFrame(Time.frameCount);
        }

        private void OnDestroy()
        {
            _sink?.Dispose();
        }

        private void EmitFrame()
        {
            var frame = new TelemetryFrame
            {
                frame = Time.frameCount,
                t = _elapsed,
                playerPos = transform.position,
                playerVel = Vector3.zero,
                fsmState = "",
                fatigueLeft = 0f,
                fatigueRight = 0f,
            };
            foreach (var k in _input.HeldKeysSnapshot)
            {
                frame.heldKeys.Add(k.ToString());
            }
            _sink.WriteFrame(frame);
        }

        private void Finish()
        {
            _finished = true;
            WriteReport();
            _sink?.Dispose();
            Debug.Log($"[UATRecords] Scenario '{_scenario.name}' finished after {_elapsed:F2}s.");
#if UNITY_EDITOR
            // Application.Quit is a no-op in the Editor's play mode.
            // In -batchmode the playModeStateChanged hook for EnteredEditMode is
            // unreliable (the editor update pump can stall after isPlaying=false),
            // so we schedule EditorApplication.Exit via delayCall ourselves.
            UnityEditor.EditorApplication.isPlaying = false;
            UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit(0);
#endif
        }

        private void WriteReport()
        {
            var path = Path.Combine(_artefactsDir, "report.md");
            var dur = _elapsed.ToString("F2", CultureInfo.InvariantCulture);
            var content =
                $"# UAT Record — {_scenario.name}\n\n" +
                $"- Scene: `{_scenario.scene}`\n" +
                $"- Duration: {dur}s (target {_scenario.durationSeconds}s)\n" +
                $"- Screenshots: {_screenshots.CapturedCount}\n" +
                $"- Steps: {_scenario.steps?.Count ?? 0}\n";
            File.WriteAllText(path, content);
        }

        private void InstallScriptedInputOnAllReaders()
        {
            // FindObjectsByType is the 2022.3+ replacement for FindObjectsOfType.
            var readers = FindObjectsByType<InputReader>(FindObjectsSortMode.None);
            foreach (var reader in readers)
            {
                reader.OverrideSource(_input);
            }
            Debug.Log($"[UATRecords] Installed ScriptedInputSource on {readers.Length} InputReader(s).");
        }

        private static IEventBus FindSceneEventBus()
        {
            // Scenes typically expose the bus via a singleton or service locator.
            // Until a GameContext is finalised, return null — TelemetrySink degrades to frames-only.
            return null;
        }
    }
}
