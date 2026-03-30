using UnityEngine;

namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Abstract base for anatomical joint constraints that limit the swing angle
    /// between a parent bone and a child bone.
    ///
    /// Convention:
    ///   parentAxis     = bone ABOVE the joint (flowing into the joint).
    ///   childDirection = bone BELOW the joint (flowing out of the joint).
    ///   angle          = Vector3.Angle(parentAxis, childDirection).
    ///
    /// Subclasses only provide their ROM values via property overrides.
    /// </summary>
    public abstract class SwingConstraint : IJointConstraint
    {
        public abstract string JointName { get; }
        public abstract int DegreesOfFreedom { get; }

        /// <summary>Minimum allowed angle between parent and child axes (degrees).</summary>
        public abstract float MinSwingAngleDeg { get; }

        /// <summary>Maximum allowed angle between parent and child axes (degrees).</summary>
        public abstract float MaxSwingAngleDeg { get; }

        public Vector3 ClampDirection(Vector3 parentAxis, Vector3 childDirection)
        {
            if (parentAxis.sqrMagnitude < 0.0001f || childDirection.sqrMagnitude < 0.0001f)
                return childDirection;

            parentAxis.Normalize();
            childDirection.Normalize();

            var angle = Vector3.Angle(parentAxis, childDirection);

            if (angle >= MinSwingAngleDeg && angle <= MaxSwingAngleDeg)
                return childDirection;

            var clamped = Mathf.Clamp(angle, MinSwingAngleDeg, MaxSwingAngleDeg);
            var rotAxis = Vector3.Cross(parentAxis, childDirection);

            if (rotAxis.sqrMagnitude < 0.0001f)
            {
                rotAxis = Vector3.Cross(parentAxis, Vector3.up);
                if (rotAxis.sqrMagnitude < 0.0001f)
                    rotAxis = Vector3.Cross(parentAxis, Vector3.right);
            }

            rotAxis.Normalize();
            return Quaternion.AngleAxis(clamped, rotAxis) * parentAxis;
        }

        /// <summary>
        /// Law-of-cosines conversion: angular ROM → valid IK end-effector distance range.
        /// Clinical convention: 0° = straight, higher = more bent.
        /// MaxSwingAngle → most bent → shortest distance.
        /// MinSwingAngle → least bent → longest distance.
        /// </summary>
        public (float minDist, float maxDist) ComputeDistanceRange(float upperLen, float lowerLen)
        {
            var a2b2 = upperLen * upperLen + lowerLen * lowerLen;
            var twoAB = 2f * upperLen * lowerLen;

            var cosAtMaxBend = Mathf.Cos(Mathf.PI - MaxSwingAngleDeg * Mathf.Deg2Rad);
            var minDist = Mathf.Sqrt(Mathf.Max(0.001f, a2b2 - twoAB * cosAtMaxBend));

            var cosAtMinBend = Mathf.Cos(Mathf.PI - MinSwingAngleDeg * Mathf.Deg2Rad);
            var maxDist = Mathf.Sqrt(Mathf.Max(0.001f, a2b2 - twoAB * cosAtMinBend));

            return (minDist, maxDist);
        }
    }
}
