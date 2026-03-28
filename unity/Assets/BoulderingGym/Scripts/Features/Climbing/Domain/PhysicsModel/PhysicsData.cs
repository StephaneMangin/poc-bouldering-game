using Project.Features.Climbing.Presentation;
using UnityEngine;

namespace Project.Features.Climbing.Domain.PhysicsModel
{
    public enum WallAngleCategory
    {
        Slab,
        Vertical,
        Overhang,
        StrongOverhang,
        Roof,
    }

    public enum WallSurfaceType
    {
        Rough,
        Smooth,
    }

    public readonly struct ContactPoint
    {
        public readonly Vector3 Position;
        public readonly bool IsHand;
        public readonly bool IsActive;
        public readonly HandSide? Side;

        public ContactPoint(Vector3 position, bool isHand, bool isActive, HandSide? side = null)
        {
            Position = position;
            IsHand = isHand;
            IsActive = isActive;
            Side = side;
        }
    }

    public struct COGResult
    {
        public Vector3 Position;
        public float CogOffset;
        public float CogOffsetPenalty;
        public bool IsStable;
        public bool IsInsideSupportPolygon;
    }

    public struct WeightDistribution
    {
        public float WeightOnLeftHand;
        public float WeightOnRightHand;
        public float WeightOnFeet;
        public float TotalHandWeight;
    }

    public struct WallAngleMultipliers
    {
        public float EnduranceMult;
        public float FatigueMult;
        public float GripDrainMult;
    }

    public struct PhysicsSnapshot
    {
        public COGResult COG;
        public WeightDistribution Weight;
        public WallAngleCategory WallCategory;
        public WallAngleMultipliers Multipliers;
        public bool HasBodyWallContact;
        public float BodyWallFriction;
        public float WallAngleDegrees;
    }
}
