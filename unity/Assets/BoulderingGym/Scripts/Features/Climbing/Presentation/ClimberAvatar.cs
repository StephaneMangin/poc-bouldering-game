using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    /// <summary>
    /// Poses the humanoid character on the wall: body placed by the pose solver, limbs driven by
    /// Animator IK toward their goals with elbows and knees pushed away from the wall. After the IK
    /// pass, hands and feet are re-aimed from their own child bones (fingers up the wall, toes into it),
    /// which does not depend on the rig's bone axes.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class ClimberAvatar : MonoBehaviour
    {
        private const float HandToGrip = 0.08f;
        private const float AnkleToSole = 0.09f;

        private readonly LimbGoal[] _goals = new LimbGoal[4];
        private Animator _animator;
        private Vector3 _bodyToHips;
        private bool _calibrated;

        private struct LimbGoal
        {
            public Vector3 Position;
            public Vector3 Hint;
            public Vector3 Aim;
            public float Weight;
        }

        public bool IkActive { get; set; }

        public Vector3 PelvisTarget { get; set; }

        public Quaternion BodyRotation { get; set; } = Quaternion.identity;

        public Animator Animator => _animator;

        /// <summary>Height of the hip joints above the soles in the standing pose.</summary>
        public float StandingHipsHeight { get; private set; }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            StandingHipsHeight = _animator.GetBoneTransform(HumanBodyBones.Hips).position.y - transform.position.y;
        }

        /// <summary>
        /// Sets where a limb reaches. For a hand, contact is the grip point and aim the fingers direction;
        /// for a foot, contact is where the sole rests and aim the toes direction.
        /// </summary>
        public void SetGoal(LimbId limb, Vector3 contact, Vector3 aim, float weight, Vector3 hint)
        {
            var isHand = ContactSet.IsHand(limb);
            _goals[(int)limb] = new LimbGoal
            {
                Position = isHand ? contact - aim * HandToGrip : contact + Vector3.up * AnkleToSole,
                Hint = hint,
                Aim = aim,
                Weight = weight,
            };
        }

        /// <summary>Where a hand or foot is drawn right now.</summary>
        public Vector3 LimbPosition(LimbId limb)
        {
            var bone = limb switch
            {
                LimbId.LeftHand => HumanBodyBones.LeftHand,
                LimbId.RightHand => HumanBodyBones.RightHand,
                LimbId.LeftFoot => HumanBodyBones.LeftFoot,
                _ => HumanBodyBones.RightFoot,
            };
            return _animator.GetBoneTransform(bone).position;
        }

        /// <summary>Upper body bone the camera looks at.</summary>
        public Transform Chest => _animator.GetBoneTransform(HumanBodyBones.Chest) ?? _animator.GetBoneTransform(HumanBodyBones.Hips);

        /// <summary>Bone lengths of the rig, so reach rules match what the IK can actually do.</summary>
        public ClimberAnatomy Measure()
        {
            Vector3 Bone(HumanBodyBones bone) => _animator.GetBoneTransform(bone).position;
            float Chain(HumanBodyBones a, HumanBodyBones b, HumanBodyBones c) => Vector3.Distance(Bone(a), Bone(b)) + Vector3.Distance(Bone(b), Bone(c));
            var shoulders = (Bone(HumanBodyBones.LeftUpperArm) + Bone(HumanBodyBones.RightUpperArm)) * 0.5f;
            return new ClimberAnatomy
            {
                ArmReach = Chain(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand) + HandToGrip,
                LegReach = Chain(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot) + AnkleToSole,
                TorsoLength = Vector3.Distance(Bone(HumanBodyBones.Hips), shoulders),
                ShoulderHalfWidth = Vector3.Distance(Bone(HumanBodyBones.LeftUpperArm), Bone(HumanBodyBones.RightUpperArm)) * 0.5f,
                HipHalfWidth = Vector3.Distance(Bone(HumanBodyBones.LeftUpperLeg), Bone(HumanBodyBones.RightUpperLeg)) * 0.5f,
            };
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_calibrated)
            {
                _bodyToHips = _animator.GetBoneTransform(HumanBodyBones.Hips).position - _animator.bodyPosition;
                _calibrated = true;
            }

            var weightScale = IkActive ? 1f : 0f;
            if (IkActive)
            {
                _animator.bodyPosition = PelvisTarget - _bodyToHips;
                _animator.bodyRotation = BodyRotation;
            }

            ApplyGoal(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, _goals[(int)LimbId.LeftHand], weightScale);
            ApplyGoal(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, _goals[(int)LimbId.RightHand], weightScale);
            ApplyGoal(AvatarIKGoal.LeftFoot, AvatarIKHint.LeftKnee, _goals[(int)LimbId.LeftFoot], weightScale);
            ApplyGoal(AvatarIKGoal.RightFoot, AvatarIKHint.RightKnee, _goals[(int)LimbId.RightFoot], weightScale);
        }

        private void ApplyGoal(AvatarIKGoal goal, AvatarIKHint hint, LimbGoal limb, float weightScale)
        {
            var weight = limb.Weight * weightScale;
            _animator.SetIKPositionWeight(goal, weight);
            _animator.SetIKRotationWeight(goal, 0f);
            _animator.SetIKHintPositionWeight(hint, weight);
            if (weight <= 0f)
            {
                return;
            }

            _animator.SetIKPosition(goal, limb.Position);
            _animator.SetIKHintPosition(hint, limb.Hint);
        }

        private void LateUpdate()
        {
            if (!IkActive)
            {
                return;
            }

            AimBone(HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, _goals[(int)LimbId.LeftHand]);
            AimBone(HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, _goals[(int)LimbId.RightHand]);
            AimBone(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, _goals[(int)LimbId.LeftFoot]);
            AimBone(HumanBodyBones.RightFoot, HumanBodyBones.RightToes, _goals[(int)LimbId.RightFoot]);
        }

        /// <summary>Turns a hand or foot so its child bone points along the aim.</summary>
        private void AimBone(HumanBodyBones boneId, HumanBodyBones childId, LimbGoal goal)
        {
            var bone = _animator.GetBoneTransform(boneId);
            var child = _animator.GetBoneTransform(childId);
            if (bone == null || child == null || goal.Weight <= 0f)
            {
                return;
            }

            var aimed = Quaternion.FromToRotation(child.position - bone.position, goal.Aim) * bone.rotation;
            bone.rotation = Quaternion.Slerp(bone.rotation, aimed, goal.Weight);
        }
    }
}
