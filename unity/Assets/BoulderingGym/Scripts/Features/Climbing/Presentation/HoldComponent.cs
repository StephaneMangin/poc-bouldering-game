using System;
using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public enum HoldQuality
    {
        Fragile,
        Standard,
        Reliable,
    }

    [Serializable]
    public struct HoldData
    {
        public HoldQuality holdQuality;
        public GripType gripType;
        [Range(0.5f, 2.0f)] public float gripTextureHold;
        [Range(0.5f, 2.0f)] public float gripTextureWall;
    }

    public sealed class HoldComponent : MonoBehaviour
    {
        public HoldData data;

        [Tooltip("Point used as IK target when the hand successfully grabs this hold.")]
        public Transform gripPoint;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly Color GrabbedColor = new(0.3f, 0.9f, 0.35f, 1f);
        private static readonly Color HighlightColor = new(1f, 0.85f, 0.2f, 1f);

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Color _baseColor = new(0.8f, 0.8f, 0.8f, 1f);
        private bool _isGrabbed;
        private bool _isHighlighted;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            if (_renderer != null && _renderer.sharedMaterial != null)
            {
                _baseColor = _renderer.sharedMaterial.color;
            }
        }

        private void OnValidate()
        {
            if (gripPoint == null)
            {
                gripPoint = transform;
            }
        }

        public void SetFeedback(bool grabbed)
        {
            _isGrabbed = grabbed;
            ApplyColor();
        }

        public void SetHighlight(bool highlighted)
        {
            _isHighlighted = highlighted;
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_renderer == null)
            {
                return;
            }

            var color = _isGrabbed ? GrabbedColor : _isHighlighted ? HighlightColor : _baseColor;
            _propBlock ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ColorProperty, color);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
