using Project.Features.Gym.Domain;
using UnityEngine;

namespace Project.Features.Gym.Presentation
{
    /// <summary>
    /// Tuning of problem generation (variables_gameplay.md, "Problem generation"): per-circuit move
    /// spacing and hold choice, and the climber sizes every generated problem must suit.
    /// </summary>
    [CreateAssetMenu(fileName = "GymTuning", menuName = "Project/Gym Tuning")]
    public sealed class GymTuning : ScriptableObject
    {
        public CircuitParameters[] circuits = CircuitParameters.Defaults();

        [Range(0.8f, 1f)] public float smallestClimberScale = 0.94f;
        [Range(1f, 1.2f)] public float largestClimberScale = 1.04f;
    }
}
