using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.CareerMode
{
    /// <summary>
    /// ScriptableObject defining parameters for a single career stage.
    /// Contains stage name, description, unlock requirements, available match types,
    /// opponent difficulty range, rewards, venue list, and team roster.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCareerStage", menuName = "MetaCricket/Career Mode/Career Stage Data")]
    public class CareerStageData : ScriptableObject
    {
        [Header("Stage Identity")]
        [Tooltip("The career stage this data represents.")]
        public CareerStage Stage;

        [Tooltip("Display name for this stage.")]
        public string StageName;

        [Tooltip("Description of this stage.")]
        [TextArea(3, 5)]
        public string Description;

        [Header("Unlock Requirements")]
        [Tooltip("Minimum XP required to unlock this stage.")]
        public int RequiredXP;

        [Tooltip("Minimum matches won in previous stage.")]
        public int RequiredWins;

        [Tooltip("Minimum total runs scored in previous stage.")]
        public int RequiredRuns;

        [Tooltip("Previous stage that must be completed.")]
        public CareerStage PrerequisiteStage;

        [Tooltip("Special achievement needed to unlock (empty = none).")]
        public string RequiredAchievement;

        [Header("Match Configuration")]
        [Tooltip("Available match types in this stage.")]
        public List<MatchType> AvailableMatchTypes;

        [Tooltip("Minimum difficulty level for matches.")]
        public DifficultyLevel MinDifficulty;

        [Tooltip("Maximum difficulty level for matches.")]
        public DifficultyLevel MaxDifficulty;

        [Tooltip("Default overs for matches in this stage.")]
        public int DefaultOvers = 20;

        [Header("Opponents")]
        [Tooltip("Teams the player can face in this stage.")]
        public List<string> OpponentTeams;

        [Tooltip("Venues available in this stage.")]
        public List<string> AvailableVenues;

        [Header("Rewards")]
        [Tooltip("XP multiplier for matches in this stage.")]
        public float XPMultiplier = 1.0f;

        [Tooltip("Coins earned per win.")]
        public int CoinsPerWin;

        [Tooltip("Shot types unlocked at this stage.")]
        public List<ShotType> UnlockedShots;

        [Tooltip("Stadiums unlocked at this stage.")]
        public List<string> UnlockedStadiums;

        [Header("Stage Progression")]
        [Tooltip("Total matches required to complete this stage.")]
        public int MatchesToComplete;

        [Tooltip("Wins needed to advance to next stage.")]
        public int WinsToAdvance;

        [Tooltip("Minimum score in any match to advance (0 = no requirement).")]
        public int MinimumScoreRequired;

        /// <summary>
        /// Check if the player meets the requirements to unlock this stage.
        /// </summary>
        public bool MeetsUnlockRequirements(int currentXP, int previousStageWins, int previousStageRuns)
        {
            return currentXP >= RequiredXP
                && previousStageWins >= RequiredWins
                && previousStageRuns >= RequiredRuns;
        }

        /// <summary>
        /// Get a random opponent team for this stage.
        /// </summary>
        public string GetRandomOpponent()
        {
            if (OpponentTeams == null || OpponentTeams.Count == 0)
                return "Unknown XI";
            return OpponentTeams[Random.Range(0, OpponentTeams.Count)];
        }

        /// <summary>
        /// Get a random venue for this stage.
        /// </summary>
        public string GetRandomVenue()
        {
            if (AvailableVenues == null || AvailableVenues.Count == 0)
                return "Local Ground";
            return AvailableVenues[Random.Range(0, AvailableVenues.Count)];
        }

        /// <summary>
        /// Get a random difficulty level within this stage's range.
        /// </summary>
        public DifficultyLevel GetRandomDifficulty()
        {
            int min = (int)MinDifficulty;
            int max = (int)MaxDifficulty;
            return (DifficultyLevel)Random.Range(min, max + 1);
        }

        /// <summary>
        /// Create default stage data for all career stages.
        /// </summary>
        public static CareerStageData[] CreateAllStageDefaults()
        {
            return new CareerStageData[]
            {
                CreateGullyCricket(),
                CreateTennisBallTournament(),
                CreateDistrict(),
                CreateState(),
                CreateRanjiTrophy(),
                CreateIPL(),
                CreateInternational(),
                CreateWorldCupFinals()
            };
        }

        private static CareerStageData CreateGullyCricket()
        {
            CareerStageData data = CreateInstance<CareerStageData>();
            data.Stage = CareerStage.GullyCricket;
            data.StageName = "Gully Cricket";
            data.Description = "Start your journey in the streets and local grounds. Master the basics of batting.";
            data.RequiredXP = 0;
            data.RequiredWins = 0;
            data.RequiredRuns = 0;
            data.MinDifficulty = DifficultyLevel.Easy;
            data.MaxDifficulty = DifficultyLevel.Easy;
            data.DefaultOvers = 5;
            data.MatchesToComplete = 5;
            data.WinsToAdvance = 3;
            data.XPMultiplier = 1.0f;
            data.CoinsPerWin = 50;
            data.AvailableMatchTypes = new List<MatchType> { MatchType.T20 };
            data.OpponentTeams = new List<string> { "Street XI", "Mohalla Warriors", "Gully Kings" };
            data.AvailableVenues = new List<string> { "Street Ground", "Local Park", "Gully Pitch" };
            data.UnlockedShots = new List<ShotType> { ShotType.StraightDrive, ShotType.DefensiveBlock, ShotType.Flick };
            data.UnlockedStadiums = new List<string> { "Street Ground", "Local Park" };
            return data;
        }

        private static CareerStageData CreateTennisBallTournament()
        {
            CareerStageData data = CreateInstance<CareerStageData>();
            data.Stage = CareerStage.TennisBallTournament;
            data.StageName = "Tennis Ball Tournament";
            data.Description = "Compete in local tennis ball tournaments. Develop your attacking shots.";
            data.RequiredXP = 500;
            data.RequiredWins = 3;
            data.RequiredRuns = 100;
            data.PrerequisiteStage = CareerStage.GullyCricket;
            data.MinDifficulty = DifficultyLevel.Easy;
            data.MaxDifficulty = DifficultyLevel.Medium;
            data.DefaultOvers = 10;
            data.MatchesToComplete = 8;
            data.WinsToAdvance = 5;
            data.XPMultiplier = 1.2f;
            data.CoinsPerWin = 100;
            data.AvailableMatchTypes = new List<MatchType> { MatchType.T20 };
            data.OpponentTeams = new List<string> { "Colony Stars", "Park Champions", "Night Riders" };
            data.AvailableVenues = new List<string> { "Community Ground", "Night Ground", "Tennis Ball Arena" };
            data.UnlockedShots = new List<ShotType> { ShotType.CoverDrive, ShotType.PullShot };
            data.UnlockedStadiums = new List<string> { "Community Ground" };
            return data;
        }

        private static CareerStageData CreateDistrict()
        {
            CareerStageData data = CreateInstance<CareerStageData>();
            data.Stage = CareerStage.District;
            data.StageName = "District Level";
            data.Description = "Represent your district in official leather ball cricket. Face real pace and spin.";
            data.RequiredXP = 2000;
            data.RequiredWins = 5;
            data.RequiredRuns = 300;
            data.PrerequisiteStage = CareerStage.TennisBallTournament;
            data.MinDifficulty = DifficultyLevel.Medium;
            data.MaxDifficulty = DifficultyLevel.Medium;
            data.DefaultOvers = 20;
            data.MatchesToComplete = 10;
            data.WinsToAdvance = 6;
            data.XPMultiplier = 1.5f;
            data.CoinsPerWin = 200;
            data.AvailableMatchTypes = new List<MatchType> { MatchType.T20, MatchType.ODI };
            data.OpponentTeams = new List<string> { "District A XI", "District B XI", "City Challengers" };
            data.AvailableVenues = new List<string> { "District Stadium", "Municipal Ground", "College Ground" };
            data.UnlockedShots = new List<ShotType> { ShotType.Uppercut };
            data.UnlockedStadiums = new List<string> { "District Stadium" };
            return data;
        }

        private static CareerStageData CreateState()
        {
            CareerStageData data = CreateInstance<CareerStageData>();
            data.Stage = CareerStage.State;
            data.StageName = "State Level";
            data.Description = "Compete at the state level. Face quality bowlers and prove your worth.";
            data.RequiredXP = 5000;
            data.RequiredWins = 6;
            data.RequiredRuns = 500;
            data.PrerequisiteStage = CareerStage.District;
            data.MinDifficulty = DifficultyLevel.Medium;
            data.MaxDifficulty = DifficultyLevel.Hard;
            data.DefaultOvers = 20;
            data.MatchesToComplete = 12;
            data.WinsToAdvance = 7;
            data.XPMultiplier = 1.8f;
            data.CoinsPerWin = 350;
            data.AvailableMatchTypes = new List<MatchType> { MatchType.T20, MatchType.ODI };
            data.OpponentTeams = new List<string> { "State A", "State B", "State Colts" };
            data.AvailableVenues = new List<string> { "State Stadium", "Cricket Academy Ground" };
            data.UnlockedShots = new List<ShotType> { ShotType.SwitchHit };
            data.UnlockedStadiums = new List<string> { "State Stadium" };
            return data;
        }

        private static CareerStageData CreateRanjiTrophy()
        {
            CareerStageData data = CreateInstance<CareerStageData>();
            data.Stage = CareerStage.RanjiTrophy;
            data.StageName = "Ranji Trophy";
            data.Description = "The pinnacle of domestic cricket. Face the best bowlers in first-class cricket.";
            data.RequiredXP = 10000;
            data.RequiredWins = 7;
            data.RequiredRuns = 800;
            data.PrerequisiteStage = CareerStage.State;
            data.MinDifficulty = DifficultyLevel.Hard;
            data.MaxDifficulty = DifficultyLevel.Hard;
            data.DefaultOvers = 50;
            data.MatchesToComplete = 10;
            data.WinsToAdvance = 6;
            data.MinimumScoreRequired = 50;
            data.XPMultiplier = 2.0f;
            data.CoinsPerWin = 500;
            data.AvailableMatchTypes = new List<MatchType> { MatchType.ODI, MatchType.Test };
            data.OpponentTeams = new List<string> { "Mumbai", "Karnataka", "Tamil Nadu", "Delhi" };
            data.AvailableVenues = new List<string> { "Wankhede", "Chinnaswamy", "Eden Gardens" };
            data.UnlockedShots = new List<ShotType> { ShotType.HelicopterShot };
            data.UnlockedStadiums = new List<string> { "Wankhede", "Chinnaswamy", "Eden Gardens" };
            return data;
        }

        private static CareerStageData CreateIPL()
        {
            CareerStageData data = CreateInstance<CareerStageData>();
            data.Stage = CareerStage.IPL;
            data.StageName = "IPL";
            data.Description = "The world's biggest T20 league. Face international stars and perform under pressure.";
            data.RequiredXP = 20000;
            data.RequiredWins = 6;
            data.RequiredRuns = 500;
            data.PrerequisiteStage = CareerStage.RanjiTrophy;
            data.MinDifficulty = DifficultyLevel.Hard;
            data.MaxDifficulty = DifficultyLevel.Legend;
            data.DefaultOvers = 20;
            data.MatchesToComplete = 14;
            data.WinsToAdvance = 8;
            data.XPMultiplier = 2.5f;
            data.CoinsPerWin = 1000;
            data.AvailableMatchTypes = new List<MatchType> { MatchType.T20 };
            data.OpponentTeams = new List<string> {
                "Mumbai Mavericks", "Chennai Kings", "Delhi Dynamos",
                "Kolkata Knights", "Bangalore Bulls", "Hyderabad Hawks",
                "Punjab Panthers"
            };
            data.AvailableVenues = new List<string> { "Wankhede", "Chinnaswamy", "Eden Gardens", "Mohali" };
            data.UnlockedStadiums = new List<string> { "Mohali" };
            return data;
        }

        private static CareerStageData CreateInternational()
        {
            CareerStageData data = CreateInstance<CareerStageData>();
            data.Stage = CareerStage.International;
            data.StageName = "International";
            data.Description = "Represent your nation on the world stage. Face the best bowlers from across the globe.";
            data.RequiredXP = 40000;
            data.RequiredWins = 8;
            data.RequiredRuns = 1000;
            data.PrerequisiteStage = CareerStage.IPL;
            data.MinDifficulty = DifficultyLevel.Hard;
            data.MaxDifficulty = DifficultyLevel.Legend;
            data.DefaultOvers = 50;
            data.MatchesToComplete = 15;
            data.WinsToAdvance = 9;
            data.MinimumScoreRequired = 30;
            data.XPMultiplier = 3.0f;
            data.CoinsPerWin = 2000;
            data.AvailableMatchTypes = new List<MatchType> { MatchType.T20, MatchType.ODI, MatchType.Test };
            data.OpponentTeams = new List<string> { "Australia", "England", "South Africa", "New Zealand", "West Indies" };
            data.AvailableVenues = new List<string> { "Lords", "MCG", "Eden Gardens", "Mohali" };
            data.UnlockedStadiums = new List<string> { "Lords", "MCG" };
            return data;
        }

        private static CareerStageData CreateWorldCupFinals()
        {
            CareerStageData data = CreateInstance<CareerStageData>();
            data.Stage = CareerStage.WorldCupFinals;
            data.StageName = "World Cup Finals";
            data.Description = "The ultimate challenge. Win the World Cup and become a legend.";
            data.RequiredXP = 75000;
            data.RequiredWins = 9;
            data.RequiredRuns = 1500;
            data.PrerequisiteStage = CareerStage.International;
            data.MinDifficulty = DifficultyLevel.Legend;
            data.MaxDifficulty = DifficultyLevel.Legend;
            data.DefaultOvers = 50;
            data.MatchesToComplete = 7;
            data.WinsToAdvance = 5;
            data.MinimumScoreRequired = 50;
            data.XPMultiplier = 4.0f;
            data.CoinsPerWin = 5000;
            data.AvailableMatchTypes = new List<MatchType> { MatchType.ODI };
            data.OpponentTeams = new List<string> { "Australia", "England", "India", "New Zealand", "Pakistan" };
            data.AvailableVenues = new List<string> { "Lords", "MCG", "Wankhede", "Eden Gardens" };
            data.UnlockedStadiums = new List<string>();
            return data;
        }
    }
}
