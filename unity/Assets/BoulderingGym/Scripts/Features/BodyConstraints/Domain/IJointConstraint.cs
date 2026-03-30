using UnityEngine;

namespace Project.Features.BodyConstraints.Domain
{
    /// <summary>
    /// Centralized interface for all anatomical joint constraints.
    /// Each joint exposes its ROM (Range of Motion) limits and provides
    /// direction clamping to prevent impossible poses during IK solving.
    ///
    /// Convention:
    ///   parentAxis  = direction of the bone ABOVE the joint (flowing into the joint).
    ///   childDirection = direction of the bone BELOW the joint (flowing out of the joint).
    ///   angle = Vector3.Angle(parentAxis, childDirection).
    ///
    /// For hinge joints (1 DoF), the angle fully describes the joint state.
    /// For multi-DoF joints, ClampDirection enforces the primary swing constraint;
    /// twist and secondary-plane limits are exposed as constants on each type.
    /// </summary>
    public interface IJointConstraint
    {
        /// <summary>Display name of the joint (e.g. "Elbow", "Shoulder").</summary>
        string JointName { get; }

        /// <summary>Number of degrees of freedom for this joint.</summary>
        int DegreesOfFreedom { get; }

        /// <summary>
        /// Clamp <paramref name="childDirection"/> so that the child bone stays
        /// within this joint's ROM relative to its parent bone axis.
        /// Returns the (potentially clamped) direction, always normalized.
        /// If either input has near-zero magnitude, returns childDirection unchanged.
        /// </summary>
        Vector3 ClampDirection(Vector3 parentAxis, Vector3 childDirection);
    }
}
