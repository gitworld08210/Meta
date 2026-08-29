using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Animated splash screen with Meta Cricket logo reveal,
    /// gold particle trail, background fade from black, typewriter tagline,
    /// and auto-transition to MainMenu after 3 seconds.
    /// </summary>
    public class SplashScreenUI : MonoBehaviour
    {
        [Header("Logo")]
        [SerializeField] private Image _logoImage;
        [SerializeField] private CanvasGroup _logoCanvasGroup;
        [SerializeField] private float _logoRevealDuration = 1.0f;
        [SerializeField] private float _logoScaleFrom = 0.5f;

        [Header("Background")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _backgroundStartColor = Color.black;
        [SerializeField] private Color _backgroundEndColor = new Color(0.102f, 0.102f, 0.102f, 1f);
        [SerializeField] private float _backgroundFadeDuration = 1.5f;

        [Header("Tagline")]
        [SerializeField] private Text _taglineText;
        [SerializeField] private string _tagline = "Your Cricket. Your Way.";
        [SerializeField] private float _typewriterSpeed = 0.05f;

        [Header("Gold Particles")]
        [SerializeField] private ParticleSystem _goldParticleTrail;
        [SerializeField] private float _particleStartDelay = 0.3f;

        [Header("Transition")]
        [SerializeField] private float _autoTransitionDelay = 3.0f;
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private CanvasGroup _screenCanvasGroup;

        private Sequence _splashSequence;
        private bool _transitionTriggered;

        private void Start()
        {
            InitializeElements();
            PlaySplashAnimation();
        }

        private void OnDestroy()
        {
            _splashSequence?.Kill();
        }

        private void InitializeElements()
        {
            // Set initial states
            if (_logoCanvasGroup != null)
            {
                _logoCanvasGroup.alpha = 0f;
            }

            if (_logoImage != null)
            {
                _logoImage.transform.localScale = Vector3.one * _logoScaleFrom;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = _backgroundStartColor;
            }

            if (_taglineText != null)
            {
                _taglineText.text = "";
            }

            if (_goldParticleTrail != null)
            {
                _goldParticleTrail.Stop();
            }
        }

        private void PlaySplashAnimation()
        {
            _splashSequence = DOTween.Sequence();

            // Background fade from black to deep black
            if (_backgroundImage != null)
            {
                _splashSequence.Append(
                    _backgroundImage.DOColor(_backgroundEndColor, _backgroundFadeDuration)
                        .SetEase(Ease.OutQuad)
                );
            }

            // Logo reveal with scale and fade
            if (_logoCanvasGroup != null && _logoImage != null)
            {
                _splashSequence.Insert(0.3f,
                    _logoCanvasGroup.DOFade(1f, _logoRevealDuration)
                        .SetEase(Ease.OutCubic)
                );
                _splashSequence.Insert(0.3f,
                    _logoImage.transform.DOScale(1f, _logoRevealDuration)
                        .SetEase(Ease.OutBack)
                );
            }

            // Start gold particle trail
            if (_goldParticleTrail != null)
            {
                _splashSequence.InsertCallback(_particleStartDelay, () =>
                {
                    _goldParticleTrail.Play();
                });
            }

            // Typewriter tagline effect
            if (_taglineText != null)
            {
                float taglineStartTime = _logoRevealDuration + 0.5f;
                _splashSequence.InsertCallback(taglineStartTime, () =>
                {
                    StartTypewriterEffect();
                });
            }

            // Auto-transition to Main Menu
            _splashSequence.InsertCallback(_autoTransitionDelay, () =>
            {
                TransitionToMainMenu();
            });

            _splashSequence.SetUpdate(true);
        }

        private void StartTypewriterEffect()
        {
            if (_taglineText == null) return;

            _taglineText.text = "";
            int charIndex = 0;

            DOTween.To(
                () => charIndex,
                x =>
                {
                    charIndex = x;
                    if (charIndex <= _tagline.Length)
                    {
                        _taglineText.text = _tagline.Substring(0, charIndex);
                    }
                },
                _tagline.Length,
                _tagline.Length * _typewriterSpeed
            ).SetEase(Ease.Linear).SetUpdate(true);
        }

        private void TransitionToMainMenu()
        {
            if (_transitionTriggered) return;
            _transitionTriggered = true;

            if (_screenCanvasGroup != null)
            {
                _screenCanvasGroup.DOFade(0f, _fadeOutDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        // Publish UI transition event
                        EventBus.Publish(new UITransitionEvent
                        {
                            FromScreen = UIScreen.Splash,
                            ToScreen = UIScreen.MainMenu,
                            Animated = true
                        });

                        gameObject.SetActive(false);
                    });
            }
            else
            {
                EventBus.Publish(new UITransitionEvent
                {
                    FromScreen = UIScreen.Splash,
                    ToScreen = UIScreen.MainMenu,
                    Animated = true
                });

                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Skip splash animation (e.g., on screen tap).
        /// </summary>
        public void SkipSplash()
        {
            _splashSequence?.Kill();
            TransitionToMainMenu();
        }
    }
}
