using System;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Centralized 1930s Fleischer / early Disney rubber-hose cartoon visual theme for Cuphead 3-Player Co-op.
    /// Provides authentic game typography (CupheadVogue / CupheadMemphis), color palette,
    /// 9-slice card borders, buttons, slots, and cartoon glove cursor.
    /// </summary>
    public static class CupheadUITheme
    {
        // 1930s cartoon palette
        public static readonly Color InkBlack = new Color(0.08f, 0.05f, 0.03f, 1f);        // Deep cartoon ink line
        public static readonly Color InkBorderLight = new Color(0.20f, 0.13f, 0.08f, 1f); // Dark inner groove
        public static readonly Color CocoaDark = new Color(0.14f, 0.09f, 0.06f, 0.98f);    // Chalkboard / woodcard fill
        public static readonly Color CocoaSlot = new Color(0.09f, 0.06f, 0.04f, 0.95f);    // Recessed table / scroll slot
        public static readonly Color VintageGold = new Color(0.88f, 0.68f, 0.25f, 1f);     // Antique gold border
        public static readonly Color VintageGoldBright = new Color(1f, 0.86f, 0.38f, 1f);  // Bright gold highlight
        public static readonly Color ParchmentCream = new Color(0.97f, 0.92f, 0.83f, 1f);  // Off-white parchment text
        public static readonly Color SepiaMuted = new Color(0.68f, 0.60f, 0.50f, 1f);      // Muted sepia footers / dates
        public static readonly Color SepiaDark = new Color(0.60f, 0.52f, 0.42f, 1f);       // Inactive text (WCAG AA 4.65:1)

        // Character / Accent Colors
        public static readonly Color CupheadRed = new Color(0.85f, 0.20f, 0.18f, 1f);      // Cuphead shorts/nose red (P1)
        public static readonly Color CupheadRedHover = new Color(0.95f, 0.26f, 0.22f, 1f); // Active / hover button red
        public static readonly Color MugmanBlue = new Color(0.28f, 0.56f, 0.92f, 1f);      // Mugman blue (P2)
        public static readonly Color ChaliceGold = new Color(0.94f, 0.76f, 0.20f, 1f);     // Ms. Chalice gold
        public static readonly Color P3Green = new Color(0.32f, 0.78f, 0.36f, 1f);         // Player 3 green tint
        public static readonly Color HardcoreRed = new Color(0.98f, 0.30f, 0.28f, 1f);     // Hardcore skull red

        public static string GetPlayerName(PlayerId id)
        {
            switch (id)
            {
                case PlayerId.PlayerOne: return "Cuphead";
                case PlayerId.PlayerTwo: return "Mugman";
                case PlayerId.PlayerThree: return "Gracz 3";
                default: return id.ToString();
            }
        }

        public static string GetPlayerFullName(PlayerId id)
        {
            switch (id)
            {
                case PlayerId.PlayerOne: return "Cuphead (Czerwony)";
                case PlayerId.PlayerTwo: return "Mugman (Niebieski)";
                case PlayerId.PlayerThree: return "Gracz 3 (Zielony)";
                default: return id.ToString();
            }
        }

        public static Font VogueFont { get; private set; }
        public static Font MemphisFont { get; private set; }
        public static Font PosterFont { get; private set; }

        private static Font _titleFont;
        private static Font _bodyFont;

        public static float FontAspectScale
        {
            get
            {
                if (Plugin.UIFontAspectScale == null) return 0.88f;
                return Mathf.Clamp(Plugin.UIFontAspectScale.Value, 0.70f, 1.20f);
            }
        }

        public static Font TitleFont
        {
            get
            {
                if (_titleFont == null && (Plugin.UIFontMode == null || Plugin.UIFontMode.Value == 0)) ResolveFonts();
                return _titleFont;
            }
        }

        public static Font BodyFont
        {
            get
            {
                if (_bodyFont == null && (Plugin.UIFontMode == null || Plugin.UIFontMode.Value == 0)) ResolveFonts();
                return _bodyFont;
            }
        }

        public static void ReloadFonts()
        {
            _titleFont = null;
            _bodyFont = null;
            ResolveFonts();
        }

        public static void ResolveFonts()
        {
            try
            {
                if (VogueFont == null) VogueFont = FontLoader.GetFont(FontLoader.FontType.CupheadVogue_Bold_merged);
                if (MemphisFont == null) MemphisFont = FontLoader.GetFont(FontLoader.FontType.CupheadMemphis_Medium_merged);
                if (PosterFont == null) PosterFont = FontLoader.GetFont(FontLoader.FontType.CupheadPoster_Regular66Cyr_Lat_English99);
            }
            catch (Exception) {}

            try
            {
                if (VogueFont == null || MemphisFont == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<Font>();
                    if (all != null)
                    {
                        foreach (var f in all)
                        {
                            if (f == null || string.IsNullOrEmpty(f.name)) continue;
                            if (VogueFont == null && f.name.Contains("CupheadVogue")) VogueFont = f;
                            if (MemphisFont == null && f.name.Contains("CupheadMemphis")) MemphisFont = f;
                            if (PosterFont == null && f.name.Contains("CupheadPoster")) PosterFont = f;
                        }
                    }
                }
            }
            catch (Exception) {}

            int mode = Plugin.UIFontMode != null ? Plugin.UIFontMode.Value : 0;
            if (mode == 1)
            {
                // Clean Standard Sans (Unity default GUI font - perfectly proportioned)
                _titleFont = null;
                _bodyFont = null;
            }
            else
            {
                // Authentic Cuphead typography (Vogue & Memphis)
                _titleFont = VogueFont ?? PosterFont;
                _bodyFont = MemphisFont ?? VogueFont ?? PosterFont;
            }
        }

        public static void DrawLabel(Rect rect, string text, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text)) return;
            float scale = FontAspectScale;
            if (Mathf.Approximately(scale, 1.0f))
            {
                GUI.Label(rect, text, style);
                return;
            }

            Vector2 pivot;
            if (style.alignment == TextAnchor.MiddleCenter || style.alignment == TextAnchor.UpperCenter || style.alignment == TextAnchor.LowerCenter)
            {
                pivot = rect.center;
            }
            else if (style.alignment == TextAnchor.MiddleRight || style.alignment == TextAnchor.UpperRight || style.alignment == TextAnchor.LowerRight)
            {
                pivot = new Vector2(rect.xMax, rect.center.y);
            }
            else
            {
                pivot = new Vector2(rect.xMin, rect.center.y);
            }

            Matrix4x4 prev = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(scale, 1.0f), pivot);
            GUI.Label(rect, text, style);
            GUI.matrix = prev;
        }

        public static bool DrawButton(Rect rect, string text, GUIStyle style)
        {
            bool clicked = GUI.Button(rect, GUIContent.none, style);
            float scale = FontAspectScale;
            if (Mathf.Approximately(scale, 1.0f) || string.IsNullOrEmpty(text))
            {
                GUI.Label(rect, text, style);
            }
            else
            {
                Matrix4x4 prev = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(scale, 1.0f), rect.center);
                GUI.Label(rect, text, style);
                GUI.matrix = prev;
            }
            return clicked;
        }

        public static Texture2D CreateVintageCardTexture(int size, Color inkBorder, Color goldBorder, Color fillColor, bool rounded = true)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Min(x, size - 1 - x);
                    int dy = Mathf.Min(y, size - 1 - y);
                    int dist = Mathf.Min(dx, dy);

                    // Rounded corner cut
                    if (rounded && ((x == 0 && (y == 0 || y == size - 1)) || (x == size - 1 && (y == 0 || y == size - 1))))
                    {
                        tex.SetPixel(x, y, transparent);
                        continue;
                    }
                    if (rounded && ((dx == 0 && dy == 1) || (dx == 1 && dy == 0)))
                    {
                        tex.SetPixel(x, y, inkBorder);
                        continue;
                    }

                    if (dist < 2)
                    {
                        // Outer ink border (2px)
                        tex.SetPixel(x, y, inkBorder);
                    }
                    else if (dist == 2)
                    {
                        // Inner dark groove
                        tex.SetPixel(x, y, new Color(inkBorder.r * 1.4f, inkBorder.g * 1.4f, inkBorder.b * 1.4f, 1f));
                    }
                    else if (dist >= 3 && dist <= 4)
                    {
                        // Antique gold decorative pinstripe (2px)
                        tex.SetPixel(x, y, goldBorder);
                    }
                    else if (dist == 5)
                    {
                        // Inner ink hairline (1px)
                        tex.SetPixel(x, y, inkBorder);
                    }
                    else
                    {
                        // Center card fill
                        tex.SetPixel(x, y, fillColor);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateButtonTexture(int size, Color inkBorder, Color goldBorder, Color fillColor)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Min(x, size - 1 - x);
                    int dy = Mathf.Min(y, size - 1 - y);
                    int dist = Mathf.Min(dx, dy);

                    if (dist == 0)
                    {
                        tex.SetPixel(x, y, inkBorder);
                    }
                    else if (dist == 1)
                    {
                        tex.SetPixel(x, y, goldBorder);
                    }
                    else
                    {
                        // Subtle vertical lighting
                        float factor = 1f + (y - size / 2f) / (float)size * 0.16f;
                        Color c = new Color(
                            Mathf.Clamp01(fillColor.r * factor),
                            Mathf.Clamp01(fillColor.g * factor),
                            Mathf.Clamp01(fillColor.b * factor),
                            fillColor.a
                        );
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateInsetSlotTexture(int size, Color border, Color fill)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Min(x, size - 1 - x);
                    int dy = Mathf.Min(y, size - 1 - y);
                    int dist = Mathf.Min(dx, dy);

                    if (dist == 0)
                    {
                        tex.SetPixel(x, y, border);
                    }
                    else
                    {
                        tex.SetPixel(x, y, fill);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateSolid(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateCartoonGloveCursor()
        {
            int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32 black = new Color32(18, 10, 6, 255);       // Ink black
            Color32 white = new Color32(252, 248, 240, 255);   // Vintage glove white
            Color32 cuff = new Color32(236, 228, 214, 255);    // Shaded cuff roll

            for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;

            string[] mask = new string[]
            {
                "X                       ", // 0: tip of pointing index finger
                "XX                      ", // 1
                "X.X                     ", // 2
                "X..X                    ", // 3
                "X...X                   ", // 4
                "X....X                  ", // 5
                "X.....X    XXXX         ", // 6: knuckle curve
                "X......X  X....X        ", // 7
                "X.......XX......X   XX  ", // 8: middle finger curled
                "X........X.......X X..X ", // 9
                " X........X.......X....X", // 10: ring finger
                "  X........X...........X", // 11
                "   X...X.X..X..........X", // 12: glove stitch darts
                "    X...X....X........X ", // 13: glove stitch darts
                "    X.........X......X  ", // 14
                "     X..............X   ", // 15: palm curve
                "      X............X    ", // 16
                "      XXXXXXXXXXXXXX    ", // 17: black wrist seam
                "     X..............X   ", // 18: white cuff roll
                "     X..............X   ", // 19
                "      XXXXXXXXXXXXXX    "  // 20: bottom of cuff
            };

            for (int row = 0; row < mask.Length; row++)
            {
                string line = mask[row];
                int y = size - 1 - row;
                for (int col = 0; col < line.Length && col < size; col++)
                {
                    char c = line[col];
                    if (c == 'X') pixels[y * size + col] = black;
                    else if (c == '.') pixels[y * size + col] = (row >= 18) ? cuff : white;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
