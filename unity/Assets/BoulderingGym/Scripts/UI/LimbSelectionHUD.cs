using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Presentation;
using Project.Features.Player.Presentation;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// IMGUI overlay showing the 4 limbs as labeled circles at the bottom of the screen.
    /// Highlights which limb is currently suggested or active.
    /// </summary>
    public sealed class LimbSelectionHUD : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachineDriver driver;

        private GUIStyle _labelStyle;
        private GUIStyle _activeStyle;
        private GUIStyle _holdingStyle;

        private static readonly Color IdleColor = new(0.3f, 0.3f, 0.3f, 0.6f);
        private static readonly Color SuggestedColor = new(1f, 0.85f, 0.2f, 0.9f);
        private static readonly Color HoldingColor = new(0.2f, 0.8f, 0.2f, 0.9f);

        private static readonly LimbId[] Limbs =
        {
            LimbId.LeftHand, LimbId.RightHand,
            LimbId.LeftFoot, LimbId.RightFoot,
        };

        private static readonly string[] Labels = { "A:LH", "E:RH", "Q:LF", "D:RF" };

        private void Awake()
        {
            _labelStyle = new GUIStyle
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            _activeStyle = new GUIStyle(_labelStyle);
            _holdingStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(0.9f, 1f, 0.9f) },
            };
        }

        private void OnGUI()
        {
            if (driver == null || driver.PlayerHands == null)
            {
                return;
            }

            const float circleSize = 50f;
            const float spacing = 10f;
            const float totalWidth = circleSize * 4 + spacing * 3;
            var startX = (Screen.width - totalWidth) / 2f;
            var y = Screen.height - circleSize - 20f;

            var hands = driver.PlayerHands;
            var suggested = driver.SuggestedLimb;

            for (var i = 0; i < Limbs.Length; i++)
            {
                var limb = Limbs[i];
                var x = startX + i * (circleSize + spacing);
                var rect = new Rect(x, y, circleSize, circleSize);

                var isHolding = hands.IsLimbHolding(limb);
                var isSuggested = suggested.HasValue && suggested.Value == limb;

                Color bgColor;
                GUIStyle style;
                if (isHolding)
                {
                    bgColor = HoldingColor;
                    style = _holdingStyle;
                }
                else if (isSuggested)
                {
                    bgColor = SuggestedColor;
                    style = _activeStyle;
                }
                else
                {
                    bgColor = IdleColor;
                    style = _labelStyle;
                }

                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = bgColor;
                GUI.Box(rect, "", GUI.skin.box);
                GUI.backgroundColor = prevColor;

                GUI.Label(rect, Labels[i], style);
            }
        }
    }
}
