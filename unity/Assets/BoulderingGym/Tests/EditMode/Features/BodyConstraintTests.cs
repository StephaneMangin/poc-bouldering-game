using NUnit.Framework;
using Project.Features.BodyConstraints.Domain;
using UnityEngine;

namespace Project.Tests.EditMode.Features
{
    /// <summary>
    /// Tests for the BodyConstraints feature.
    /// SwingConstraint base class is tested once via a representative constraint.
    /// Per-joint tests only validate ROM values and metadata.
    /// </summary>
    public class BodyConstraintTests
    {
        private const float Tolerance = 1f; // degrees

        // ═══════════════════════════════════════════════════════════════════
        //  SwingConstraint base behavior (tested via ElbowConstraint)
        // ═══════════════════════════════════════════════════════════════════

        private static readonly SwingConstraint Representative = ElbowConstraint.Default;

        [Test]
        public void SwingConstraint_WithinRange_NoClamp()
        {
            var parent = Vector3.down;
            var child = Vector3.forward; // 90° — within [5°, 145°]
            var result = Representative.ClampDirection(parent, child);

            Assert.AreEqual(90f, Vector3.Angle(parent, result), Tolerance);
        }

        [Test]
        public void SwingConstraint_BelowMin_ClampsToMin()
        {
            var parent = Vector3.down;
            var child = new Vector3(0f, -0.9994f, 0.0349f).normalized; // ~2°
            var result = Representative.ClampDirection(parent, child);

            Assert.GreaterOrEqual(Vector3.Angle(parent, result),
                Representative.MinSwingAngleDeg - Tolerance);
        }

        [Test]
        public void SwingConstraint_AboveMax_ClampsToMax()
        {
            var parent = Vector3.down;
            var child = new Vector3(0f, 0.9848f, 0.1736f).normalized; // ~170°
            var result = Representative.ClampDirection(parent, child);

            Assert.LessOrEqual(Vector3.Angle(parent, result),
                Representative.MaxSwingAngleDeg + Tolerance);
        }

        [Test]
        public void SwingConstraint_PreservesNormalization()
        {
            var result = Representative.ClampDirection(Vector3.down, Vector3.forward);
            Assert.AreEqual(1f, result.magnitude, 0.01f);
        }

        [Test]
        public void SwingConstraint_NearZeroInput_NoCrash()
        {
            var result = Representative.ClampDirection(Vector3.down, new Vector3(0.00001f, 0f, 0f));
            Assert.IsFalse(float.IsNaN(result.x));
        }

        [Test]
        public void SwingConstraint_ParallelVectors_NoCrash()
        {
            var result = Representative.ClampDirection(Vector3.down, Vector3.down);
            Assert.IsFalse(float.IsNaN(result.x));
        }

        [Test]
        public void SwingConstraint_ComputeDistanceRange_ValidRange()
        {
            var (minDist, maxDist) = Representative.ComputeDistanceRange(0.30f, 0.25f);

            Assert.Greater(minDist, 0f);
            Assert.Less(minDist, maxDist);
            Assert.Less(maxDist, 0.55f, "Max dist < chain length");
        }

        [Test]
        public void SwingConstraint_ComputeDistanceRange_SymmetricBones()
        {
            var (minDist, maxDist) = Representative.ComputeDistanceRange(0.3f, 0.3f);

            Assert.Greater(minDist, 0f);
            Assert.Less(maxDist, 0.6f);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  All constraints: interface compliance + ROM values
        // ═══════════════════════════════════════════════════════════════════

        private static readonly IJointConstraint[] AllConstraints =
        {
            ShoulderConstraint.Default,
            ElbowConstraint.Default,
            ForearmConstraint.Default,
            WristConstraint.Default,
            SpineConstraint.Default,
            HipConstraint.Default,
            KneeConstraint.Default,
            AnkleConstraint.Default,
        };

        [Test]
        public void AllConstraints_HaveNonEmptyJointName()
        {
            foreach (var c in AllConstraints)
                Assert.IsNotEmpty(c.JointName, $"{c.GetType().Name} must have a JointName");
        }

        [Test]
        public void AllConstraints_HavePositiveDegreesOfFreedom()
        {
            foreach (var c in AllConstraints)
                Assert.Greater(c.DegreesOfFreedom, 0, $"{c.JointName} DoF must be > 0");
        }

        // ROM value validation (table-driven)
        [TestCase("Shoulder", 3, 0f, 180f)]
        [TestCase("Elbow", 1, 5f, 145f)]
        [TestCase("Wrist", 2, 0f, 80f)]
        [TestCase("Spine", 3, 0f, 45f)]
        [TestCase("Hip", 3, 0f, 150f)]
        [TestCase("Knee", 1, 5f, 140f)]
        [TestCase("Ankle", 2, 70f, 140f)]
        public void SwingConstraint_ROM_MatchesExpected(
            string name, int dof, float expectedMin, float expectedMax)
        {
            SwingConstraint c = name switch
            {
                "Shoulder" => ShoulderConstraint.Default,
                "Elbow" => ElbowConstraint.Default,
                "Wrist" => WristConstraint.Default,
                "Spine" => SpineConstraint.Default,
                "Hip" => HipConstraint.Default,
                "Knee" => KneeConstraint.Default,
                "Ankle" => AnkleConstraint.Default,
                _ => null
            };

            Assert.IsNotNull(c);
            Assert.AreEqual(name, c.JointName);
            Assert.AreEqual(dof, c.DegreesOfFreedom);
            Assert.AreEqual(expectedMin, c.MinSwingAngleDeg, 0.01f, $"{name} min");
            Assert.AreEqual(expectedMax, c.MaxSwingAngleDeg, 0.01f, $"{name} max");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Hinge convenience: static ComputeDistanceRange delegates
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void Elbow_StaticComputeDistanceRange_DelegatesToInstance()
        {
            var fromStatic = ElbowConstraint.ComputeDistanceRange(0.30f, 0.25f);
            var fromInstance = ((SwingConstraint)ElbowConstraint.Default).ComputeDistanceRange(0.30f, 0.25f);
            Assert.AreEqual(fromStatic.minDist, fromInstance.minDist, 0.001f);
            Assert.AreEqual(fromStatic.maxDist, fromInstance.maxDist, 0.001f);
        }

        [Test]
        public void Knee_StaticComputeDistanceRange_DelegatesToInstance()
        {
            var fromStatic = KneeConstraint.ComputeDistanceRange(0.45f, 0.42f);
            var fromInstance = ((SwingConstraint)KneeConstraint.Default).ComputeDistanceRange(0.45f, 0.42f);
            Assert.AreEqual(fromStatic.minDist, fromInstance.minDist, 0.001f);
            Assert.AreEqual(fromStatic.maxDist, fromInstance.maxDist, 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  ForearmConstraint (special: twist-only, not SwingConstraint)
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void Forearm_ClampDirection_PassThrough()
        {
            var childDir = new Vector3(0.5f, 0.5f, 0.707f).normalized;
            var result = ForearmConstraint.Default.ClampDirection(Vector3.forward, childDir);

            Assert.Less(Vector3.Angle(result, childDir), Tolerance);
        }

        [Test]
        public void Forearm_ClampTwist_WithinRange_NoClamp()
        {
            Assert.AreEqual(45f, ForearmConstraint.ClampTwist(45f), 0.01f);
        }

        [Test]
        public void Forearm_ClampTwist_ExcessivePronation_Clamped()
        {
            Assert.AreEqual(ForearmConstraint.MaxPronationDeg,
                ForearmConstraint.ClampTwist(100f), 0.01f);
        }

        [Test]
        public void Forearm_ClampTwist_ExcessiveSupination_Clamped()
        {
            Assert.AreEqual(-ForearmConstraint.MaxSupinationDeg,
                ForearmConstraint.ClampTwist(-100f), 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  AnkleConstraint: static ClampFootForward convenience
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void Ankle_ClampFootForward_DelegatesToClampDirection()
        {
            var shin = Vector3.down;
            var foot = Vector3.up; // extreme → will be clamped

            var fromStatic = AnkleConstraint.ClampFootForward(shin, foot);
            var fromInstance = AnkleConstraint.Default.ClampDirection(shin, foot);

            Assert.Less(Vector3.Distance(fromStatic, fromInstance), 0.001f);
        }
    }
}
