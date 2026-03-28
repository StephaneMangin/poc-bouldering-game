using UnityEngine;

namespace Project.Core.Utilities
{
    public static class TwoBoneIKSolver
    {
        public static void Solve(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 bendHint)
        {
            if (upper == null || lower == null || end == null)
            {
                return;
            }

            var upperPosition = upper.position;
            var lowerPosition = lower.position;
            var endPosition = end.position;
            var upperLength = Vector3.Distance(upperPosition, lowerPosition);
            var lowerLength = Vector3.Distance(lowerPosition, endPosition);
            if (upperLength <= 0.0001f || lowerLength <= 0.0001f)
            {
                return;
            }

            var targetOffset = target - upperPosition;
            if (targetOffset.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var clampedDistance = Mathf.Clamp(
                targetOffset.magnitude,
                Mathf.Abs(upperLength - lowerLength) + 0.01f,
                upperLength + lowerLength - 0.01f);
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
            RotateTowardChild(lower, endPosition, target);
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
