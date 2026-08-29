using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Calibration interface with camera feed background, skeleton overlay,
    /// step instructions, progress ring, T-pose guide silhouette, and confirmation animation.
    /// </summary>
    public class CalibrationScreenUI : MonoBehaviour
    {
        [Header("Camera Feed")]
        [SerializeField] private RawImage _cameraFeedImage;
        [SerializeField] private Image _skeletonOverlay;

        [Header("Instructions")]
        [SerializeField] private Text _instructionText;
        [SerializeField] private Text _stepIndicatorText;
        [SerializeField] private CanvasGroup _instructionGroup;
        [SerializeField] private float _instructionFadeDuration = 0.3f;

        [Header("Progress Ring")]
        [SerializeField] private Image _progressRing;
        [SerializeField] private Image _progressRingBackground;
        [SerializeField] private Text _progressPercentText;
        [SerializeField] private Color _progressColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _progressCompleteColor = new Color(0.298f, 0.686f, 0.314f, 1f);

        [Header("T-Pose Guide")]
        [SerializeField] private Image _tPoseGuide;
        [SerializeField] private CanvasGroup _tPoseGuideGroup;
        [SerializeField] private float _guidePulseSpeed = 1.5f;

        [Header("Confirmation")]
        [SerializeField] private CanvasGroup _confirmationGroup;
        [SerializeField] private Image _checkmarkIcon;
        [SerializeField] private Text _confirmationText;
        [SerializeField] private ParticleSystem _confirmationParticles;

        [Header("Buttons")]
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private GoldGradientButton _continueButton;

        [Header("Step Messages")]
        [SerializeField] private string[] _stepInstructions = new string[]
        {
            "Stand 2 meters from your device",
            "Raise both arms to form a T-pose",
            "Hold still while we calibrate",
            "Calibration complete!"
        };

        private int _currentStep;
        private float _currentProgress;
        private Tween _progressTween;
        private Tween _guidePulseTween;
        private bool _calibrationComplete;

        private void Start()
        {
            InitializeUI();
            SetupButtons();
            ShowStep(0);
        }

        private void OnDisable()
        {
            _progressTween?.Kill();
            _guidePulseTween?.Kill();
        }

        private void InitializeUI()
        {
            if (_progressRing != null)
            {
                _progressRing.fillAmount = 0f;
                _progressRing.color = _progressColor;
            }

            if (_confirmationGroup != null) _confirmationGroup.alpha = 0f;
            if (_tPoseGuideGroup != null) _tPoseGuideGroup.alpha = 0.6f;

            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(false);
            }

            // Start T-pose guide pulse
            if (_tPoseGuideGroup != null)
            {
                _guidePulseTween = _tPoseGuideGroup.DOFade(0.3f, 1f / _guidePulseSpeed)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }

        private void SetupButtons()
        {
            if (_skipButton != null)
                _skipButton.onClick.AddListener(OnSkipClicked);

            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);

            if (_continueButton != null)
            {
                Button btn = _continueButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OnContinueClicked);
            }
        }

        /// <summary>
        /// Show a specific calibration step.
        /// </summary>
        public void ShowStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= _stepInstructions.Length) return;

            _currentStep = stepIndex;

            // Fade transition for instruction text
            if (_instructionGroup != null)
            {
                _instructionGroup.DOFade(0f, _instructionFadeDuration * 0.5f)
                    .OnComplete(() =>
                    {
                        if (_instructionText != null)
                            _instructionText.text = _stepInstructions[stepIndex];

                        if (_stepIndicatorText != null)
                            _stepIndicatorText.text = $"Step {stepIndex + 1} of {_stepInstructions.Length}";

                        _instructionGroup.DOFade(1f, _instructionFadeDuration * 0.5f);
                    });
            }
        }

        /// <summary>
        /// Update the progress ring fill amount.
        /// </summary>
        public void UpdateProgress(float progress)
        {
            _currentProgress = Mathf.Clamp01(progress);

            _progressTween?.Kill();

            if (_progressRing != null)
            {
                _progressTween = _progressRing.DOFillAmount(_currentProgress, 0.3f)
                    .SetEase(Ease.OutQuad);
            }

            if (_progressPercentText != null)
            {
                _progressPercentText.text = $"{Mathf.RoundToInt(_currentProgress * 100)}%";
            }

            // Check if calibration is complete
            if (_currentProgress >= 1f && !_calibrationComplete)
            {
                ShowCalibrationComplete();
            }
        }

        /// <summary>
        /// Show skeleton tracking overlay for body pose detection feedback.
        /// </summary>
        public void UpdateSkeletonOverlay(bool isTracking)
        {
            if (_skeletonOverlay != null)
            {
                Color color = _skeletonOverlay.color;
                color.a = isTracking ? 1f : 0.3f;
                _skeletonOverlay.color = color;
            }
        }

        private void ShowCalibrationComplete()
        {
            _calibrationComplete = true;

            // Change progress ring color
            if (_progressRing != null)
            {
                _progressRing.DOColor(_progressCompleteColor, 0.3f);
            }

            // Hide T-pose guide
            _guidePulseTween?.Kill();
            if (_tPoseGuideGroup != null)
            {
                _tPoseGuideGroup.DOFade(0f, 0.3f);
            }

            // Show last step instruction
            ShowStep(_stepInstructions.Length - 1);

            // Animate confirmation
            if (_confirmationGroup != null)
            {
                _confirmationGroup.DOFade(1f, 0.4f).SetEase(Ease.OutCubic);

                if (_checkmarkIcon != null)
                {
                    _checkmarkIcon.transform.localScale = Vector3.zero;
                    _checkmarkIcon.transform.DOScale(1f, 0.5f)
                        .SetDelay(0.2f)
                        .SetEase(Ease.OutBack);
                }
            }

            // Play particles
            if (_confirmationParticles != null)
            {
                _confirmationParticles.Play();
            }

            // Show continue button
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(true);
                RectTransform btnRect = _continueButton.GetComponent<RectTransform>();
                if (btnRect != null)
                {
                    btnRect.localScale = Vector3.zero;
                    btnRect.DOScale(1f, 0.4f).SetDelay(0.5f).SetEase(Ease.OutBack);
                }
            }
        }

        private void OnSkipClicked()
        {
            // Skip calibration and proceed with defaults
            EventBus.Publish(new CalibrationCompleteEvent
            {
                Success = false,
                PitchPosition = Vector3.zero,
                PitchRotation = Quaternion.identity,
                PitchScale = 1f
            });

            TransitionToMatch();
        }

        private void OnRetryClicked()
        {
            _calibrationComplete = false;
            _currentProgress = 0f;

            if (_progressRing != null)
            {
                _progressRing.fillAmount = 0f;
                _progressRing.color = _progressColor;
            }

            if (_confirmationGroup != null) _confirmationGroup.alpha = 0f;
            if (_continueButton != null) _continueButton.gameObject.SetActive(false);

            InitializeUI();
            ShowStep(0);
        }

        private void OnContinueClicked()
        {
            EventBus.Publish(new CalibrationCompleteEvent
            {
                Success = true,
                PitchPosition = Vector3.zero,
                PitchRotation = Quaternion.identity,
                PitchScale = 1f
            });

            TransitionToMatch();
        }

        private void TransitionToMatch()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.Calibration,
                ToScreen = UIScreen.InMatch,
                Animated = true
            });
        }
    }
}
