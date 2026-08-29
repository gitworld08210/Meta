using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;
using MetaCricket.MatchEngine;

namespace MetaCricket.CareerMode
{
    /// <summary>
    /// MonoBehaviour managing career progression: tracks current stage, XP, reputation, unlocks.
    /// Handles stage advancement logic with win conditions per stage.
    /// Persists career data via SaveSystem. Implements all 8 career stages from
    /// GullyCricket to WorldCupFinals.
    /// </summary>
    public class CareerManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RewardSystem _rewardSystem;
        [SerializeField] private AchievementSystem _achievementSystem;

        [Header("Career Stage Data")]
        [SerializeField] private List<CareerStageData> _stageDefinitions;

        [Header("Reputation Settings")]
        [SerializeField] private int _xpPerReputationLevel = 5000;
        [SerializeField] private int _maxReputationLevel = 50;

        // Runtime state
        private CareerProgress _progress;
        private CareerStageData _currentStageData;
        private bool _isInitialized;

        /// <summary>
        /// Current career progress data.
        /// </summary>
        public CareerProgress Progress => _progress;

        /// <summary>
        /// Current career stage.
        /// </summary>
        public CareerStage CurrentStage => _progress != null ? _progress.CurrentStage : CareerStage.GullyCricket;

        /// <summary>
        /// Current stage definition data.
        /// </summary>
        public CareerStageData CurrentStageData => _currentStageData;

        /// <summary>
        /// Whether the career system is initialized and ready.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<CareerManager>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<MatchEndEvent>(OnMatchEnd);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MatchEndEvent>(OnMatchEnd);
        }

        /// <summary>
        /// Initialize the career system. Loads existing progress or creates new.
        /// </summary>
        public void Initialize()
        {
            if (_stageDefinitions == null || _stageDefinitions.Count == 0)
            {
                InitializeDefaultStageDefinitions();
            }

            LoadOrCreateProgress();
            UpdateCurrentStageData();

            if (_achievementSystem != null)
            {
                _achievementSystem.SetCareerProgress(_progress);
            }

            _isInitialized = true;
            Debug.Log($"[CareerManager] Initialized. Current stage: {_progress.CurrentStage}");
        }

        /// <summary>
        /// Create a new career from scratch (new game).
        /// </summary>
        public void StartNewCareer()
        {
            _progress = new CareerProgress();
            UpdateCurrentStageData();

            if (_achievementSystem != null)
            {
                _achievementSystem.SetCareerProgress(_progress);
            }

            SaveProgress();

            Debug.Log("[CareerManager] New career started.");
        }

        /// <summary>
        /// Get the match configuration for the next career match.
        /// </summary>
        public MatchConfig GetNextMatchConfig()
        {
            if (_currentStageData == null) return null;

            MatchConfig config = ScriptableObject.CreateInstance<MatchConfig>();
            config.MatchFormat = GetMatchTypeForStage();
            config.OversLimit = _currentStageData.DefaultOvers;
            config.Difficulty = _currentStageData.GetRandomDifficulty();
            config.OpponentTeamName = _currentStageData.GetRandomOpponent();
            config.VenueName = _currentStageData.GetRandomVenue();
            config.IsCareerMatch = true;
            config.CareerMatchStage = _progress.CurrentStage;
            config.MaxWickets = Constants.GameBalance.MaxWickets;
            config.HasPowerplay = config.MatchFormat != MatchType.Test;

            return config;
        }

        /// <summary>
        /// Process the result of a completed career match.
        /// </summary>
        /// <param name="playerScore">Player's score in the match.</param>
        /// <param name="fours">Fours hit.</param>
        /// <param name="sixes">Sixes hit.</param>
        /// <param name="isWin">Whether the player won.</param>
        /// <param name="matchFormat">Match format played.</param>
        public void ProcessMatchResult(int playerScore, int fours, int sixes, bool isWin, MatchType matchFormat)
        {
            if (_progress == null) return;

            // Update match stats
            _progress.TotalMatchesPlayed++;
            _progress.MatchesPlayedInStage++;
            _progress.TotalRunsScored += playerScore;
            _progress.RunsScoredInStage += playerScore;
            _progress.TotalFours += fours;
            _progress.TotalSixes += sixes;

            if (isWin)
            {
                _progress.TotalMatchesWon++;
                _progress.MatchesWonInStage++;
                _progress.CurrentWinStreak++;
                if (_progress.CurrentWinStreak > _progress.BestWinStreak)
                    _progress.BestWinStreak = _progress.CurrentWinStreak;
            }
            else
            {
                _progress.CurrentWinStreak = 0;
            }

            // Update high scores
            if (playerScore > _progress.HighestScore)
                _progress.HighestScore = playerScore;
            if (playerScore > _progress.HighestScoreInStage)
                _progress.HighestScoreInStage = playerScore;

            // Centuries and half-centuries
            if (playerScore >= 100) _progress.Centuries++;
            else if (playerScore >= 50) _progress.HalfCenturies++;

            // Calculate XP earned
            float xpMultiplier = _currentStageData != null ? _currentStageData.XPMultiplier : 1f;
            int xpEarned = _rewardSystem != null
                ? _rewardSystem.CalculateMatchXP(playerScore, fours, sixes, isWin, xpMultiplier)
                : CalculateDefaultXP(playerScore, fours, sixes, isWin, xpMultiplier);

            _progress.TotalXP += xpEarned;

            // Calculate coins earned
            int coinsPerWin = _currentStageData != null ? _currentStageData.CoinsPerWin : 100;
            int coinsEarned = _rewardSystem != null
                ? _rewardSystem.CalculateMatchCoins(playerScore, isWin, coinsPerWin)
                : (isWin ? coinsPerWin : 0);

            _progress.TotalCoins += coinsEarned;

            // Update reputation
            UpdateReputation();

            // Check for stage advancement
            CheckStageAdvancement();

            // Check for new rewards
            if (_rewardSystem != null)
            {
                _rewardSystem.CheckNewUnlocks(_progress);
            }

            // Check achievements
            if (_achievementSystem != null)
            {
                _achievementSystem.CheckAchievements(_progress);
            }

            // Update last played
            _progress.LastPlayedDate = System.DateTime.Now;

            // Save progress
            SaveProgress();
        }

        /// <summary>
        /// Check if the player meets the conditions to advance to the next stage.
        /// </summary>
        private void CheckStageAdvancement()
        {
            if (_currentStageData == null) return;

            // Check wins requirement
            if (_progress.MatchesWonInStage < _currentStageData.WinsToAdvance)
                return;

            // Check minimum score requirement (if any)
            if (_currentStageData.MinimumScoreRequired > 0 &&
                _progress.HighestScoreInStage < _currentStageData.MinimumScoreRequired)
                return;

            // Player has met all requirements - advance!
            AdvanceToNextStage();
        }

        /// <summary>
        /// Advance the player to the next career stage.
        /// </summary>
        private void AdvanceToNextStage()
        {
            CareerStage previousStage = _progress.CurrentStage;

            // Mark current stage as completed
            if (!_progress.CompletedStages.Contains(previousStage))
            {
                _progress.CompletedStages.Add(previousStage);
            }

            // Determine next stage
            CareerStage nextStage = GetNextStage(previousStage);
            if (nextStage == previousStage)
            {
                // Already at the highest stage
                Debug.Log("[CareerManager] Player is already at the highest career stage!");
                return;
            }

            // Advance
            _progress.CurrentStage = nextStage;

            // Unlock new stage
            if (!_progress.UnlockedStages.Contains(nextStage))
            {
                _progress.UnlockedStages.Add(nextStage);
            }

            // Reset stage-specific counters
            _progress.MatchesPlayedInStage = 0;
            _progress.MatchesWonInStage = 0;
            _progress.RunsScoredInStage = 0;
            _progress.HighestScoreInStage = 0;

            // Update current stage data reference
            UpdateCurrentStageData();

            // Unlock stage rewards (shots, stadiums)
            if (_currentStageData != null)
            {
                if (_currentStageData.UnlockedShots != null)
                {
                    foreach (ShotType shot in _currentStageData.UnlockedShots)
                    {
                        if (!_progress.UnlockedShots.Contains(shot))
                            _progress.UnlockedShots.Add(shot);
                    }
                }

                if (_currentStageData.UnlockedStadiums != null)
                {
                    foreach (string stadium in _currentStageData.UnlockedStadiums)
                    {
                        if (!_progress.UnlockedStadiums.Contains(stadium))
                            _progress.UnlockedStadiums.Add(stadium);
                    }
                }
            }

            // Award stage completion coins
            _progress.TotalCoins += _currentStageData != null ? _currentStageData.CoinsPerWin * 5 : 1000;

            // Publish career progress event
            EventBus.Publish(new CareerProgressEvent
            {
                PreviousStage = previousStage,
                NewStage = nextStage,
                IsPromotion = true,
                ExperienceGained = _progress.TotalXP
            });

            Debug.Log($"[CareerManager] Career advanced: {previousStage} -> {nextStage}");
        }

        /// <summary>
        /// Get the next career stage in the progression.
        /// </summary>
        private CareerStage GetNextStage(CareerStage current)
        {
            switch (current)
            {
                case CareerStage.GullyCricket: return CareerStage.TennisBallTournament;
                case CareerStage.TennisBallTournament: return CareerStage.District;
                case CareerStage.District: return CareerStage.State;
                case CareerStage.State: return CareerStage.RanjiTrophy;
                case CareerStage.RanjiTrophy: return CareerStage.IPL;
                case CareerStage.IPL: return CareerStage.International;
                case CareerStage.International: return CareerStage.WorldCupFinals;
                case CareerStage.WorldCupFinals: return CareerStage.WorldCupFinals; // Max stage
                default: return CareerStage.GullyCricket;
            }
        }

        /// <summary>
        /// Get a random match type appropriate for the current stage.
        /// </summary>
        private MatchType GetMatchTypeForStage()
        {
            if (_currentStageData == null || _currentStageData.AvailableMatchTypes == null
                || _currentStageData.AvailableMatchTypes.Count == 0)
            {
                return MatchType.T20;
            }

            int index = Random.Range(0, _currentStageData.AvailableMatchTypes.Count);
            return _currentStageData.AvailableMatchTypes[index];
        }

        /// <summary>
        /// Update the current stage data reference.
        /// </summary>
        private void UpdateCurrentStageData()
        {
            _currentStageData = GetStageData(_progress.CurrentStage);
        }

        /// <summary>
        /// Get the stage data for a specific career stage.
        /// </summary>
        public CareerStageData GetStageData(CareerStage stage)
        {
            if (_stageDefinitions == null) return null;

            foreach (CareerStageData data in _stageDefinitions)
            {
                if (data.Stage == stage) return data;
            }

            return null;
        }

        /// <summary>
        /// Update the player's reputation level based on XP.
        /// </summary>
        private void UpdateReputation()
        {
            int newLevel = Mathf.Min(_progress.TotalXP / _xpPerReputationLevel + 1, _maxReputationLevel);
            _progress.ReputationLevel = newLevel;
        }

        /// <summary>
        /// Calculate XP without the reward system (fallback).
        /// </summary>
        private int CalculateDefaultXP(int runs, int fours, int sixes, bool isWin, float multiplier)
        {
            int xp = runs * 10 + fours * 50 + sixes * 100;
            if (isWin) xp += 500;
            if (runs >= 100) xp += 1000;
            else if (runs >= 50) xp += 500;
            return Mathf.RoundToInt(xp * multiplier);
        }

        /// <summary>
        /// Handle match end event to process results.
        /// </summary>
        private void OnMatchEnd(MatchEndEvent evt)
        {
            // Only process career matches
            // The actual processing is done via ProcessMatchResult called by the match flow
        }

        /// <summary>
        /// Get a summary of the current career stage progress.
        /// </summary>
        public string GetStageProgressSummary()
        {
            if (_progress == null || _currentStageData == null)
                return "Career not initialized";

            return $"Stage: {_currentStageData.StageName}\n" +
                   $"Matches: {_progress.MatchesPlayedInStage}/{_currentStageData.MatchesToComplete}\n" +
                   $"Wins: {_progress.MatchesWonInStage}/{_currentStageData.WinsToAdvance}\n" +
                   $"Runs: {_progress.RunsScoredInStage}\n" +
                   $"XP: {_progress.TotalXP}";
        }

        /// <summary>
        /// Get the percentage completion of the current stage (0-1).
        /// </summary>
        public float GetStageCompletionPercent()
        {
            if (_currentStageData == null || _currentStageData.WinsToAdvance == 0)
                return 0f;

            return Mathf.Clamp01((float)_progress.MatchesWonInStage / _currentStageData.WinsToAdvance);
        }

        /// <summary>
        /// Initialize the default stage definitions if none are set.
        /// </summary>
        private void InitializeDefaultStageDefinitions()
        {
            _stageDefinitions = new List<CareerStageData>();
            CareerStageData[] defaults = CareerStageData.CreateAllStageDefaults();
            foreach (CareerStageData stage in defaults)
            {
                _stageDefinitions.Add(stage);
            }
        }

        /// <summary>
        /// Load existing career progress or create a new one.
        /// </summary>
        private void LoadOrCreateProgress()
        {
            // Attempt to load from SaveSystem
            SaveSystem saveSystem = ServiceLocator.Get<SaveSystem>();
            if (saveSystem != null && saveSystem.CurrentSaveData != null
                && saveSystem.CurrentSaveData.Career != null)
            {
                // Map from Core.CareerProgress to local data
                Core.CareerProgress coreSave = saveSystem.CurrentSaveData.Career;
                _progress = new CareerProgress
                {
                    CurrentStage = coreSave.CurrentStage,
                    MatchesPlayedInStage = coreSave.MatchesPlayedInStage,
                    MatchesWonInStage = coreSave.MatchesWonInStage,
                    TotalCoins = coreSave.TotalCoins
                };

                if (coreSave.UnlockedAchievements != null)
                    _progress.UnlockedAchievements = new List<string>(coreSave.UnlockedAchievements);
            }
            else
            {
                _progress = new CareerProgress();
            }
        }

        /// <summary>
        /// Save the current career progress.
        /// </summary>
        private async void SaveProgress()
        {
            SaveSystem saveSystem = ServiceLocator.Get<SaveSystem>();
            if (saveSystem == null) return;

            SaveData saveData = saveSystem.CurrentSaveData ?? new SaveData();
            saveData.Career = new Core.CareerProgress
            {
                CurrentStage = _progress.CurrentStage,
                MatchesPlayedInStage = _progress.MatchesPlayedInStage,
                MatchesWonInStage = _progress.MatchesWonInStage,
                TotalCoins = _progress.TotalCoins,
                UnlockedAchievements = _progress.UnlockedAchievements,
                StageUnlocked = true
            };

            await saveSystem.SaveGameData(saveData);
        }
    }
}
