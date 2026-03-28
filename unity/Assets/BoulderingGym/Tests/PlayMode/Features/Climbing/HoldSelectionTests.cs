using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode.Features.Climbing
{
    /// <summary>
    /// Tests the hold selection algorithm in PlayerHandsController.TryGrab.
    /// Isolated setup (no scene loading) to test pure selection logic.
    /// </summary>
    [TestFixture]
    public sealed class HoldSelectionTests
    {
        private GameObject _root;
        private PlayerHandsController _hands;
        private TuningConfig _tuning;
        private Transform _leftHand;
        private Transform _rightHand;
        private readonly List<GameObject> _holdObjects = new();

        private static readonly Vector3 WallNormal = Vector3.back;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _holdObjects.Clear();

            _tuning = ScriptableObject.CreateInstance<TuningConfig>();
            _tuning.grabRange = 1.6f;
            _tuning.grabAngleMax = 35f;

            _root = new GameObject("TestPlayer");
            _hands = _root.AddComponent<PlayerHandsController>();

            var leftAnchorGo = new GameObject("LeftHand");
            leftAnchorGo.transform.SetParent(_root.transform);
            leftAnchorGo.transform.localPosition = new Vector3(-0.3f, 1.5f, 0f);
            _leftHand = leftAnchorGo.transform;

            var rightAnchorGo = new GameObject("RightHand");
            rightAnchorGo.transform.SetParent(_root.transform);
            rightAnchorGo.transform.localPosition = new Vector3(0.3f, 1.5f, 0f);
            _rightHand = rightAnchorGo.transform;

            SetPrivateField(_hands, "tuningConfig", _tuning);
            SetPrivateField(_hands, "leftHandAnchor", _leftHand);
            SetPrivateField(_hands, "rightHandAnchor", _rightHand);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in _holdObjects)
                Object.Destroy(go);
            _holdObjects.Clear();

            if (_root != null) Object.Destroy(_root);
            if (_tuning != null) Object.DestroyImmediate(_tuning);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosestHold_IsSelected()
        {
            var farHold = CreateHold("FarHold", new Vector3(-0.3f, 1.5f, 1.0f));
            var closeHold = CreateHold("CloseHold", new Vector3(-0.3f, 1.5f, 0.5f));

            var candidates = new[] { farHold, closeHold };
            bool grabbed = _hands.TryGrab(HandSide.Left, candidates, WallNormal, out var selected);
            yield return null;

            Assert.IsTrue(grabbed, "Should grab a hold");
            Assert.AreEqual(closeHold, selected, "Should select the closest hold");
        }

        [UnityTest]
        public IEnumerator HoldOutOfRange_IsIgnored()
        {
            var distantHold = CreateHold("DistantHold", new Vector3(-0.3f, 1.5f, 5f));

            var candidates = new[] { distantHold };
            bool grabbed = _hands.TryGrab(HandSide.Left, candidates, WallNormal, out _);
            yield return null;

            Assert.IsFalse(grabbed, "Should not grab a hold that is out of range");
        }

        [UnityTest]
        public IEnumerator HoldBeyondAngle_IsIgnored()
        {
            var sideHold = CreateHold("SideHold", new Vector3(3f, 1.5f, 0.5f));

            var candidates = new[] { sideHold };
            bool grabbed = _hands.TryGrab(HandSide.Left, candidates, WallNormal, out _);
            yield return null;

            Assert.IsFalse(grabbed, "Should not grab a hold beyond max angle");
        }

        [UnityTest]
        public IEnumerator AlreadyHeldHold_IsSkipped()
        {
            var hold1 = CreateHold("Hold1", new Vector3(-0.3f, 1.5f, 0.5f));
            var hold2 = CreateHold("Hold2", new Vector3(0.3f, 1.5f, 0.5f));

            _hands.TryGrab(HandSide.Left, new[] { hold1, hold2 }, WallNormal, out _);

            bool grabbed = _hands.TryGrab(HandSide.Right, new[] { hold1, hold2 }, WallNormal, out var selected);
            yield return null;

            Assert.IsTrue(grabbed, "Right hand should grab a hold");
            Assert.AreEqual(hold2, selected, "Right hand should skip already-held hold1 and take hold2");
        }

        [UnityTest]
        public IEnumerator NoCandidates_ReturnsFalse()
        {
            bool grabbed = _hands.TryGrab(HandSide.Left, new HoldComponent[0], WallNormal, out _);
            yield return null;

            Assert.IsFalse(grabbed, "Should return false with no candidates");
        }

        [UnityTest]
        public IEnumerator HoldWithGripPoint_UsesGripPosition()
        {
            var hold = CreateHoldWithGripPoint(
                "HoldWithGrip",
                holdWorldPos: new Vector3(-0.3f, 1.5f, 5f),
                gripWorldPos: new Vector3(-0.3f, 1.5f, 0.3f)
            );

            var candidates = new[] { hold };
            bool grabbed = _hands.TryGrab(HandSide.Left, candidates, WallNormal, out _);
            yield return null;

            Assert.IsTrue(grabbed, "Should grab hold using gripPoint position despite far hold transform");
        }

        [UnityTest]
        public IEnumerator Release_RestoresHandToRest()
        {
            // Oracle: rest position must be set explicitly since Awake() runs
            // before SetPrivateField injects the anchors
            var oracleRestPos = _leftHand.localPosition;
            SetPrivateField(_hands, "_leftRestLocalPosition", oracleRestPos);
            SetPrivateField(_hands, "_leftRestLocalRotation", _leftHand.localRotation);

            var hold = CreateHold("Hold", new Vector3(-0.3f, 1.5f, 0.5f));
            _hands.TryGrab(HandSide.Left, new[] { hold }, WallNormal, out _);

            _hands.Release(HandSide.Left);
            yield return null;

            Assert.IsNull(_hands.LeftCurrentHold, "Left hold should be null after release");
            Assert.AreEqual(0, _hands.SupportCount, "SupportCount should be 0");
            Assert.AreEqual(oracleRestPos.x, _leftHand.localPosition.x, 0.01f,
                "Hand X should return to rest position after release");
            Assert.AreEqual(oracleRestPos.y, _leftHand.localPosition.y, 0.01f,
                "Hand Y should return to rest position after release");
            Assert.AreEqual(oracleRestPos.z, _leftHand.localPosition.z, 0.01f,
                "Hand Z should return to rest position after release");
        }

        [UnityTest]
        public IEnumerator GrabSameHandTwice_ReturnsFalse()
        {
            var hold1 = CreateHold("Hold1", new Vector3(-0.3f, 1.5f, 0.5f));
            var hold2 = CreateHold("Hold2", new Vector3(-0.3f, 1.5f, 0.8f));

            _hands.TryGrab(HandSide.Left, new[] { hold1 }, WallNormal, out _);
            bool second = _hands.TryGrab(HandSide.Left, new[] { hold2 }, WallNormal, out _);
            yield return null;

            Assert.IsFalse(second, "Should not grab with same hand twice without releasing");
        }

        [UnityTest]
        public IEnumerator ReleaseAll_ClearsBothHands()
        {
            var hold1 = CreateHold("Hold1", new Vector3(-0.3f, 1.5f, 0.5f));
            var hold2 = CreateHold("Hold2", new Vector3(0.3f, 1.5f, 0.5f));

            _hands.TryGrab(HandSide.Left, new[] { hold1, hold2 }, WallNormal, out _);
            _hands.TryGrab(HandSide.Right, new[] { hold1, hold2 }, WallNormal, out _);

            Assert.AreEqual(2, _hands.SupportCount);

            _hands.ReleaseAll();
            yield return null;

            Assert.AreEqual(0, _hands.SupportCount, "SupportCount should be 0 after ReleaseAll");
            Assert.IsNull(_hands.LeftCurrentHold);
            Assert.IsNull(_hands.RightCurrentHold);
        }

        // ─── Helpers ───

        private HoldComponent CreateHold(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshFilter>();
            var hold = go.AddComponent<HoldComponent>();
            _holdObjects.Add(go);
            return hold;
        }

        private HoldComponent CreateHoldWithGripPoint(string name, Vector3 holdWorldPos, Vector3 gripWorldPos)
        {
            var go = new GameObject(name);
            go.transform.position = holdWorldPos;
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshFilter>();
            var hold = go.AddComponent<HoldComponent>();

            var gripGo = new GameObject(name + "_Grip");
            gripGo.transform.SetParent(go.transform);
            gripGo.transform.position = gripWorldPos;
            hold.gripPoint = gripGo.transform;

            _holdObjects.Add(go);
            return hold;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                field = type.GetField("_" + fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
            }
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {type.Name}");
            field.SetValue(target, value);
        }
    }
}
