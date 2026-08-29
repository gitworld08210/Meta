using System;
using UnityEngine;
using DG.Tweening;

namespace MetaCricket.UI
{
    /// <summary>
    /// Direction for slide transitions.
    /// </summary>
    public enum TransitionDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// Type of transition animation.
    /// </summary>
    public enum TransitionType
    {
        Fade,
        Slide,
        Scale,
        SlideAndFade,
        ScaleAndFade
    }

    /// <summary>
    /// Reusable screen transition component supporting multiple animation types.
    /// Uses DOTween for smooth, configurable transitions between UI screens.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AnimatedTransition : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private TransitionType _transitionType = TransitionType.SlideAndFade;
        [SerializeField] private TransitionDirection _direction = TransitionDirection.Right;
        [SerializeField] private float _duration = 0.4f;
        [SerializeField] private Ease _showEase = Ease.OutCubic;
        [SerializeField] private Ease _hideEase = Ease.InCubic;

        [Header("Slide Settings")]
        [SerializeField] private float _slideDistance = 300f;

        [Header("Scale Settings")]
        [SerializeField] private float _scaleFrom = 0.8f;
        [SerializeField] private float _scaleTo = 1f;

        [Header("Fade Settings")]
        [SerializeField] private float _fadeFrom = 0f;
        [SerializeField] private float _fadeTo = 1f;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector2 _originalPosition;
        private Sequence _currentSequence;
        private bool _isVisible;

        /// <summary>
        /// Whether the element is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Duration of the transition.
        /// </summary>
        public float Duration
        {
            get => _duration;
            set => _duration = Mathf.Max(0.01f, value);
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _originalPosition = _rectTransform.anchoredPosition;
        }

        private void OnDestroy()
        {
            _currentSequence?.Kill();
        }

        /// <summary>
        /// Show the element with the configured transition animation.
        /// </summary>
        /// <param name="onComplete">Callback when transition completes.</param>
        public void Show(Action onComplete = null)
        {
            _currentSequence?.Kill();
            gameObject.SetActive(true);

            _currentSequence = DOTween.Sequence();

            switch (_transitionType)
            {
                case TransitionType.Fade:
                    SetupFadeIn(_currentSequence);
                    break;
                case TransitionType.Slide:
                    SetupSlideIn(_currentSequence);
                    break;
                case TransitionType.Scale:
                    SetupScaleIn(_currentSequence);
                    break;
                case TransitionType.SlideAndFade:
                    SetupSlideIn(_currentSequence);
                    SetupFadeIn(_currentSequence);
                    break;
                case TransitionType.ScaleAndFade:
                    SetupScaleIn(_currentSequence);
                    SetupFadeIn(_currentSequence);
                    break;
            }

            _currentSequence.SetEase(_showEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _isVisible = true;
                    _canvasGroup.interactable = true;
                    _canvasGroup.blocksRaycasts = true;
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// Hide the element with the configured transition animation.
        /// </summary>
        /// <param name="onComplete">Callback when transition completes.</param>
        public void Hide(Action onComplete = null)
        {
            _currentSequence?.Kill();

            _currentSequence = DOTween.Sequence();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            switch (_transitionType)
            {
                case TransitionType.Fade:
                    SetupFadeOut(_currentSequence);
                    break;
                case TransitionType.Slide:
                    SetupSlideOut(_currentSequence);
                    break;
                case TransitionType.Scale:
                    SetupScaleOut(_currentSequence);
                    break;
                case TransitionType.SlideAndFade:
                    SetupSlideOut(_currentSequence);
                    SetupFadeOut(_currentSequence);
                    break;
                case TransitionType.ScaleAndFade:
                    SetupScaleOut(_currentSequence);
                    SetupFadeOut(_currentSequence);
                    break;
            }

            _currentSequence.SetEase(_hideEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _isVisible = false;
                    gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// Instantly show without animation.
        /// </summary>
        public void ShowImmediate()
        {
            _currentSequence?.Kill();
            gameObject.SetActive(true);
            _canvasGroup.alpha = _fadeTo;
            _rectTransform.anchoredPosition = _originalPosition;
            _rectTransform.localScale = Vector3.one * _scaleTo;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _isVisible = true;
        }

        /// <summary>
        /// Instantly hide without animation.
        /// </summary>
        public void HideImmediate()
        {
            _currentSequence?.Kill();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _isVisible = false;
            gameObject.SetActive(false);
        }

        private void SetupFadeIn(Sequence sequence)
        {
            _canvasGroup.alpha = _fadeFrom;
            sequence.Join(_canvasGroup.DOFade(_fadeTo, _duration));
        }

        private void SetupFadeOut(Sequence sequence)
        {
            sequence.Join(_canvasGroup.DOFade(_fadeFrom, _duration));
        }

        private void SetupSlideIn(Sequence sequence)
        {
            Vector2 startOffset = GetDirectionOffset();
            _rectTransform.anchoredPosition = _originalPosition + startOffset;
            sequence.Join(_rectTransform.DOAnchorPos(_originalPosition, _duration));
        }

        private void SetupSlideOut(Sequence sequence)
        {
            Vector2 endOffset = GetDirectionOffset();
            sequence.Join(_rectTransform.DOAnchorPos(_originalPosition + endOffset, _duration));
        }

        private void SetupScaleIn(Sequence sequence)
        {
            _rectTransform.localScale = Vector3.one * _scaleFrom;
            sequence.Join(_rectTransform.DOScale(_scaleTo, _duration));
        }

        private void SetupScaleOut(Sequence sequence)
        {
            sequence.Join(_rectTransform.DOScale(_scaleFrom, _duration));
        }

        private Vector2 GetDirectionOffset()
        {
            switch (_direction)
            {
                case TransitionDirection.Left: return new Vector2(-_slideDistance, 0f);
                case TransitionDirection.Right: return new Vector2(_slideDistance, 0f);
                case TransitionDirection.Up: return new Vector2(0f, _slideDistance);
                case TransitionDirection.Down: return new Vector2(0f, -_slideDistance);
                default: return Vector2.zero;
            }
        }
    }
}
