using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UATRecords.Editor
{
    /// <summary>
    /// Batchmode entry point. Drives the full lifecycle: load scenario, open
    /// scene, spawn <see cref="PlaytestBootstrap"/>, enter play mode, and exit
    /// the Editor when the scenario quits (or a safety timeout elapses).
    /// </summary>
    /// <remarks>
    /// Persists scenario + artefacts paths into <see cref="SessionState"/> so
    /// the bootstrap can recover them across the play-mode domain reload.
    /// The launcher MUST NOT pass <c>-quit</c>: shutdown is owned here.
    /// </remarks>
    public static class PlaytestCLI
    {
        public const string SessionKeyScenario = "UATRecords.ScenarioPath";
        public const string SessionKeyArtefacts = "UATRecords.ArtefactsDir";
        public const string SessionKeyDeadline = "UATRecords.DeadlineEpochMs";

        private const float TimeoutSafetyMarginSeconds = 30f;
        private static double _safetyDeadlineRealtime;

        public static void Run()
        {
            try
            {
                var scenarioPath = ReadCliArg("-scenario");
                var artefactsDir = ReadCliArg("-artefacts");
                if (string.IsNullOrEmpty(scenarioPath) || string.IsNullOrEmpty(artefactsDir))
                {
                    Debug.LogError("[UATRecords] Missing -scenario or -artefacts CLI arg.");
                    EditorApplication.Exit(2);
                    return;
                }

                Directory.CreateDirectory(artefactsDir);
                var scenario = ScenarioLoader.LoadFromFile(scenarioPath);
                PersistRunContext(scenarioPath, artefactsDir, scenario);
                OpenScenarioScene(scenario);
                SpawnBootstrap(scenario, artefactsDir);
                ScheduleShutdownHooks(scenario, artefactsDir);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UATRecords] PlaytestCLI.Run failed: {ex}");
                EditorApplication.Exit(3);
            }
        }

        private static void PersistRunContext(string scenarioPath, string artefactsDir, Scenario scenario)
        {
            SessionState.SetString(SessionKeyScenario, scenarioPath);
            SessionState.SetString(SessionKeyArtefacts, artefactsDir);
            // Used by the bootstrap if it loses its Configure() values across a reload.
            var deadlineMs = scenario.durationSeconds * 1000f;
            SessionState.SetInt(SessionKeyDeadline, Mathf.RoundToInt(deadlineMs));
        }

        private static string ReadCliArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static void OpenScenarioScene(Scenario scenario)
        {
            var guids = AssetDatabase.FindAssets($"t:Scene {scenario.scene}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == scenario.scene)
                {
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    return;
                }
            }
            throw new FileNotFoundException($"Scene '{scenario.scene}' not found in project.");
        }

        private static void SpawnBootstrap(Scenario scenario, string artefactsDir)
        {
            var go = new GameObject("UATRecords.Bootstrap");
            var bs = go.AddComponent<PlaytestBootstrap>();
            bs.Configure(scenario, artefactsDir);
            var active = SceneManager.GetActiveScene();
            SceneManager.MoveGameObjectToScene(go, active);
            EditorSceneManager.MarkSceneDirty(active);
        }

        private static void ScheduleShutdownHooks(Scenario scenario, string artefactsDir)
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            _safetyDeadlineRealtime = EditorApplication.timeSinceStartup
                                      + scenario.durationSeconds
                                      + TimeoutSafetyMarginSeconds;
            EditorApplication.update += SafetyTimeoutTick;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // The bootstrap calls Application.Quit -> Editor leaves play mode.
            // Wait until we are fully back in Edit mode before exiting the Editor,
            // so TelemetrySink.Dispose has flushed the JSONL files.
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                CleanShutdown(0);
            }
        }

        private static void SafetyTimeoutTick()
        {
            if (EditorApplication.timeSinceStartup < _safetyDeadlineRealtime) return;
            Debug.LogError("[UATRecords] Safety timeout reached — forcing Editor exit.");
            CleanShutdown(4);
        }

        private static void CleanShutdown(int code)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= SafetyTimeoutTick;
            EditorApplication.Exit(code);
        }
    }
}
