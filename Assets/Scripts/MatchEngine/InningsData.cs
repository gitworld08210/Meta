using System;
using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.MatchEngine
{
    /// <summary>
    /// Data class for innings: total runs, wickets, overs bowled, run rate,
    /// individual batsman scores, bowling figures, and extras.
    /// </summary>
    [Serializable]
    public class InningsData
    {
        [Header("Innings Summary")]
        public int TotalRuns;
        public int Wickets;
        public int OversCompleted;
        public int BallsInCurrentOver;
        public float RunRate;

        [Header("Extras")]
        public int Wides;
        public int NoBalls;
        public int Byes;
        public int LegByes;
        public int TotalExtras;

        [Header("Individual Scores")]
        public List<BatsmanInningsData> BatsmanScores;

        [Header("Bowling Figures")]
        public List<BowlerInningsData> BowlingFigures;

        [Header("Partnerships")]
        public List<PartnershipData> Partnerships;

        [Header("Fall of Wickets")]
        public List<FallOfWicketData> FallOfWickets;

        public InningsData()
        {
            TotalRuns = 0;
            Wickets = 0;
            OversCompleted = 0;
            BallsInCurrentOver = 0;
            RunRate = 0f;
            Wides = 0;
            NoBalls = 0;
            Byes = 0;
            LegByes = 0;
            TotalExtras = 0;
            BatsmanScores = new List<BatsmanInningsData>();
            BowlingFigures = new List<BowlerInningsData>();
            Partnerships = new List<PartnershipData>();
            FallOfWickets = new List<FallOfWicketData>();
        }

        /// <summary>
        /// Get total overs as float (e.g., 4.3 = 4 overs and 3 balls).
        /// </summary>
        public float GetOversFloat()
        {
            return OversCompleted + BallsInCurrentOver / 10f;
        }

        /// <summary>
        /// Get overs string (e.g., "4.3" for 4 overs and 3 balls).
        /// </summary>
        public string GetOversString()
        {
            return $"{OversCompleted}.{BallsInCurrentOver}";
        }

        /// <summary>
        /// Get total legal deliveries bowled.
        /// </summary>
        public int GetTotalBalls()
        {
            return OversCompleted * 6 + BallsInCurrentOver;
        }

        /// <summary>
        /// Calculate and update the current run rate.
        /// </summary>
        public void UpdateRunRate()
        {
            float totalOvers = OversCompleted + BallsInCurrentOver / 6f;
            RunRate = totalOvers > 0f ? TotalRuns / totalOvers : 0f;
        }

        /// <summary>
        /// Add runs to the total and update run rate.
        /// </summary>
        public void AddRuns(int runs, bool isExtra = false)
        {
            TotalRuns += runs;
            if (isExtra) TotalExtras += runs;
            UpdateRunRate();
        }

        /// <summary>
        /// Record a ball bowled (advances ball count).
        /// </summary>
        public void RecordBall()
        {
            BallsInCurrentOver++;
            if (BallsInCurrentOver >= 6)
            {
                OversCompleted++;
                BallsInCurrentOver = 0;
            }
            UpdateRunRate();
        }

        /// <summary>
        /// Record a wicket fall.
        /// </summary>
        public void RecordWicket(DismissalType dismissal, int batsmanScore, int ballsFaced)
        {
            Wickets++;
            FallOfWickets.Add(new FallOfWicketData
            {
                WicketNumber = Wickets,
                Score = TotalRuns,
                Overs = GetOversString(),
                BatsmanScore = batsmanScore,
                DismissalMethod = dismissal
            });
        }
    }

    /// <summary>
    /// Individual batsman score data within an innings.
    /// </summary>
    [Serializable]
    public class BatsmanInningsData
    {
        public string BatsmanName;
        public int Runs;
        public int BallsFaced;
        public int Fours;
        public int Sixes;
        public float StrikeRate;
        public DismissalType Dismissal;
        public bool IsNotOut;
        public Dictionary<ShotType, int> ShotDistribution;

        public BatsmanInningsData()
        {
            BatsmanName = "";
            Runs = 0;
            BallsFaced = 0;
            Fours = 0;
            Sixes = 0;
            StrikeRate = 0f;
            Dismissal = DismissalType.NotOut;
            IsNotOut = true;
            ShotDistribution = new Dictionary<ShotType, int>();
        }

        /// <summary>
        /// Update strike rate calculation.
        /// </summary>
        public void UpdateStrikeRate()
        {
            StrikeRate = BallsFaced > 0 ? (Runs * 100f) / BallsFaced : 0f;
        }

        /// <summary>
        /// Add runs scored on this ball.
        /// </summary>
        public void AddRuns(int runs, ShotType shotType)
        {
            Runs += runs;
            BallsFaced++;

            if (runs == 4) Fours++;
            if (runs == 6) Sixes++;

            if (ShotDistribution.ContainsKey(shotType))
                ShotDistribution[shotType]++;
            else
                ShotDistribution[shotType] = 1;

            UpdateStrikeRate();
        }

        /// <summary>
        /// Record a dot ball (ball faced, no runs).
        /// </summary>
        public void AddDotBall()
        {
            BallsFaced++;
            UpdateStrikeRate();
        }
    }

    /// <summary>
    /// Bowling figures for a single bowler within an innings.
    /// </summary>
    [Serializable]
    public class BowlerInningsData
    {
        public string BowlerName;
        public int Overs;
        public int Maidens;
        public int RunsConceded;
        public int Wickets;
        public float EconomyRate;
        public int DotBalls;
        public int Wides;
        public int NoBalls;

        public BowlerInningsData()
        {
            BowlerName = "";
            Overs = 0;
            Maidens = 0;
            RunsConceded = 0;
            Wickets = 0;
            EconomyRate = 0f;
            DotBalls = 0;
            Wides = 0;
            NoBalls = 0;
        }

        /// <summary>
        /// Update economy rate.
        /// </summary>
        public void UpdateEconomyRate()
        {
            EconomyRate = Overs > 0 ? (float)RunsConceded / Overs : 0f;
        }
    }

    /// <summary>
    /// Partnership data between two batsmen.
    /// </summary>
    [Serializable]
    public class PartnershipData
    {
        public int PartnershipNumber;
        public string Batsman1Name;
        public string Batsman2Name;
        public int TotalRuns;
        public int Balls;
        public int Batsman1Contribution;
        public int Batsman2Contribution;

        public PartnershipData()
        {
            PartnershipNumber = 0;
            Batsman1Name = "";
            Batsman2Name = "";
            TotalRuns = 0;
            Balls = 0;
            Batsman1Contribution = 0;
            Batsman2Contribution = 0;
        }
    }

    /// <summary>
    /// Fall of wicket data.
    /// </summary>
    [Serializable]
    public class FallOfWicketData
    {
        public int WicketNumber;
        public int Score;
        public string Overs;
        public int BatsmanScore;
        public DismissalType DismissalMethod;
    }
}
