using NUnit.Framework;
using UATRecords;

namespace UATRecords.Tests.EditMode
{
    public sealed class ScenarioLoaderTests
    {
        [Test]
        public void Parse_ValidScenario_ReturnsPopulated()
        {
            const string json = @"{
                ""name"": ""walk"",
                ""scene"": ""VerticalPrototype"",
                ""durationSeconds"": 5.0,
                ""screenshotEveryNFrames"": 30,
                ""steps"": [
                    { ""atSeconds"": 0.0, ""durationSeconds"": 2.0, ""key"": ""W"", ""mouseButton"": -1 }
                ]
            }";
            var scenario = ScenarioLoader.Parse(json);
            Assert.AreEqual("walk", scenario.name);
            Assert.AreEqual(5.0f, scenario.durationSeconds);
            Assert.AreEqual(1, scenario.steps.Count);
            Assert.AreEqual("W", scenario.steps[0].key);
        }

        [Test]
        public void Parse_ZeroDuration_Throws()
        {
            const string json = @"{ ""durationSeconds"": 0.0, ""steps"": [] }";
            Assert.Throws<System.InvalidOperationException>(() => ScenarioLoader.Parse(json));
        }

        [Test]
        public void Parse_StepWithBothKeyAndMouse_Throws()
        {
            const string json = @"{
                ""durationSeconds"": 1.0,
                ""steps"": [{ ""atSeconds"": 0.0, ""durationSeconds"": 0.1, ""key"": ""W"", ""mouseButton"": 0 }]
            }";
            Assert.Throws<System.InvalidOperationException>(() => ScenarioLoader.Parse(json));
        }

        [Test]
        public void Parse_StepWithNeitherKeyNorMouse_Throws()
        {
            const string json = @"{
                ""durationSeconds"": 1.0,
                ""steps"": [{ ""atSeconds"": 0.0, ""durationSeconds"": 0.1, ""key"": """", ""mouseButton"": -1 }]
            }";
            Assert.Throws<System.InvalidOperationException>(() => ScenarioLoader.Parse(json));
        }

        [Test]
        public void Parse_NegativeTiming_Throws()
        {
            const string json = @"{
                ""durationSeconds"": 1.0,
                ""steps"": [{ ""atSeconds"": -1.0, ""durationSeconds"": 0.1, ""key"": ""W"", ""mouseButton"": -1 }]
            }";
            Assert.Throws<System.InvalidOperationException>(() => ScenarioLoader.Parse(json));
        }
    }
}
