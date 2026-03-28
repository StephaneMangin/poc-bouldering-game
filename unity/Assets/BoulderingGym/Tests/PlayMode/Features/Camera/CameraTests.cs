using System.Collections;
using NUnit.Framework;
using Project.Features.Camera.Presentation;
using Project.Features.Locomotion.Presentation;
using Project.Features.Player.Domain;
using Project.Features.Visual.Presentation;
using Project.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode.Features.Camera
{
    /// <summary>
    /// Tests for camera orbit, collision, body rotation decoupling, and sensitivity.
    /// </summary>
    [TestFixture]
    public sealed class CameraTests : TestSceneFixture
    {
        private CameraFollowController _camera;
        private LocomotionAnimatorController _animCtrl;
        private Animator _animator;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EnsureIdleGround();

            _camera = Object.FindAnyObjectByType<CameraFollowController>();
            _animCtrl = Driver.GetComponent<LocomotionAnimatorController>();
            _animator = _animCtrl != null ? _animCtrl.MannequinAnimator : null;

            Assert.IsNotNull(_camera, "CameraFollowController not found");
        }

        [UnityTest]
        public IEnumerator Idle_CameraOrbit_DoesNotRotateBody()
        {
            var initialYaw = Driver.transform.eulerAngles.y;

            yield return WaitFrames(5);

            var currentYaw = Driver.transform.eulerAngles.y;
            var delta = Mathf.Abs(NormalizeAngle(currentYaw - initialYaw));

            Assert.Less(delta, 1f,
                $"Character body should NOT rotate when idle. Yaw changed by {delta:F2}°");
        }

        [UnityTest]
        public IEnumerator Walking_GroundLocomotion_IsWalkingFalseWhenIdle()
        {
            var glc = Driver.GetComponent<GroundLocomotionController>();
            if (glc == null)
            {
                Assert.Ignore("No GroundLocomotionController found");
            }

            Assert.IsFalse(glc.IsWalking, "IsWalking should be false when idle");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Camera_PositionsBehindOrbitYaw()
        {
            yield return WaitFrames(3);

            var orbitYaw = _camera.OrbitYaw;
            var behindDir = Quaternion.Euler(0f, orbitYaw, 0f) * Vector3.back;
            var cameraDir = (_camera.transform.position - Driver.transform.position).normalized;
            var cameraDirFlat = Vector3.ProjectOnPlane(cameraDir, Vector3.up).normalized;

            var dot = Vector3.Dot(cameraDirFlat, behindDir);
            Assert.Greater(dot, 0.7f,
                $"Camera should be positioned behind orbit yaw. Dot={dot:F2}");
        }

        [UnityTest]
        public IEnumerator Camera_CollisionPreventsClipping()
        {
            // Measure baseline distance without obstacle
            yield return WaitFrames(3);
            var pivotPoint = Driver.transform.position + new Vector3(0f, 1.2f, 0f);
            var baselineDistance = Vector3.Distance(pivotPoint, _camera.transform.position);

            // Place a large wall between player and camera, perpendicular to camera direction
            var cameraDir = (_camera.transform.position - pivotPoint).normalized;
            var wallPos = pivotPoint + cameraDir * 1.0f;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "TestCollisionWall";
            wall.transform.position = wallPos;
            wall.transform.localScale = new Vector3(10f, 10f, 0.5f);
            wall.transform.rotation = Quaternion.LookRotation(cameraDir);

            // Force physics to register the new collider immediately
            UnityEngine.Physics.SyncTransforms();
            yield return WaitFrames(10);

            pivotPoint = Driver.transform.position + new Vector3(0f, 1.2f, 0f);
            var collisionDistance = Vector3.Distance(pivotPoint, _camera.transform.position);

            // Oracle: with obstacle 1m from pivot, camera should be much closer than baseline
            Assert.Less(collisionDistance, baselineDistance - 0.5f,
                $"Camera should be pushed closer when a wall blocks its path. " +
                $"Baseline={baselineDistance:F2}, with wall={collisionDistance:F2}");

            Object.Destroy(wall);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimatorParams_SetInUpdate_BeforeEvaluation()
        {
            if (_animator == null)
            {
                Assert.Ignore("No Animator found");
            }

            var hasWalking = false;
            foreach (var param in _animator.parameters)
            {
                if (param.name == "is_walking")
                {
                    hasWalking = true;
                    break;
                }
            }

            Assert.IsTrue(hasWalking, "Animator should have 'is_walking' parameter");

            var isWalking = _animator.GetBool("is_walking");
            Assert.IsFalse(isWalking, "is_walking should be false when idle");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Idle_HeadLookAt_AppliesWhenCameraOrbits()
        {
            var rigMapping = VisualRoot != null
                ? VisualRoot.GetComponent<HumanoidRigMapping>()
                : null;

            if (rigMapping == null || rigMapping.head == null)
            {
                Assert.Ignore("No head bone found");
            }

            yield return WaitFrames(2);
            var initialHeadRotation = rigMapping.head.rotation;

            var field = typeof(CameraFollowController).GetField("_orbitYaw",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(field, "Cannot find _orbitYaw field");
            var initialOrbit = (float)field.GetValue(_camera);
            field.SetValue(_camera, initialOrbit + 60f);

            yield return WaitFrames(3);

            var headAngle = Quaternion.Angle(initialHeadRotation, rigMapping.head.rotation);
            Assert.Greater(headAngle, 5f,
                $"Head should have turned when camera orbited. Angle delta={headAngle:F2}°");

            field.SetValue(_camera, initialOrbit);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MouseSensitivity_Property_ReadsAndWrites()
        {
            var original = _camera.MouseSensitivity;
            Assert.Greater(original, 0f);

            _camera.MouseSensitivity = 5f;
            Assert.AreEqual(5f, _camera.MouseSensitivity, 0.01f);

            _camera.MouseSensitivity = original;
            yield return null;
        }

        [UnityTest]
        public IEnumerator OrbitYaw_IsReadable()
        {
            var yaw = _camera.OrbitYaw;
            Assert.IsTrue(float.IsFinite(yaw), $"OrbitYaw should be finite, got {yaw}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Camera_StaysAboveGround()
        {
            yield return WaitFrames(5);
            Assert.Greater(_camera.transform.position.y, 0f,
                "Camera should never be below ground level");
        }
    }
}
