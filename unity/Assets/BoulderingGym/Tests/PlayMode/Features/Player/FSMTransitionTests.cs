using System.Collections;
using NUnit.Framework;
using Project.Features.Locomotion.Presentation;
using Project.Features.Player.Domain;
using Project.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode.Features.Player
{
    /// <summary>
    /// Scene-integration FSM tests. Unlike EditMode StateMachineTests,
    /// these verify side effects in the scene: CC enabled state,
    /// position validity, and SupportCount consistency.
    /// </summary>
    [TestFixture]
    public sealed class FSMTransitionTests : TestSceneFixture
    {
        private GroundLocomotionController _glc;
        private FallPhysicsController _fpc;
        private CharacterController _cc;

        [UnitySetUp]
        public IEnumerator ResetToIdleGround()
        {
            if (_glc == null)
                _glc = Driver.GetComponent<GroundLocomotionController>();
            if (_fpc == null)
                _fpc = Driver.GetComponent<FallPhysicsController>();
            if (_cc == null)
                _cc = Driver.GetComponent<CharacterController>();

            // Re-enable controllers for setUp recovery
            if (_glc != null) _glc.enabled = true;
            if (_fpc != null) _fpc.enabled = true;

            yield return EnsureIdleGround();
        }

        /// <summary>
        /// Disable controllers that auto-transition FROM Falling state,
        /// so tests can assert Falling persists.
        /// </summary>
        private void DisableAutoLanding()
        {
            if (_glc != null) _glc.enabled = false;
            if (_fpc != null) _fpc.enabled = false;
        }

        private void EnableAutoLanding()
        {
            if (_glc != null) _glc.enabled = true;
            if (_fpc != null) _fpc.enabled = true;
        }

        // ─── Valid transitions from IdleGround ───

        [UnityTest]
        public IEnumerator IdleGround_GrabRequested_TransitionsToReach()
        {
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return null;

            Assert.AreEqual(PlayerStateId.Reach, Driver.StateMachine.CurrentState.Id);
            AssertPositionFinite("After IdleGround→Reach");
            if (_cc != null)
                Assert.IsTrue(_cc.enabled, "CC should remain enabled in Reach");
        }

        // ─── Valid transitions from Reach ───

        [UnityTest]
        public IEnumerator Reach_GrabSucceeded_TransitionsToGripStable()
        {
            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return null;
            Assert.AreEqual(PlayerStateId.Reach, Driver.StateMachine.CurrentState.Id);

            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return null;

            Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);
            AssertPositionFinite("After Reach→GripStable");
        }

        [UnityTest]
        public IEnumerator Reach_GrabFailed_NoSupport_TransitionsToIdleGround()
        {
            Hands?.ReleaseAll();

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return null;

            Driver.StateMachine.Fire(PlayerTrigger.GrabFailed);
            yield return null;

            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);
            Assert.AreEqual(0, Hands.SupportCount, "No support after failed grab");
            if (_cc != null)
                Assert.IsTrue(_cc.enabled, "CC should be enabled back in IdleGround");
        }

        [UnityTest]
        public IEnumerator Reach_LostSupport_TransitionsToFalling()
        {
            DisableAutoLanding();

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return null;

            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return null;

            Assert.AreEqual(PlayerStateId.Falling, Driver.StateMachine.CurrentState.Id);
            Assert.AreEqual(0, Hands.SupportCount, "No holds should be held while falling");
            AssertPositionFinite("After Reach→Falling");

            EnableAutoLanding();
        }

        // ─── Valid transitions from GripStable ───

        [UnityTest]
        public IEnumerator GripStable_GrabRequested_StaysGripStable()
        {
            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return null;
            Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return null;

            Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);
        }

        [UnityTest]
        public IEnumerator GripStable_GrabSucceeded_StaysGripStable()
        {
            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return null;

            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return null;

            Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);
        }

        [UnityTest]
        public IEnumerator GripStable_GrabFailed_StaysGripStable()
        {
            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return null;

            Driver.StateMachine.Fire(PlayerTrigger.GrabFailed);
            yield return null;

            Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);
        }

        [UnityTest]
        public IEnumerator GripStable_LostSupport_TransitionsToFalling()
        {
            DisableAutoLanding();

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return null;
            Assert.AreEqual(PlayerStateId.GripStable, Driver.StateMachine.CurrentState.Id);

            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return null;

            Assert.AreEqual(PlayerStateId.Falling, Driver.StateMachine.CurrentState.Id);

            EnableAutoLanding();
        }

        [UnityTest]
        public IEnumerator GripStable_ReleaseRequested_NoSupport_TransitionsToReach()
        {
            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return null;

            Hands?.ReleaseAll();

            Driver.StateMachine.Fire(PlayerTrigger.ReleaseRequested);
            yield return null;

            Assert.AreEqual(PlayerStateId.Reach, Driver.StateMachine.CurrentState.Id);
        }

        // ─── Valid transitions from Falling ───

        [UnityTest]
        public IEnumerator Falling_Landed_TransitionsToIdleGround()
        {
            DisableAutoLanding();

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return null;
            Assert.AreEqual(PlayerStateId.Falling, Driver.StateMachine.CurrentState.Id);

            Driver.StateMachine.Fire(PlayerTrigger.Landed);
            yield return null;

            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);
            if (_cc != null)
                Assert.IsTrue(_cc.enabled, "CC should be enabled after landing");
            AssertPositionFinite("After Falling→IdleGround");

            EnableAutoLanding();
        }

        // ─── Invalid triggers (should NOT change state) ───

        [UnityTest]
        public IEnumerator IdleGround_Landed_StateUnchanged()
        {
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            var result = Driver.StateMachine.Fire(PlayerTrigger.Landed);
            yield return null;

            Assert.IsFalse(result, "Landed should be rejected in IdleGround");
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);
        }

        [UnityTest]
        public IEnumerator IdleGround_GrabSucceeded_StateUnchanged()
        {
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            var result = Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return null;

            Assert.IsFalse(result, "GrabSucceeded should be rejected in IdleGround");
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);
        }

        [UnityTest]
        public IEnumerator IdleGround_ReleaseRequested_StateUnchanged()
        {
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            var result = Driver.StateMachine.Fire(PlayerTrigger.ReleaseRequested);
            yield return null;

            Assert.IsFalse(result, "ReleaseRequested should be rejected in IdleGround");
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);
        }

        [UnityTest]
        public IEnumerator IdleGround_LostSupport_StateUnchanged()
        {
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);

            var result = Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return null;

            Assert.IsFalse(result, "LostSupport should be rejected in IdleGround");
            Assert.AreEqual(PlayerStateId.IdleGround, Driver.StateMachine.CurrentState.Id);
        }

        [UnityTest]
        public IEnumerator Falling_GrabRequested_StateUnchanged()
        {
            DisableAutoLanding();

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return null;
            Assert.AreEqual(PlayerStateId.Falling, Driver.StateMachine.CurrentState.Id);

            var result = Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            yield return null;

            Assert.IsFalse(result, "GrabRequested should be rejected in Falling");
            Assert.AreEqual(PlayerStateId.Falling, Driver.StateMachine.CurrentState.Id);

            EnableAutoLanding();
        }

        [UnityTest]
        public IEnumerator Falling_ReleaseRequested_StateUnchanged()
        {
            DisableAutoLanding();

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return null;
            Assert.AreEqual(PlayerStateId.Falling, Driver.StateMachine.CurrentState.Id);

            var result = Driver.StateMachine.Fire(PlayerTrigger.ReleaseRequested);
            yield return null;

            Assert.IsFalse(result, "ReleaseRequested should be rejected in Falling");
            Assert.AreEqual(PlayerStateId.Falling, Driver.StateMachine.CurrentState.Id);

            EnableAutoLanding();
        }

        [UnityTest]
        public IEnumerator Falling_GrabSucceeded_StateUnchanged()
        {
            DisableAutoLanding();

            Driver.StateMachine.Fire(PlayerTrigger.GrabRequested);
            Driver.StateMachine.Fire(PlayerTrigger.LostSupport);
            yield return null;
            Assert.AreEqual(PlayerStateId.Falling, Driver.StateMachine.CurrentState.Id);

            var result = Driver.StateMachine.Fire(PlayerTrigger.GrabSucceeded);
            yield return null;

            Assert.IsFalse(result, "GrabSucceeded should be rejected in Falling");
            Assert.AreEqual(PlayerStateId.Falling, Driver.StateMachine.CurrentState.Id);

            EnableAutoLanding();
        }

        // ─── Helpers ───

        private void AssertPositionFinite(string context)
        {
            var pos = Driver.transform.position;
            Assert.IsFalse(float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z),
                $"{context}: Player position contains NaN ({pos})");
            Assert.IsFalse(float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z),
                $"{context}: Player position contains Infinity ({pos})");
        }
    }
}
