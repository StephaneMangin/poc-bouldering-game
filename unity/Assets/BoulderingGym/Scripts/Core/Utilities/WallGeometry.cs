using UnityEngine;

namespace Project.Core.Utilities
{
    public static class WallGeometry
    {
        public static Vector3 GetWallNormal(Transform wallNormalReference)
        {
            return wallNormalReference != null ? wallNormalReference.forward : Vector3.back;
        }

        public static Vector3 GetApproachDirection(Vector3 wallNormal)
        {
            var approach = Vector3.ProjectOnPlane(-wallNormal, Vector3.up).normalized;
            return approach.sqrMagnitude < 0.0001f ? Vector3.forward : approach;
        }

        public static Vector3 GetLateralDirection(Vector3 approachDirection)
        {
            return Vector3.Cross(Vector3.up, approachDirection).normalized;
        }

        public static Vector3 InputToWorldDirection(Vector2 input, Vector3 wallNormal)
        {
            var approach = GetApproachDirection(wallNormal);
            var lateral = GetLateralDirection(approach);
            var direction = (lateral * input.x) + (approach * input.y);
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }

        public static void ClampDistanceFromWall(Transform target, Vector3 wallNormal, float minDistance, float maxDistance)
        {
            if (target == null)
            {
                return;
            }

            var dot = Vector3.Dot(target.position, -wallNormal);
            var clamped = Mathf.Clamp(dot, minDistance, maxDistance);
            target.position += -wallNormal * (clamped - dot);
        }
    }
}
