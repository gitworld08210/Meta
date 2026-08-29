using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// In-match HUD displaying scoreboard, batsman stats, shot type indicator,
    /// timing feedback popup, boundary celebration overlay, and over summary panel.
    /// </summary>
    public class MatchUI : MonoBehaviour
    {
        [Header("Scoreboard")]
        [SerializeField] private Text _runsText;
        [SerializeField] private Text _wicketsText;
        [SerializeField] private Text _oversText;
        [SerializeField] private Text _strikeRateText;
        [SerializeField] private Text _targetText;
        [SerializeField] private GlassMorphismEffect _scoreboardPanel;

        [Header("Batsman Stats")]
        [SerializeField] private Text _batsmanNameText;
        [SerializeField] private Text _batsmanRunsText;
        [SerializeField] private Text _batsmanBallsText;
        [SerializeField] private Text _batsmanFoursText;
        [SerializeField] private Text _batsmanSixesText;

        [Header("Shot Indicator")]
        [SerializeField] private Text _shotTypeText;
        [SerializeField] private Image _shotTypeIcon;
        [SerializeField] private CanvasGroup _shotIndicatorGroup;
        [SerializeField] private float _shotIndicatorDuration = 2f;

        [Header("Timing Feedback")]
        [SerializeField] private Text _timingFeedbackText;
        [SerializeField] private CanvasGroup _timingFeedbackGroup;
        [SerializeField] private RectTransform _timingFeedbackRect;
        [SerializeField] private Color _perfectColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _goodColor = new Color(0.298f, 0.686f, 0.314f, 1f);
        [SerializeField] private Color _earlyColor = new Color(1f, 0.596f, 0f, 1f);
        [SerializeField] private Color _lateColor = new Color(0.898f, 0.224f, 0.208f, 1f);

        [Header("Boundary Celebration")]
        [SerializeField] private CanvasGroup _celebrationOverlay;
        [SerializeField] private Text _celebrationText;
        [SerializeField] private Image _celebrationFlash;
        [SerializeField] private ParticleSystem _celebrationParticles;
        [SerializeField] private float _celebrationDuration = 2.5f;

        [Header("Over Summary")]
        [SerializeField] private CanvasGroup _overSummaryGroup;
        [SerializeField] private RectTransform _overSummaryPanel;
        [SerializeField] private Text _overRunsText;
        [SerializeField] private Text[] _ballResultTexts;

        [Header("Pause Button")]
        [SerializeField] private Button _pauseButton;

        private Tween _shotIndicatorTween;
        private Tween _timingTween;
        private Sequence _celebrationSequence;
        private Sequence _overSummarySequence;

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            _shotIndicatorTween?.Kill();
            _timingTween?.Kill();
            _celebrationSequence?.Kill();
            _overSummarySequence?.Kill();
        }

        private void InitializeUI()
        {
            if (_shotIndicatorGroup != null) _shotIndicatorGroup.alpha = 0f;
            if (_timingFeedbackGroup != null) _timingFeedbackGroup.alpha = 0f;
            if (_celebrationOverlay != null) _celebrationOverlay.alpha = 0f;
            if (_overSummaryGroup != null) _overSummaryGroup.alpha = 0f;

            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(OnPauseClicked);
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<ScoreUpdateEvent>(OnScoreUpdate);
            EventBus.Subscribe<BoundaryEvent>(OnBoundary);
            EventBus.Subscribe<ShotPlayedEvent>(OnShotPlayed);
            EventBus.Subscribe<WicketEvent>(OnWicket);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<ScoreUpdateEvent>(OnScoreUpdate);
            EventBus.Unsubscribe<BoundaryEvent>(OnBoundary);
            EventBus.Unsubscribe<ShotPlayedEvent>(OnShotPlayed);
            EventBus.Unsubscribe<WicketEvent>(OnWicket);
        }

        private void OnScoreUpdate(ScoreUpdateEvent evt)
        {
            UpdateScoreboard(evt.TotalRuns, evt.Wickets, evt.Overs, evt.BallsInOver, evt.StrikeRate);
        }

        /// <summary>
        /// Update the scoreboard display.
        /// </summary>
        public void UpdateScoreboard(int runs, int wickets, int overs, int balls, float strikeRate)
        {
            if (_runsText != null) _runsText.text = runs.ToString();
            if (_wicketsText != null) _wicketsText.text = wickets.ToString();
            if (_oversText != null) _oversText.text = $"{overs}.{balls}";
            if (_strikeRateText != null) _strikeRateText.text = $"SR: {strikeRate:F1}";

            // Punch animation on score change
            if (_runsText != null)
            {
                _runsText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
            }
        }

        /// <summary>
        /// Set the target score display.
        /// </summary>
        public void SetTarget(int target)
        {
            if (_targetText != null) _targetText.text = $"Target: {target}";
        }

        /// <summary>
        /// Update batsman information display.
        /// </summary>
        public void UpdateBatsmanStats(string name, int runs, int balls, int fours, int sixes)
        {
            if (_batsmanNameText != null) _batsmanNameText.text = name;
            if (_batsmanRunsText != null) _batsmanRunsText.text = runs.ToString();
            if (_batsmanBallsText != null) _batsmanBallsText.text = $"({balls})";
            if (_batsmanFoursText != null) _batsmanFoursText.text = $"4s: {fours}";
            if (_batsmanSixesText != null) _batsmanSixesText.text = $"6s: {sixes}";
        }

        private void OnShotPlayed(ShotPlayedEvent evt)
        {
            ShowShotIndicator(evt.Type);
            ShowTimingFeedback(evt.TimingAccuracy);
        }

        /// <summary>
        /// Display the shot type indicator with animation.
        /// </summary>
        public void ShowShotIndicator(ShotType shotType)
        {
            if (_shotIndicatorGroup == null) return;

            _shotIndicatorTween?.Kill();

            if (_shotTypeText != null) _shotTypeText.text = FormatShotType(shotType);
            _shotIndicatorGroup.alpha = 1f;

            _shotIndicatorTween = _shotIndicatorGroup.DOFade(0f, 0.5f)
                .SetDelay(_shotIndicatorDuration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// Show timing feedback popup (Perfect!/Good/Early/Late).
        /// </summary>
        public void ShowTimingFeedback(float accuracy)
        {
            if (_timingFeedbackGroup == null || _timingFeedbackText == null) return;

            _timingTween?.Kill();

            string feedbackText;
            Color feedbackColor;

            if (accuracy >= 0.9f)
            {
                feedbackText = "PERFECT!";
                feedbackColor = _perfectColor;
            }
            else if (accuracy >= 0.7f)
            {
                feedbackText = "Good";
                feedbackColor = _goodColor;
            }
            else if (accuracy >= 0.5f)
            {
                feedbackText = "Early";
                feedbackColor = _earlyColor;
            }
            else
            {
                feedbackText = "Late";
                feedbackColor = _lateColor;
            }

            _timingFeedbackText.text = feedbackText;
            _timingFeedbackText.color = feedbackColor;
            _timingFeedbackGroup.alpha = 1f;

            if (_timingFeedbackRect != null)
            {
                _timingFeedbackRect.localScale = Vector3.one * 1.5f;
                _timingFeedbackRect.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            }

            _timingTween = _timingFeedbackGroup.DOFade(0f, 0.5f)
                .SetDelay(1.5f)
                .SetEase(Ease.OutQuad);
        }

        private void OnBoundary(BoundaryEvent evt)
        {
            ShowBoundaryCelebration(evt.Runs, evt.IsSix);
        }

        /// <summary>
        /// Show boundary celebration overlay.
        /// </summary>
        public void ShowBoundaryCelebration(int runs, bool isSix)
        {
            if (_celebrationOverlay == null) return;

            _celebrationSequence?.Kill();
            _celebrationSequence = DOTween.Sequence();

            string text = isSix ? "SIX!" : "FOUR!";
            Color flashColor = isSix
                ? new Color(1f, 0.843f, 0f, 0.5f)
                : new Color(0.129f, 0.588f, 0.953f, 0.5f);

            if (_celebrationText != null)
            {
                _celebrationText.text = text;
                _celebrationText.transform.localScale = Vector3.zero;
            }

            // Screen flash
            if (_celebrationFlash != null)
            {
                _celebrationFlash.color = flashColor;
                _celebrationSequence.Append(_celebrationFlash.DOFade(1f, 0.1f));
                _celebrationSequence.Append(_celebrationFlash.DOFade(0f, 0.3f));
            }

            // Show overlay
            _celebrationSequence.Insert(0f, _celebrationOverlay.DOFade(1f, 0.1f));

            // Scale up text
            if (_celebrationText != null)
            {
                _celebrationSequence.Insert(0.1f,
                    _celebrationText.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack)
                );
            }

            // Play particles
            if (_celebrationParticles != null)
            {
                _celebrationSequence.InsertCallback(0.1f, () => _celebrationParticles.Play());
            }

            // Fade out
            _celebrationSequence.Append(
                _celebrationOverlay.DOFade(0f, 0.5f).SetDelay(_celebrationDuration - 1f)
            );

            _celebrationSequence.SetUpdate(true);
        }

        private void OnWicket(WicketEvent evt)
        {
            // Show wicket notification
            ShowTimingFeedback(0f); // reuse timing display for wicket
            if (_timingFeedbackText != null)
            {
                _timingFeedbackText.text = "WICKET!";
                _timingFeedbackText.color = _lateColor;
            }
        }

        /// <summary>
        /// Show end of over summary panel.
        /// </summary>
        public void ShowOverSummary(int overRuns, int[] ballResults)
        {
            if (_overSummaryGroup == null) return;

            _overSummarySequence?.Kill();
            _overSummarySequence = DOTween.Sequence();

            if (_overRunsText != null) _overRunsText.text = $"{overRuns} runs";

            if (_ballResultTexts != null)
            {
                for (int i = 0; i < _ballResultTexts.Length && i < ballResults.Length; i++)
                {
                    if (_ballResultTexts[i] != null)
                    {
                        _ballResultTexts[i].text = ballResults[i].ToString();
                    }
                }
            }

            // Slide in from bottom
            if (_overSummaryPanel != null)
            {
                Vector2 originalPos = _overSummaryPanel.anchoredPosition;
                _overSummaryPanel.anchoredPosition = originalPos + new Vector2(0f, -200f);

                _overSummarySequence.Append(_overSummaryGroup.DOFade(1f, 0.3f));
                _overSummarySequence.Join(
                    _overSummaryPanel.DOAnchorPos(originalPos, 0.4f).SetEase(Ease.OutBack)
                );

                // Auto-hide after 3 seconds
                _overSummarySequence.Append(_overSummaryGroup.DOFade(0f, 0.3f).SetDelay(3f));
            }
        }

        private void OnPauseClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }
        }

        private string FormatShotType(ShotType type)
        {
            switch (type)
            {
                case ShotType.CoverDrive: return "Cover Drive";
                case ShotType.PullShot: return "Pull Shot";
                case ShotType.StraightDrive: return "Straight Drive";
                case ShotType.HelicopterShot: return "Helicopter Shot";
                case ShotType.Uppercut: return "Uppercut";
                case ShotType.SwitchHit: return "Switch Hit";
                case ShotType.Flick: return "Flick";
                case ShotType.DefensiveBlock: return "Defensive Block";
                default: return type.ToString();
            }
        }
    }
}
