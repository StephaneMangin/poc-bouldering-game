using System;
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
        [Range(0.5f, 2.0f)] public float gripTextureHold;
    }

    public sealed class HoldComponent : MonoBehaviour
    {
        public HoldData data;

        [Tooltip("Point used as IK target when the hand successfully grabs this hold.")]
        public Transform gripPoint;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly Color GrabbedColor = new(0.3f, 0.9f, 0.35f, 1f);

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Color _baseColor = new(0.8f, 0.8f, 0.8f, 1f);

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            if (_renderer != null)
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
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_renderer == null)
            {
                return;
            }

            _propBlock ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ColorProperty, grabbed ? GrabbedColor : _baseColor);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
