using System;
using System.Collections.Generic;

namespace Project.Features.Gym.Domain
{
    /// <summary>Tunable generation parameters of one color circuit (see variables_gameplay.md).</summary>
    [Serializable]
    public sealed class CircuitParameters
    {
        public string colorId;

        /// <summary>Vertical distance between consecutive hand holds, meters.</summary>
        public float moveStepMin;
        public float moveStepMax;

        /// <summary>Sideways distance of a hand hold from the problem line, meters.</summary>
        public float lateralMin;
        public float lateralMax;

        public HoldKind[] handKinds;
        public HoldKind[] footKinds;

        /// <summary>Chance that a hand hold gets a dedicated foothold below it.</summary>
        public float footDensity;

        public float holdScale;

        /// <summary>Relative chance to set this circuit on vertical/slab, overhanging and steep faces.</summary>
        public float weightVertical;
        public float weightOverhang;
        public float weightSteep;

        public static CircuitParameters[] Defaults() => new[]
        {
            new CircuitParameters
            {
                colorId = "orange", moveStepMin = 0.38f, moveStepMax = 0.52f, lateralMin = 0.10f, lateralMax = 0.28f,
                handKinds = new[] { HoldKind.Jug, HoldKind.Jug, HoldKind.Macro, HoldKind.Edge },
                footKinds = new[] { HoldKind.Edge, HoldKind.Jug }, footDensity = 1f, holdScale = 1.15f,
                weightVertical = 3f, weightOverhang = 1.5f, weightSteep = 0.6f,
            },
            new CircuitParameters
            {
                colorId = "blue", moveStepMin = 0.48f, moveStepMax = 0.62f, lateralMin = 0.15f, lateralMax = 0.38f,
                handKinds = new[] { HoldKind.Jug, HoldKind.Edge, HoldKind.Macro, HoldKind.Sloper },
                footKinds = new[] { HoldKind.Edge, HoldKind.Foothold }, footDensity = 0.9f, holdScale = 1f,
                weightVertical = 3f, weightOverhang = 2.5f, weightSteep = 1.5f,
            },
            new CircuitParameters
            {
                colorId = "red", moveStepMin = 0.58f, moveStepMax = 0.72f, lateralMin = 0.20f, lateralMax = 0.45f,
                handKinds = new[] { HoldKind.Edge, HoldKind.Crimp, HoldKind.Pinch, HoldKind.Sloper, HoldKind.Jug },
                footKinds = new[] { HoldKind.Foothold, HoldKind.Edge }, footDensity = 0.8f, holdScale = 0.95f,
                weightVertical = 2f, weightOverhang = 3f, weightSteep = 2.5f,
            },
            new CircuitParameters
            {
                colorId = "white", moveStepMin = 0.66f, moveStepMax = 0.82f, lateralMin = 0.25f, lateralMax = 0.52f,
                handKinds = new[] { HoldKind.Crimp, HoldKind.Sloper, HoldKind.Pinch, HoldKind.Pocket },
                footKinds = new[] { HoldKind.Foothold }, footDensity = 0.7f, holdScale = 0.9f,
                weightVertical = 1f, weightOverhang = 2f, weightSteep = 2.5f,
            },
            new CircuitParameters
            {
                colorId = "black", moveStepMin = 0.74f, moveStepMax = 0.92f, lateralMin = 0.30f, lateralMax = 0.60f,
                handKinds = new[] { HoldKind.Crimp, HoldKind.Sloper, HoldKind.Pocket, HoldKind.Pinch },
                footKinds = new[] { HoldKind.Foothold }, footDensity = 0.6f, holdScale = 0.85f,
                weightVertical = 0.5f, weightOverhang = 1f, weightSteep = 2f,
            },
            new CircuitParameters
            {
                colorId = "lime", moveStepMin = 0.82f, moveStepMax = 1.00f, lateralMin = 0.35f, lateralMax = 0.65f,
                handKinds = new[] { HoldKind.Crimp, HoldKind.Pocket, HoldKind.Sloper },
                footKinds = new[] { HoldKind.Foothold }, footDensity = 0.5f, holdScale = 0.8f,
                weightVertical = 0.2f, weightOverhang = 0.4f, weightSteep = 0.8f,
            },
        };

        public FloatRange MoveStep => new(moveStepMin, moveStepMax);

        public FloatRange Lateral => new(lateralMin, lateralMax);
    }

    /// <summary>
    /// Color circuit of the Block'Out Rennes grading board: the hold color gives the grade band,
    /// ranked from the easiest (orange) to the hardest (lime).
    /// </summary>
    public sealed class Circuit
    {
        private static readonly (string id, string name, string label, string[] grades)[] Board =
        {
            ("orange", "Orange", "Fairly easy", new[] { "4A", "4B", "4C" }),
            ("blue", "Blue", "Hard", new[] { "5A", "5B", "5C" }),
            ("red", "Red", "Very hard", new[] { "6A", "6B" }),
            ("white", "White", "Extremely hard", new[] { "6C", "7A" }),
            ("black", "Black", "Mega hard", new[] { "7A+", "7B" }),
            ("lime", "Lime", "Impossible", new[] { "7B+", "7C" }),
        };

        private Circuit(int rank, string colorId, string displayName, string label, string[] grades, CircuitParameters parameters)
        {
            Rank = rank;
            ColorId = colorId;
            DisplayName = displayName;
            Label = label;
            Grades = grades;
            Parameters = parameters;
        }

        public int Rank { get; }

        public string ColorId { get; }

        public string DisplayName { get; }

        public string Label { get; }

        public IReadOnlyList<string> Grades { get; }

        public CircuitParameters Parameters { get; }

        public static IReadOnlyList<Circuit> CreateAll(IReadOnlyList<CircuitParameters> parameters)
        {
            var circuits = new List<Circuit>();
            for (var rank = 0; rank < Board.Length; rank++)
            {
                var entry = Board[rank];
                circuits.Add(new Circuit(rank, entry.id, entry.name, entry.label, entry.grades, Find(parameters, entry.id)));
            }

            return circuits;
        }

        private static CircuitParameters Find(IReadOnlyList<CircuitParameters> parameters, string colorId)
        {
            foreach (var candidate in parameters)
            {
                if (candidate.colorId == colorId)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException($"No generation parameters for circuit '{colorId}'.");
        }
    }
}
