using Project.Features.Climbing.Domain.PhysicsModel;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class WallPanelComponent : MonoBehaviour
    {
        [Header("Wall Properties")]
        [Range(-90f, 90f)]
        [Tooltip("Positive = slab (tilted toward climber), 0 = vertical, negative = overhang")]
        public float wallAngleDegrees;

        public WallSurfaceType surfaceType = WallSurfaceType.Rough;

        /// <summary>
        /// Wall normal in world space (forward direction of the panel).
        /// Convention: forward points away from climbing surface (toward climber).
        /// </summary>
        public Vector3 WallNormal => -transform.forward;

        /// <summary>
        /// Angle from vertical in degrees (90° = vertical wall).
        /// Slab: 70-85°, Vertical: 85-95°, Overhang: 95-115°,
        /// StrongOverhang: 115-145°, Roof: 145-180°.
        /// </summary>
        public float AngleFromVertical => 90f - wallAngleDegrees;

        public WallAngleCategory Category => ClassifyAngle(AngleFromVertical);

        public static WallAngleCategory ClassifyAngle(float angleFromVertical)
        {
            return angleFromVertical switch
            {
                < 85f => WallAngleCategory.Slab,
                < 95f => WallAngleCategory.Vertical,
                < 115f => WallAngleCategory.Overhang,
                < 145f => WallAngleCategory.StrongOverhang,
                _ => WallAngleCategory.Roof,
            };
        }
    }
}
