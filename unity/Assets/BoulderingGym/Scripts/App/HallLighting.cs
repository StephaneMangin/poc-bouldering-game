using Project.Features.Gym.Domain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.App
{
    /// <summary>
    /// Daylight from the roof: a soft sun through the skylights and a sky / walls / mats ambient.
    /// Blockout passes use flat neutral light so shapes read clearly; the art pass warms it up.
    /// </summary>
    public static class HallLighting
    {
        private static readonly Color SkyArt = new(0.80f, 0.83f, 0.88f);
        private static readonly Color EquatorArt = new(0.58f, 0.58f, 0.57f);
        private static readonly Color GroundArt = new(0.42f, 0.42f, 0.41f);
        private static readonly Color SunArt = new(1f, 0.96f, 0.90f);
        private static readonly Color Neutral = new(0.72f, 0.72f, 0.72f);

        public static void Apply(Light sun, VisualPass pass)
        {
            var art = pass == VisualPass.Art;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = art ? SkyArt : Neutral;
            RenderSettings.ambientEquatorColor = art ? EquatorArt : Neutral * 0.8f;
            RenderSettings.ambientGroundColor = art ? GroundArt : Neutral * 0.6f;
            RenderSettings.fog = false;
            sun.type = LightType.Directional;
            sun.color = art ? SunArt : Color.white;
            sun.intensity = art ? 0.95f : 0.8f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(58f, -35f, 0f);
        }
    }
}
