using System;
using System.Collections.Generic;

namespace Project.Features.Gym.Domain
{
    /// <summary>
    /// Baked problems and volumes (Data/Gameplay/GymScenes/blockout_rennes_problems.json): generation
    /// takes seconds, so it runs once in the editor and the game loads the result.
    /// </summary>
    [Serializable]
    public sealed class ContentDefinition
    {
        public ProblemDefinition[] problems;
        public PlacedVolumeDefinition[] volumes;
    }

    [Serializable]
    public sealed class ProblemDefinition
    {
        public string id;
        public int number;
        public string face;
        public string circuit;
        public string grade;
        public HoldDefinition[] holds;
    }

    [Serializable]
    public sealed class HoldDefinition
    {
        public int id;
        public HoldKind kind;
        public HoldRole role;
        public float s;
        public float y;
        public float scale;
        public float roll;
    }

    [Serializable]
    public sealed class PlacedVolumeDefinition
    {
        public string face;
        public VolumeShape shape;
        public string color;
        public float s;
        public float y;
        public float size;
        public float depth;
        public float roll;
    }

    /// <summary>Converts generated content to its baked form and back.</summary>
    public static class ContentBaking
    {
        public static ContentDefinition ToDefinition(GymContent content)
        {
            var problems = new List<ProblemDefinition>();
            var volumes = new List<PlacedVolumeDefinition>();
            foreach (var sector in content.Sectors)
            {
                foreach (var problem in sector.Problems)
                {
                    problems.Add(ToDefinition(problem));
                }

                foreach (var volume in sector.Volumes)
                {
                    volumes.Add(new PlacedVolumeDefinition
                    {
                        face = volume.Face.Id, shape = volume.Shape, color = volume.ColorId,
                        s = volume.S, y = volume.Y, size = volume.Size, depth = volume.Depth, roll = volume.RollDeg,
                    });
                }
            }

            return new ContentDefinition { problems = problems.ToArray(), volumes = volumes.ToArray() };
        }

        public static GymContent FromDefinition(GymLayout layout, ContentDefinition definition, IReadOnlyList<Circuit> circuits)
        {
            var faces = new Dictionary<string, WallFace>();
            foreach (var face in layout.Faces)
            {
                faces[face.Id] = face;
            }

            var problems = new Dictionary<GymSector, List<BoulderProblem>>();
            var volumes = new Dictionary<GymSector, List<VolumePlacement>>();
            foreach (var sector in layout.Sectors)
            {
                problems[sector] = new List<BoulderProblem>();
                volumes[sector] = new List<VolumePlacement>();
            }

            foreach (var problem in definition.problems)
            {
                var face = Face(faces, problem.face);
                problems[face.Sector].Add(FromDefinition(problem, face, circuits));
            }

            foreach (var volume in definition.volumes)
            {
                var face = Face(faces, volume.face);
                volumes[face.Sector].Add(new VolumePlacement(face, volume.shape, volume.color, new UnityEngine.Vector2(volume.s, volume.y))
                {
                    Size = volume.size, Depth = volume.depth, RollDeg = volume.roll,
                });
            }

            var sectors = new List<SectorContent>();
            foreach (var sector in layout.Sectors)
            {
                sectors.Add(new SectorContent(sector, problems[sector], volumes[sector]));
            }

            return GymContent.FromSectors(sectors);
        }

        private static ProblemDefinition ToDefinition(BoulderProblem problem)
        {
            var holds = new HoldDefinition[problem.Holds.Count];
            for (var i = 0; i < holds.Length; i++)
            {
                var hold = problem.Holds[i];
                holds[i] = new HoldDefinition { id = hold.Id, kind = hold.Kind, role = hold.Role, s = hold.S, y = hold.Y, scale = hold.Scale, roll = hold.RollDeg };
            }

            return new ProblemDefinition
            {
                id = problem.Id, number = problem.Number, face = problem.Face.Id,
                circuit = problem.Circuit.ColorId, grade = problem.Grade, holds = holds,
            };
        }

        private static BoulderProblem FromDefinition(ProblemDefinition definition, WallFace face, IReadOnlyList<Circuit> circuits)
        {
            var holds = new List<ProblemHold>();
            foreach (var hold in definition.holds)
            {
                holds.Add(new ProblemHold(face, hold.kind, hold.s, hold.y) { Id = hold.id, Role = hold.role, Scale = hold.scale, RollDeg = hold.roll });
            }

            return new BoulderProblem(face.Sector, face, CircuitById(circuits, definition.circuit), holds)
            {
                Id = definition.id, Number = definition.number, Grade = definition.grade,
            };
        }

        private static WallFace Face(Dictionary<string, WallFace> faces, string id)
        {
            if (!faces.TryGetValue(id, out var face))
            {
                throw new InvalidOperationException($"Baked problems reference face '{id}' missing from the layout: bake them again.");
            }

            return face;
        }

        private static Circuit CircuitById(IReadOnlyList<Circuit> circuits, string colorId)
        {
            foreach (var circuit in circuits)
            {
                if (circuit.ColorId == colorId)
                {
                    return circuit;
                }
            }

            throw new InvalidOperationException($"Baked problems use unknown circuit '{colorId}'.");
        }
    }
}
