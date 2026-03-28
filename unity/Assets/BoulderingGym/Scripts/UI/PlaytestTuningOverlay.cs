using Project.Core.Domain;
using Project.Features.Camera.Presentation;
using UnityEngine;

namespace Project.UI
{
    public sealed class PlaytestTuningOverlay : MonoBehaviour
    {
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private CameraFollowController cameraFollowController;
        [SerializeField] private bool visible = true;

        private GUIStyle _labelStyle;
        private float _grabRange;
        private float _hipsMoveSpeed;
        private float _mouseSensitivity;

        private const float DefaultGrabRange = 1.6f;
        private const float DefaultMoveSpeed = 2.0f;
        private const float DefaultMouseSensitivity = 2.0f;

        private void Awake()
        {
            _labelStyle = new GUIStyle
            {
                fontSize = 16,
                normal = { textColor = Color.white },
            };

            SyncFromCurrentValues();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            const float panelX = 20f;
            const float panelY = 120f;
            const float panelW = 460f;
            const float panelH = 240f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "Playtest Tuning (F1 to hide)");

            var rowY = panelY + 30f;
            GUI.Label(new Rect(panelX + 10f, rowY, 200f, 24f), $"Grab Range: {_grabRange:F2}", _labelStyle);
            _grabRange = GUI.HorizontalSlider(new Rect(panelX + 180f, rowY + 5f, 250f, 20f), _grabRange, 0.8f, 3.0f);

            rowY += 46f;
            GUI.Label(new Rect(panelX + 10f, rowY, 200f, 24f), $"Move Speed: {_hipsMoveSpeed:F2}", _labelStyle);
            _hipsMoveSpeed = GUI.HorizontalSlider(new Rect(panelX + 180f, rowY + 5f, 250f, 20f), _hipsMoveSpeed, 1.0f, 8.0f);

            rowY += 46f;
            GUI.Label(new Rect(panelX + 10f, rowY, 200f, 24f), $"Mouse Sensitivity: {_mouseSensitivity:F2}", _labelStyle);
            _mouseSensitivity = GUI.HorizontalSlider(new Rect(panelX + 180f, rowY + 5f, 250f, 20f), _mouseSensitivity, 0.5f, 5.0f);

            rowY += 54f;
            if (GUI.Button(new Rect(panelX + 10f, rowY, 130f, 28f), "Apply"))
            {
                Apply();
            }

            if (GUI.Button(new Rect(panelX + 155f, rowY, 130f, 28f), "Reset defaults"))
            {
                ResetDefaults();
                Apply();
            }

            if (GUI.Button(new Rect(panelX + 300f, rowY, 130f, 28f), "Refresh values"))
            {
                SyncFromCurrentValues();
            }
        }

        private void Apply()
        {
            if (tuningConfig != null)
            {
                tuningConfig.grabRange = _grabRange;
                tuningConfig.hipsMoveSpeed = _hipsMoveSpeed;
            }

            if (cameraFollowController != null)
            {
                cameraFollowController.MouseSensitivity = _mouseSensitivity;
            }
        }

        private void ResetDefaults()
        {
            _grabRange = DefaultGrabRange;
            _hipsMoveSpeed = DefaultMoveSpeed;
            _mouseSensitivity = DefaultMouseSensitivity;
        }

        private void SyncFromCurrentValues()
        {
            _grabRange = tuningConfig != null ? tuningConfig.grabRange : DefaultGrabRange;
            _hipsMoveSpeed = tuningConfig != null ? tuningConfig.hipsMoveSpeed : DefaultMoveSpeed;
            _mouseSensitivity = cameraFollowController != null
                ? cameraFollowController.MouseSensitivity
                : DefaultMouseSensitivity;
        }
    }
}
