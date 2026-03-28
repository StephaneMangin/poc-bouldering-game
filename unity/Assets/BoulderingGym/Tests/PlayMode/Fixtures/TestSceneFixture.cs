using System.Collections;
using NUnit.Framework;
using Project.Features.Climbing.Presentation;
using Project.Features.Locomotion.Presentation;
using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using Project.Features.Visual.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode.Fixtures
{
    /// <summary>
    /// Base fixture that loads VerticalPrototype and resolves key components.
    /// Inherit from this in test classes that need the full scene.
    /// </summary>
    public abstract class TestSceneFixture
    {
        private const string ScenePath = "BoulderingGym/Scenes/VerticalPrototype";

        protected PlayerStateMachineDriver Driver { get; private set; }
        protected PlayerHandsController Hands { get; private set; }
        protected FallPhysicsController FallPhysics { get; private set; }
        protected ClimberPostureController Posture { get; private set; }
        protected ClimberVisualOrchestrator VisualOrchestrator { get; private set; }
        protected Transform VisualRoot { get; private set; }

        [UnityOneTimeSetUp]
        public IEnumerator LoadScene()
        {
            var op = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (op != null && !op.isDone)
            {
                yield return null;
            }

            // Allow one frame for Awake/Start
            yield return null;

            ResolveComponents();
            Assert.IsNotNull(Driver, "PlayerStateMachineDriver not found in scene");
            Assert.IsNotNull(Driver.StateMachine, "StateMachine not initialized");
        }

        private void ResolveComponents()
        {
            Driver = Object.FindAnyObjectByType<PlayerStateMachineDriver>();
            if (Driver == null) return;

            Hands = Driver.PlayerHands;
            if (Hands == null)
            {
                Hands = Driver.GetComponent<PlayerHandsController>();
            }

            FallPhysics = Driver.GetComponent<FallPhysicsController>();
            Posture = Driver.GetComponent<ClimberPostureController>();
            if (Posture == null)
            {
                Posture = Driver.GetComponentInChildren<ClimberPostureController>();
            }

            VisualOrchestrator = Driver.GetComponent<ClimberVisualOrchestrator>();
            if (VisualOrchestrator == null)
            {
                VisualOrchestrator = Driver.GetComponentInChildren<ClimberVisualOrchestrator>();
            }

            var bodyVisual = Driver.transform.Find("BodyVisual");
            VisualRoot = bodyVisual != null ? bodyVisual : Driver.transform;
        }

        protected IEnumerator EnsureIdleGround()
        {
            var current = Driver.StateMachine.CurrentState.Id;
            if (current == PlayerStateId.IdleGround)
                yield break;

            // From GripStable or Reach, transition through Falling first
            if (current == PlayerStateId.GripStable || current == PlayerStateId.Reach)
            {
                Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
                yield return null;
            }

            // From Falling, land
            if (Driver.StateMachine.CurrentState.Id == PlayerStateId.Falling)
            {
                Driver.StateMachine.Fire(PlayerTrigger.Landed);
            }

            yield return WaitForState(PlayerStateId.IdleGround, timeout: 3f);
        }

        protected IEnumerator WaitForState(PlayerStateId target, float timeout = 5f)
        {
            var elapsed = 0f;
            while (Driver.StateMachine.CurrentState.Id != target && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.AreEqual(target, Driver.StateMachine.CurrentState.Id,
                $"Timed out waiting for state {target} (still in {Driver.StateMachine.CurrentState.Id})");
        }

        protected IEnumerator WaitFrames(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        protected static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
