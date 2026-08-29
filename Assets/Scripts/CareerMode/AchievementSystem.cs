using System;
using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;
using MetaCricket.MatchEngine;

namespace MetaCricket.CareerMode
{
    /// <summary>
    /// Tracks and awards achievements throughout the career mode.
    /// Achievements include: Century Scorer, Six Machine, Perfect Over,
    /// Stage Complete, and many more. Integrates with EventBus for automatic tracking.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        [Header("Achievement Definitions")]
        [SerializeField] private List<AchievementDefinition> _achievements;

        /// <summary>
        /// Event fired when an achievement is earned.
        /// </summary>
        public event Action<AchievementDefinition> OnAchievementEarned;

        // Runtime tracking
        private CareerProgress _careerProgress;
        private int _sixesInMatch;
        private int _foursInMatch;
        private int _consecutiveBoundaries;
        private int _dotBallsInOver;

        private void Awake()
        {
            if (_achievements == null || _achievements.Count == 0)
            {
                _achievements = CreateDefaultAchievements();
            }

            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<AchievementSystem>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<MatchEndEvent>(OnMatchEnd);
            EventBus.Subscribe<MilestoneEvent>(OnMilestone);
            EventBus.Subscribe<ScoreUpdateEvent>(OnScoreUpdate);
            EventBus.Subscribe<BoundaryEvent>(OnBoundary);
            EventBus.Subscribe<CareerProgressEvent>(OnCareerProgress);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MatchEndEvent>(OnMatchEnd);
            EventBus.Unsubscribe<MilestoneEvent>(OnMilestone);
            EventBus.Unsubscribe<ScoreUpdateEvent>(OnScoreUpdate);
            EventBus.Unsubscribe<BoundaryEvent>(OnBoundary);
            EventBus.Unsubscribe<CareerProgressEvent>(OnCareerProgress);
        }

        /// <summary>
        /// Set the current career progress for achievement tracking.
        /// </summary>
        public void SetCareerProgress(CareerProgress progress)
        {
            _careerProgress = progress;
        }

        /// <summary>
        /// Reset per-match tracking at the start of a new match.
        /// </summary>
        public void ResetMatchTracking()
        {
            _sixesInMatch = 0;
            _foursInMatch = 0;
            _consecutiveBoundaries = 0;
            _dotBallsInOver = 0;
        }

        /// <summary>
        /// Check and award any achievements based on current progress.
        /// </summary>
        /// <param name="progress">Current career progress.</param>
        /// <returns>List of newly awarded achievements.</returns>
        public List<AchievementDefinition> CheckAchievements(CareerProgress progress)
        {
            List<AchievementDefinition> newAchievements = new List<AchievementDefinition>();

            foreach (AchievementDefinition achievement in _achievements)
            {
                if (progress.HasAchievement(achievement.AchievementId))
                    continue;

                if (IsAchievementEarned(achievement, progress))
                {
                    AwardAchievement(achievement, progress);
                    newAchievements.Add(achievement);
                }
            }

            return newAchievements;
        }

        /// <summary>
        /// Check if a specific achievement's conditions are met.
        /// </summary>
        private bool IsAchievementEarned(AchievementDefinition achievement, CareerProgress progress)
        {
            switch (achievement.Type)
            {
                case AchievementType.TotalRuns:
                    return progress.TotalRunsScored >= achievement.RequiredValue;

                case AchievementType.TotalWins:
                    return progress.TotalMatchesWon >= achievement.RequiredValue;

                case AchievementType.HighScore:
                    return progress.HighestScore >= achievement.RequiredValue;

                case AchievementType.Centuries:
                    return progress.Centuries >= achievement.RequiredValue;

                case AchievementType.HalfCenturies:
                    return progress.HalfCenturies >= achievement.RequiredValue;

                case AchievementType.TotalSixes:
                    return progress.TotalSixes >= achievement.RequiredValue;

                case AchievementType.TotalFours:
                    return progress.TotalFours >= achievement.RequiredValue;

                case AchievementType.WinStreak:
                    return progress.BestWinStreak >= achievement.RequiredValue;

                case AchievementType.StageComplete:
                    return progress.IsStageCompleted(achievement.RequiredCareerStage);

                case AchievementType.ReputationLevel:
                    return progress.ReputationLevel >= achievement.RequiredValue;

                case AchievementType.SixesInMatch:
                    return _sixesInMatch >= achievement.RequiredValue;

                case AchievementType.FoursInMatch:
                    return _foursInMatch >= achievement.RequiredValue;

                case AchievementType.ConsecutiveBoundaries:
                    return _consecutiveBoundaries >= achievement.RequiredValue;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Award an achievement to the player.
        /// </summary>
        private void AwardAchievement(AchievementDefinition achievement, CareerProgress progress)
        {
            if (progress.UnlockedAchievements == null)
                progress.UnlockedAchievements = new List<string>();

            progress.UnlockedAchievements.Add(achievement.AchievementId);

            // Award XP and coins
            progress.TotalXP += achievement.XPReward;
            progress.TotalCoins += achievement.CoinReward;

            OnAchievementEarned?.Invoke(achievement);

            // Publish event
            EventBus.Publish(new AchievementUnlockedEvent
            {
                AchievementId = achievement.AchievementId,
                AchievementName = achievement.Name,
                Description = achievement.Description
            });

            Debug.Log($"[AchievementSystem] Achievement unlocked: {achievement.Name}");
        }

        // Event handlers

        private void OnMatchEnd(MatchEndEvent evt)
        {
            if (_careerProgress == null) return;
            CheckAchievements(_careerProgress);
        }

        private void OnMilestone(MilestoneEvent evt)
        {
            if (_careerProgress == null) return;

            if (evt.Milestone >= 100) _careerProgress.Centuries++;
            else if (evt.Milestone >= 50) _careerProgress.HalfCenturies++;

            CheckAchievements(_careerProgress);
        }

        private void OnScoreUpdate(ScoreUpdateEvent evt)
        {
            if (evt.RunsThisBall == 0)
            {
                _dotBallsInOver++;
                _consecutiveBoundaries = 0;
            }
        }

        private void OnBoundary(BoundaryEvent evt)
        {
            if (evt.IsSix)
            {
                _sixesInMatch++;
                if (_careerProgress != null) _careerProgress.TotalSixes++;
            }
            else
            {
                _foursInMatch++;
                if (_careerProgress != null) _careerProgress.TotalFours++;
            }

            _consecutiveBoundaries++;

            if (_careerProgress != null)
            {
                CheckAchievements(_careerProgress);
            }
        }

        private void OnCareerProgress(CareerProgressEvent evt)
        {
            if (_careerProgress == null) return;
            if (evt.IsPromotion)
            {
                CheckAchievements(_careerProgress);
            }
        }

        /// <summary>
        /// Get all achievement definitions.
        /// </summary>
        public List<AchievementDefinition> GetAllAchievements()
        {
            return _achievements;
        }

        /// <summary>
        /// Get all unlocked achievements for display.
        /// </summary>
        public List<AchievementDefinition> GetUnlockedAchievements(CareerProgress progress)
        {
            List<AchievementDefinition> unlocked = new List<AchievementDefinition>();
            foreach (AchievementDefinition achievement in _achievements)
            {
                if (progress.HasAchievement(achievement.AchievementId))
                {
                    unlocked.Add(achievement);
                }
            }
            return unlocked;
        }

        /// <summary>
        /// Get completion percentage for achievements.
        /// </summary>
        public float GetCompletionPercentage(CareerProgress progress)
        {
            if (_achievements.Count == 0) return 0f;
            int unlocked = 0;
            foreach (AchievementDefinition achievement in _achievements)
            {
                if (progress.HasAchievement(achievement.AchievementId))
                    unlocked++;
            }
            return (unlocked * 100f) / _achievements.Count;
        }

        /// <summary>
        /// Create the default set of achievements.
        /// </summary>
        private List<AchievementDefinition> CreateDefaultAchievements()
        {
            return new List<AchievementDefinition>
            {
                // Scoring achievements
                new AchievementDefinition
                {
                    AchievementId = "century_scorer",
                    Name = "Century Scorer",
                    Description = "Score your first century (100 runs in a match)",
                    Type = AchievementType.HighScore,
                    RequiredValue = 100,
                    XPReward = 1000,
                    CoinReward = 500
                },
                new AchievementDefinition
                {
                    AchievementId = "double_century",
                    Name = "Double Centurion",
                    Description = "Score 200 runs in a single match",
                    Type = AchievementType.HighScore,
                    RequiredValue = 200,
                    XPReward = 5000,
                    CoinReward = 2000
                },
                new AchievementDefinition
                {
                    AchievementId = "run_machine_1000",
                    Name = "Run Machine",
                    Description = "Score 1000 total career runs",
                    Type = AchievementType.TotalRuns,
                    RequiredValue = 1000,
                    XPReward = 2000,
                    CoinReward = 1000
                },
                new AchievementDefinition
                {
                    AchievementId = "run_machine_5000",
                    Name = "Run Accumulator",
                    Description = "Score 5000 total career runs",
                    Type = AchievementType.TotalRuns,
                    RequiredValue = 5000,
                    XPReward = 5000,
                    CoinReward = 3000
                },

                // Boundary achievements
                new AchievementDefinition
                {
                    AchievementId = "six_machine",
                    Name = "Six Machine",
                    Description = "Hit 5 sixes in a single match",
                    Type = AchievementType.SixesInMatch,
                    RequiredValue = 5,
                    XPReward = 500,
                    CoinReward = 300
                },
                new AchievementDefinition
                {
                    AchievementId = "boundary_king",
                    Name = "Boundary King",
                    Description = "Hit 100 total career fours",
                    Type = AchievementType.TotalFours,
                    RequiredValue = 100,
                    XPReward = 1500,
                    CoinReward = 750
                },
                new AchievementDefinition
                {
                    AchievementId = "maximum_master",
                    Name = "Maximum Master",
                    Description = "Hit 50 total career sixes",
                    Type = AchievementType.TotalSixes,
                    RequiredValue = 50,
                    XPReward = 2000,
                    CoinReward = 1000
                },
                new AchievementDefinition
                {
                    AchievementId = "consecutive_boundaries",
                    Name = "Boundary Blitz",
                    Description = "Hit 4 boundaries in consecutive balls",
                    Type = AchievementType.ConsecutiveBoundaries,
                    RequiredValue = 4,
                    XPReward = 800,
                    CoinReward = 400
                },

                // Win achievements
                new AchievementDefinition
                {
                    AchievementId = "first_win",
                    Name = "First Victory",
                    Description = "Win your first match",
                    Type = AchievementType.TotalWins,
                    RequiredValue = 1,
                    XPReward = 200,
                    CoinReward = 100
                },
                new AchievementDefinition
                {
                    AchievementId = "ten_wins",
                    Name = "Consistent Winner",
                    Description = "Win 10 matches",
                    Type = AchievementType.TotalWins,
                    RequiredValue = 10,
                    XPReward = 1000,
                    CoinReward = 500
                },
                new AchievementDefinition
                {
                    AchievementId = "fifty_wins",
                    Name = "Champion",
                    Description = "Win 50 matches",
                    Type = AchievementType.TotalWins,
                    RequiredValue = 50,
                    XPReward = 5000,
                    CoinReward = 2500
                },
                new AchievementDefinition
                {
                    AchievementId = "win_streak_5",
                    Name = "On a Roll",
                    Description = "Win 5 matches in a row",
                    Type = AchievementType.WinStreak,
                    RequiredValue = 5,
                    XPReward = 1500,
                    CoinReward = 750
                },

                // Career stage achievements
                new AchievementDefinition
                {
                    AchievementId = "stage_district",
                    Name = "District Player",
                    Description = "Complete the District level",
                    Type = AchievementType.StageComplete,
                    RequiredCareerStage = CareerStage.District,
                    XPReward = 2000,
                    CoinReward = 1000
                },
                new AchievementDefinition
                {
                    AchievementId = "stage_state",
                    Name = "State Cricketer",
                    Description = "Complete the State level",
                    Type = AchievementType.StageComplete,
                    RequiredCareerStage = CareerStage.State,
                    XPReward = 3000,
                    CoinReward = 1500
                },
                new AchievementDefinition
                {
                    AchievementId = "stage_ranji",
                    Name = "Ranji Champion",
                    Description = "Complete the Ranji Trophy",
                    Type = AchievementType.StageComplete,
                    RequiredCareerStage = CareerStage.RanjiTrophy,
                    XPReward = 5000,
                    CoinReward = 2500
                },
                new AchievementDefinition
                {
                    AchievementId = "stage_ipl",
                    Name = "IPL Star",
                    Description = "Complete the IPL stage",
                    Type = AchievementType.StageComplete,
                    RequiredCareerStage = CareerStage.IPL,
                    XPReward = 8000,
                    CoinReward = 4000
                },
                new AchievementDefinition
                {
                    AchievementId = "stage_international",
                    Name = "International Cricketer",
                    Description = "Complete the International stage",
                    Type = AchievementType.StageComplete,
                    RequiredCareerStage = CareerStage.International,
                    XPReward = 10000,
                    CoinReward = 5000
                },
                new AchievementDefinition
                {
                    AchievementId = "stage_worldcup",
                    Name = "World Cup Winner",
                    Description = "Win the World Cup Finals",
                    Type = AchievementType.StageComplete,
                    RequiredCareerStage = CareerStage.WorldCupFinals,
                    XPReward = 20000,
                    CoinReward = 10000
                },

                // Milestone achievements
                new AchievementDefinition
                {
                    AchievementId = "five_centuries",
                    Name = "Century Master",
                    Description = "Score 5 centuries in your career",
                    Type = AchievementType.Centuries,
                    RequiredValue = 5,
                    XPReward = 3000,
                    CoinReward = 1500
                },
                new AchievementDefinition
                {
                    AchievementId = "ten_half_centuries",
                    Name = "Consistent Performer",
                    Description = "Score 10 half-centuries in your career",
                    Type = AchievementType.HalfCenturies,
                    RequiredValue = 10,
                    XPReward = 2000,
                    CoinReward = 1000
                }
            };
        }
    }

    /// <summary>
    /// Defines a single achievement in the game.
    /// </summary>
    [Serializable]
    public class AchievementDefinition
    {
        public string AchievementId;
        public string Name;
        public string Description;
        public AchievementType Type;
        public int RequiredValue;
        public CareerStage RequiredCareerStage;
        public int XPReward;
        public int CoinReward;
    }

    /// <summary>
    /// Types of achievements that can be tracked.
    /// </summary>
    public enum AchievementType
    {
        TotalRuns,
        TotalWins,
        HighScore,
        Centuries,
        HalfCenturies,
        TotalSixes,
        TotalFours,
        WinStreak,
        StageComplete,
        ReputationLevel,
        SixesInMatch,
        FoursInMatch,
        ConsecutiveBoundaries
    }
}
