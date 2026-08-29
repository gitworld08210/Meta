using UnityEngine;

namespace MetaCricket.UI
{
    /// <summary>
    /// ScriptableObject defining the complete color palette for the premium gold+black theme.
    /// Create instances via Assets > Create > MetaCricket > UI > Theme Colors.
    /// </summary>
    [CreateAssetMenu(fileName = "ThemeColors", menuName = "MetaCricket/UI/Theme Colors")]
    public class ThemeColors : ScriptableObject
    {
        [Header("Primary Colors")]
        [Tooltip("Primary Gold - #FFD700")]
        public Color PrimaryGold = new Color(1f, 0.843f, 0f, 1f);

        [Tooltip("Deep Black - #1A1A1A")]
        public Color DeepBlack = new Color(0.102f, 0.102f, 0.102f, 1f);

        [Tooltip("Accent Gold - #FFC107")]
        public Color AccentGold = new Color(1f, 0.757f, 0.027f, 1f);

        [Header("Glass and Glow")]
        [Tooltip("Glass White - #FFFFFF at 10% opacity")]
        public Color GlassWhite = new Color(1f, 1f, 1f, 0.1f);

        [Tooltip("Neon Glow - #FFE066")]
        public Color NeonGlow = new Color(1f, 0.878f, 0.4f, 1f);

        [Header("Text Colors")]
        [Tooltip("Primary text color - White")]
        public Color TextPrimary = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Secondary text color - Light gray")]
        public Color TextSecondary = new Color(0.75f, 0.75f, 0.75f, 1f);

        [Header("Status Colors")]
        [Tooltip("Success indicator - Green")]
        public Color SuccessGreen = new Color(0.298f, 0.686f, 0.314f, 1f);

        [Tooltip("Danger/Error indicator - Red")]
        public Color DangerRed = new Color(0.898f, 0.224f, 0.208f, 1f);

        [Header("Gradient Definitions")]
        [Tooltip("Gold gradient start color")]
        public Color GoldGradientStart = new Color(1f, 0.843f, 0f, 1f);

        [Tooltip("Gold gradient end color")]
        public Color GoldGradientEnd = new Color(1f, 0.757f, 0.027f, 1f);

        [Tooltip("Background gradient start (darker)")]
        public Color BackgroundGradientStart = new Color(0.05f, 0.05f, 0.05f, 1f);

        [Tooltip("Background gradient end (slightly lighter)")]
        public Color BackgroundGradientEnd = new Color(0.15f, 0.15f, 0.15f, 1f);

        [Header("Boundary Effect Colors")]
        [Tooltip("Six boundary fireworks color - Gold")]
        public Color SixColor = new Color(1f, 0.843f, 0f, 1f);

        [Tooltip("Four boundary streak color - Blue")]
        public Color FourColor = new Color(0.129f, 0.588f, 0.953f, 1f);

        /// <summary>
        /// Get a color with modified alpha.
        /// </summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
