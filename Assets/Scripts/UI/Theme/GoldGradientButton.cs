using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace MetaCricket.UI
{
    /// <summary>
    /// Custom premium button component with gold gradient background,
    /// glow pulse animation on hover/press, press scale animation, and haptic feedback trigger.
    /// Uses DOTween for smooth animations matching the PUBG Mobile / FIFA Mobile luxury UI feel.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GoldGradientButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Button Colors")]
        [SerializeField] private Color _gradientStartColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _gradientEndColor = new Color(1f, 0.757f, 0.027f, 1f);
        [SerializeField] private Color _pressedColor = new Color(0.8f, 0.67f, 0f, 1f);
        [SerializeField] private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Glow Settings")]
        [SerializeField] private bool _enableGlow = true;
        [SerializeField] private float _glowIntensity = 1.2f;
        [SerializeField] private float _glowPulseSpeed = 1.5f;
        [SerializeField] private Color _glowColor = new Color(1f, 0.878f, 0.4f, 0.6f);

        [Header("Animation Settings")]
        [SerializeField] private float _pressScale = 0.95f;
        [SerializeField] private float _hoverScale = 1.03f;
        [SerializeField] private float _pressAnimDuration = 0.1f;
        [SerializeField] private float _releaseAnimDuration = 0.2f;
        [SerializeField] private Ease _pressEase = Ease.OutQuad;
        [SerializeField] private Ease _releaseEase = Ease.OutBack;

        [Header("Haptic Feedback")]
        [SerializeField] private bool _enableHaptics = true;

        [Header("References")]
        [SerializeField] private Image _glowImage;
        [SerializeField] private Button _button;

        private Image _backgroundImage;
        private RectTransform _rectTransform;
        private Tween _glowPulseTween;
        private Tween _scaleTween;
        private Vector3 _originalScale;
        private bool _isPressed;
        private bool _isInteractable = true;

        /// <summary>
        /// Whether the button is interactable.
        /// </summary>
        public bool Interactable
        {
            get => _isInteractable;
            set
            {
                _isInteractable = value;
                UpdateInteractableState();
            }
        }

        private void Awake()
        {
            _backgroundImage = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = _rectTransform.localScale;

            if (_button == null)
                _button = GetComponent<Button>();

            SetupGradient();
            StartGlowPulse();
        }

        private void OnEnable()
        {
            StartGlowPulse();
        }

        private void OnDisable()
        {
            StopGlowPulse();
            _scaleTween?.Kill();
            _rectTransform.localScale = _originalScale;
        }

        private void OnDestroy()
        {
            _glowPulseTween?.Kill();
            _scaleTween?.Kill();
        }

        private void SetupGradient()
        {
            // Apply gradient color to background (midpoint of gradient)
            if (_backgroundImage != null)
            {
                _backgroundImage.color = Color.Lerp(_gradientStartColor, _gradientEndColor, 0.5f);
            }
        }

        private void StartGlowPulse()
        {
            if (!_enableGlow || _glowImage == null) return;

            _glowImage.color = _glowColor;
            _glowPulseTween?.Kill();
            _glowPulseTween = _glowImage.DOFade(0.2f, 1f / _glowPulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private void StopGlowPulse()
        {
            _glowPulseTween?.Kill();
            if (_glowImage != null)
            {
                _glowImage.color = ThemeColors.WithAlpha(_glowColor, 0f);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            _isPressed = true;

            // Press scale animation
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(_originalScale * _pressScale, _pressAnimDuration)
                .SetEase(_pressEase)
                .SetUpdate(true);

            // Darken button
            if (_backgroundImage != null)
            {
                _backgroundImage.DOColor(_pressedColor, _pressAnimDuration).SetUpdate(true);
            }

            // Intensify glow
            if (_enableGlow && _glowImage != null)
            {
                _glowImage.DOFade(_glowIntensity * 0.8f, _pressAnimDuration).SetUpdate(true);
            }

            // Trigger haptic feedback
            if (_enableHaptics)
            {
                TriggerHaptic();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            _isPressed = false;

            // Release scale animation with overshoot
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(_originalScale, _releaseAnimDuration)
                .SetEase(_releaseEase)
                .SetUpdate(true);

            // Restore gradient color
            if (_backgroundImage != null)
            {
                _backgroundImage.DOColor(
                    Color.Lerp(_gradientStartColor, _gradientEndColor, 0.5f),
                    _releaseAnimDuration
                ).SetUpdate(true);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable || _isPressed) return;

            // Subtle hover scale
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(_originalScale * _hoverScale, 0.15f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            // Brighten glow on hover
            if (_enableGlow && _glowImage != null)
            {
                _glowImage.DOFade(_glowIntensity * 0.5f, 0.15f).SetUpdate(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable || _isPressed) return;

            // Return to normal scale
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(_originalScale, 0.2f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void UpdateInteractableState()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _isInteractable
                    ? Color.Lerp(_gradientStartColor, _gradientEndColor, 0.5f)
                    : _disabledColor;
            }

            if (_button != null)
            {
                _button.interactable = _isInteractable;
            }

            if (!_isInteractable)
            {
                StopGlowPulse();
            }
            else
            {
                StartGlowPulse();
            }
        }

        private void TriggerHaptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    // Light haptic feedback (10ms)
                    vibrator.Call("vibrate", 10L);
                }
            }
            catch (System.Exception)
            {
                // Haptic feedback not available - silently fail
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// Play a punch scale animation for emphasis (e.g., on click confirmation).
        /// </summary>
        public void PlayPunchAnimation()
        {
            _scaleTween?.Kill();
            _rectTransform.localScale = _originalScale;
            _scaleTween = _rectTransform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5, 0.5f)
                .SetUpdate(true);
        }
    }
}
