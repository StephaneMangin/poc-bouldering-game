using System.Linq;
using NUnit.Framework;
using Project.Features.Climbing.Domain;
using Project.Features.Gym.Domain;
using static Project.Tests.EditMode.Features.Gym.GymTestLayouts;

namespace Project.Tests.EditMode.Features.Gym
{
    [TestFixture]
    public sealed class GymContentTests
    {
        private static readonly ClimberAnatomy SmallClimber = ClimberAnatomy.Default.Scaled(0.94f);
        private static readonly ClimberAnatomy LargeClimber = ClimberAnatomy.Default.Scaled(1.04f);

        private static GymContent Plan(LayoutDefinition definition)
        {
            var circuits = Circuit.CreateAll(CircuitParameters.Defaults());
            return GymContent.Plan(GymLayoutFactory.Build(definition), circuits, SmallClimber, LargeClimber);
        }

        [Test]
        public void Plan_VerticalIsland_EveryProblemCanBeToppedByDefaultClimber()
        {
            var content = Plan(Island(6f, 3f, Segment(WallHeight, 0f)));
            var validator = new ClimbabilityValidator(ClimberAnatomy.Default);

            Assert.GreaterOrEqual(content.ProblemCount, 4);
            foreach (var problem in content.Problems)
            {
                Assert.IsTrue(validator.CanTop(problem.Holds), problem.Id);
            }
        }

        [Test]
        public void Plan_OverhangingIsland_SetsClimbableProblems()
        {
            var content = Plan(Island(8f, 6f, Segment(1f, 0f), Segment(WallHeight - 1f, 25f)));
            var validator = new ClimbabilityValidator(ClimberAnatomy.Default);

            Assert.GreaterOrEqual(content.ProblemCount, 4);
            foreach (var problem in content.Problems)
            {
                Assert.IsTrue(validator.CanTop(problem.Holds), problem.Id);
            }
        }

        [Test]
        public void Plan_EachProblemHasStartHoldsAndOneTopHold()
        {
            var content = Plan(Island(6f, 3f, Segment(WallHeight, 0f)));

            foreach (var problem in content.Problems)
            {
                Assert.That(problem.StartHolds.Count(), Is.InRange(1, 2), problem.Id);
                Assert.AreEqual(1, problem.Holds.Count(hold => hold.Role == HoldRole.Top), problem.Id);
            }
        }

        [Test]
        public void Plan_HoldsStayInsideTheirFace()
        {
            var content = Plan(Island(8f, 6f, Segment(1f, 0f), Segment(WallHeight - 1f, 25f)));

            Assert.Greater(content.HoldCount, 0);
            foreach (var hold in content.Problems.SelectMany(problem => problem.Holds))
            {
                Assert.IsTrue(hold.Face.RangeAt(hold.Y).Contains(hold.S), $"hold {hold.Id} at s={hold.S}");
                Assert.That(hold.Y, Is.InRange(0f, WallHeight));
            }
        }

        [Test]
        public void Plan_SameSeedGivesSameHolds()
        {
            var first = Plan(Island(6f, 3f, Segment(WallHeight, 0f)));
            var second = Plan(Island(6f, 3f, Segment(WallHeight, 0f)));

            var firstHolds = first.Problems.SelectMany(problem => problem.Holds).Select(hold => (hold.S, hold.Y, hold.Kind));
            var secondHolds = second.Problems.SelectMany(problem => problem.Holds).Select(hold => (hold.S, hold.Y, hold.Kind));
            CollectionAssert.AreEqual(firstHolds.ToList(), secondHolds.ToList());
        }
    }
}
