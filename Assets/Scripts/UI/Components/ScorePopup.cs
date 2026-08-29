using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Floating score popup that shows runs scored per ball.
    /// +1 (white), +4 (blue flash), +6 (gold fireworks trigger), wicket (red).
    /// Animates with scale-up and float-away effect.
    /// </summary>
    public class ScorePopup : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rectTransform;

        [Header("Colors")]
        [SerializeField] private Color _singleColor = Color.white;
        [SerializeField] private Color _fourColor = new Color(0.129f, 0.588f, 0.953f, 1f);
        [SerializeField] private Color _sixColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _wicketColor = new Color(0.898f, 0.224f, 0.208f, 1f);
        [SerializeField] private Color _dotBallColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Animation Settings")]
        [SerializeField] private float _floatDistance = 100f;
        [SerializeField] private float _animationDuration = 1.5f;
        [SerializeField] private float _scaleUpDuration = 0.2f;
        [SerializeField] private float _initialScale = 0.5f;
        [SerializeField] private float _maxScale = 1.5f;
        [SerializeField] private Ease _floatEase = Ease.OutCubic;

        [Header("Six Special Effects")]
        [SerializeField] private ParticleSystem _fireworksParticles;
        [SerializeField] private Image _flashOverlay;
        [SerializeField] private float _flashDuration = 0.3f;

        private Sequence _animSequence;

        /// <summary>
        /// Show a score popup with appropriate styling and animation.
        /// </summary>
        /// <param name="runs">Number of runs (0 for dot ball, -1 for wicket).</param>
        /// <param name="position">Screen position to spawn the popup.</param>
        public void Show(int runs, Vector2 position)
        {
            if (_rectTransform == null || _scoreText == null) return;

            _rectTransform.anchoredPosition = position;
            gameObject.SetActive(true);

            // Set text and color
            SetPopupStyle(runs);

            // Play animation
            PlayAnimation(runs);
        }

        private void SetPopupStyle(int runs)
        {
            switch (runs)
            {
                case -1: // Wicket
                    _scoreText.text = "W";
                    _scoreText.color = _wicketColor;
                    break;
                case 0: // Dot ball
                    _scoreText.text = "0";
                    _scoreText.color = _dotBallColor;
                    break;
                case 4:
                    _scoreText.text = "+4";
                    _scoreText.color = _fourColor;
                    break;
                case 6:
                    _scoreText.text = "+6";
                    _scoreText.color = _sixColor;
                    break;
                default:
                    _scoreText.text = $"+{runs}";
                    _scoreText.color = _singleColor;
                    break;
            }
        }

        private void PlayAnimation(int runs)
        {
            _animSequence?.Kill();
            _animSequence = DOTween.Sequence();

            // Initial state
            _rectTransform.localScale = Vector3.one * _initialScale;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            // Scale up with bounce
            _animSequence.Append(
                _rectTransform.DOScale(_maxScale, _scaleUpDuration)
                    .SetEase(Ease.OutBack)
            );

            // Scale back to normal
            _animSequence.Append(
                _rectTransform.DOScale(1f, _scaleUpDuration * 0.5f)
                    .SetEase(Ease.InOutQuad)
            );

            // Float up
            Vector2 startPos = _rectTransform.anchoredPosition;
            _animSequence.Join(
                _rectTransform.DOAnchorPosY(startPos.y + _floatDistance, _animationDuration - _scaleUpDuration)
                    .SetEase(_floatEase)
            );

            // Fade out
            if (_canvasGroup != null)
            {
                _animSequence.Insert(_animationDuration * 0.6f,
                    _canvasGroup.DOFade(0f, _animationDuration * 0.4f)
                );
            }

            // Special effects for six
            if (runs == 6)
            {
                PlaySixEffect();
            }
            else if (runs == 4)
            {
                PlayFourEffect();
            }

            // Self-destroy after animation
            _animSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
                Destroy(gameObject, 0.1f);
            });

            _animSequence.SetUpdate(true);
        }

        private void PlaySixEffect()
        {
            // Gold fireworks
            if (_fireworksParticles != null)
            {
                _fireworksParticles.Play();
            }

            // Screen flash (gold)
            if (_flashOverlay != null)
            {
                _flashOverlay.color = ThemeColors.WithAlpha(_sixColor, 0.5f);
                _flashOverlay.gameObject.SetActive(true);

                Sequence flashSequence = DOTween.Sequence();
                flashSequence.Append(_flashOverlay.DOFade(0.5f, _flashDuration * 0.3f));
                flashSequence.Append(_flashOverlay.DOFade(0f, _flashDuration * 0.7f));
                flashSequence.OnComplete(() => _flashOverlay.gameObject.SetActive(false));
            }
        }

        private void PlayFourEffect()
        {
            // Blue flash for four
            if (_flashOverlay != null)
            {
                _flashOverlay.color = ThemeColors.WithAlpha(_fourColor, 0.3f);
                _flashOverlay.gameObject.SetActive(true);

                Sequence flashSequence = DOTween.Sequence();
                flashSequence.Append(_flashOverlay.DOFade(0.3f, 0.1f));
                flashSequence.Append(_flashOverlay.DOFade(0f, 0.2f));
                flashSequence.OnComplete(() => _flashOverlay.gameObject.SetActive(false));
            }
        }

        private void OnDestroy()
        {
            _animSequence?.Kill();
        }
    }
}
