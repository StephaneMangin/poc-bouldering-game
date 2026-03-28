using Project.Features.Climbing.Domain.PhysicsModel;
using Project.Features.Climbing.Presentation;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Runtime debug overlay for the Physics Model (Sprint 2.0).
    /// Toggle with F2. Shows COG, weight, wall angle, stability.
    /// Also draws Gizmos in Scene view.
    /// </summary>
    public sealed class PhysicsDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PhysicsModelDriver physicsDriver;
        [SerializeField] private bool visible;

        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _stableStyle;
        private GUIStyle _unstableStyle;

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
            _stableStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green },
            };
            _unstableStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red },
            };
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible || physicsDriver == null) return;

            var snapshot = physicsDriver.CurrentSnapshot;
            var panelX = Screen.width - 320f;
            const float panelY = 10f;
            const float panelW = 310f;
            const float panelH = 220f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");

            var rowY = panelY + 8f;
            GUI.Label(new Rect(panelX + 10f, rowY, 290f, 24f), "Physics Model (F2)", _headerStyle);

            rowY += 28f;
            GUI.Label(new Rect(panelX + 10f, rowY, 290f, 20f),
                $"COG Offset: {snapshot.COG.CogOffset:F3} m  (penalty: {snapshot.COG.CogOffsetPenalty:F2})", _labelStyle);

            // COG offset bar
            rowY += 22f;
            var barRect = new Rect(panelX + 10f, rowY, 200f, 10f);
            GUI.Box(barRect, "");
            var fillRatio = Mathf.Clamp01(snapshot.COG.CogOffset / 0.8f);
            var fillColor = Color.Lerp(Color.green, Color.red, fillRatio);
            var oldColor = GUI.color;
            GUI.color = fillColor;
            GUI.Box(new Rect(barRect.x, barRect.y, barRect.width * fillRatio, barRect.height), "");
            GUI.color = oldColor;

            rowY += 18f;
            GUI.Label(new Rect(panelX + 10f, rowY, 290f, 20f),
                $"Wall: {snapshot.WallAngleDegrees:F0}° ({snapshot.WallCategory})", _labelStyle);

            rowY += 22f;
            var handsPercent = snapshot.Weight.TotalHandWeight * 100f;
            var feetPercent = snapshot.Weight.WeightOnFeet * 100f;
            GUI.Label(new Rect(panelX + 10f, rowY, 290f, 20f),
                $"Weight: Hands {handsPercent:F0}% / Feet {feetPercent:F0}%", _labelStyle);

            rowY += 22f;
            GUI.Label(new Rect(panelX + 10f, rowY, 290f, 20f),
                $"  L: {snapshot.Weight.WeightOnLeftHand * 100f:F0}%  R: {snapshot.Weight.WeightOnRightHand * 100f:F0}%", _labelStyle);

            rowY += 22f;
            var stabilityStyle = snapshot.COG.IsStable ? _stableStyle : _unstableStyle;
            var stabilityText = snapshot.COG.IsStable ? "STABLE" : "UNSTABLE";
            GUI.Label(new Rect(panelX + 10f, rowY, 120f, 20f), $"Stability: ", _labelStyle);
            GUI.Label(new Rect(panelX + 80f, rowY, 120f, 20f), stabilityText, stabilityStyle);

            rowY += 22f;
            var contactText = snapshot.HasBodyWallContact ? "YES" : "NO";
            var contactColor = snapshot.HasBodyWallContact ? Color.cyan : Color.gray;
            GUI.Label(new Rect(panelX + 10f, rowY, 290f, 20f),
                $"Body Contact: {contactText}  (friction: {snapshot.BodyWallFriction:F2})", _labelStyle);

            rowY += 22f;
            var mults = snapshot.Multipliers;
            GUI.Label(new Rect(panelX + 10f, rowY, 290f, 20f),
                $"Mults: E×{mults.EnduranceMult:F1}  F×{mults.FatigueMult:F1}  G×{mults.GripDrainMult:F1}", _labelStyle);
        }

        private void OnDrawGizmos()
        {
            if (physicsDriver == null) return;

            var snapshot = physicsDriver.CurrentSnapshot;
            var cogPos = snapshot.COG.Position;

            if (cogPos.sqrMagnitude < 0.001f) return;

            // COG sphere: green if stable, red if unstable
            Gizmos.color = snapshot.COG.IsStable ? Color.green : Color.red;
            Gizmos.DrawSphere(cogPos, 0.08f);

            // COG offset line (red) from COG toward wall
            if (snapshot.COG.CogOffset > 0.01f)
            {
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
                var wallPanel = physicsDriver.ActiveWallPanel;
                var wallNormal = wallPanel != null ? wallPanel.WallNormal : Vector3.back;
                Gizmos.DrawLine(cogPos, cogPos - wallNormal * snapshot.COG.CogOffset);
            }

            // Body-wall contact indicator
            if (snapshot.HasBodyWallContact)
            {
                Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
                Gizmos.DrawWireSphere(cogPos + Vector3.down * 0.3f, 0.12f);
            }
        }
    }
}
