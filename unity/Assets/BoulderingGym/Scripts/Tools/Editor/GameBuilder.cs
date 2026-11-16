using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Project.Tools.Editor
{
    /// <summary>
    /// Builds the Linux player into unity/Builds/Linux. In batch mode the editor exits with 1 on failure.
    /// Batch: -executeMethod Project.Tools.Editor.GameBuilder.BuildLinux -quit
    /// </summary>
    public static class GameBuilder
    {
        private const string OutputPath = "Builds/Linux/BlockOutRennes.x86_64";

        [MenuItem("Project/Game/Build Linux Player")]
        public static void BuildLinux()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { GameSetup.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None,
            });
            var summary = report.summary;
            Debug.Log($"[GameBuilder] {summary.result}: {summary.outputPath}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalWarnings} warnings.");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
