using System;
using System.IO;
using UnityEngine;

namespace UATRecords
{
    /// <summary>Load + validate a <see cref="Scenario"/> from a JSON file path.</summary>
    public static class ScenarioLoader
    {
        public static Scenario LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Scenario path is empty.", nameof(path));
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Scenario file not found: {path}", path);
            }

            var json = File.ReadAllText(path);
            return ParseAndValidate(json, path);
        }

        public static Scenario Parse(string json)
        {
            return ParseAndValidate(json, "<inline>");
        }

        private static Scenario ParseAndValidate(string json, string origin)
        {
            Scenario scenario;
            try
            {
                scenario = JsonUtility.FromJson<Scenario>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Scenario JSON parse failed ({origin}): {ex.Message}", ex);
            }

            if (scenario == null)
            {
                throw new InvalidOperationException($"Scenario JSON parsed to null ({origin}).");
            }
            if (scenario.durationSeconds <= 0f)
            {
                throw new InvalidOperationException($"Scenario.durationSeconds must be > 0 ({origin}).");
            }
            ValidateSteps(scenario, origin);
            return scenario;
        }

        private static void ValidateSteps(Scenario scenario, string origin)
        {
            if (scenario.steps == null)
            {
                return;
            }
            for (var i = 0; i < scenario.steps.Count; i++)
            {
                var step = scenario.steps[i];
                if (step == null)
                {
                    throw new InvalidOperationException($"Scenario step #{i} is null ({origin}).");
                }
                var hasKey = !string.IsNullOrEmpty(step.key);
                var hasMouse = step.mouseButton >= 0;
                if (hasKey == hasMouse)
                {
                    throw new InvalidOperationException(
                        $"Scenario step #{i} must define exactly one of 'key' or 'mouseButton' ({origin}).");
                }
                if (step.atSeconds < 0f || step.durationSeconds < 0f)
                {
                    throw new InvalidOperationException(
                        $"Scenario step #{i} has negative timing ({origin}).");
                }
            }
        }
    }
}
