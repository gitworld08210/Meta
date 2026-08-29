using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Post-match results screen with animated scorecard reveal,
    /// performance grade (S/A/B/C/D), XP earned with fill animation,
    /// career progress update, share button, and next match button.
    /// </summary>
    public class MatchResultUI : MonoBehaviour
    {
        [Header("Scorecard")]
        [SerializeField] private CanvasGroup _scorecardGroup;
        [SerializeField] private GlassMorphismEffect _scorecardPanel;
        [SerializeField] private Text _playerScoreText;
        [SerializeField] private Text _opponentScoreText;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _ballsFacedText;
        [SerializeField] private Text _foursText;
        [SerializeField] private Text _sixesText;
        [SerializeField] private Text _strikeRateText;

        [Header("Performance Grade")]
        [SerializeField] private Text _gradeText;
        [SerializeField] private Image _gradeBackground;
        [SerializeField] private ParticleSystem _gradeParticles;
        [SerializeField] private Color _gradeS = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _gradeA = new Color(0.298f, 0.686f, 0.314f, 1f);
        [SerializeField] private Color _gradeB = new Color(0.129f, 0.588f, 0.953f, 1f);
        [SerializeField] private Color _gradeC = new Color(1f, 0.596f, 0f, 1f);
        [SerializeField] private Color _gradeD = new Color(0.898f, 0.224f, 0.208f, 1f);

        [Header("XP Progress")]
        [SerializeField] private ProgressBar _xpProgressBar;
        [SerializeField] private Text _xpEarnedText;
        [SerializeField] private Text _currentLevelText;
        [SerializeField] private CanvasGroup _levelUpGroup;

        [Header("Career Progress")]
        [SerializeField] private CanvasGroup _careerProgressGroup;
        [SerializeField] private Text _careerStageText;
        [SerializeField] private ProgressBar _careerProgressBar;
        [SerializeField] private Text _matchesRemainingText;

        [Header("Buttons")]
        [SerializeField] private GoldGradientButton _shareButton;
        [SerializeField] private GoldGradientButton _nextMatchButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Animation")]
        [SerializeField] private float _scorecardRevealDelay = 0.5f;
        [SerializeField] private float _gradeRevealDelay = 1.5f;
        [SerializeField] private float _xpAnimDelay = 2.5f;
        [SerializeField] private float _careerRevealDelay = 3.5f;

        private Sequence _resultSequence;
        private MatchResult _matchResult;

        private void Start()
        {
            SetupButtons();
            InitializeUI();
        }

        private void OnDestroy()
        {
            _resultSequence?.Kill();
        }

        private void InitializeUI()
        {
            if (_scorecardGroup != null) _scorecardGroup.alpha = 0f;
            if (_gradeText != null) _gradeText.transform.localScale = Vector3.zero;
            if (_careerProgressGroup != null) _careerProgressGroup.alpha = 0f;
            if (_levelUpGroup != null) _levelUpGroup.alpha = 0f;
        }

        private void SetupButtons()
        {
            if (_shareButton != null)
            {
                Button btn = _shareButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OnShareClicked);
            }

            if (_nextMatchButton != null)
            {
                Button btn = _nextMatchButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OnNextMatchClicked);
            }

            if (_mainMenuButton != null)
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        /// <summary>
        /// Display match results with animated reveal sequence.
        /// </summary>
        public void ShowResults(MatchResult result, int previousXP, int previousLevel)
        {
            _matchResult = result;
            PlayResultAnimation(result, previousXP, previousLevel);
        }

        private void PlayResultAnimation(MatchResult result, int previousXP, int previousLevel)
        {
            _resultSequence?.Kill();
            _resultSequence = DOTween.Sequence();

            // Phase 1: Scorecard reveal
            _resultSequence.InsertCallback(_scorecardRevealDelay, () =>
            {
                RevealScorecard(result);
            });

            // Phase 2: Performance grade
            _resultSequence.InsertCallback(_gradeRevealDelay, () =>
            {
                RevealGrade(result);
            });

            // Phase 3: XP animation
            _resultSequence.InsertCallback(_xpAnimDelay, () =>
            {
                AnimateXP(result.ExperienceEarned, previousXP, previousLevel);
            });

            // Phase 4: Career progress
            _resultSequence.InsertCallback(_careerRevealDelay, () =>
            {
                RevealCareerProgress();
            });

            _resultSequence.SetUpdate(true);
        }

        private void RevealScorecard(MatchResult result)
        {
            if (_scorecardGroup == null) return;

            _scorecardGroup.DOFade(1f, 0.5f).SetEase(Ease.OutCubic);

            if (_playerScoreText != null)
            {
                _playerScoreText.text = "0";
                int targetScore = result.PlayerScore;
                DOTween.To(
                    () => 0,
                    x => _playerScoreText.text = x.ToString(),
                    targetScore,
                    1f
                ).SetEase(Ease.OutCubic);
            }

            if (_opponentScoreText != null) _opponentScoreText.text = result.OpponentScore.ToString();
            if (_ballsFacedText != null) _ballsFacedText.text = $"Balls: {result.BallsFaced}";
            if (_foursText != null) _foursText.text = $"4s: {result.Fours}";
            if (_sixesText != null) _sixesText.text = $"6s: {result.Sixes}";
            if (_strikeRateText != null) _strikeRateText.text = $"SR: {result.StrikeRate:F1}";

            if (_resultText != null)
            {
                _resultText.text = result.IsWon ? "VICTORY!" : "DEFEAT";
                _resultText.color = result.IsWon
                    ? new Color(1f, 0.843f, 0f, 1f)
                    : new Color(0.898f, 0.224f, 0.208f, 1f);

                _resultText.transform.localScale = Vector3.one * 1.5f;
                _resultText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            }
        }

        private void RevealGrade(MatchResult result)
        {
            string grade = CalculateGrade(result);
            Color gradeColor = GetGradeColor(grade);

            if (_gradeText != null)
            {
                _gradeText.text = grade;
                _gradeText.color = gradeColor;
                _gradeText.transform.localScale = Vector3.zero;
                _gradeText.transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack);
            }

            if (_gradeBackground != null)
            {
                _gradeBackground.color = ThemeColors.WithAlpha(gradeColor, 0.2f);
            }

            // Play particles for S or A grade
            if (_gradeParticles != null && (grade == "S" || grade == "A"))
            {
                _gradeParticles.Play();
            }
        }

        private void AnimateXP(int xpEarned, int previousXP, int previousLevel)
        {
            if (_xpEarnedText != null)
            {
                _xpEarnedText.text = $"+{xpEarned} XP";
                _xpEarnedText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 5, 0.5f);
            }

            if (_xpProgressBar != null)
            {
                int xpForLevel = (previousLevel + 1) * 1000;
                float startProgress = (float)previousXP / xpForLevel;
                float endProgress = (float)(previousXP + xpEarned) / xpForLevel;

                _xpProgressBar.SetProgressImmediate(startProgress);
                _xpProgressBar.SetProgress(Mathf.Min(endProgress, 1f), animated: true);

                // Level up check
                if (endProgress >= 1f && _levelUpGroup != null)
                {
                    _levelUpGroup.DOFade(1f, 0.5f).SetDelay(1f);
                }
            }

            if (_currentLevelText != null)
            {
                _currentLevelText.text = $"Level {previousLevel}";
            }
        }

        private void RevealCareerProgress()
        {
            if (_careerProgressGroup == null) return;

            _careerProgressGroup.DOFade(1f, 0.5f).SetEase(Ease.OutCubic);
        }

        /// <summary>
        /// Set career progress display.
        /// </summary>
        public void SetCareerProgress(CareerStage stage, int matchesWon, int matchesRequired)
        {
            if (_careerStageText != null)
                _careerStageText.text = stage.ToString();

            if (_careerProgressBar != null)
            {
                float progress = matchesRequired > 0 ? (float)matchesWon / matchesRequired : 0f;
                _careerProgressBar.SetProgress(progress, animated: true);
            }

            if (_matchesRemainingText != null)
            {
                int remaining = matchesRequired - matchesWon;
                _matchesRemainingText.text = $"{remaining} match{(remaining != 1 ? "es" : "")} to next stage";
            }
        }

        private string CalculateGrade(MatchResult result)
        {
            float score = result.StrikeRate * 0.4f + result.PlayerScore * 0.3f;
            if (result.IsWon) score += 30f;
            score += result.Sixes * 5f + result.Fours * 3f;

            if (score >= 150f) return "S";
            if (score >= 100f) return "A";
            if (score >= 70f) return "B";
            if (score >= 40f) return "C";
            return "D";
        }

        private Color GetGradeColor(string grade)
        {
            switch (grade)
            {
                case "S": return _gradeS;
                case "A": return _gradeA;
                case "B": return _gradeB;
                case "C": return _gradeC;
                case "D": return _gradeD;
                default: return Color.white;
            }
        }

        private void OnShareClicked()
        {
            if (_matchResult == null) return;

            string shareText = $"I scored {_matchResult.PlayerScore} runs in Meta Cricket! " +
                               $"Strike Rate: {_matchResult.StrikeRate:F1}. Can you beat me?";

#if UNITY_ANDROID
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            {
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), shareText);
                intent.Call<AndroidJavaObject>("setType", "text/plain");

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share Score");
                    activity.Call("startActivity", chooser);
                }
            }
#endif
        }

        private void OnNextMatchClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.GameOver,
                ToScreen = UIScreen.Calibration,
                Animated = true
            });
        }

        private void OnMainMenuClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.GameOver,
                ToScreen = UIScreen.MainMenu,
                Animated = true
            });
        }
    }
}
