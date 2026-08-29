namespace MetaCricket.Core
{
    /// <summary>
    /// Static class listing all game scenes for reference and build settings.
    /// Scene files should be created in the Unity Editor and added to Build Settings.
    /// </summary>
    public static class SceneList
    {
        /// <summary>
        /// All scene names in the game, in build index order.
        /// </summary>
        public static readonly string[] AllScenes = new string[]
        {
            "SplashScreen",
            "MainMenu",
            "Calibration",
            "Match",
            "CareerHub",
            "Settings",
            "Loading"
        };

        /// <summary>
        /// Index of the SplashScreen scene in build settings.
        /// </summary>
        public const int SplashScreenIndex = 0;

        /// <summary>
        /// Index of the MainMenu scene in build settings.
        /// </summary>
        public const int MainMenuIndex = 1;

        /// <summary>
        /// Index of the Calibration scene in build settings.
        /// </summary>
        public const int CalibrationIndex = 2;

        /// <summary>
        /// Index of the Match scene in build settings.
        /// </summary>
        public const int MatchIndex = 3;

        /// <summary>
        /// Index of the CareerHub scene in build settings.
        /// </summary>
        public const int CareerHubIndex = 4;

        /// <summary>
        /// Index of the Settings scene in build settings.
        /// </summary>
        public const int SettingsIndex = 5;

        /// <summary>
        /// Index of the Loading scene in build settings.
        /// </summary>
        public const int LoadingIndex = 6;
    }
}
