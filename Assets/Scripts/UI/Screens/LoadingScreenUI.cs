using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Loading screen with gold gradient progress bar,
    /// rotating cricket ball icon, and random tips/facts text rotation.
    /// </summary>
    public class LoadingScreenUI : MonoBehaviour
    {
        [Header("Progress Bar")]
        [SerializeField] private ProgressBar _progressBar;
        [SerializeField] private Text _progressPercentText;

        [Header("Cricket Ball Icon")]
        [SerializeField] private RectTransform _cricketBallIcon;
        [SerializeField] private float _rotationSpeed = 180f;

        [Header("Tips & Facts")]
        [SerializeField] private Text _tipText;
        [SerializeField] private CanvasGroup _tipGroup;
        [SerializeField] private float _tipRotationInterval = 3f;
        [SerializeField] private string[] _cricketTips = new string[]
        {
            "Tip: Time your shot at the moment of ball bounce for maximum power",
            "Did you know? A cricket ball can reach speeds of over 160 km/h",
            "Tip: Use the Cover Drive against full-length deliveries",
            "Did you know? The longest cricket match lasted 12 days",
            "Tip: Watch the bowler's hand to predict the delivery type",
            "Did you know? The record for most sixes in a T20 is 16",
            "Tip: Pull shots work best against short-pitched deliveries",
            "Did you know? Cricket originated in England in the 16th century",
            "Tip: Calibrate before each session for best accuracy",
            "Did you know? The fastest century was scored in just 12 balls",
            "Tip: The Helicopter Shot is devastating against yorkers",
            "Did you know? IPL is the most-attended cricket league in the world"
        };

        [Header("Background")]
        [SerializeField] private Image _backgroundImage;

        private float _tipTimer;
        private int _currentTipIndex;
        private float _targetProgress;
        private bool _isRotating = true;
        private Tween _tipFadeTween;

        private void OnEnable()
        {
            ResetLoading();
            ShowRandomTip();
        }

        private void Update()
        {
            RotateBall();
            UpdateTipRotation();
        }

        private void OnDisable()
        {
            _tipFadeTween?.Kill();
        }

        private void ResetLoading()
        {
            _targetProgress = 0f;

            if (_progressBar != null)
                _progressBar.SetProgressImmediate(0f);

            if (_progressPercentText != null)
                _progressPercentText.text = "0%";
        }

        private void RotateBall()
        {
            if (!_isRotating || _cricketBallIcon == null) return;

            _cricketBallIcon.Rotate(0f, 0f, -_rotationSpeed * Time.deltaTime);
        }

        private void UpdateTipRotation()
        {
            _tipTimer += Time.deltaTime;

            if (_tipTimer >= _tipRotationInterval)
            {
                _tipTimer = 0f;
                ShowNextTip();
            }
        }

        /// <summary>
        /// Set the loading progress (0 to 1).
        /// </summary>
        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);

            if (_progressBar != null)
            {
                _progressBar.SetProgress(_targetProgress, animated: true);
            }

            if (_progressPercentText != null)
            {
                _progressPercentText.text = $"{Mathf.RoundToInt(_targetProgress * 100)}%";
            }
        }

        /// <summary>
        /// Complete loading and trigger transition.
        /// </summary>
        public void CompleteLoading()
        {
            SetProgress(1f);
            _isRotating = false;

            // Scale up the ball on completion
            if (_cricketBallIcon != null)
            {
                _cricketBallIcon.DOScale(1.2f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        _cricketBallIcon.DOScale(0f, 0.3f)
                            .SetDelay(0.2f);
                    });
            }
        }

        private void ShowRandomTip()
        {
            if (_cricketTips == null || _cricketTips.Length == 0) return;

            _currentTipIndex = Random.Range(0, _cricketTips.Length);
            if (_tipText != null)
            {
                _tipText.text = _cricketTips[_currentTipIndex];
            }
        }

        private void ShowNextTip()
        {
            if (_cricketTips == null || _cricketTips.Length == 0 || _tipGroup == null) return;

            _tipFadeTween?.Kill();

            // Fade out, change text, fade in
            _tipFadeTween = _tipGroup.DOFade(0f, 0.3f)
                .OnComplete(() =>
                {
                    _currentTipIndex = (_currentTipIndex + 1) % _cricketTips.Length;
                    if (_tipText != null)
                    {
                        _tipText.text = _cricketTips[_currentTipIndex];
                    }
                    _tipGroup.DOFade(1f, 0.3f);
                });
        }

        /// <summary>
        /// Set a custom loading message below the progress bar.
        /// </summary>
        public void SetLoadingMessage(string message)
        {
            if (_tipText != null)
            {
                _tipText.text = message;
            }
        }
    }
}
