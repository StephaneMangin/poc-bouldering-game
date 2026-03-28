using Project.Features.Climbing.Presentation;
using UnityEngine;

namespace Project.UI
{
    public sealed class ClimbingStateOverlay : MonoBehaviour
    {
        [SerializeField] private ClimbingStateMachineDriver driver;

        private GUIStyle _style;

        private void Awake()
        {
            _style = new GUIStyle
            {
                fontSize = 20,
                normal = { textColor = Color.white },
            };
        }

        private void OnGUI()
        {
            if (driver == null || driver.StateMachine == null)
            {
                GUI.Label(new Rect(20f, 20f, 500f, 30f), "FSM: <missing driver>", _style);
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
            GUI.Label(new Rect(20f, 20f, 1600f, 30f), $"FSM: {state} | Last Trigger: {lastTrigger} | Status: {driver.LastActionStatus}", _style);
            GUI.Label(new Rect(20f, 50f, 1600f, 30f), $"Hands: Left={leftHold} | Right={rightHold} | Supports={supportCount}", _style);
            GUI.Label(new Rect(20f, 80f, 1600f, 30f), "InputReader defaults: ZQSD/WASD/arrows or gamepad left stick move | J left grab | E or L right grab | U/O release | R release all | C recenter camera (R3 gamepad) | F fall | Space land", _style);
        }
    }
}
