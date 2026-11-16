using Project.Core.Domain;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Resources.Domain
{
    /// <summary>What the climber hangs on this frame, as seen by the resource rules.</summary>
    public readonly struct ClimbLoad
    {
        public ClimbLoad(GripType? leftGrip, GripType? rightGrip, int feetSupporting, float wallAngleDeg)
        {
            LeftGrip = leftGrip;
            RightGrip = rightGrip;
            FeetSupporting = feetSupporting;
            WallAngleDeg = wallAngleDeg;
        }

        /// <summary>Grip of the left hand, null when the hand is free.</summary>
        public GripType? LeftGrip { get; }

        public GripType? RightGrip { get; }

        public int FeetSupporting { get; }

        public float WallAngleDeg { get; }

        public int HandsHolding => (LeftGrip.HasValue ? 1 : 0) + (RightGrip.HasValue ? 1 : 0);
    }

    /// <summary>
    /// Endurance, grip (adherence) and per-arm fatigue working together while climbing:
    /// arms pump according to the share of body weight they carry (wall angle, feet on holds) and the
    /// hold type; a holding arm reaching the failure threshold slips. Low adherence and an empty
    /// endurance make arms pump faster; chalk restores adherence; standing on the floor recovers.
    /// </summary>
    public sealed class ClimberResources
    {
        private const float SlabAngle = -5f;
        private const float VerticalAngle = 8f;
        private const float OverhangAngle = 25f;
        private const float StrongOverhangAngle = 45f;
        private const float OneArmLoadReference = 0.5f;

        private readonly TuningConfig _config;

        public ClimberResources(TuningConfig config)
        {
            _config = config;
            Endurance = new EnduranceSystem(config);
            Grip = new GripSystem(config);
            Fatigue = new LocalFatigueSystem(config);
        }

        public EnduranceSystem Endurance { get; }

        public GripSystem Grip { get; }

        public LocalFatigueSystem Fatigue { get; }

        public void OnLimbPlaced(float wallAngleDeg)
        {
            Endurance.ApplyActionCost(ClimbingAction.Grab, 0f, WallMultiplier(wallAngleDeg, Kind.Endurance));
        }

        /// <summary>Advances one frame on the wall; returns the hand that slips, if any.</summary>
        public LimbId? UpdateOnWall(float deltaTime, ClimbLoad load)
        {
            var handShare = HandLoadShare(load);
            var perArm = load.HandsHolding > 0 ? handShare / load.HandsHolding / OneArmLoadReference : 0f;
            var multiplier = WallMultiplier(load.WallAngleDeg, Kind.Fatigue) * AdherenceFactor() * EnduranceFactor();
            Fatigue.Update(
                deltaTime,
                load.LeftGrip ?? GripType.Open, perArm, load.LeftGrip.HasValue,
                load.RightGrip ?? GripType.Open, perArm, load.RightGrip.HasValue,
                multiplier);
            Grip.UpdateDrain(deltaTime, handShare, WallMultiplier(load.WallAngleDeg, Kind.GripDrain) * AverageGripDrain(load), 1f);

            var failure = _config.fatigueFailureThreshold;
            if (load.LeftGrip.HasValue && Fatigue.LeftFatigue >= failure)
            {
                return LimbId.LeftHand;
            }

            return load.RightGrip.HasValue && Fatigue.RightFatigue >= failure ? LimbId.RightHand : null;
        }

        /// <summary>Standing on the floor: endurance refills and both arms recover.</summary>
        public void UpdateResting(float deltaTime)
        {
            Endurance.UpdateRegen(deltaTime);
            Grip.UpdateCooldown(deltaTime);
            Fatigue.Update(deltaTime, GripType.Open, 0f, false, GripType.Open, 0f, false, 1f);
        }

        public bool TryChalk(ClimbLoad load) => load.HandsHolding < 2 && Grip.TryUseChalk();

        public void Reset()
        {
            Endurance.Reset();
            Grip.Reset();
            Fatigue.Reset();
        }

        /// <summary>Share of the body weight carried by the arms.</summary>
        private float HandLoadShare(ClimbLoad load)
        {
            var share = WallMultiplier(load.WallAngleDeg, Kind.HandWeight);
            return load.FeetSupporting switch
            {
                >= 2 => share,
                1 => share + (1f - share) * _config.oneFootHandLoadShare,
                _ => 1f,
            };
        }

        private float AdherenceFactor()
        {
            if (Grip.Current <= 0f)
            {
                return _config.adherenceEmptyFatigueMult;
            }

            return Grip.IsMicroSlipping ? _config.adherenceLowFatigueMult : 1f;
        }

        private float EnduranceFactor()
        {
            return Mathf.Lerp(_config.enduranceEmptyFatigueMult, 1f, Endurance.Ratio);
        }

        private float AverageGripDrain(ClimbLoad load)
        {
            var sum = 0f;
            sum += load.LeftGrip.HasValue ? Grip.GetGripTypeDrainMult(load.LeftGrip.Value) : 0f;
            sum += load.RightGrip.HasValue ? Grip.GetGripTypeDrainMult(load.RightGrip.Value) : 0f;
            return load.HandsHolding > 0 ? sum / load.HandsHolding : 1f;
        }

        private enum Kind
        {
            Endurance,
            Fatigue,
            GripDrain,
            HandWeight,
        }

        private float WallMultiplier(float angleDeg, Kind kind)
        {
            var band = angleDeg < SlabAngle ? 0 : angleDeg <= VerticalAngle ? 1 : angleDeg <= OverhangAngle ? 2 : angleDeg <= StrongOverhangAngle ? 3 : 4;
            return kind switch
            {
                Kind.Endurance => Pick(band, _config.wallAngleEnduranceMultSlab, _config.wallAngleEnduranceMultVertical, _config.wallAngleEnduranceMultOverhang, _config.wallAngleEnduranceMultStrongOverhang, _config.wallAngleEnduranceMultRoof),
                Kind.Fatigue => Pick(band, _config.wallAngleFatigueMultSlab, _config.wallAngleFatigueMultVertical, _config.wallAngleFatigueMultOverhang, _config.wallAngleFatigueMultStrongOverhang, _config.wallAngleFatigueMultRoof),
                Kind.GripDrain => Pick(band, _config.wallAngleGripDrainMultSlab, _config.wallAngleGripDrainMultVertical, _config.wallAngleGripDrainMultOverhang, _config.wallAngleGripDrainMultStrongOverhang, _config.wallAngleGripDrainMultRoof),
                _ => Pick(band, _config.weightRatioHandsSlab, _config.weightRatioHandsVertical, _config.weightRatioHandsOverhang, _config.weightRatioHandsStrongOverhang, _config.weightRatioHandsRoof),
            };
        }

        private static float Pick(int band, params float[] values) => values[band];
    }
}
