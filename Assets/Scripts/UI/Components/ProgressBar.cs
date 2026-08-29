using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MetaCricket.UI
{
    /// <summary>
    /// Reusable animated progress bar with gold gradient fill,
    /// glow effect on completion, optional label, and DOTween smooth fill animation.
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        [Header("Fill")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Color _fillColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _fillCompleteColor = new Color(0.298f, 0.686f, 0.314f, 1f);
        [SerializeField] private Image _fillGradientOverlay;

        [Header("Background")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Header("Glow")]
        [SerializeField] private Image _glowImage;
        [SerializeField] private Color _glowColor = new Color(1f, 0.878f, 0.4f, 0.6f);
        [SerializeField] private bool _glowOnComplete = true;
        [SerializeField] private float _glowPulseSpeed = 2f;

        [Header("Label")]
        [SerializeField] private Text _labelText;
        [SerializeField] private string _labelFormat = "{0:P0}";
        [SerializeField] private bool _showLabel = true;

        [Header("Animation")]
        [SerializeField] private float _fillAnimDuration = 0.5f;
        [SerializeField] private Ease _fillEase = Ease.OutCubic;
        [SerializeField] private bool _animateOnStart;
        [SerializeField] private float _startValue;

        private float _currentProgress;
        private Tween _fillTween;
        private Tween _glowTween;
        private bool _isComplete;

        /// <summary>
        /// Current progress value (0 to 1).
        /// </summary>
        public float Progress => _currentProgress;

        /// <summary>
        /// Whether the progress bar is at 100%.
        /// </summary>
        public bool IsComplete => _isComplete;

        private void Awake()
        {
            InitializeBar();
        }

        private void Start()
        {
            if (_animateOnStart)
            {
                SetProgressImmediate(_startValue);
                SetProgress(_startValue, animated: true);
            }
        }

        private void OnDestroy()
        {
            _fillTween?.Kill();
            _glowTween?.Kill();
        }

        private void InitializeBar()
        {
            if (_backgroundImage != null)
                _backgroundImage.color = _backgroundColor;

            if (_fillImage != null)
            {
                _fillImage.color = _fillColor;
                _fillImage.fillAmount = 0f;
            }

            if (_glowImage != null)
            {
                _glowImage.color = ThemeColors.WithAlpha(_glowColor, 0f);
            }

            UpdateLabel(0f);
        }

        /// <summary>
        /// Set progress with optional animation.
        /// </summary>
        /// <param name="progress">Target progress value (0 to 1).</param>
        /// <param name="animated">Whether to animate the transition.</param>
        public void SetProgress(float progress, bool animated = true)
        {
            float targetProgress = Mathf.Clamp01(progress);

            _fillTween?.Kill();

            if (animated)
            {
                _fillTween = DOTween.To(
                    () => _currentProgress,
                    x =>
                    {
                        _currentProgress = x;
                        ApplyProgress(x);
                    },
                    targetProgress,
                    _fillAnimDuration
                ).SetEase(_fillEase).SetUpdate(true);
            }
            else
            {
                _currentProgress = targetProgress;
                ApplyProgress(targetProgress);
            }
        }

        /// <summary>
        /// Set progress immediately without animation.
        /// </summary>
        public void SetProgressImmediate(float progress)
        {
            _fillTween?.Kill();
            _currentProgress = Mathf.Clamp01(progress);
            ApplyProgress(_currentProgress);
        }

        /// <summary>
        /// Reset progress to zero.
        /// </summary>
        public void Reset()
        {
            _fillTween?.Kill();
            _glowTween?.Kill();
            _currentProgress = 0f;
            _isComplete = false;
            ApplyProgress(0f);

            if (_glowImage != null)
                _glowImage.color = ThemeColors.WithAlpha(_glowColor, 0f);
        }

        private void ApplyProgress(float value)
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = value;

                // Change color when complete
                if (value >= 1f && !_isComplete)
                {
                    _isComplete = true;
                    _fillImage.DOColor(_fillCompleteColor, 0.3f);
                    if (_glowOnComplete) PlayCompletionGlow();
                }
                else if (value < 1f && _isComplete)
                {
                    _isComplete = false;
                    _fillImage.color = _fillColor;
                    StopCompletionGlow();
                }
            }

            UpdateLabel(value);
        }

        private void UpdateLabel(float value)
        {
            if (!_showLabel || _labelText == null) return;

            _labelText.text = string.Format(_labelFormat, value);
        }

        private void PlayCompletionGlow()
        {
            if (_glowImage == null) return;

            _glowTween?.Kill();
            _glowImage.color = _glowColor;
            _glowTween = _glowImage.DOFade(0.2f, 1f / _glowPulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private void StopCompletionGlow()
        {
            _glowTween?.Kill();
            if (_glowImage != null)
                _glowImage.color = ThemeColors.WithAlpha(_glowColor, 0f);
        }

        /// <summary>
        /// Set custom fill color.
        /// </summary>
        public void SetFillColor(Color color)
        {
            _fillColor = color;
            if (_fillImage != null && !_isComplete)
                _fillImage.color = color;
        }

        /// <summary>
        /// Set the label format string. Use {0} for the progress value (0-1) or {0:P0} for percentage.
        /// </summary>
        public void SetLabelFormat(string format)
        {
            _labelFormat = format;
            UpdateLabel(_currentProgress);
        }
    }
}
