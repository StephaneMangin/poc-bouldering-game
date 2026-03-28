using System.Text;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class HumanoidRigMapping : MonoBehaviour
    {
        [Header("Core")]
        public Transform head;
        public Transform torso;
        public Transform pelvis;

        [Header("Left Arm")]
        public Transform leftUpperArm;
        public Transform leftForearm;
        public Transform leftHand;

        [Header("Right Arm")]
        public Transform rightUpperArm;
        public Transform rightForearm;
        public Transform rightHand;

        [Header("Left Leg")]
        public Transform leftUpperLeg;
        public Transform leftLowerLeg;
        public Transform leftFoot;

        [Header("Right Leg")]
        public Transform rightUpperLeg;
        public Transform rightLowerLeg;
        public Transform rightFoot;

        public void AutoAssignFromRoot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            pelvis = FindBest(root, "hips", "pelvis");
            torso = FindBest(root, "chest", "upperchest", "torso", "spine2", "spine1", "spine", "ribcage", "body");
            head = FindBest(root, "head");

            leftUpperArm = FindBest(root, "leftupperarm", "lupperarm", "upperarml", "arm_l", "upperarml");
            leftForearm = FindBest(root, "leftlowerarm", "leftforearm", "lforearm", "lowerarml", "forearm_l", "forearml");
            leftHand = FindBest(root, "lefthand", "lhand", "hand_l", "handl");

            rightUpperArm = FindBest(root, "rightupperarm", "rupperarm", "upperarmr", "arm_r", "upperarmr");
            rightForearm = FindBest(root, "rightlowerarm", "rightforearm", "rforearm", "lowerarmr", "forearm_r", "forearmr");
            rightHand = FindBest(root, "righthand", "rhand", "hand_r", "handr");

            leftUpperLeg = FindBest(root, "leftupperleg", "leftupleg", "lupperleg", "thigh_l", "upperlegl", "thighl");
            leftLowerLeg = FindBest(root, "leftlowerleg", "leftleg", "lleg", "calf_l", "shin_l", "lowerlegl", "shinl", "calfl");
            leftFoot = FindBest(root, "leftfoot", "lfoot", "foot_l", "footl");

            rightUpperLeg = FindBest(root, "rightupperleg", "rightupleg", "rupperleg", "thigh_r", "upperlegr", "thighr");
            rightLowerLeg = FindBest(root, "rightlowerleg", "rightleg", "rleg", "calf_r", "shin_r", "lowerlegr", "shinr", "calfr");
            rightFoot = FindBest(root, "rightfoot", "rfoot", "foot_r", "footr");
        }

        public bool ValidateRig(out string report)
        {
            var builder = new StringBuilder();
            var valid = true;

            valid &= Check(head, nameof(head), builder);
            valid &= Check(torso, nameof(torso), builder);
            valid &= Check(pelvis, nameof(pelvis), builder);
            valid &= Check(leftUpperArm, nameof(leftUpperArm), builder);
            valid &= Check(leftForearm, nameof(leftForearm), builder);
            valid &= Check(leftHand, nameof(leftHand), builder);
            valid &= Check(rightUpperArm, nameof(rightUpperArm), builder);
            valid &= Check(rightForearm, nameof(rightForearm), builder);
            valid &= Check(rightHand, nameof(rightHand), builder);
            valid &= Check(leftUpperLeg, nameof(leftUpperLeg), builder);
            valid &= Check(leftLowerLeg, nameof(leftLowerLeg), builder);
            valid &= Check(leftFoot, nameof(leftFoot), builder);
            valid &= Check(rightUpperLeg, nameof(rightUpperLeg), builder);
            valid &= Check(rightLowerLeg, nameof(rightLowerLeg), builder);
            valid &= Check(rightFoot, nameof(rightFoot), builder);

            report = builder.ToString();
            return valid;
        }

        [ContextMenu("Validate Humanoid Rig")]
        private void ValidateHumanoidRig()
        {
            if (ValidateRig(out var report))
            {
                Debug.Log("Humanoid rig mapping is valid.", this);
                return;
            }

            Debug.LogWarning($"Humanoid rig mapping is incomplete:\n{report}", this);
        }

        private static bool Check(Transform value, string label, StringBuilder builder)
        {
            if (value != null)
            {
                return true;
            }

            builder.Append("- Missing ").Append(label).Append('\n');
            return false;
        }

        private static Transform FindBest(Transform root, params string[] aliases)
        {
            Transform best = null;
            var bestScore = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);

            foreach (var candidate in transforms)
            {
                var normalizedName = Normalize(candidate.name);
                foreach (var alias in aliases)
                {
                    var normalizedAlias = Normalize(alias);
                    var score = Score(normalizedName, normalizedAlias);
                    if (score <= bestScore)
                    {
                        continue;
                    }

                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static int Score(string candidate, string alias)
        {
            if (candidate == alias)
            {
                return 300;
            }

            if (candidate.EndsWith(alias))
            {
                return 200;
            }

            if (candidate.Contains(alias))
            {
                return 100;
            }

            return 0;
        }

        private static string Normalize(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }
    }
}
