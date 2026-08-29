using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Player profile card component displaying avatar frame with gold border,
    /// player name, level, XP bar, and current career stage badge.
    /// </summary>
    public class PlayerCard : MonoBehaviour
    {
        [Header("Avatar")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Image _avatarFrame;
        [SerializeField] private Color _frameColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private float _framePulseSpeed = 2f;

        [Header("Player Info")]
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _titleText;

        [Header("XP Bar")]
        [SerializeField] private ProgressBar _xpProgressBar;
        [SerializeField] private Text _xpText;

        [Header("Career Stage Badge")]
        [SerializeField] private Image _stageBadgeImage;
        [SerializeField] private Text _stageBadgeText;
        [SerializeField] private CanvasGroup _badgeGroup;

        [Header("Card Background")]
        [SerializeField] private GlassMorphismEffect _cardGlass;
        [SerializeField] private Image _cardBackground;

        [Header("Animation")]
        [SerializeField] private bool _animateOnEnable = true;
        [SerializeField] private float _revealDuration = 0.4f;

        private Tween _framePulseTween;
        private string _currentPlayerName;
        private int _currentLevel;

        private void OnEnable()
        {
            if (_animateOnEnable)
            {
                PlayRevealAnimation();
            }
            StartFramePulse();
        }

        private void OnDisable()
        {
            _framePulseTween?.Kill();
        }

        /// <summary>
        /// Set player data to display on the card.
        /// </summary>
        /// <param name="playerName">Player display name.</param>
        /// <param name="level">Current player level.</param>
        /// <param name="xp">Current experience points.</param>
        /// <param name="careerStage">Current career stage.</param>
        public void SetPlayerData(string playerName, int level, int xp, CareerStage careerStage)
        {
            _currentPlayerName = playerName;
            _currentLevel = level;

            if (_playerNameText != null)
                _playerNameText.text = playerName;

            if (_levelText != null)
                _levelText.text = $"Lv.{level}";

            if (_titleText != null)
                _titleText.text = GetStageTitle(careerStage);

            // Update XP progress
            if (_xpProgressBar != null)
            {
                int xpForNextLevel = (level + 1) * 1000;
                int xpForCurrentLevel = level * 1000;
                float progress = (float)(xp - xpForCurrentLevel) / (xpForNextLevel - xpForCurrentLevel);
                _xpProgressBar.SetProgress(Mathf.Clamp01(progress), animated: true);
            }

            if (_xpText != null)
            {
                int xpForNextLevel = (level + 1) * 1000;
                _xpText.text = $"{xp}/{xpForNextLevel}";
            }

            // Update career badge
            UpdateCareerBadge(careerStage);
        }

        /// <summary>
        /// Set avatar image sprite.
        /// </summary>
        public void SetAvatar(Sprite avatarSprite)
        {
            if (_avatarImage != null && avatarSprite != null)
            {
                _avatarImage.sprite = avatarSprite;
            }
        }

        private void UpdateCareerBadge(CareerStage stage)
        {
            if (_stageBadgeText != null)
            {
                _stageBadgeText.text = GetStageBadgeText(stage);
            }

            if (_stageBadgeImage != null)
            {
                _stageBadgeImage.color = GetStageBadgeColor(stage);
            }
        }

        private void StartFramePulse()
        {
            if (_avatarFrame == null) return;

            _framePulseTween?.Kill();
            _avatarFrame.color = _frameColor;
            _framePulseTween = _avatarFrame.DOFade(0.6f, 1f / _framePulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void PlayRevealAnimation()
        {
            RectTransform rect = GetComponent<RectTransform>();
            CanvasGroup group = GetComponent<CanvasGroup>();

            if (rect != null)
            {
                rect.localScale = Vector3.one * 0.8f;
                rect.DOScale(1f, _revealDuration).SetEase(Ease.OutBack);
            }

            if (group != null)
            {
                group.alpha = 0f;
                group.DOFade(1f, _revealDuration * 0.7f);
            }
        }

        private string GetStageTitle(CareerStage stage)
        {
            switch (stage)
            {
                case CareerStage.GullyCricket: return "Street Batsman";
                case CareerStage.TennisBallTournament: return "Local Champion";
                case CareerStage.District: return "District Star";
                case CareerStage.State: return "State Player";
                case CareerStage.RanjiTrophy: return "Ranji Trophy Pro";
                case CareerStage.IPL: return "IPL Star";
                case CareerStage.International: return "International Player";
                case CareerStage.WorldCupFinals: return "World Champion";
                default: return "Rookie";
            }
        }

        private string GetStageBadgeText(CareerStage stage)
        {
            switch (stage)
            {
                case CareerStage.GullyCricket: return "GULLY";
                case CareerStage.TennisBallTournament: return "LOCAL";
                case CareerStage.District: return "DISTRICT";
                case CareerStage.State: return "STATE";
                case CareerStage.RanjiTrophy: return "RANJI";
                case CareerStage.IPL: return "IPL";
                case CareerStage.International: return "INTL";
                case CareerStage.WorldCupFinals: return "WC";
                default: return "";
            }
        }

        private Color GetStageBadgeColor(CareerStage stage)
        {
            switch (stage)
            {
                case CareerStage.GullyCricket: return new Color(0.6f, 0.6f, 0.6f, 1f);
                case CareerStage.TennisBallTournament: return new Color(0.4f, 0.8f, 0.4f, 1f);
                case CareerStage.District: return new Color(0.129f, 0.588f, 0.953f, 1f);
                case CareerStage.State: return new Color(0.611f, 0.153f, 0.69f, 1f);
                case CareerStage.RanjiTrophy: return new Color(1f, 0.596f, 0f, 1f);
                case CareerStage.IPL: return new Color(1f, 0.843f, 0f, 1f);
                case CareerStage.International: return new Color(0.298f, 0.686f, 0.314f, 1f);
                case CareerStage.WorldCupFinals: return new Color(1f, 0.843f, 0f, 1f);
                default: return Color.gray;
            }
        }
    }
}
