using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Presentation
{
    /// <summary>Template materials; instances are tinted at runtime so their shaders ship with the build.</summary>
    [Serializable]
    public sealed class GymMaterials
    {
        public Material wall;
        public Material plain;
        public Material hold;
        public Material marker;
        public Material label;
        public Font labelFont;
    }

    /// <summary>Caches tinted copies of the template materials.</summary>
    public sealed class MaterialLibrary
    {
        private const string EmissionKeyword = "_EMISSION";
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private readonly Dictionary<(Material, Color), Material> _tinted = new();

        public MaterialLibrary(GymMaterials templates)
        {
            Templates = templates;
        }

        public GymMaterials Templates { get; }

        public Material Tinted(Material template, Color color)
        {
            if (_tinted.TryGetValue((template, color), out var material))
            {
                return material;
            }

            material = new Material(template) { color = color, name = $"{template.name} {ColorUtility.ToHtmlStringRGB(color)}" };
            if (material.IsKeywordEnabled(EmissionKeyword))
            {
                material.SetColor(EmissionColor, color);
            }

            _tinted[(template, color)] = material;
            return material;
        }

        public Material Textured(Texture2D texture)
        {
            return new Material(Templates.wall) { mainTexture = texture, color = Color.white, name = texture.name };
        }
    }
}
