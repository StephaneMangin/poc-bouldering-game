using System.Collections.Generic;
using Project.Core.Domain;
using Project.Features.Climbing.Presentation;
using UnityEngine;

namespace Project.Features.Climbing.Domain.PhysicsModel
{
    /// <summary>
    /// Pure domain class — computes COG, weight distribution, wall angle multipliers.
    /// No MonoBehaviour dependency; driven by <see cref="PhysicsModelDriver"/>.
    /// </summary>
    public sealed class ClimbingPhysicsModel
    {
        private readonly TuningConfig _config;

        public ClimbingPhysicsModel(TuningConfig config)
        {
            _config = config;
        }

        public PhysicsSnapshot ComputeSnapshot(
            ContactPoint[] contacts,
            Vector3 pelvisPosition,
            Vector3 wallNormal,
            WallAngleCategory wallCategory,
            float wallAngleDegrees,
            float bodyWallDistance)
        {
            var cog = ComputeCOG(contacts, pelvisPosition, wallNormal);
            var weight = ComputeWeight(contacts, cog.Position, wallCategory);
            var multipliers = GetMultipliers(wallCategory);
            var hasBodyContact = wallCategory == WallAngleCategory.Slab
                                 && bodyWallDistance < _config.bodyWallContactThreshold
                                 && bodyWallDistance >= 0f;

            return new PhysicsSnapshot
            {
                COG = cog,
                Weight = weight,
                WallCategory = wallCategory,
                Multipliers = multipliers,
                HasBodyWallContact = hasBodyContact,
                BodyWallFriction = hasBodyContact ? _config.slabBodyFriction : 0f,
                WallAngleDegrees = wallAngleDegrees,
            };
        }

        public COGResult ComputeCOG(ContactPoint[] contacts, Vector3 pelvisPosition, Vector3 wallNormal)
        {
            if (contacts == null || contacts.Length == 0)
            {
                return new COGResult
                {
                    Position = pelvisPosition,
                    CogOffset = 0f,
                    CogOffsetPenalty = 0f,
                    IsStable = false,
                    IsInsideSupportPolygon = false,
                };
            }

            var hasHands = false;
            var hasFeet = false;
            foreach (var c in contacts)
            {
                if (!c.IsActive) continue;
                if (c.IsHand) hasHands = true;
                else hasFeet = true;
            }

            // Weight factors: hands 40% / feet 60% when both exist
            var handWeight = hasHands && hasFeet ? 0.4f : (hasHands ? 1f : 0f);
            var footWeight = hasHands && hasFeet ? 0.6f : (hasFeet ? 1f : 0f);

            var accumulated = Vector3.zero;
            var totalWeight = 0f;

            foreach (var c in contacts)
            {
                if (!c.IsActive) continue;
                var w = c.IsHand ? handWeight : footWeight;
                accumulated += c.Position * w;
                totalWeight += w;
            }

            var cogPosition = totalWeight > 0f ? accumulated / totalWeight : pelvisPosition;

            // COG offset: horizontal distance from COG to wall plane
            var wallPlanePoint = pelvisPosition; // approximate wall plane through pelvis
            var cogToWall = Vector3.Dot(cogPosition - wallPlanePoint, -wallNormal);
            var cogOffset = Mathf.Abs(cogToWall);

            // COG offset penalty
            var penaltyRange = _config.cogMaxOffset - _config.cogOffsetPenaltyStart;
            var cogOffsetPenalty = penaltyRange > 0f
                ? Mathf.Max(0f, (cogOffset - _config.cogOffsetPenaltyStart) / penaltyRange) * _config.cogOffsetPenaltyMult
                : 0f;

            var isStable = cogOffset < _config.cogMaxOffset;
            var isInside = IsInsideSupportPolygon(contacts, cogPosition);

            return new COGResult
            {
                Position = cogPosition,
                CogOffset = cogOffset,
                CogOffsetPenalty = cogOffsetPenalty,
                IsStable = isStable && isInside,
                IsInsideSupportPolygon = isInside,
            };
        }

        public WeightDistribution ComputeWeight(ContactPoint[] contacts, Vector3 cogPosition, WallAngleCategory wallCategory)
        {
            var handRatio = GetHandWeightRatio(wallCategory);
            var feetRatio = 1f - handRatio;

            var leftHandDist = 0f;
            var rightHandDist = 0f;
            var hasLeft = false;
            var hasRight = false;

            if (contacts != null)
            {
                foreach (var c in contacts)
                {
                    if (!c.IsActive || !c.IsHand) continue;
                    var dist = Vector3.Distance(c.Position, cogPosition);
                    if (dist < 0.001f) dist = 0.001f;

                    if (c.Side == HandSide.Left) { leftHandDist = dist; hasLeft = true; }
                    else if (c.Side == HandSide.Right) { rightHandDist = dist; hasRight = true; }
                }
            }

            float leftWeight, rightWeight;
            if (hasLeft && hasRight)
            {
                // Inverse distance weighting: closer hand bears more weight
                var totalDist = leftHandDist + rightHandDist;
                leftWeight = (1f - leftHandDist / totalDist) * handRatio;
                rightWeight = (1f - rightHandDist / totalDist) * handRatio;
            }
            else if (hasLeft)
            {
                leftWeight = handRatio;
                rightWeight = 0f;
            }
            else if (hasRight)
            {
                leftWeight = 0f;
                rightWeight = handRatio;
            }
            else
            {
                leftWeight = 0f;
                rightWeight = 0f;
                feetRatio = 1f;
            }

            return new WeightDistribution
            {
                WeightOnLeftHand = leftWeight,
                WeightOnRightHand = rightWeight,
                WeightOnFeet = feetRatio,
                TotalHandWeight = leftWeight + rightWeight,
            };
        }

        public WallAngleMultipliers GetMultipliers(WallAngleCategory category)
        {
            return category switch
            {
                WallAngleCategory.Slab => new WallAngleMultipliers
                {
                    EnduranceMult = _config.wallAngleEnduranceMultSlab,
                    FatigueMult = _config.wallAngleFatigueMultSlab,
                    GripDrainMult = _config.wallAngleGripDrainMultSlab,
                },
                WallAngleCategory.Vertical => new WallAngleMultipliers
                {
                    EnduranceMult = _config.wallAngleEnduranceMultVertical,
                    FatigueMult = _config.wallAngleFatigueMultVertical,
                    GripDrainMult = _config.wallAngleGripDrainMultVertical,
                },
                WallAngleCategory.Overhang => new WallAngleMultipliers
                {
                    EnduranceMult = _config.wallAngleEnduranceMultOverhang,
                    FatigueMult = _config.wallAngleFatigueMultOverhang,
                    GripDrainMult = _config.wallAngleGripDrainMultOverhang,
                },
                WallAngleCategory.StrongOverhang => new WallAngleMultipliers
                {
                    EnduranceMult = _config.wallAngleEnduranceMultStrongOverhang,
                    FatigueMult = _config.wallAngleFatigueMultStrongOverhang,
                    GripDrainMult = _config.wallAngleGripDrainMultStrongOverhang,
                },
                WallAngleCategory.Roof => new WallAngleMultipliers
                {
                    EnduranceMult = _config.wallAngleEnduranceMultRoof,
                    FatigueMult = _config.wallAngleFatigueMultRoof,
                    GripDrainMult = _config.wallAngleGripDrainMultRoof,
                },
                _ => new WallAngleMultipliers
                {
                    EnduranceMult = 1f,
                    FatigueMult = 1f,
                    GripDrainMult = 1f,
                },
            };
        }

        /// <summary>
        /// Tests whether the COG projection (XY plane) lies inside the convex hull
        /// of active contact points. Simplified for 2-4 contact points.
        /// </summary>
        public static bool IsInsideSupportPolygon(ContactPoint[] contacts, Vector3 cogPosition)
        {
            var activePoints = new List<Vector2>(4);
            foreach (var c in contacts)
            {
                if (c.IsActive)
                {
                    activePoints.Add(new Vector2(c.Position.x, c.Position.y));
                }
            }

            if (activePoints.Count < 3)
            {
                // With fewer than 3 points, no polygon — stable only if COG is near the line or point
                if (activePoints.Count == 0) return false;
                if (activePoints.Count == 1)
                {
                    return Vector2.Distance(new Vector2(cogPosition.x, cogPosition.y), activePoints[0]) < 0.3f;
                }
                // 2 points: check if COG is near the line segment
                return DistanceToSegment(new Vector2(cogPosition.x, cogPosition.y), activePoints[0], activePoints[1]) < 0.2f;
            }

            var hull = ConvexHull(activePoints);
            return IsPointInConvexPolygon(new Vector2(cogPosition.x, cogPosition.y), hull);
        }

        private float GetHandWeightRatio(WallAngleCategory category)
        {
            return category switch
            {
                WallAngleCategory.Slab => _config.weightRatioHandsSlab,
                WallAngleCategory.Vertical => _config.weightRatioHandsVertical,
                WallAngleCategory.Overhang => _config.weightRatioHandsOverhang,
                WallAngleCategory.StrongOverhang => _config.weightRatioHandsStrongOverhang,
                WallAngleCategory.Roof => _config.weightRatioHandsRoof,
                _ => _config.weightRatioHandsVertical,
            };
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var ap = p - a;
            var t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
            var projection = a + ab * t;
            return Vector2.Distance(p, projection);
        }

        /// <summary>
        /// Gift-wrapping (Jarvis march) for small point sets.
        /// </summary>
        private static List<Vector2> ConvexHull(List<Vector2> points)
        {
            if (points.Count <= 3) return points;

            var hull = new List<Vector2>();

            // Find leftmost point
            var startIdx = 0;
            for (var i = 1; i < points.Count; i++)
            {
                if (points[i].x < points[startIdx].x ||
                    (Mathf.Approximately(points[i].x, points[startIdx].x) && points[i].y < points[startIdx].y))
                {
                    startIdx = i;
                }
            }

            var current = startIdx;
            do
            {
                hull.Add(points[current]);
                var next = 0;
                for (var i = 0; i < points.Count; i++)
                {
                    if (i == current) continue;
                    if (next == current || Cross(points[current], points[next], points[i]) < 0)
                    {
                        next = i;
                    }
                }
                current = next;
            } while (current != startIdx && hull.Count < points.Count);

            return hull;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }

        private static bool IsPointInConvexPolygon(Vector2 point, List<Vector2> polygon)
        {
            if (polygon.Count < 3) return false;

            var sign = 0;
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                var cross = Cross(a, b, point);

                if (Mathf.Approximately(cross, 0f)) continue;

                var currentSign = cross > 0 ? 1 : -1;
                if (sign == 0) sign = currentSign;
                else if (sign != currentSign) return false;
            }

            return true;
        }
    }
}
