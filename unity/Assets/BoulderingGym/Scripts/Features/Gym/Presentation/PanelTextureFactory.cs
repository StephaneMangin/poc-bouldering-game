using Project.Features.Gym.Domain;
using UnityEngine;

namespace Project.Features.Gym.Presentation
{
    /// <summary>Turns the painted panel artwork of a face into a material mapped on the face UVs (meters).</summary>
    public sealed class PanelTextureFactory
    {
        private const int PixelsPerMeter = 96;
        private const int MaxTextureSize = 2048;

        private readonly MaterialLibrary _materials;

        public PanelTextureFactory(MaterialLibrary materials)
        {
            _materials = materials;
        }

        public Material CreateMaterial(WallFace face, GymLayout layout, GymContent content)
        {
            var canvas = PanelPainter.Paint(face, layout, PixelsPerMeter, MaxTextureSize, content.HandHoldSpots(face));
            var material = _materials.Textured(ToTexture(canvas, face.Id, TextureWrapMode.Clamp));
            material.mainTextureScale = new Vector2(1f / canvas.MetersWide, 1f / canvas.MetersHigh);
            material.mainTextureOffset = new Vector2(-canvas.OriginU / canvas.MetersWide, 0f);
            return material;
        }

        /// <summary>Hall surfaces: the floor canvas spans the whole hall, cladding and roof tile every meter.</summary>
        public Material CreateHallMaterial(PanelCanvas canvas, TextureWrapMode wrap)
        {
            var material = new Material(_materials.Templates.plain) { mainTexture = ToTexture(canvas, "Hall", wrap), color = Color.white };
            material.mainTextureScale = new Vector2(1f / canvas.MetersWide, 1f / canvas.MetersHigh);
            return material;
        }

        private static Texture2D ToTexture(PanelCanvas canvas, string name, TextureWrapMode wrap)
        {
            var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = wrap,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };
            texture.SetPixels32(canvas.Pixels);
            texture.Apply(true, true);
            return texture;
        }
    }
}
