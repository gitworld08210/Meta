using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Leaderboard display with tabs (Global, Friends, Career),
    /// scrollable player list with rank/name/score, gold highlight for top 3,
    /// and player's own rank highlighted.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        /// <summary>
        /// Data for a single leaderboard entry.
        /// </summary>
        [System.Serializable]
        public class LeaderboardEntry
        {
            public int Rank;
            public string PlayerId;
            public string PlayerName;
            public int Score;
            public string CareerStage;
            public bool IsCurrentPlayer;
        }

        [Header("Tabs")]
        [SerializeField] private Button _globalTab;
        [SerializeField] private Button _friendsTab;
        [SerializeField] private Button _careerTab;
        [SerializeField] private Image _tabIndicator;
        [SerializeField] private Color _activeTabColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("List")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentContainer;
        [SerializeField] private GameObject _entryPrefab;
        [SerializeField] private int _maxEntries = 100;

        [Header("Top 3 Highlight")]
        [SerializeField] private Color _firstPlaceColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _secondPlaceColor = new Color(0.753f, 0.753f, 0.753f, 1f);
        [SerializeField] private Color _thirdPlaceColor = new Color(0.804f, 0.498f, 0.196f, 1f);

        [Header("Current Player")]
        [SerializeField] private GlassMorphismEffect _currentPlayerBar;
        [SerializeField] private Text _currentPlayerRankText;
        [SerializeField] private Text _currentPlayerNameText;
        [SerializeField] private Text _currentPlayerScoreText;
        [SerializeField] private Color _currentPlayerHighlight = new Color(1f, 0.843f, 0f, 0.2f);

        [Header("Loading")]
        [SerializeField] private CanvasGroup _loadingGroup;
        [SerializeField] private Image _loadingSpinner;
        [SerializeField] private Text _emptyStateText;

        [Header("Animation")]
        [SerializeField] private AnimatedTransition _screenTransition;
        [SerializeField] private float _entryStaggerDelay = 0.05f;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        private enum LeaderboardTab { Global, Friends, Career }
        private LeaderboardTab _currentTab = LeaderboardTab.Global;
        private List<LeaderboardEntry> _currentEntries = new List<LeaderboardEntry>();
        private List<GameObject> _instantiatedEntries = new List<GameObject>();

        private void Start()
        {
            SetupButtons();
            SelectTab(LeaderboardTab.Global);
        }

        private void OnEnable()
        {
            if (_screenTransition != null)
                _screenTransition.Show();

            RefreshCurrentTab();
        }

        private void SetupButtons()
        {
            if (_globalTab != null) _globalTab.onClick.AddListener(() => SelectTab(LeaderboardTab.Global));
            if (_friendsTab != null) _friendsTab.onClick.AddListener(() => SelectTab(LeaderboardTab.Friends));
            if (_careerTab != null) _careerTab.onClick.AddListener(() => SelectTab(LeaderboardTab.Career));
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
        }

        private void SelectTab(LeaderboardTab tab)
        {
            _currentTab = tab;

            // Update tab colors
            UpdateTabVisuals(tab);

            // Refresh data for selected tab
            RefreshCurrentTab();
        }

        private void UpdateTabVisuals(LeaderboardTab activeTab)
        {
            if (_globalTab != null)
            {
                Text globalText = _globalTab.GetComponentInChildren<Text>();
                if (globalText != null)
                    globalText.color = activeTab == LeaderboardTab.Global ? _activeTabColor : _inactiveTabColor;
            }

            if (_friendsTab != null)
            {
                Text friendsText = _friendsTab.GetComponentInChildren<Text>();
                if (friendsText != null)
                    friendsText.color = activeTab == LeaderboardTab.Friends ? _activeTabColor : _inactiveTabColor;
            }

            if (_careerTab != null)
            {
                Text careerText = _careerTab.GetComponentInChildren<Text>();
                if (careerText != null)
                    careerText.color = activeTab == LeaderboardTab.Career ? _activeTabColor : _inactiveTabColor;
            }

            // Animate tab indicator
            if (_tabIndicator != null)
            {
                float targetX = (int)activeTab * 120f;
                _tabIndicator.rectTransform.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.OutCubic);
            }
        }

        private void RefreshCurrentTab()
        {
            ShowLoading(true);

            // Data loading would be triggered via LeaderboardService
            // For now show loading state
            Debug.Log($"[LeaderboardUI] Refreshing {_currentTab} leaderboard");
        }

        /// <summary>
        /// Populate the leaderboard with entries.
        /// </summary>
        public void PopulateLeaderboard(List<LeaderboardEntry> entries)
        {
            ShowLoading(false);
            _currentEntries = entries;

            ClearEntries();

            if (entries == null || entries.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            for (int i = 0; i < entries.Count && i < _maxEntries; i++)
            {
                CreateEntryUI(entries[i], i);
            }
        }

        private void CreateEntryUI(LeaderboardEntry entry, int index)
        {
            if (_entryPrefab == null || _contentContainer == null) return;

            GameObject entryObj = Object.Instantiate(_entryPrefab, _contentContainer);
            _instantiatedEntries.Add(entryObj);

            // Set entry data (assuming prefab has standard layout)
            Text rankText = entryObj.transform.Find("RankText")?.GetComponent<Text>();
            Text nameText = entryObj.transform.Find("NameText")?.GetComponent<Text>();
            Text scoreText = entryObj.transform.Find("ScoreText")?.GetComponent<Text>();
            Image bgImage = entryObj.GetComponent<Image>();

            if (rankText != null) rankText.text = $"#{entry.Rank}";
            if (nameText != null) nameText.text = entry.PlayerName;
            if (scoreText != null) scoreText.text = entry.Score.ToString("N0");

            // Apply colors based on rank
            if (bgImage != null)
            {
                if (entry.Rank == 1) bgImage.color = ThemeColors.WithAlpha(_firstPlaceColor, 0.2f);
                else if (entry.Rank == 2) bgImage.color = ThemeColors.WithAlpha(_secondPlaceColor, 0.15f);
                else if (entry.Rank == 3) bgImage.color = ThemeColors.WithAlpha(_thirdPlaceColor, 0.15f);
                else if (entry.IsCurrentPlayer) bgImage.color = _currentPlayerHighlight;
                else bgImage.color = Color.clear;
            }

            // Rank text color for top 3
            if (rankText != null)
            {
                if (entry.Rank == 1) rankText.color = _firstPlaceColor;
                else if (entry.Rank == 2) rankText.color = _secondPlaceColor;
                else if (entry.Rank == 3) rankText.color = _thirdPlaceColor;
            }

            // Stagger animation
            CanvasGroup entryGroup = entryObj.GetComponent<CanvasGroup>();
            if (entryGroup == null) entryGroup = entryObj.AddComponent<CanvasGroup>();

            entryGroup.alpha = 0f;
            entryGroup.DOFade(1f, 0.2f)
                .SetDelay(index * _entryStaggerDelay)
                .SetEase(Ease.OutQuad);

            // Update current player bar
            if (entry.IsCurrentPlayer)
            {
                UpdateCurrentPlayerBar(entry);
            }
        }

        private void UpdateCurrentPlayerBar(LeaderboardEntry entry)
        {
            if (_currentPlayerRankText != null) _currentPlayerRankText.text = $"#{entry.Rank}";
            if (_currentPlayerNameText != null) _currentPlayerNameText.text = entry.PlayerName;
            if (_currentPlayerScoreText != null) _currentPlayerScoreText.text = entry.Score.ToString("N0");
        }

        private void ClearEntries()
        {
            foreach (var entry in _instantiatedEntries)
            {
                if (entry != null) Destroy(entry);
            }
            _instantiatedEntries.Clear();
        }

        private void ShowLoading(bool show)
        {
            if (_loadingGroup != null)
            {
                _loadingGroup.alpha = show ? 1f : 0f;
                _loadingGroup.gameObject.SetActive(show);
            }

            if (_loadingSpinner != null && show)
            {
                _loadingSpinner.transform.DORotate(new Vector3(0f, 0f, -360f), 1f, RotateMode.LocalAxisAdd)
                    .SetLoops(-1, LoopType.Restart)
                    .SetEase(Ease.Linear);
            }
        }

        private void ShowEmptyState()
        {
            if (_emptyStateText != null)
            {
                _emptyStateText.gameObject.SetActive(true);
                _emptyStateText.text = "No entries yet. Play matches to appear on the leaderboard!";
            }
        }

        private void OnBackClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.ScoreCard,
                ToScreen = UIScreen.MainMenu,
                Animated = true
            });
        }
    }
}
