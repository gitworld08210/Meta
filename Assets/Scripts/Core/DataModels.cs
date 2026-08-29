using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetaCricket.Core
{
    /// <summary>
    /// Represents the player's profile information.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string PlayerId;
        public string PlayerName;
        public string TeamName;
        public int Level;
        public int ExperiencePoints;
        public int TotalRuns;
        public int MatchesPlayed;
        public int MatchesWon;
        public float BattingAverage;
        public float StrikeRate;
        public int HighScore;
        public int Centuries;
        public int HalfCenturies;
        public CareerStage CurrentCareerStage;
        public DateTime CreatedAt;
        public DateTime LastPlayedAt;
    }

    /// <summary>
    /// Stores the result of a completed match.
    /// </summary>
    [Serializable]
    public class MatchResult
    {
        public string MatchId;
        public MatchType MatchFormat;
        public int PlayerScore;
        public int OpponentScore;
        public int BallsFaced;
        public int Fours;
        public int Sixes;
        public float StrikeRate;
        public bool IsWon;
        public DismissalType DismissalMethod;
        public ShotType BestShot;
        public int ExperienceEarned;
        public DateTime PlayedAt;
        public List<ShotType> ShotsPlayed;
    }

    /// <summary>
    /// Tracks the player's career mode progression.
    /// </summary>
    [Serializable]
    public class CareerProgress
    {
        public CareerStage CurrentStage;
        public int StageProgress;
        public int TotalMatchesInStage;
        public int MatchesWonInStage;
        public int MatchesPlayedInStage;
        public bool StageUnlocked;
        public List<string> UnlockedAchievements;
        public List<string> CompletedChallenges;
        public int CoinsEarned;
        public int TotalCoins;
    }

    /// <summary>
    /// Detailed player statistics across all matches.
    /// </summary>
    [Serializable]
    public class PlayerStats
    {
        public int TotalRuns;
        public int TotalBallsFaced;
        public int TotalFours;
        public int TotalSixes;
        public int TotalMatchesPlayed;
        public int TotalMatchesWon;
        public int HighestScore;
        public float OverallBattingAverage;
        public float OverallStrikeRate;
        public int Centuries;
        public int HalfCenturies;
        public int DucksOut;
        public Dictionary<ShotType, int> ShotCounts;
        public Dictionary<ShotType, float> ShotSuccessRates;
        public Dictionary<CareerStage, int> MatchesPerStage;
    }

    /// <summary>
    /// Configuration for a match setup.
    /// </summary>
    [Serializable]
    public class MatchSettings
    {
        public MatchType MatchFormat;
        public DifficultyLevel Difficulty;
        public int Overs;
        public int TargetScore;
        public string OpponentTeamName;
        public bool IsCareerMatch;
        public CareerStage CareerMatchStage;
        public string VenueName;
        public CommentaryLanguage Commentary;
    }

    /// <summary>
    /// Global game settings and preferences.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public float MasterVolume;
        public float MusicVolume;
        public float SFXVolume;
        public bool HapticsEnabled;
        public bool MotionCalibrationAssist;
        public CommentaryLanguage PreferredLanguage;
        public DifficultyLevel DefaultDifficulty;
        public bool ShowTutorials;
        public bool HighQualityGraphics;
        public int TargetFrameRate;
        public bool ARPlaneVisualization;
        public float CameraSensitivity;

        /// <summary>
        /// Creates default game settings.
        /// </summary>
        public static GameSettings CreateDefault()
        {
            return new GameSettings
            {
                MasterVolume = 1.0f,
                MusicVolume = 0.7f,
                SFXVolume = 1.0f,
                HapticsEnabled = true,
                MotionCalibrationAssist = true,
                PreferredLanguage = CommentaryLanguage.English,
                DefaultDifficulty = DifficultyLevel.Medium,
                ShowTutorials = true,
                HighQualityGraphics = true,
                TargetFrameRate = 60,
                ARPlaneVisualization = false,
                CameraSensitivity = 1.0f
            };
        }
    }

    /// <summary>
    /// Complete save data structure wrapping all persistent data.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public PlayerProfile Profile;
        public CareerProgress Career;
        public PlayerStats Stats;
        public GameSettings Settings;
        public List<MatchResult> MatchHistory;
        public int SaveVersion;
        public DateTime LastSaved;
    }
}
