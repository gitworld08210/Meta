namespace MetaCricket.Core
{
    /// <summary>
    /// Game-wide constants including scene names, layer masks, tags, and other static values.
    /// </summary>
    public static class Constants
    {
        // Scene Names
        public static class Scenes
        {
            public const string SplashScreen = "SplashScreen";
            public const string MainMenu = "MainMenu";
            public const string Calibration = "Calibration";
            public const string Match = "Match";
            public const string CareerHub = "CareerHub";
            public const string Settings = "Settings";
            public const string Loading = "Loading";
        }

        // Layer Masks
        public static class Layers
        {
            public const int Default = 0;
            public const int UI = 5;
            public const int Ball = 6;
            public const int Bat = 7;
            public const int Pitch = 8;
            public const int Fielder = 9;
            public const int Boundary = 10;
            public const int ARPlane = 11;
            public const int Stumps = 12;

            public const string BallLayerName = "Ball";
            public const string BatLayerName = "Bat";
            public const string PitchLayerName = "Pitch";
            public const string FielderLayerName = "Fielder";
            public const string BoundaryLayerName = "Boundary";
            public const string ARPlaneLayerName = "ARPlane";
            public const string StumpsLayerName = "Stumps";
        }

        // Tags
        public static class Tags
        {
            public const string Player = "Player";
            public const string Ball = "Ball";
            public const string Bat = "Bat";
            public const string Pitch = "Pitch";
            public const string Fielder = "Fielder";
            public const string Boundary = "Boundary";
            public const string Stumps = "Stumps";
            public const string ARPlane = "ARPlane";
            public const string UI = "UI";
        }

        // Animation Parameters
        public static class AnimParams
        {
            public const string IsBowling = "IsBowling";
            public const string ShotTrigger = "ShotTrigger";
            public const string ShotType = "ShotType";
            public const string IsRunning = "IsRunning";
            public const string CelebrationTrigger = "CelebrationTrigger";
            public const string DismissalTrigger = "DismissalTrigger";
            public const string BowlSpeed = "BowlSpeed";
        }

        // Career Stage Display Names
        public static class CareerStageNames
        {
            public const string GullyCricket = "Gully Cricket";
            public const string TennisBallTournament = "Tennis Ball Tournament";
            public const string District = "District Level";
            public const string State = "State Level";
            public const string RanjiTrophy = "Ranji Trophy";
            public const string IPL = "IPL";
            public const string International = "International";
            public const string WorldCupFinals = "World Cup Finals";
        }

        // Shot Type Display Names
        public static class ShotTypeNames
        {
            public const string CoverDrive = "Cover Drive";
            public const string PullShot = "Pull Shot";
            public const string StraightDrive = "Straight Drive";
            public const string HelicopterShot = "Helicopter Shot";
            public const string Uppercut = "Uppercut";
            public const string SwitchHit = "Switch Hit";
            public const string Flick = "Flick";
            public const string DefensiveBlock = "Defensive Block";
        }

        // Game Balance Constants
        public static class GameBalance
        {
            public const int MaxOversT20 = 20;
            public const int MaxOversODI = 50;
            public const int MaxWickets = 10;
            public const float BallTravelTime = 0.6f;
            public const float ShotWindowDuration = 0.3f;
            public const float CalibrationTimeout = 30f;
            public const int BaseExperiencePerRun = 10;
            public const int BonusExperiencePerBoundary = 50;
            public const int BonusExperiencePerSix = 100;
            public const int WinBonusExperience = 500;
        }

        // File Paths
        public static class FilePaths
        {
            public const string SaveFileName = "metacricket_save.json";
            public const string SettingsFileName = "metacricket_settings.json";
            public const string ProfileFileName = "metacricket_profile.json";
        }

        // PlayerPrefs Keys
        public static class PrefsKeys
        {
            public const string FirstLaunch = "FirstLaunch";
            public const string LastVersion = "LastVersion";
            public const string CalibrationComplete = "CalibrationComplete";
            public const string TutorialComplete = "TutorialComplete";
            public const string SelectedLanguage = "SelectedLanguage";
        }
    }
}
