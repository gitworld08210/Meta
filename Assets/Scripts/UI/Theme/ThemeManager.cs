using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Singleton manager for the premium gold+black UI theme.
    /// Provides global access to theme colors, gradient definitions, and glow settings.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        private static ThemeManager _instance;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[ThemeManager] Instance is null. Ensure ThemeManager exists in the scene.");
                }
                return _instance;
            }
        }

        [Header("Theme Configuration")]
        [SerializeField] private ThemeColors _themeColors;

        [Header("Glow Settings")]
        [SerializeField] private float _glowIntensity = 1.5f;
        [SerializeField] private float _glowPulseSpeed = 2f;
        [SerializeField] private float _glowPulseMinIntensity = 0.8f;
        [SerializeField] private float _glowPulseMaxIntensity = 1.5f;

        [Header("Animation Settings")]
        [SerializeField] private float _defaultTransitionDuration = 0.3f;
        [SerializeField] private float _buttonPressScale = 0.95f;
        [SerializeField] private float _buttonHoverScale = 1.05f;

        /// <summary>
        /// Current theme colors reference.
        /// </summary>
        public ThemeColors Colors => _themeColors;

        /// <summary>
        /// Current glow intensity for UI elements.
        /// </summary>
        public float GlowIntensity => _glowIntensity;

        /// <summary>
        /// Speed of the glow pulse animation.
        /// </summary>
        public float GlowPulseSpeed => _glowPulseSpeed;

        /// <summary>
        /// Minimum glow intensity during pulse.
        /// </summary>
        public float GlowPulseMinIntensity => _glowPulseMinIntensity;

        /// <summary>
        /// Maximum glow intensity during pulse.
        /// </summary>
        public float GlowPulseMaxIntensity => _glowPulseMaxIntensity;

        /// <summary>
        /// Default duration for UI transitions.
        /// </summary>
        public float DefaultTransitionDuration => _defaultTransitionDuration;

        /// <summary>
        /// Scale value when button is pressed.
        /// </summary>
        public float ButtonPressScale => _buttonPressScale;

        /// <summary>
        /// Scale value when button is hovered/highlighted.
        /// </summary>
        public float ButtonHoverScale => _buttonHoverScale;

        // Cached color values for quick access (hex defined in spec)
        /// <summary>Primary Gold #FFD700</summary>
        public Color PrimaryGold => _themeColors != null ? _themeColors.PrimaryGold : HexToColor("FFD700");

        /// <summary>Deep Black #1A1A1A</summary>
        public Color DeepBlack => _themeColors != null ? _themeColors.DeepBlack : HexToColor("1A1A1A");

        /// <summary>Accent Gold #FFC107</summary>
        public Color AccentGold => _themeColors != null ? _themeColors.AccentGold : HexToColor("FFC107");

        /// <summary>Glass White #FFFFFF1A (10% opacity)</summary>
        public Color GlassWhite => _themeColors != null ? _themeColors.GlassWhite : new Color(1f, 1f, 1f, 0.1f);

        /// <summary>Neon Glow #FFE066</summary>
        public Color NeonGlow => _themeColors != null ? _themeColors.NeonGlow : HexToColor("FFE066");

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Initialize()
        {
            // Create default theme colors if not assigned
            if (_themeColors == null)
            {
                _themeColors = ScriptableObject.CreateInstance<ThemeColors>();
                Debug.LogWarning("[ThemeManager] No ThemeColors assigned. Using default theme.");
            }

            ServiceLocator.Register(this);
            Debug.Log("[ThemeManager] Theme system initialized with premium gold+black palette.");
        }

        /// <summary>
        /// Get a gradient between two gold colors for button backgrounds.
        /// </summary>
        public Gradient GetGoldGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(_themeColors.GoldGradientStart, 0f);
            colorKeys[1] = new GradientColorKey(_themeColors.GoldGradientEnd, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Get a gradient for background elements (dark to slightly lighter).
        /// </summary>
        public Gradient GetBackgroundGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(_themeColors.BackgroundGradientStart, 0f);
            colorKeys[1] = new GradientColorKey(_themeColors.BackgroundGradientEnd, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Calculate current glow intensity based on pulse animation.
        /// </summary>
        public float GetPulsedGlowIntensity()
        {
            float t = (Mathf.Sin(Time.time * _glowPulseSpeed) + 1f) * 0.5f;
            return Mathf.Lerp(_glowPulseMinIntensity, _glowPulseMaxIntensity, t);
        }

        /// <summary>
        /// Convert hex color string to Color.
        /// </summary>
        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString($"#{hex}", out Color color))
            {
                return color;
            }
            return Color.white;
        }
    }
}
