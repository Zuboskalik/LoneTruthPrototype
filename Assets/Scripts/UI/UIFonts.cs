using UnityEngine;

namespace LoneTruth.UI
{
    /// <summary>
    /// The one font all runtime UI text uses. The built-in LegacyRuntime.ttf has no Cyrillic of its
    /// own and borrows glyphs from OS fonts, which WebGL builds do not have — so the game ships
    /// PT Sans (OFL) in Resources/Fonts and falls back to the built-in font only if it is missing.
    /// </summary>
    public static class UIFonts
    {
        private const string ResourcePath = "Fonts/PTSans-Regular";
        private static Font cached;

        public static Font Default
        {
            get
            {
                if (cached == null)
                {
                    cached = Resources.Load<Font>(ResourcePath);
                    if (cached == null)
                    {
                        cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }

                return cached;
            }
        }
    }
}
