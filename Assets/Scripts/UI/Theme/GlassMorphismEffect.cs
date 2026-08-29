using UnityEngine;
using UnityEngine.UI;

namespace MetaCricket.UI
{
    /// <summary>
    /// UI component that applies a glassmorphism visual effect to a panel.
    /// Creates a semi-transparent background with blur, rounded corners, and subtle border glow.
    /// Requires a shader that supports blur (referenced at runtime).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GlassMorphismEffect : MonoBehaviour
    {
        [Header("Glass Settings")]
        [SerializeField] [Range(0f, 1f)] private float _opacity = 0.1f;
        [SerializeField] [Range(0f, 20f)] private float _blurRadius = 10f;
        [SerializeField] private Color _tintColor = new Color(1f, 1f, 1f, 0.1f);

        [Header("Border Settings")]
        [SerializeField] private bool _showBorder = true;
        [SerializeField] private Color _borderColor = new Color(1f, 0.843f, 0f, 0.3f);
        [SerializeField] [Range(0f, 5f)] private float _borderWidth = 1f;
        [SerializeField] private float _borderGlowIntensity = 0.5f;

        [Header("Corner Settings")]
        [SerializeField] [Range(0f, 50f)] private float _cornerRadius = 12f;

        [Header("Shader Reference")]
        [SerializeField] private Material _glassMaterial;
        [SerializeField] private string _blurShaderName = "UI/GlassMorphism";

        private Image _backgroundImage;
        private Outline _borderOutline;
        private static readonly int BlurRadiusProperty = Shader.PropertyToID("_BlurRadius");
        private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");
        private static readonly int TintColorProperty = Shader.PropertyToID("_TintColor");

        /// <summary>
        /// Glass opacity (0 = fully transparent, 1 = fully opaque).
        /// </summary>
        public float Opacity
        {
            get => _opacity;
            set
            {
                _opacity = Mathf.Clamp01(value);
                ApplyEffect();
            }
        }

        /// <summary>
        /// Blur radius for the background blur effect.
        /// </summary>
        public float BlurRadius
        {
            get => _blurRadius;
            set
            {
                _blurRadius = Mathf.Clamp(value, 0f, 20f);
                ApplyEffect();
            }
        }

        private void Awake()
        {
            _backgroundImage = GetComponent<Image>();
            SetupGlassEffect();
        }

        private void OnEnable()
        {
            ApplyEffect();
        }

        private void SetupGlassEffect()
        {
            // Setup background image as glass surface
            if (_backgroundImage != null)
            {
                if (_glassMaterial != null)
                {
                    _backgroundImage.material = new Material(_glassMaterial);
                }
                else
                {
                    // Fallback: use standard transparency if custom shader not available
                    _backgroundImage.color = _tintColor;
                }
            }

            // Setup border glow using Outline component
            if (_showBorder)
            {
                _borderOutline = GetComponent<Outline>();
                if (_borderOutline == null)
                {
                    _borderOutline = gameObject.AddComponent<Outline>();
                }
                _borderOutline.effectColor = _borderColor;
                _borderOutline.effectDistance = new Vector2(_borderWidth, _borderWidth);
            }
        }

        /// <summary>
        /// Apply the glassmorphism effect with current settings.
        /// </summary>
        public void ApplyEffect()
        {
            if (_backgroundImage == null) return;

            if (_backgroundImage.material != null && _backgroundImage.material.HasProperty(BlurRadiusProperty))
            {
                // Custom shader path - set material properties
                _backgroundImage.material.SetFloat(BlurRadiusProperty, _blurRadius);
                _backgroundImage.material.SetFloat(OpacityProperty, _opacity);
                _backgroundImage.material.SetColor(TintColorProperty, _tintColor);
            }
            else
            {
                // Fallback path - simulate glass with semi-transparent color
                Color glassColor = _tintColor;
                glassColor.a = _opacity;
                _backgroundImage.color = glassColor;
            }

            // Update border glow
            if (_borderOutline != null)
            {
                Color glowColor = _borderColor;
                glowColor.a = _borderGlowIntensity;
                _borderOutline.effectColor = glowColor;
            }
        }

        /// <summary>
        /// Animate the glass opacity for reveal effects.
        /// </summary>
        public void SetOpacityImmediate(float targetOpacity)
        {
            _opacity = Mathf.Clamp01(targetOpacity);
            ApplyEffect();
        }

        /// <summary>
        /// Set the border glow color (useful for state changes).
        /// </summary>
        public void SetBorderGlow(Color color, float intensity)
        {
            _borderColor = color;
            _borderGlowIntensity = intensity;
            if (_borderOutline != null)
            {
                Color glowColor = color;
                glowColor.a = intensity;
                _borderOutline.effectColor = glowColor;
            }
        }

        /// <summary>
        /// Reset effect to default theme values.
        /// </summary>
        public void ResetToThemeDefaults()
        {
            if (ThemeManager.Instance != null)
            {
                _tintColor = ThemeManager.Instance.GlassWhite;
                _borderColor = ThemeColors.WithAlpha(ThemeManager.Instance.PrimaryGold, 0.3f);
                ApplyEffect();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            ApplyEffect();
        }
#endif
    }
}
