using UnityEngine;

namespace Project.Core.Utilities
{
    public static class TwoBoneIKSolver
    {
        /// <summary>
        /// Solves a two-bone IK chain with optional bend angle constraints.
        /// Constraints are enforced via distance clamping (not Euler decomposition).
        /// </summary>
        /// <param name="constraintMinDist">
        /// Minimum end-effector distance from root (from max bend angle constraint).
        /// Use 0 for no constraint (defaults to geometric minimum).
        /// </param>
        /// <param name="constraintMaxDist">
        /// Maximum end-effector distance from root (from min bend angle constraint).
        /// Use 0 for no constraint (defaults to geometric maximum).
        /// </param>
        public static void Solve(
            Transform upper, Transform lower, Transform end,
            Vector3 target, Vector3 bendHint,
            float constraintMinDist = 0f, float constraintMaxDist = 0f)
        {
            if (upper == null || lower == null || end == null)
            {
                return;
            }

            var upperPosition = upper.position;
            var lowerPosition = lower.position;
            var upperLength = Vector3.Distance(upperPosition, lowerPosition);
            var lowerLength = Vector3.Distance(lowerPosition, end.position);
            if (upperLength <= 0.0001f || lowerLength <= 0.0001f)
            {
                return;
            }

            var targetOffset = target - upperPosition;
            if (targetOffset.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // Geometric limits: fully folded → fully extended
            var geoMin = Mathf.Abs(upperLength - lowerLength) + 0.01f;
            var geoMax = upperLength + lowerLength - 0.01f;

            // Tighten with anatomical constraints if provided
            var distMin = constraintMinDist > 0f ? Mathf.Max(geoMin, constraintMinDist) : geoMin;
            var distMax = constraintMaxDist > 0f ? Mathf.Min(geoMax, constraintMaxDist) : geoMax;

            var clampedDistance = Mathf.Clamp(targetOffset.magnitude, distMin, distMax);
            var directionToTarget = targetOffset.normalized;
            var bendDirection = Vector3.ProjectOnPlane(bendHint - upperPosition, directionToTarget);
            if (bendDirection.sqrMagnitude <= 0.0001f)
            {
                bendDirection = Vector3.ProjectOnPlane(Vector3.up, directionToTarget);
            }

            if (bendDirection.sqrMagnitude <= 0.0001f)
            {
                bendDirection = Vector3.ProjectOnPlane(Vector3.right, directionToTarget);
            }

            bendDirection.Normalize();

            var upperProjection = ((upperLength * upperLength) - (lowerLength * lowerLength) + (clampedDistance * clampedDistance)) / (2f * clampedDistance);
            var bendHeight = Mathf.Sqrt(Mathf.Max(0f, (upperLength * upperLength) - (upperProjection * upperProjection)));
            var desiredLowerPosition = upperPosition + (directionToTarget * upperProjection) + (bendDirection * bendHeight);

            RotateTowardChild(upper, lowerPosition, desiredLowerPosition);
            // Use current end.position (post upper-rotation), NOT the cached endPosition.
            // After rotating upper, both lower and end move — using the stale position
            // computes wrong FromToRotation and the end-effector misses the target.
            RotateTowardChild(lower, end.position, upperPosition + directionToTarget * clampedDistance);
        }

        public static void RotateBoneToward(Transform bone, Vector3 target, float maxAngle)
        {
            if (bone == null)
            {
                return;
            }

            var desiredDirection = target - bone.position;
            if (desiredDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var desiredRotation = Quaternion.LookRotation(desiredDirection.normalized, Vector3.up);
            bone.rotation = Quaternion.RotateTowards(bone.rotation, desiredRotation, maxAngle);
        }

        private static void RotateTowardChild(Transform parent, Vector3 currentChildPosition, Vector3 desiredChildPosition)
        {
            var currentDirection = currentChildPosition - parent.position;
            var desiredDirection = desiredChildPosition - parent.position;
            if (currentDirection.sqrMagnitude <= 0.0001f || desiredDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            parent.rotation = Quaternion.FromToRotation(currentDirection, desiredDirection) * parent.rotation;
        }
    }
}
