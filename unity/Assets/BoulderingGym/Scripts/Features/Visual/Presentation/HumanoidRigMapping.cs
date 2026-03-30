using System.Text;
using UnityEngine;

namespace Project.Features.Visual.Presentation
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
        public Transform leftToeBase;

        [Header("Right Leg")]
        public Transform rightUpperLeg;
        public Transform rightLowerLeg;
        public Transform rightFoot;
        public Transform rightToeBase;

        public void AutoAssignFromRoot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            // Mixamo rig: bones are named "mixamorig:BoneName"
            // Unity may strip the prefix depending on import settings
            pelvis = FindBest(root, "hips");
            torso = FindBest(root, "spine2", "spine1", "spine");
            head = FindBest(root, "head");

            leftUpperArm = FindBest(root, "leftarm");
            leftForearm = FindBest(root, "leftforearm");
            leftHand = FindBest(root, "lefthand");

            rightUpperArm = FindBest(root, "rightarm");
            rightForearm = FindBest(root, "rightforearm");
            rightHand = FindBest(root, "righthand");

            leftUpperLeg = FindBest(root, "leftupleg");
            leftLowerLeg = FindBest(root, "leftleg");
            leftFoot = FindBest(root, "leftfoot");
            leftToeBase = FindBest(root, "lefttoebase");

            rightUpperLeg = FindBest(root, "rightupleg");
            rightLowerLeg = FindBest(root, "rightleg");
            rightFoot = FindBest(root, "rightfoot");
            rightToeBase = FindBest(root, "righttoebase");
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
