using Project.Features.Resources.Domain;
using Project.Features.Resources.Presentation;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Runtime HUD for resource systems (Sprint 3.0).
    /// Toggle with F3. Shows endurance, grip, arm fatigue gauges + contextual prompts.
    /// </summary>
    public sealed class ResourceHUD : MonoBehaviour
    {
        [SerializeField] private ResourceSystemDriver resourceDriver;
        [SerializeField] private bool visible = true;

        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _promptStyle;

        private string _currentPrompt = "";
        private float _lastPromptTime;
        private const float PromptMinInterval = 2.5f;

        private void Awake()
        {
            _labelStyle = new GUIStyle
            {
                fontSize = 14,
                normal = { textColor = Color.white },
            };
            _headerStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            _promptStyle = new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.9f, 0.3f) },
            };
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                visible = !visible;
            }

            UpdatePrompts();
        }

        private void OnGUI()
        {
            if (!visible || resourceDriver == null) return;

            var snap = resourceDriver.CurrentSnapshot;
            DrawResourceBars(snap);
            DrawPrompt();
        }

        private void DrawResourceBars(ResourceSnapshot snap)
        {
            const float x = 20f;
            var y = 190f;
            const float panelW = 300f;
            const float panelH = 145f;

            GUI.Box(new Rect(x - 5f, y - 5f, panelW, panelH), "");
            GUI.Label(new Rect(x, y, 280f, 22f), "Resources (F3)", _headerStyle);
            y += 24f;

            DrawBar("Endurance", snap.EnduranceRatio, x, ref y,
                GetResourceColor(snap.EnduranceRatio));
            DrawBar("Grip", snap.GripRatio, x, ref y,
                GetResourceColor(snap.GripRatio));
            DrawBar("L Arm", 1f - snap.LeftArmFatigue / 100f, x, ref y,
                GetFatigueColor(snap.LeftArmFatigue));
            DrawBar("R Arm", 1f - snap.RightArmFatigue / 100f, x, ref y,
                GetFatigueColor(snap.RightArmFatigue));

            if (snap.ChalkAvailable)
            {
                GUI.Label(new Rect(x, y + 2f, 280f, 20f), "Chalk: READY (C)", _labelStyle);
            }
            else
            {
                GUI.Label(new Rect(x, y + 2f, 280f, 20f),
                    $"Chalk: {snap.ChalkCooldownRemaining:F1}s", _labelStyle);
            }
        }

        private void DrawBar(string label, float ratio, float x, ref float y, Color color)
        {
            ratio = Mathf.Clamp01(ratio);
            GUI.Label(new Rect(x, y, 70f, 18f), label, _labelStyle);

            var barRect = new Rect(x + 75f, y + 2f, 150f, 12f);
            GUI.Box(barRect, "");

            var oldColor = GUI.color;
            GUI.color = color;
            GUI.Box(new Rect(barRect.x, barRect.y, barRect.width * ratio, barRect.height), "");
            GUI.color = oldColor;

            GUI.Label(new Rect(x + 232f, y, 50f, 18f), $"{ratio * 100f:F0}%", _labelStyle);
            y += 20f;
        }

        private void DrawPrompt()
        {
            if (string.IsNullOrEmpty(_currentPrompt)) return;

            var promptRect = new Rect(
                Screen.width / 2f - 200f,
                Screen.height - 80f,
                400f, 30f);
            GUI.Label(promptRect, _currentPrompt, _promptStyle);
        }

        private void UpdatePrompts()
        {
            if (resourceDriver == null) return;

            var snap = resourceDriver.CurrentSnapshot;
            var now = Time.time;

            if (now - _lastPromptTime < PromptMinInterval) return;

            if (snap.GripRatio < 0.3f && snap.ChalkAvailable)
            {
                SetPrompt("Low Grip — Use Chalk (C)", now);
            }
            else if (snap.LeftArmFatigue > 80f || snap.RightArmFatigue > 80f)
            {
                SetPrompt("Critical Fatigue!", now);
            }
            else if (snap.LeftArmFatigue > 60f || snap.RightArmFatigue > 60f)
            {
                SetPrompt("Arm Fatigued — Switch Arms", now);
            }
            else if (snap.EnduranceRatio < 0.3f)
            {
                SetPrompt("Low Endurance — Rest!", now);
            }
            else
            {
                _currentPrompt = "";
            }
        }

        private void SetPrompt(string message, float time)
        {
            _currentPrompt = message;
            _lastPromptTime = time;
        }

        private static Color GetResourceColor(float ratio)
        {
            if (ratio > 0.6f) return Color.green;
            if (ratio > 0.3f) return Color.yellow;
            return Color.red;
        }

        private static Color GetFatigueColor(float fatigue)
        {
            if (fatigue < 60f) return Color.green;
            if (fatigue < 80f) return Color.yellow;
            return Color.red;
        }
    }
}
