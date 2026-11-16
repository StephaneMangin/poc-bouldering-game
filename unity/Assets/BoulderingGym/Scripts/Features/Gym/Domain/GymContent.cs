using System.Collections.Generic;
using Project.Features.Climbing.Domain;
using Random = System.Random;

namespace Project.Features.Gym.Domain
{
    public sealed class SectorContent
    {
        public SectorContent(GymSector sector, List<BoulderProblem> problems, List<VolumePlacement> volumes)
        {
            Sector = sector;
            Problems = problems;
            Volumes = volumes;
        }

        public GymSector Sector { get; }

        public IReadOnlyList<BoulderProblem> Problems { get; }

        public IReadOnlyList<VolumePlacement> Volumes { get; }
    }

    /// <summary>
    /// Problems and volumes of the whole gym, generated deterministically from each sector seed.
    /// Volumes fill the space left free by the problems.
    /// </summary>
    public sealed class GymContent
    {
        private readonly Dictionary<int, ProblemHold> _holdsById = new();
        private readonly Dictionary<string, BoulderProblem> _problemsById = new();

        private GymContent(IReadOnlyList<SectorContent> sectors)
        {
            Sectors = sectors;
            foreach (var sector in sectors)
            {
                foreach (var problem in sector.Problems)
                {
                    _problemsById[problem.Id] = problem;
                    foreach (var hold in problem.Holds)
                    {
                        _holdsById[hold.Id] = hold;
                    }
                }
            }
        }

        public IReadOnlyList<SectorContent> Sectors { get; }

        public IEnumerable<BoulderProblem> Problems => _problemsById.Values;

        public int ProblemCount => _problemsById.Count;

        public int HoldCount => _holdsById.Count;

        public ProblemHold HoldById(int id) => _holdsById.TryGetValue(id, out var hold) ? hold : null;

        public BoulderProblem ProblemById(string id) => _problemsById.TryGetValue(id, out var problem) ? problem : null;

        public static GymContent Plan(GymLayout layout, IReadOnlyList<Circuit> circuits, params ClimberAnatomy[] validationAnatomies)
        {
            var generator = new ProblemGenerator(circuits, validationAnatomies);
            var sectors = new List<SectorContent>();
            foreach (var sector in layout.Sectors)
            {
                var random = new Random(sector.Seed);
                var occupancy = new Dictionary<WallFace, FaceOccupancy>();
                foreach (var face in sector.Faces)
                {
                    occupancy[face] = new FaceOccupancy();
                }

                var problems = generator.Generate(sector, random, occupancy);
                var volumes = VolumePlanner.Place(sector, random, occupancy);
                sectors.Add(new SectorContent(sector, problems, volumes));
            }

            return new GymContent(sectors);
        }
    }
}
