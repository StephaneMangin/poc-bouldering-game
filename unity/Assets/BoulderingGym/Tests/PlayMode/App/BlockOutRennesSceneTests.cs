using System.Collections;
using NUnit.Framework;
using Project.App;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode.App
{
    [TestFixture]
    public sealed class BlockOutRennesSceneTests
    {
        private const string SceneName = "BlockOutRennes";

        [UnityTest]
        public IEnumerator Start_BuildsTheGymWithProblemsAndAClimber()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var game = Object.FindFirstObjectByType<GameBootstrap>();
            Assert.IsNotNull(game, "the scene has no GameBootstrap");
            Assert.Greater(game.Content.ProblemCount, 0, "no boulder problem in the gym");
            Assert.IsNotNull(game.Climber, "no climber spawned");
        }
    }
}
