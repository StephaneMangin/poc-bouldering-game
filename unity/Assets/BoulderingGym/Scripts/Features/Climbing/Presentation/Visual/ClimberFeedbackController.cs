using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class ClimberFeedbackController : MonoBehaviour
    {
        [SerializeField] private ClimbingStateMachineDriver driver;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Renderer leftHandMarker;
        [SerializeField] private Renderer rightHandMarker;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _bodyPropBlock;
        private MaterialPropertyBlock _leftMarkerPropBlock;
        private MaterialPropertyBlock _rightMarkerPropBlock;

        private static readonly Color StableColor = new(0.82f, 0.82f, 0.86f, 1f);
        private static readonly Color ReachColor = new(0.75f, 0.86f, 1f, 1f);
        private static readonly Color GripColor = new(0.68f, 0.95f, 0.72f, 1f);
        private static readonly Color FallColor = new(0.98f, 0.55f, 0.45f, 1f);
        private static readonly Color HoldColor = new(0.30f, 0.90f, 0.35f, 1f);
        private static readonly Color LeftReadyColor = new(0.20f, 0.90f, 1.00f, 1f);
        private static readonly Color RightReadyColor = new(1.00f, 0.25f, 0.85f, 1f);

        public void Initialize(
            ClimbingStateMachineDriver driverRef,
            Transform visual,
            Renderer bodyRenderer,
            Renderer leftMarker,
            Renderer rightMarker)
        {
            driver = driverRef;
            visualRoot = visual;
            visualRenderer = bodyRenderer;
            leftHandMarker = leftMarker;
            rightHandMarker = rightMarker;
            _bodyPropBlock = new MaterialPropertyBlock();
            _leftMarkerPropBlock = new MaterialPropertyBlock();
            _rightMarkerPropBlock = new MaterialPropertyBlock();
        }

        private void Awake()
        {
            _bodyPropBlock = new MaterialPropertyBlock();
            _leftMarkerPropBlock = new MaterialPropertyBlock();
            _rightMarkerPropBlock = new MaterialPropertyBlock();
        }

        public void ApplyFeedback(ClimbingStateId state)
        {
            ApplyColors(state);
            ApplyHandMarkers(state);
        }

        private void ApplyColors(ClimbingStateId state)
        {
            if (visualRenderer == null)
            {
                visualRenderer = visualRoot != null ? FindPrimaryRenderer(visualRoot) : null;
            }

            if (visualRenderer == null)
            {
                return;
            }

            var color = state switch
            {
                ClimbingStateId.Reach => ReachColor,
                ClimbingStateId.GripStable => GripColor,
                ClimbingStateId.Falling => FallColor,
                _ => StableColor,
            };

            _bodyPropBlock ??= new MaterialPropertyBlock();
            visualRenderer.GetPropertyBlock(_bodyPropBlock);
            _bodyPropBlock.SetColor(ColorProperty, color);
            visualRenderer.SetPropertyBlock(_bodyPropBlock);
        }

        private void ApplyHandMarkers(ClimbingStateId state)
        {
            var hands = driver != null ? driver.PlayerHands : null;
            if (hands == null)
            {
                return;
            }

            var leftColor = hands.LeftCurrentHold != null ? HoldColor : LeftReadyColor;
            var rightColor = hands.RightCurrentHold != null ? HoldColor : RightReadyColor;

            if (state == ClimbingStateId.Falling)
            {
                leftColor = FallColor;
                rightColor = FallColor;
            }

            ApplyMarkerColor(leftHandMarker, ref _leftMarkerPropBlock, leftColor);
            ApplyMarkerColor(rightHandMarker, ref _rightMarkerPropBlock, rightColor);
        }

        private static void ApplyMarkerColor(Renderer marker, ref MaterialPropertyBlock propBlock, Color color)
        {
            if (marker == null)
            {
                return;
            }

            propBlock ??= new MaterialPropertyBlock();
            marker.GetPropertyBlock(propBlock);
            propBlock.SetColor(ColorProperty, color);
            marker.SetPropertyBlock(propBlock);
        }

        private static Renderer FindPrimaryRenderer(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            return renderers.Length > 0 ? renderers[0] : null;
        }
    }
}
