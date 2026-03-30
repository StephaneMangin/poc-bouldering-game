using Project.Features.Player.Presentation;
using UnityEngine;

namespace Project.UI
{
    public sealed class ClimbingStateOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachineDriver driver;

        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _helpStyle;

        private void Awake()
        {
            _headerStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            _labelStyle = new GUIStyle
            {
                fontSize = 14,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
            };
            _valueStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            _helpStyle = new GUIStyle
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
            };
        }

        private void OnGUI()
        {
            if (driver == null || driver.StateMachine == null)
            {
                GUI.Box(new Rect(15f, 10f, 260f, 40f), "");
                GUI.Label(new Rect(20f, 14f, 250f, 22f), "Player State", _headerStyle);
                GUI.Label(new Rect(20f, 32f, 250f, 18f), "<missing driver>", _labelStyle);
                return;
            }

            var state = driver.StateMachine.CurrentState.Id;
            var lastTrigger = driver.StateMachine.LastTrigger?.ToString() ?? "None";
            var leftHold = driver.PlayerHands != null && driver.PlayerHands.LeftCurrentHold != null
                ? driver.PlayerHands.LeftCurrentHold.name
                : "None";
            var rightHold = driver.PlayerHands != null && driver.PlayerHands.RightCurrentHold != null
                ? driver.PlayerHands.RightCurrentHold.name
                : "None";
            var supportCount = driver.PlayerHands != null ? driver.PlayerHands.SupportCount : 0;
            var status = driver.LastActionStatus ?? "Ready";

            const float panelX = 15f;
            const float panelY = 10f;
            const float panelW = 380f;
            const float panelH = 170f;
            const float labelW = 100f;
            const float valueX = panelX + labelW + 10f;
            const float valueW = panelW - labelW - 25f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");

            var y = panelY + 6f;
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 22f), "Player State", _headerStyle);
            y += 24f;

            GUI.Label(new Rect(panelX + 10f, y, labelW, 18f), "FSM State", _labelStyle);
            GUI.Label(new Rect(valueX, y, valueW, 18f), state.ToString(), _valueStyle);
            y += 20f;

            GUI.Label(new Rect(panelX + 10f, y, labelW, 18f), "Last Trigger", _labelStyle);
            GUI.Label(new Rect(valueX, y, valueW, 18f), lastTrigger, _valueStyle);
            y += 20f;

            GUI.Label(new Rect(panelX + 10f, y, labelW, 18f), "Left Hand", _labelStyle);
            GUI.Label(new Rect(valueX, y, valueW, 18f), leftHold, _valueStyle);
            y += 20f;

            GUI.Label(new Rect(panelX + 10f, y, labelW, 18f), "Right Hand", _labelStyle);
            GUI.Label(new Rect(valueX, y, valueW, 18f), rightHold, _valueStyle);
            y += 20f;

            GUI.Label(new Rect(panelX + 10f, y, labelW, 18f), "Supports", _labelStyle);
            GUI.Label(new Rect(valueX, y, valueW, 18f), supportCount.ToString(), _valueStyle);
            y += 20f;

            GUI.Label(new Rect(panelX + 10f, y, labelW, 18f), "Status", _labelStyle);
            GUI.Label(new Rect(valueX, y, valueW, 18f), status, _valueStyle);
            y += 22f;

            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 30f),
                "Arrows move | Q/E hands, A/D feet | LClick quick-place | R release all | F fall",
                _helpStyle);
        }
    }
}
