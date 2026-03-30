using UnityEngine;

namespace Project.Features.Climbing.Domain
{
    /// <summary>
    /// Pure static solver: given limb contact positions + anatomy,
    /// computes where the player's pelvis should be.
    /// No MonoBehaviour — stateless and fully testable.
    /// </summary>
    public static class ClimbingBodySolver
    {
        public struct AnatomyParams
        {
            /// <summary>Distance from pelvis center to shoulder joint (vertical).</summary>
            public float TorsoHeight;

            /// <summary>Total arm chain length (upper arm + forearm + hand).</summary>
            public float ArmLength;

            /// <summary>Total leg chain length (upper leg + lower leg + foot).</summary>
            public float LegLength;

            /// <summary>Horizontal offset from pelvis center to shoulder (half shoulder width).</summary>
            public float ShoulderHalfWidth;

            /// <summary>Horizontal offset from pelvis center to hip joint (half hip width).</summary>
            public float HipHalfWidth;

            /// <summary>Wall-normal offset from the wall plane to the pelvis.</summary>
            public float WallOffset;

            public static AnatomyParams Default => new()
            {
                TorsoHeight = 0.48f,
                ArmLength = 0.52f,
                LegLength = 0.82f,
                ShoulderHalfWidth = 0.18f,
                HipHalfWidth = 0.12f,
                WallOffset = 0.35f,
            };

            /// <summary>
            /// Builds anatomy params from measured bone positions (typically in T-pose).
            /// Ensures the solver and IK controller use identical chain lengths.
            /// </summary>
            public static AnatomyParams FromMeasurements(
                Vector3 pelvisPos,
                Vector3 leftShoulderPos, Vector3 leftElbowPos, Vector3 leftHandPos,
                Vector3 rightShoulderPos, Vector3 rightElbowPos, Vector3 rightHandPos,
                Vector3 leftHipPos, Vector3 leftKneePos, Vector3 leftFootPos,
                Vector3 rightHipPos, Vector3 rightKneePos, Vector3 rightFootPos)
            {
                var leftArm = Vector3.Distance(leftShoulderPos, leftElbowPos)
                            + Vector3.Distance(leftElbowPos, leftHandPos);
                var rightArm = Vector3.Distance(rightShoulderPos, rightElbowPos)
                             + Vector3.Distance(rightElbowPos, rightHandPos);
                var leftLeg = Vector3.Distance(leftHipPos, leftKneePos)
                            + Vector3.Distance(leftKneePos, leftFootPos);
                var rightLeg = Vector3.Distance(rightHipPos, rightKneePos)
                             + Vector3.Distance(rightKneePos, rightFootPos);

                var shoulderMidY = (leftShoulderPos.y + rightShoulderPos.y) * 0.5f;

                return new AnatomyParams
                {
                    TorsoHeight = Mathf.Max(0.1f, shoulderMidY - pelvisPos.y),
                    ArmLength = (leftArm + rightArm) * 0.5f,
                    LegLength = (leftLeg + rightLeg) * 0.5f,
                    ShoulderHalfWidth = Mathf.Abs(leftShoulderPos.x - rightShoulderPos.x) * 0.5f,
                    HipHalfWidth = Mathf.Abs(leftHipPos.x - rightHipPos.x) * 0.5f,
                    WallOffset = 0.35f,
                };
            }
        }

        public struct LimbContacts
        {
            public Vector3? LeftHand;
            public Vector3? RightHand;
            public Vector3? LeftFoot;
            public Vector3? RightFoot;

            public readonly bool HasAnyHand => LeftHand.HasValue || RightHand.HasValue;
            public readonly bool HasAnyFoot => LeftFoot.HasValue || RightFoot.HasValue;
            public readonly bool HasAny => HasAnyHand || HasAnyFoot;

            public readonly int Count =>
                (LeftHand.HasValue ? 1 : 0)
                + (RightHand.HasValue ? 1 : 0)
                + (LeftFoot.HasValue ? 1 : 0)
                + (RightFoot.HasValue ? 1 : 0);
        }

        public struct BodySolution
        {
            public Vector3 PelvisPosition;
            public bool IsValid;
        }

        /// <summary>
        /// Computes the optimal pelvis position for the given limb contacts.
        /// The pelvis sits below the hands and above the feet, constrained by anatomy.
        /// </summary>
        public static BodySolution Solve(LimbContacts contacts, AnatomyParams anatomy, Vector3 wallNormal)
        {
            if (!contacts.HasAny)
            {
                return new BodySolution { IsValid = false };
            }

            var footCenter = ComputeFootCenter(contacts);
            var handCenter = ComputeHandCenter(contacts);

            var pelvisY = ComputePelvisY(contacts, anatomy, footCenter, handCenter);
            var pelvisX = ComputePelvisX(contacts, footCenter, handCenter);
            var pelvisZ = ComputePelvisZ(contacts, anatomy, wallNormal);

            var pelvis = new Vector3(pelvisX, pelvisY, pelvisZ);
            pelvis = ClampPelvisToReachAllContacts(pelvis, contacts, anatomy);

            return new BodySolution
            {
                PelvisPosition = pelvis,
                IsValid = true,
            };
        }

        /// <summary>
        /// Checks if a limb can physically reach a hold from the given pelvis position.
        /// </summary>
        public static bool CanReach(
            Vector3 pelvisPosition,
            Vector3 holdPosition,
            bool isHand,
            bool isLeft,
            AnatomyParams anatomy)
        {
            var jointOrigin = isHand
                ? GetShoulderPosition(pelvisPosition, isLeft, anatomy)
                : GetHipPosition(pelvisPosition, isLeft, anatomy);

            var chainLength = isHand ? anatomy.ArmLength : anatomy.LegLength;
            var distance = Vector3.Distance(jointOrigin, holdPosition);

            return distance <= chainLength;
        }

        /// <summary>
        /// Computes proposed pelvis position if a new limb were placed, then checks all contacts are still reachable.
        /// </summary>
        public static bool CanPlaceAndStillReachAll(
            LimbContacts currentContacts,
            Vector3 newHoldPosition,
            bool isHand,
            bool isLeft,
            AnatomyParams anatomy,
            Vector3 wallNormal)
        {
            var proposed = currentContacts;
            if (isHand)
            {
                if (isLeft) proposed.LeftHand = newHoldPosition;
                else proposed.RightHand = newHoldPosition;
            }
            else
            {
                if (isLeft) proposed.LeftFoot = newHoldPosition;
                else proposed.RightFoot = newHoldPosition;
            }

            var solution = Solve(proposed, anatomy, wallNormal);
            if (!solution.IsValid)
            {
                return false;
            }

            return AllContactsReachable(proposed, solution.PelvisPosition, anatomy);
        }

        public static Vector3 GetShoulderPosition(Vector3 pelvis, bool isLeft, AnatomyParams anatomy)
        {
            var side = isLeft ? -1f : 1f;
            return pelvis + new Vector3(anatomy.ShoulderHalfWidth * side, anatomy.TorsoHeight, 0f);
        }

        public static Vector3 GetHipPosition(Vector3 pelvis, bool isLeft, AnatomyParams anatomy)
        {
            var side = isLeft ? -1f : 1f;
            return pelvis + new Vector3(anatomy.HipHalfWidth * side, 0f, 0f);
        }

        /// <summary>
        /// Computes the player root position from the solver's pelvis target,
        /// accounting for the offset between the root transform and the actual pelvis bone.
        /// Without this, the root gets the pelvis position → actual pelvis ends up too high → legs at 180°.
        /// </summary>
        public static Vector3 ComputeRootPosition(Vector3 solverPelvisPosition, Vector3 pelvisBoneLocalOffset)
        {
            return solverPelvisPosition - pelvisBoneLocalOffset;
        }

        /// <summary>
        /// Returns the extension ratio of a limb: distance(joint, hold) / chainLength.
        /// 0.0 = fully folded, 1.0 = fully straight (180°), >1.0 = impossible (hyper-extension).
        /// Healthy climbing: 0.3–0.85. Above 0.95 looks unnatural.
        /// </summary>
        public static float ComputeLimbExtensionRatio(
            Vector3 pelvisPosition,
            Vector3 holdPosition,
            bool isHand,
            bool isLeft,
            AnatomyParams anatomy)
        {
            var jointOrigin = isHand
                ? GetShoulderPosition(pelvisPosition, isLeft, anatomy)
                : GetHipPosition(pelvisPosition, isLeft, anatomy);

            var chainLength = isHand ? anatomy.ArmLength : anatomy.LegLength;
            if (chainLength <= 0f) return 0f;

            return Vector3.Distance(jointOrigin, holdPosition) / chainLength;
        }

        /// <summary>
        /// Computes the IK target for an end effector bone so that its extremity
        /// (fingertip or toe tip) lands exactly on the grip position.
        /// Pulls the target back from the grip along the bone→grip direction.
        /// </summary>
        public static Vector3 ComputeExtremityIKTarget(
            Vector3 endBonePosition, Vector3 gripPosition, float extremityLength)
        {
            var boneToGrip = gripPosition - endBonePosition;
            if (boneToGrip.sqrMagnitude < 0.0001f || extremityLength <= 0f)
            {
                return gripPosition;
            }

            return gripPosition - boneToGrip.normalized * extremityLength;
        }

        /// <summary>
        /// Reconstructs the extremity tip position from the end bone (post-IK),
        /// assuming the extremity is oriented toward the grip.
        /// Returns gripPosition when the orientation is correct.
        /// </summary>
        public static Vector3 ReconstructExtremityTip(
            Vector3 endBonePostIK, Vector3 gripPosition, float extremityLength)
        {
            var toGrip = gripPosition - endBonePostIK;
            if (toGrip.sqrMagnitude < 0.0001f)
            {
                return endBonePostIK;
            }

            return endBonePostIK + toGrip.normalized * extremityLength;
        }

        private static float ComputePelvisY(
            LimbContacts contacts,
            AnatomyParams anatomy,
            Vector3? footCenter,
            Vector3? handCenter)
        {
            if (contacts.HasAnyFoot && contacts.HasAnyHand)
            {
                // Normal climbing: pelvis driven by feet pushing up, constrained by hands pulling
                var feetPushY = footCenter.Value.y + anatomy.LegLength * 0.75f;
                var handsPullY = handCenter.Value.y - anatomy.TorsoHeight;

                // Pelvis can't be higher than hands allow (hanging below shoulders)
                // Pelvis can't be lower than feet allow (legs would hyper-extend)
                var minY = footCenter.Value.y + anatomy.LegLength * 0.15f;
                return Mathf.Clamp(Mathf.Min(feetPushY, handsPullY), minY, handsPullY);
            }

            if (contacts.HasAnyHand)
            {
                // Hanging from hands only — pelvis hangs below
                return handCenter.Value.y - anatomy.TorsoHeight - anatomy.LegLength * 0.3f;
            }

            // Standing on feet only — pelvis above feet
            return footCenter.Value.y + anatomy.LegLength * 0.75f;
        }

        private static float ComputePelvisX(
            LimbContacts contacts,
            Vector3? footCenter,
            Vector3? handCenter)
        {
            if (contacts.HasAnyFoot && contacts.HasAnyHand)
            {
                // Weighted: 60% feet, 40% hands (like real climbing — hips over feet)
                return footCenter.Value.x * 0.6f + handCenter.Value.x * 0.4f;
            }

            if (contacts.HasAnyFoot)
            {
                return footCenter.Value.x;
            }

            return handCenter.Value.x;
        }

        private static float ComputePelvisZ(
            LimbContacts contacts,
            AnatomyParams anatomy,
            Vector3 wallNormal)
        {
            // Average Z of all contacts, then offset away from wall
            var sumZ = 0f;
            var count = 0;

            if (contacts.LeftHand.HasValue) { sumZ += contacts.LeftHand.Value.z; count++; }
            if (contacts.RightHand.HasValue) { sumZ += contacts.RightHand.Value.z; count++; }
            if (contacts.LeftFoot.HasValue) { sumZ += contacts.LeftFoot.Value.z; count++; }
            if (contacts.RightFoot.HasValue) { sumZ += contacts.RightFoot.Value.z; count++; }

            var wallZ = count > 0 ? sumZ / count : 0f;
            return wallZ + wallNormal.z * anatomy.WallOffset;
        }

        private static Vector3? ComputeFootCenter(LimbContacts contacts)
        {
            if (!contacts.HasAnyFoot) return null;

            var sum = Vector3.zero;
            var count = 0;
            if (contacts.LeftFoot.HasValue) { sum += contacts.LeftFoot.Value; count++; }
            if (contacts.RightFoot.HasValue) { sum += contacts.RightFoot.Value; count++; }
            return sum / count;
        }

        private static Vector3? ComputeHandCenter(LimbContacts contacts)
        {
            if (!contacts.HasAnyHand) return null;

            var sum = Vector3.zero;
            var count = 0;
            if (contacts.LeftHand.HasValue) { sum += contacts.LeftHand.Value; count++; }
            if (contacts.RightHand.HasValue) { sum += contacts.RightHand.Value; count++; }
            return sum / count;
        }

        private static bool AllContactsReachable(LimbContacts contacts, Vector3 pelvis, AnatomyParams anatomy)
        {
            if (contacts.LeftHand.HasValue && !CanReach(pelvis, contacts.LeftHand.Value, true, true, anatomy))
                return false;
            if (contacts.RightHand.HasValue && !CanReach(pelvis, contacts.RightHand.Value, true, false, anatomy))
                return false;
            if (contacts.LeftFoot.HasValue && !CanReach(pelvis, contacts.LeftFoot.Value, false, true, anatomy))
                return false;
            if (contacts.RightFoot.HasValue && !CanReach(pelvis, contacts.RightFoot.Value, false, false, anatomy))
                return false;
            return true;
        }

        /// <summary>
        /// Iteratively adjusts the pelvis Y so that all limbs remain within their chain length.
        /// Hands constrain from above (pelvis too low → arms overextend).
        /// Feet constrain from below (pelvis too high → legs overextend).
        /// </summary>
        private static Vector3 ClampPelvisToReachAllContacts(
            Vector3 pelvis, LimbContacts contacts, AnatomyParams anatomy)
        {
            const int maxIterations = 8;
            const float margin = 0.95f;

            for (var i = 0; i < maxIterations; i++)
            {
                var adjustment = 0f;
                var worstRatio = 0f;

                adjustment = AccumulateAdjustment(
                    pelvis, contacts.LeftHand, true, true, anatomy, margin, adjustment, ref worstRatio);
                adjustment = AccumulateAdjustment(
                    pelvis, contacts.RightHand, true, false, anatomy, margin, adjustment, ref worstRatio);
                adjustment = AccumulateAdjustment(
                    pelvis, contacts.LeftFoot, false, true, anatomy, margin, adjustment, ref worstRatio);
                adjustment = AccumulateAdjustment(
                    pelvis, contacts.RightFoot, false, false, anatomy, margin, adjustment, ref worstRatio);

                if (worstRatio <= 1f)
                {
                    break;
                }

                pelvis.y += adjustment;
            }

            return pelvis;
        }

        private static float AccumulateAdjustment(
            Vector3 pelvis, Vector3? contact, bool isHand, bool isLeft,
            AnatomyParams anatomy, float margin, float currentAdj, ref float worstRatio)
        {
            if (!contact.HasValue)
            {
                return currentAdj;
            }

            var ratio = ComputeLimbExtensionRatio(pelvis, contact.Value, isHand, isLeft, anatomy);
            if (ratio <= 1f)
            {
                return currentAdj;
            }

            if (ratio > worstRatio)
            {
                worstRatio = ratio;
            }

            // Hands are above: if arm overextends, pelvis must move UP toward the hand
            // Feet are below: if leg overextends, pelvis must move DOWN toward the foot
            var chainLength = isHand ? anatomy.ArmLength : anatomy.LegLength;
            var overshoot = (ratio - margin) * chainLength;
            var correction = isHand ? overshoot : -overshoot;

            // Take the adjustment with the largest magnitude per direction
            return Mathf.Abs(correction) > Mathf.Abs(currentAdj) ? correction : currentAdj;
        }
    }
}
