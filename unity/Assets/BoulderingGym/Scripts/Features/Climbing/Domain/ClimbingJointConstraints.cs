using UnityEngine;

namespace Project.Features.Climbing.Domain
{
    /// <summary>
    /// Bend angle constraint for a two-bone IK chain's middle joint.
    /// Angles are in clinical convention: 0° = straight, max° = fully bent.
    /// Constraints are enforced DURING the IK solve via distance clamping,
    /// not via Euler decomposition post-hoc.
    /// </summary>
    [System.Serializable]
    public struct BendConstraint
    {
        /// <summary>Minimum bend angle in degrees (near-straight). Prevents hyper-extension.</summary>
        public float MinAngleDeg;

        /// <summary>Maximum bend angle in degrees (fully bent). Prevents over-flexion.</summary>
        public float MaxAngleDeg;

        public BendConstraint(float min, float max)
        {
            MinAngleDeg = min;
            MaxAngleDeg = max;
        }

        /// <summary>
        /// Given upper and lower bone lengths, computes the valid end-effector
        /// distance range that keeps the middle joint within [MinAngle, MaxAngle].
        ///
        /// Law of cosines: d² = a² + b² - 2ab·cos(π - θ_clinical)
        ///   where θ_clinical is the clinical bend angle (0 = straight).
        ///
        /// MaxAngle → most bent → shortest distance.
        /// MinAngle → least bent → longest distance.
        /// </summary>
        public readonly (float minDist, float maxDist) ComputeDistanceRange(float upperLen, float lowerLen)
        {
            var a2b2 = upperLen * upperLen + lowerLen * lowerLen;
            var twoAB = 2f * upperLen * lowerLen;

            // Max clinical bend → supplement angle is small → cos is large → distance is small
            var cosAtMaxBend = Mathf.Cos(Mathf.PI - MaxAngleDeg * Mathf.Deg2Rad);
            var minDist = Mathf.Sqrt(Mathf.Max(0.001f, a2b2 - twoAB * cosAtMaxBend));

            // Min clinical bend → supplement angle is large → cos is negative → distance is large
            var cosAtMinBend = Mathf.Cos(Mathf.PI - MinAngleDeg * Mathf.Deg2Rad);
            var maxDist = Mathf.Sqrt(Mathf.Max(0.001f, a2b2 - twoAB * cosAtMinBend));

            return (minDist, maxDist);
        }

        /// <summary>Elbow: 5° min (not fully straight) to 145° max (full flexion).</summary>
        public static readonly BendConstraint Elbow = new(5f, 145f);

        /// <summary>Knee: 5° min to 140° max. Source: AAOS clinical goniometry.</summary>
        public static readonly BendConstraint Knee = new(5f, 140f);

        /// <summary>No constraint — used for limbs where bending is unrestricted.</summary>
        public static readonly BendConstraint Unconstrained = new(0f, 179f);
    }
}
