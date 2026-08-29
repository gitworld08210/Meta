using UnityEngine;

namespace MetaCricket.Core
{
    /// <summary>
    /// Event fired when a shot is played by the player.
    /// </summary>
    public struct ShotPlayedEvent
    {
        public ShotType Type;
        public float Power;
        public Vector3 Direction;
        public float TimingAccuracy;
    }

    /// <summary>
    /// Event fired when the ball crosses the boundary.
    /// </summary>
    public struct BoundaryEvent
    {
        public int Runs;
        public bool IsSix;
        public ShotType ShotUsed;
        public Vector3 LandingPosition;
    }

    /// <summary>
    /// Event fired when a wicket falls.
    /// </summary>
    public struct WicketEvent
    {
        public DismissalType DismissalMethod;
        public int BatsmanScore;
        public int BallsFaced;
        public int WicketsDown;
    }

    /// <summary>
    /// Event fired when a match ends.
    /// </summary>
    public struct MatchEndEvent
    {
        public bool IsPlayerWin;
        public int PlayerScore;
        public int OpponentScore;
        public int ExperienceEarned;
        public MatchType MatchFormat;
    }

    /// <summary>
    /// Event fired when career progress changes.
    /// </summary>
    public struct CareerProgressEvent
    {
        public CareerStage PreviousStage;
        public CareerStage NewStage;
        public bool IsPromotion;
        public int ExperienceGained;
    }

    /// <summary>
    /// Event fired when a UI screen transition occurs.
    /// </summary>
    public struct UITransitionEvent
    {
        public UIScreen FromScreen;
        public UIScreen ToScreen;
        public bool Animated;
    }

    /// <summary>
    /// Event fired when AR calibration is completed.
    /// </summary>
    public struct CalibrationCompleteEvent
    {
        public bool Success;
        public Vector3 PitchPosition;
        public Quaternion PitchRotation;
        public float PitchScale;
    }

    /// <summary>
    /// Event fired when a ball is bowled.
    /// </summary>
    public struct BallBowledEvent
    {
        public BallType DeliveryType;
        public float Speed;
        public Vector3 TargetPosition;
        public float SwingAmount;
        public int BallNumber;
        public int OverNumber;
    }

    /// <summary>
    /// Event fired when the score is updated.
    /// </summary>
    public struct ScoreUpdateEvent
    {
        public int TotalRuns;
        public int Wickets;
        public int Overs;
        public int BallsInOver;
        public int RunsThisBall;
        public float StrikeRate;
    }

    /// <summary>
    /// Event fired when the game state changes.
    /// </summary>
    public struct GameStateChangedEvent
    {
        public GameState PreviousState;
        public GameState NewState;
    }

    /// <summary>
    /// Event fired when an achievement is unlocked.
    /// </summary>
    public struct AchievementUnlockedEvent
    {
        public string AchievementId;
        public string AchievementName;
        public string Description;
    }
}
