using System.Linq;
using NUnit.Framework;
using Project.Features.Climbing.Domain;
using Project.Features.Gym.Domain;
using static Project.Tests.EditMode.Features.Gym.GymTestLayouts;

namespace Project.Tests.EditMode.Features.Gym
{
    [TestFixture]
    public sealed class ContentBakingTests
    {
        [Test]
        public void FromDefinition_RestoresBakedProblemsHoldForHold()
        {
            var circuits = Circuit.CreateAll(CircuitParameters.Defaults());
            var layout = GymLayoutFactory.Build(Island(8f, 6f, Segment(1f, 0f), Segment(WallHeight - 1f, 20f)));
            var generated = GymContent.Plan(layout, circuits, ClimberAnatomy.Default);

            var restored = ContentBaking.FromDefinition(layout, ContentBaking.ToDefinition(generated), circuits);

            Assert.AreEqual(generated.ProblemCount, restored.ProblemCount);
            foreach (var problem in generated.Problems)
            {
                var copy = restored.ProblemById(problem.Id);
                Assert.AreEqual(problem.Face, copy.Face, problem.Id);
                Assert.AreEqual(problem.Circuit, copy.Circuit, problem.Id);
                var expected = problem.Holds.Select(hold => (hold.Id, hold.Kind, hold.Role, hold.S, hold.Y));
                var actual = copy.Holds.Select(hold => (hold.Id, hold.Kind, hold.Role, hold.S, hold.Y));
                CollectionAssert.AreEqual(expected.ToList(), actual.ToList(), problem.Id);
            }
        }
    }
}
