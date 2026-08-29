namespace MetaCricket.Core
{
    /// <summary>
    /// Represents the current state of the game.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Calibrating
    }

    /// <summary>
    /// Types of cricket shots the player can execute.
    /// </summary>
    public enum ShotType
    {
        CoverDrive,
        PullShot,
        StraightDrive,
        HelicopterShot,
        Uppercut,
        SwitchHit,
        Flick,
        DefensiveBlock
    }

    /// <summary>
    /// Match format types.
    /// </summary>
    public enum MatchType
    {
        T20,
        ODI,
        Test
    }

    /// <summary>
    /// Career progression stages from street cricket to international level.
    /// </summary>
    public enum CareerStage
    {
        GullyCricket,
        TennisBallTournament,
        District,
        State,
        RanjiTrophy,
        IPL,
        International,
        WorldCupFinals
    }

    /// <summary>
    /// Types of ball deliveries bowled.
    /// </summary>
    public enum BallType
    {
        Pace,
        Swing,
        Seam,
        OffSpin,
        LegSpin,
        Googly,
        Doosra,
        Yorker,
        Bouncer,
        SlowerBall
    }

    /// <summary>
    /// Game difficulty levels.
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        Legend
    }

    /// <summary>
    /// Supported commentary languages.
    /// </summary>
    public enum CommentaryLanguage
    {
        English,
        Hindi,
        Tamil,
        Telugu,
        Bengali,
        Marathi
    }

    /// <summary>
    /// Fielding positions on the cricket field.
    /// </summary>
    public enum FieldingPosition
    {
        Wicketkeeper,
        FirstSlip,
        SecondSlip,
        ThirdSlip,
        Gully,
        Point,
        CoverPoint,
        Cover,
        MidOff,
        MidOn,
        MidWicket,
        SquareLeg,
        FineLeg,
        ThirdMan,
        LongOff,
        LongOn,
        DeepMidWicket,
        DeepSquareLeg
    }

    /// <summary>
    /// Types of dismissals in cricket.
    /// </summary>
    public enum DismissalType
    {
        Bowled,
        Caught,
        LBW,
        RunOut,
        Stumped,
        HitWicket,
        NotOut
    }

    /// <summary>
    /// Calibration states for AR setup.
    /// </summary>
    public enum CalibrationState
    {
        NotStarted,
        DetectingPlane,
        PlacingPitch,
        ConfirmingPlacement,
        Completed,
        Failed
    }

    /// <summary>
    /// UI screen identifiers.
    /// </summary>
    public enum UIScreen
    {
        Splash,
        MainMenu,
        Settings,
        CareerHub,
        MatchSetup,
        InMatch,
        ScoreCard,
        Pause,
        GameOver,
        Calibration,
        Loading,
        Shop
    }
}
