using MetaCricket.Core;

namespace MetaCricket.MatchEngine
{
    /// <summary>
    /// Event fired when an over is completed.
    /// </summary>
    public struct OverCompleteEvent
    {
        public int OverNumber;
        public int RunsInOver;
        public int WicketsInOver;
        public int TotalScore;
        public int TotalWickets;
        public float RunRate;
    }

    /// <summary>
    /// Event fired when an innings is completed.
    /// </summary>
    public struct InningsCompleteEvent
    {
        public int InningsNumber;
        public int TotalRuns;
        public int Wickets;
        public float Overs;
        public float RunRate;
        public bool IsAllOut;
        public bool IsOversComplete;
    }

    /// <summary>
    /// Event fired when a batting milestone is reached (50, 100, 150, 200, etc.).
    /// </summary>
    public struct MilestoneEvent
    {
        public int Milestone;
        public int BallsFaced;
        public float StrikeRate;
        public int Fours;
        public int Sixes;
        public string Description;
    }

    /// <summary>
    /// Event fired when a batsman is dismissed for zero.
    /// </summary>
    public struct DuckOutEvent
    {
        public int BallsFaced;
        public DismissalType DismissalMethod;
        public bool IsGoldenDuck; // Out on first ball
    }

    /// <summary>
    /// Event fired when the match state transitions.
    /// </summary>
    public struct MatchStateChangedEvent
    {
        public MatchState PreviousState;
        public MatchState NewState;
    }

    /// <summary>
    /// States of the match flow.
    /// </summary>
    public enum MatchState
    {
        NotStarted,
        TossDecision,
        InningsStart,
        BetweenBalls,
        BallInPlay,
        BetweenOvers,
        DrinkBreak,
        InningsEnd,
        InningsBreak,
        MatchEnd,
        Abandoned
    }
}
