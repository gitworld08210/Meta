using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Premium main menu screen with animated stadium background,
    /// glassmorphism panel with navigation options, gold gradient buttons,
    /// and a player profile card.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField] private Image _stadiumBackground;
        [SerializeField] private RectTransform _movingLightsContainer;
        [SerializeField] private float _lightMoveSpeed = 50f;
        [SerializeField] private float _lightMoveRange = 100f;

        [Header("Menu Panel")]
        [SerializeField] private CanvasGroup _menuPanelGroup;
        [SerializeField] private GlassMorphismEffect _menuGlassPanel;
        [SerializeField] private AnimatedTransition _menuTransition;

        [Header("Buttons")]
        [SerializeField] private GoldGradientButton _playButton;
        [SerializeField] private GoldGradientButton _careerButton;
        [SerializeField] private GoldGradientButton _settingsButton;
        [SerializeField] private GoldGradientButton _leaderboardButton;

        [Header("Player Profile Card")]
        [SerializeField] private PlayerCard _playerCard;
        [SerializeField] private CanvasGroup _profileCardGroup;
        [SerializeField] private RectTransform _profileCardRect;

        [Header("Animation Settings")]
        [SerializeField] private float _buttonStaggerDelay = 0.1f;
        [SerializeField] private float _buttonAnimDuration = 0.4f;
        [SerializeField] private float _profileSlideDistance = 200f;

        [Header("Version Text")]
        [SerializeField] private Text _versionText;

        private Sequence _entrySequence;
        private Tween _lightsTween;
        private bool _isAnimating;

        private void OnEnable()
        {
            PlayEntryAnimation();
            StartBackgroundAnimation();
        }

        private void OnDisable()
        {
            _entrySequence?.Kill();
            _lightsTween?.Kill();
        }

        private void Start()
        {
            SetupButtons();
            SetupVersionText();
        }

        private void SetupButtons()
        {
            if (_playButton != null)
            {
                Button playBtn = _playButton.GetComponent<Button>();
                if (playBtn != null) playBtn.onClick.AddListener(OnPlayClicked);
            }

            if (_careerButton != null)
            {
                Button careerBtn = _careerButton.GetComponent<Button>();
                if (careerBtn != null) careerBtn.onClick.AddListener(OnCareerClicked);
            }

            if (_settingsButton != null)
            {
                Button settingsBtn = _settingsButton.GetComponent<Button>();
                if (settingsBtn != null) settingsBtn.onClick.AddListener(OnSettingsClicked);
            }

            if (_leaderboardButton != null)
            {
                Button lbBtn = _leaderboardButton.GetComponent<Button>();
                if (lbBtn != null) lbBtn.onClick.AddListener(OnLeaderboardClicked);
            }
        }

        private void SetupVersionText()
        {
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }
        }

        private void PlayEntryAnimation()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            _entrySequence?.Kill();
            _entrySequence = DOTween.Sequence();

            // Fade in menu panel
            if (_menuPanelGroup != null)
            {
                _menuPanelGroup.alpha = 0f;
                _entrySequence.Append(
                    _menuPanelGroup.DOFade(1f, 0.5f).SetEase(Ease.OutCubic)
                );
            }

            // Stagger button animations
            GoldGradientButton[] buttons = { _playButton, _careerButton, _settingsButton, _leaderboardButton };
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                RectTransform btnRect = buttons[i].GetComponent<RectTransform>();
                CanvasGroup btnGroup = buttons[i].GetComponent<CanvasGroup>();

                if (btnRect != null)
                {
                    Vector2 originalPos = btnRect.anchoredPosition;
                    btnRect.anchoredPosition = originalPos + new Vector2(0f, -50f);

                    float delay = 0.3f + (i * _buttonStaggerDelay);
                    _entrySequence.Insert(delay,
                        btnRect.DOAnchorPos(originalPos, _buttonAnimDuration)
                            .SetEase(Ease.OutBack)
                    );

                    if (btnGroup != null)
                    {
                        btnGroup.alpha = 0f;
                        _entrySequence.Insert(delay,
                            btnGroup.DOFade(1f, _buttonAnimDuration * 0.7f)
                        );
                    }
                }
            }

            // Slide in profile card from top-right
            if (_profileCardRect != null && _profileCardGroup != null)
            {
                Vector2 originalProfilePos = _profileCardRect.anchoredPosition;
                _profileCardRect.anchoredPosition = originalProfilePos + new Vector2(_profileSlideDistance, 0f);
                _profileCardGroup.alpha = 0f;

                _entrySequence.Insert(0.5f,
                    _profileCardRect.DOAnchorPos(originalProfilePos, 0.5f)
                        .SetEase(Ease.OutCubic)
                );
                _entrySequence.Insert(0.5f,
                    _profileCardGroup.DOFade(1f, 0.4f)
                );
            }

            _entrySequence.SetUpdate(true).OnComplete(() => _isAnimating = false);
        }

        private void StartBackgroundAnimation()
        {
            if (_movingLightsContainer == null) return;

            _lightsTween?.Kill();
            _lightsTween = _movingLightsContainer
                .DOAnchorPosX(_lightMoveRange, 1f / _lightMoveSpeed * _lightMoveRange)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private void OnPlayClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.MainMenu,
                ToScreen = UIScreen.Calibration,
                Animated = true
            });
        }

        private void OnCareerClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.MainMenu,
                ToScreen = UIScreen.CareerHub,
                Animated = true
            });
        }

        private void OnSettingsClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.MainMenu,
                ToScreen = UIScreen.Settings,
                Animated = true
            });
        }

        private void OnLeaderboardClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.MainMenu,
                ToScreen = UIScreen.ScoreCard,
                Animated = true
            });
        }

        /// <summary>
        /// Refresh player profile card with latest data.
        /// </summary>
        public void RefreshPlayerCard(PlayerProfile profile)
        {
            if (_playerCard != null)
            {
                _playerCard.SetPlayerData(profile.PlayerName, profile.Level,
                    profile.ExperiencePoints, profile.CurrentCareerStage);
            }
        }
    }
}
