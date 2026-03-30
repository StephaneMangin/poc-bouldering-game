using Project.Features.Player.Domain;
using Project.Features.Player.Presentation;
using UnityEngine;

namespace Project.Features.Visual.Presentation
{
    public sealed class ClimberFeedbackController : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachineDriver driver;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Renderer visualRenderer;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _bodyPropBlock;

        private static readonly Color StableColor = new(0.82f, 0.82f, 0.86f, 1f);
        private static readonly Color ReachColor = new(0.75f, 0.86f, 1f, 1f);
        private static readonly Color GripColor = new(0.68f, 0.95f, 0.72f, 1f);
        private static readonly Color FallColor = new(0.98f, 0.55f, 0.45f, 1f);

        public void Initialize(
            PlayerStateMachineDriver driverRef,
            Transform visual,
            Renderer bodyRenderer)
        {
            driver = driverRef;
            visualRoot = visual;
            visualRenderer = bodyRenderer;
            _bodyPropBlock = new MaterialPropertyBlock();
        }

        private void Awake()
        {
            _bodyPropBlock = new MaterialPropertyBlock();
        }

        public void ApplyFeedback(PlayerStateId state)
        {
            ApplyColors(state);
        }

        private void ApplyColors(PlayerStateId state)
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
                PlayerStateId.Reach => ReachColor,
                PlayerStateId.GripStable => GripColor,
                PlayerStateId.Falling => FallColor,
                _ => StableColor,
            };

            _bodyPropBlock ??= new MaterialPropertyBlock();
            visualRenderer.GetPropertyBlock(_bodyPropBlock);
            _bodyPropBlock.SetColor(ColorProperty, color);
            visualRenderer.SetPropertyBlock(_bodyPropBlock);
        }

        private static Renderer FindPrimaryRenderer(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            return renderers.Length > 0 ? renderers[0] : null;
        }
    }
}
