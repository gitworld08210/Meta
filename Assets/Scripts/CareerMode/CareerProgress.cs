using System;
using System.Collections.Generic;
using MetaCricket.Core;

namespace MetaCricket.CareerMode
{
    /// <summary>
    /// Serializable class tracking the player's career progression:
    /// current stage, matches played, matches won, total runs, highest score,
    /// XP earned, reputation level, unlocked stages, and achievements list.
    /// </summary>
    [Serializable]
    public class CareerProgress
    {
        /// <summary>
        /// The player's current career stage.
        /// </summary>
        public CareerStage CurrentStage;

        /// <summary>
        /// Total experience points earned across career.
        /// </summary>
        public int TotalXP;

        /// <summary>
        /// Reputation level (increases with consistent performance).
        /// </summary>
        public int ReputationLevel;

        /// <summary>
        /// Total matches played across all stages.
        /// </summary>
        public int TotalMatchesPlayed;

        /// <summary>
        /// Total matches won across all stages.
        /// </summary>
        public int TotalMatchesWon;

        /// <summary>
        /// Total runs scored across career.
        /// </summary>
        public int TotalRunsScored;

        /// <summary>
        /// Highest individual score in a single match.
        /// </summary>
        public int HighestScore;

        /// <summary>
        /// Total number of centuries scored.
        /// </summary>
        public int Centuries;

        /// <summary>
        /// Total number of half-centuries scored.
        /// </summary>
        public int HalfCenturies;

        /// <summary>
        /// Total fours hit across career.
        /// </summary>
        public int TotalFours;

        /// <summary>
        /// Total sixes hit across career.
        /// </summary>
        public int TotalSixes;

        /// <summary>
        /// Matches played in the current stage.
        /// </summary>
        public int MatchesPlayedInStage;

        /// <summary>
        /// Matches won in the current stage.
        /// </summary>
        public int MatchesWonInStage;

        /// <summary>
        /// Runs scored in the current stage.
        /// </summary>
        public int RunsScoredInStage;

        /// <summary>
        /// Highest score in the current stage.
        /// </summary>
        public int HighestScoreInStage;

        /// <summary>
        /// Total coins earned.
        /// </summary>
        public int TotalCoins;

        /// <summary>
        /// List of unlocked career stages.
        /// </summary>
        public List<CareerStage> UnlockedStages;

        /// <summary>
        /// List of completed career stages.
        /// </summary>
        public List<CareerStage> CompletedStages;

        /// <summary>
        /// List of unlocked achievement IDs.
        /// </summary>
        public List<string> UnlockedAchievements;

        /// <summary>
        /// List of unlocked shot types.
        /// </summary>
        public List<ShotType> UnlockedShots;

        /// <summary>
        /// List of unlocked stadium names.
        /// </summary>
        public List<string> UnlockedStadiums;

        /// <summary>
        /// Win/Loss streak tracking.
        /// </summary>
        public int CurrentWinStreak;
        public int BestWinStreak;

        /// <summary>
        /// Date career was started.
        /// </summary>
        public DateTime CareerStartDate;

        /// <summary>
        /// Last time the career was played.
        /// </summary>
        public DateTime LastPlayedDate;

        /// <summary>
        /// Create a new career progress with default values.
        /// </summary>
        public CareerProgress()
        {
            CurrentStage = CareerStage.GullyCricket;
            TotalXP = 0;
            ReputationLevel = 1;
            TotalMatchesPlayed = 0;
            TotalMatchesWon = 0;
            TotalRunsScored = 0;
            HighestScore = 0;
            Centuries = 0;
            HalfCenturies = 0;
            TotalFours = 0;
            TotalSixes = 0;
            MatchesPlayedInStage = 0;
            MatchesWonInStage = 0;
            RunsScoredInStage = 0;
            HighestScoreInStage = 0;
            TotalCoins = 0;
            CurrentWinStreak = 0;
            BestWinStreak = 0;
            CareerStartDate = DateTime.Now;
            LastPlayedDate = DateTime.Now;

            UnlockedStages = new List<CareerStage> { CareerStage.GullyCricket };
            CompletedStages = new List<CareerStage>();
            UnlockedAchievements = new List<string>();
            UnlockedShots = new List<ShotType>
            {
                ShotType.StraightDrive,
                ShotType.DefensiveBlock,
                ShotType.Flick
            };
            UnlockedStadiums = new List<string> { "Street Ground", "Local Park" };
        }

        /// <summary>
        /// Get the overall win percentage.
        /// </summary>
        public float WinPercentage => TotalMatchesPlayed > 0
            ? (TotalMatchesWon * 100f) / TotalMatchesPlayed : 0f;

        /// <summary>
        /// Get the batting average.
        /// </summary>
        public float BattingAverage => TotalMatchesPlayed > 0
            ? (float)TotalRunsScored / TotalMatchesPlayed : 0f;

        /// <summary>
        /// Check if a specific shot is unlocked.
        /// </summary>
        public bool IsShotUnlocked(ShotType shot)
        {
            return UnlockedShots != null && UnlockedShots.Contains(shot);
        }

        /// <summary>
        /// Check if a specific stage is unlocked.
        /// </summary>
        public bool IsStageUnlocked(CareerStage stage)
        {
            return UnlockedStages != null && UnlockedStages.Contains(stage);
        }

        /// <summary>
        /// Check if a specific stage is completed.
        /// </summary>
        public bool IsStageCompleted(CareerStage stage)
        {
            return CompletedStages != null && CompletedStages.Contains(stage);
        }

        /// <summary>
        /// Check if a specific achievement is unlocked.
        /// </summary>
        public bool HasAchievement(string achievementId)
        {
            return UnlockedAchievements != null && UnlockedAchievements.Contains(achievementId);
        }
    }
}
