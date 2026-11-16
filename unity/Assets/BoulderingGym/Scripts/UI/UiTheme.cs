using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// IMGUI look of the game: dark translucent cards with rounded corners, the gym's teal and orange
    /// as accents, sizes scaled from a 1080p reference.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Teal = new(0.17f, 0.64f, 0.61f);
        public static readonly Color Orange = new(0.92f, 0.37f, 0.22f);
        public static readonly Color Ink = new(0.96f, 0.96f, 0.94f);
        public static readonly Color Muted = new(0.70f, 0.73f, 0.74f);
        public static readonly Color Card = new(0.06f, 0.08f, 0.09f, 0.86f);
        public static readonly Color Track = new(1f, 1f, 1f, 0.12f);
        public static readonly Color Disabled = new(0.35f, 0.37f, 0.38f);

        private const float ReferenceHeight = 1080f;
        private const int CornerTextureSize = 16;
        private const int CornerRadius = 6;

        private static Texture2D _rounded;
        private static Texture2D _white;
        private static float _builtForScale;

        public static float Scale => Mathf.Max(0.6f, Screen.height / ReferenceHeight);

        public static GUIStyle Title => Ensure()._title;

        public static GUIStyle Heading => Ensure()._heading;

        public static GUIStyle Body => Ensure()._body;

        public static GUIStyle Small => Ensure()._small;

        public static GUIStyle ButtonText => Ensure()._button;

        public static GUIStyle Banner => Ensure()._banner;

        public static float Px(float referencePixels) => referencePixels * Scale;

        public static void DrawCard(Rect rect) => DrawRounded(rect, Card);

        /// <summary>Rounded teal button turning orange under the mouse; disabled buttons are greyed and inert.</summary>
        public static bool DrawButton(Rect rect, string label, bool enabled = true)
        {
            var hovered = enabled && rect.Contains(Event.current.mousePosition);
            DrawRounded(rect, !enabled ? Disabled : hovered ? Orange : Teal);
            GUI.Label(rect, label, ButtonText);
            return enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        public static void DrawRounded(Rect rect, Color color)
        {
            Ensure();
            var previous = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, CardStyle);
            GUI.color = previous;
        }

        public static void DrawRect(Rect rect, Color color)
        {
            Ensure();
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _white);
            GUI.color = previous;
        }

        /// <summary>Labelled horizontal gauge filled to a ratio.</summary>
        public static void Gauge(Rect rect, string label, float ratio, Color fill)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, Px(20f)), label, Small);
            var bar = new Rect(rect.x, rect.y + Px(22f), rect.width, rect.height - Px(22f));
            DrawRect(bar, Track);
            DrawRect(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(ratio), bar.height), fill);
        }

        private static GUIStyle CardStyle { get; set; }

        private static UiThemeStyles Ensure()
        {
            if (_rounded == null || !Mathf.Approximately(_builtForScale, Scale))
            {
                Build();
            }

            return UiThemeStyles.Instance;
        }

        private static void Build()
        {
            _builtForScale = Scale;
            _rounded = _rounded != null ? _rounded : RoundedTexture();
            _white = _white != null ? _white : Texture2D.whiteTexture;
            CardStyle = new GUIStyle { normal = { background = _rounded }, border = new RectOffset(CornerRadius, CornerRadius, CornerRadius, CornerRadius) };
            UiThemeStyles.Instance._title = Text(56, FontStyle.Bold, TextAnchor.MiddleCenter, Ink);
            UiThemeStyles.Instance._heading = Text(26, FontStyle.Bold, TextAnchor.MiddleLeft, Ink);
            UiThemeStyles.Instance._body = Text(20, FontStyle.Normal, TextAnchor.MiddleLeft, Ink);
            UiThemeStyles.Instance._small = Text(16, FontStyle.Normal, TextAnchor.MiddleLeft, Muted);
            UiThemeStyles.Instance._banner = Text(64, FontStyle.Bold, TextAnchor.MiddleCenter, Ink);
            UiThemeStyles.Instance._button = ButtonStyle();
        }

        private static GUIStyle Text(int size, FontStyle fontStyle, TextAnchor anchor, Color color)
        {
            return new GUIStyle
            {
                fontSize = Mathf.RoundToInt(size * Scale),
                fontStyle = fontStyle,
                alignment = anchor,
                wordWrap = true,
                richText = true,
                normal = { textColor = color },
            };
        }

        private static GUIStyle ButtonStyle() => Text(22, FontStyle.Bold, TextAnchor.MiddleCenter, Ink);

        private static Texture2D RoundedTexture()
        {
            var texture = new Texture2D(CornerTextureSize, CornerTextureSize, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            var center = (CornerTextureSize - 1) * 0.5f;
            var inner = center - CornerRadius;
            for (var y = 0; y < CornerTextureSize; y++)
            {
                for (var x = 0; x < CornerTextureSize; x++)
                {
                    var dx = Mathf.Max(0f, Mathf.Abs(x - center) - inner);
                    var dy = Mathf.Max(0f, Mathf.Abs(y - center) - inner);
                    var alpha = Mathf.Clamp01(CornerRadius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        /// <summary>Holder so styles are rebuilt together when the screen height changes.</summary>
        private sealed class UiThemeStyles
        {
            public static readonly UiThemeStyles Instance = new();
            public GUIStyle _title;
            public GUIStyle _heading;
            public GUIStyle _body;
            public GUIStyle _small;
            public GUIStyle _button;
            public GUIStyle _banner;
        }
    }
}
