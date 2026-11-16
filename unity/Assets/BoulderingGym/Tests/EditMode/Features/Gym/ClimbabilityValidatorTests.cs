using System.Linq;
using NUnit.Framework;
using Project.Features.Climbing.Domain;
using Project.Features.Gym.Domain;
using static Project.Tests.EditMode.Features.Gym.GymTestLayouts;

namespace Project.Tests.EditMode.Features.Gym
{
    [TestFixture]
    public sealed class ClimbabilityValidatorTests
    {
        [Test]
        public void FindRoute_HandsStayOnStartHoldsUntilBothFeetLeaveTheFloor()
        {
            var circuits = Circuit.CreateAll(CircuitParameters.Defaults());
            var content = GymContent.Plan(GymLayoutFactory.Build(Island(6f, 3f, Segment(WallHeight, 0f))), circuits, ClimberAnatomy.Default);

            foreach (var problem in content.Problems)
            {
                var route = ClimbabilityValidator.FindRoute(problem.Holds, ClimberAnatomy.Default);
                var feetPlaced = 0;
                foreach (var (limb, hold) in route.Skip(2))
                {
                    if (!ContactSet.IsHand(limb))
                    {
                        feetPlaced++;
                        continue;
                    }

                    Assert.GreaterOrEqual(feetPlaced, 2, $"{problem.Id}: hand moved to {hold.Name} with a foot on the floor");
                    break;
                }
            }
        }
    }
}
