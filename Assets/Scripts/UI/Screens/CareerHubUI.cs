using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Career mode hub screen with stage progression map,
    /// current stage glow highlight, match selection panel,
    /// player stats card, and XP progress bar with gold fill animation.
    /// </summary>
    public class CareerHubUI : MonoBehaviour
    {
        [Header("Stage Progression Map")]
        [SerializeField] private RectTransform _stageMapContainer;
        [SerializeField] private Image[] _stageIcons;
        [SerializeField] private Image[] _stagePaths;
        [SerializeField] private string[] _stageNames;

        [Header("Current Stage Highlight")]
        [SerializeField] private int _currentStageIndex;
        [SerializeField] private Color _activeStageColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _completedStageColor = new Color(0.298f, 0.686f, 0.314f, 1f);
        [SerializeField] private Color _lockedStageColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private float _glowPulseSpeed = 2f;

        [Header("Match Selection Panel")]
        [SerializeField] private GlassMorphismEffect _matchPanel;
        [SerializeField] private Text _matchTitleText;
        [SerializeField] private Text _matchDescriptionText;
        [SerializeField] private Text _matchRequirementText;
        [SerializeField] private GoldGradientButton _startMatchButton;

        [Header("Player Stats Card")]
        [SerializeField] private PlayerCard _playerStatsCard;
        [SerializeField] private Text _totalRunsText;
        [SerializeField] private Text _matchesWonText;
        [SerializeField] private Text _strikeRateText;

        [Header("XP Progress Bar")]
        [SerializeField] private ProgressBar _xpProgressBar;
        [SerializeField] private Text _xpLabelText;
        [SerializeField] private Text _currentLevelText;
        [SerializeField] private Text _nextLevelText;

        [Header("Animation")]
        [SerializeField] private AnimatedTransition _screenTransition;
        [SerializeField] private float _stageRevealStagger = 0.15f;

        private Tween _currentStageGlowTween;
        private CareerProgress _currentProgress;

        private void Start()
        {
            SetupButtonListeners();
        }

        private void OnEnable()
        {
            PlayEntryAnimation();
        }

        private void OnDisable()
        {
            _currentStageGlowTween?.Kill();
        }

        private void SetupButtonListeners()
        {
            if (_startMatchButton != null)
            {
                Button btn = _startMatchButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OnStartMatchClicked);
            }
        }

        /// <summary>
        /// Initialize the career hub with player's current progress.
        /// </summary>
        public void Initialize(CareerProgress progress, PlayerProfile profile)
        {
            _currentProgress = progress;
            _currentStageIndex = (int)progress.CurrentStage;

            UpdateStageMap();
            UpdateMatchSelection();
            UpdatePlayerStats(profile);
            UpdateXPProgress(profile);
        }

        private void UpdateStageMap()
        {
            if (_stageIcons == null) return;

            for (int i = 0; i < _stageIcons.Length; i++)
            {
                if (_stageIcons[i] == null) continue;

                if (i < _currentStageIndex)
                {
                    // Completed stages
                    _stageIcons[i].color = _completedStageColor;
                }
                else if (i == _currentStageIndex)
                {
                    // Current active stage with glow
                    _stageIcons[i].color = _activeStageColor;
                    AnimateCurrentStageGlow(_stageIcons[i]);
                }
                else
                {
                    // Locked stages
                    _stageIcons[i].color = _lockedStageColor;
                }
            }

            // Color paths between stages
            if (_stagePaths != null)
            {
                for (int i = 0; i < _stagePaths.Length; i++)
                {
                    if (_stagePaths[i] == null) continue;
                    _stagePaths[i].color = i < _currentStageIndex
                        ? _completedStageColor
                        : _lockedStageColor;
                }
            }
        }

        private void AnimateCurrentStageGlow(Image stageIcon)
        {
            _currentStageGlowTween?.Kill();
            _currentStageGlowTween = stageIcon.DOFade(0.5f, 1f / _glowPulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void UpdateMatchSelection()
        {
            if (_currentProgress == null) return;

            CareerStage stage = _currentProgress.CurrentStage;

            if (_matchTitleText != null)
            {
                _matchTitleText.text = GetStageName(stage);
            }

            if (_matchDescriptionText != null)
            {
                _matchDescriptionText.text = GetStageDescription(stage);
            }

            if (_matchRequirementText != null)
            {
                int remaining = _currentProgress.TotalMatchesInStage - _currentProgress.MatchesWonInStage;
                _matchRequirementText.text = $"Win {remaining} more match{(remaining != 1 ? "es" : "")} to advance";
            }
        }

        private void UpdatePlayerStats(PlayerProfile profile)
        {
            if (_totalRunsText != null)
                _totalRunsText.text = profile.TotalRuns.ToString("N0");

            if (_matchesWonText != null)
                _matchesWonText.text = profile.MatchesWon.ToString();

            if (_strikeRateText != null)
                _strikeRateText.text = profile.StrikeRate.ToString("F1");

            if (_playerStatsCard != null)
            {
                _playerStatsCard.SetPlayerData(profile.PlayerName, profile.Level,
                    profile.ExperiencePoints, profile.CurrentCareerStage);
            }
        }

        private void UpdateXPProgress(PlayerProfile profile)
        {
            if (_xpProgressBar == null) return;

            int xpForCurrentLevel = profile.Level * 1000;
            int xpForNextLevel = (profile.Level + 1) * 1000;
            float progress = (float)(profile.ExperiencePoints - xpForCurrentLevel) /
                             (xpForNextLevel - xpForCurrentLevel);

            _xpProgressBar.SetProgress(Mathf.Clamp01(progress), animated: true);

            if (_xpLabelText != null)
                _xpLabelText.text = $"{profile.ExperiencePoints} / {xpForNextLevel} XP";

            if (_currentLevelText != null)
                _currentLevelText.text = $"Lv.{profile.Level}";

            if (_nextLevelText != null)
                _nextLevelText.text = $"Lv.{profile.Level + 1}";
        }

        private void PlayEntryAnimation()
        {
            if (_screenTransition != null)
            {
                _screenTransition.Show();
            }

            // Stagger reveal stage icons
            if (_stageIcons != null)
            {
                for (int i = 0; i < _stageIcons.Length; i++)
                {
                    if (_stageIcons[i] == null) continue;

                    RectTransform iconRect = _stageIcons[i].GetComponent<RectTransform>();
                    if (iconRect == null) continue;

                    iconRect.localScale = Vector3.zero;
                    iconRect.DOScale(1f, 0.3f)
                        .SetDelay(i * _stageRevealStagger)
                        .SetEase(Ease.OutBack);
                }
            }
        }

        private void OnStartMatchClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.CareerHub,
                ToScreen = UIScreen.Calibration,
                Animated = true
            });
        }

        /// <summary>
        /// Navigate back to main menu.
        /// </summary>
        public void OnBackClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.CareerHub,
                ToScreen = UIScreen.MainMenu,
                Animated = true
            });
        }

        private string GetStageName(CareerStage stage)
        {
            switch (stage)
            {
                case CareerStage.GullyCricket: return "Gully Cricket";
                case CareerStage.TennisBallTournament: return "Tennis Ball Tournament";
                case CareerStage.District: return "District Level";
                case CareerStage.State: return "State Level";
                case CareerStage.RanjiTrophy: return "Ranji Trophy";
                case CareerStage.IPL: return "IPL";
                case CareerStage.International: return "International";
                case CareerStage.WorldCupFinals: return "World Cup Finals";
                default: return "Unknown";
            }
        }

        private string GetStageDescription(CareerStage stage)
        {
            switch (stage)
            {
                case CareerStage.GullyCricket: return "Start your journey in the streets";
                case CareerStage.TennisBallTournament: return "Compete in local tournaments";
                case CareerStage.District: return "Represent your district";
                case CareerStage.State: return "Play at the state level";
                case CareerStage.RanjiTrophy: return "India's premier domestic competition";
                case CareerStage.IPL: return "The biggest T20 league in the world";
                case CareerStage.International: return "Represent your country";
                case CareerStage.WorldCupFinals: return "The ultimate stage of cricket";
                default: return "";
            }
        }
    }
}
