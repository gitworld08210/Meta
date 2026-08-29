using System;
using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.MatchEngine
{
    /// <summary>
    /// Tracks complete match statistics: runs, balls faced, fours, sixes, strike rate,
    /// wagon wheel data, shot distribution, and partnership details.
    /// </summary>
    [Serializable]
    public class Scorecard
    {
        [Header("Match Info")]
        public string MatchId;
        public MatchType MatchFormat;
        public string PlayerTeam;
        public string OpponentTeam;
        public string Venue;
        public DateTime MatchDate;

        [Header("Player Batting Stats")]
        public int PlayerRuns;
        public int PlayerBallsFaced;
        public int PlayerFours;
        public int PlayerSixes;
        public float PlayerStrikeRate;
        public DismissalType PlayerDismissal;
        public bool PlayerNotOut;

        [Header("Match Totals")]
        public int PlayerTeamTotal;
        public int PlayerTeamWickets;
        public float PlayerTeamOvers;
        public int OpponentTeamTotal;
        public int OpponentTeamWickets;
        public float OpponentTeamOvers;

        [Header("Wagon Wheel")]
        public List<WagonWheelEntry> WagonWheelData;

        [Header("Shot Distribution")]
        public Dictionary<ShotType, ShotStats> ShotDistribution;

        [Header("Over-by-Over")]
        public List<OverSummary> OverByOver;

        [Header("Milestones")]
        public List<int> MilestonesReached;
        public int HighestPartnership;

        /// <summary>
        /// Initialize a new scorecard for a match.
        /// </summary>
        public Scorecard()
        {
            MatchId = Guid.NewGuid().ToString();
            MatchDate = DateTime.Now;
            WagonWheelData = new List<WagonWheelEntry>();
            ShotDistribution = new Dictionary<ShotType, ShotStats>();
            OverByOver = new List<OverSummary>();
            MilestonesReached = new List<int>();
            PlayerDismissal = DismissalType.NotOut;
            PlayerNotOut = true;

            // Initialize shot distribution for all shot types
            foreach (ShotType shotType in Enum.GetValues(typeof(ShotType)))
            {
                ShotDistribution[shotType] = new ShotStats();
            }
        }

        /// <summary>
        /// Record a shot played by the player.
        /// </summary>
        /// <param name="runs">Runs scored on this shot.</param>
        /// <param name="shotType">Type of shot played.</param>
        /// <param name="direction">Direction the ball traveled.</param>
        /// <param name="distance">Distance the ball traveled.</param>
        public void RecordShot(int runs, ShotType shotType, Vector3 direction, float distance)
        {
            PlayerBallsFaced++;
            PlayerRuns += runs;

            if (runs == 4) PlayerFours++;
            if (runs == 6) PlayerSixes++;

            UpdateStrikeRate();

            // Record wagon wheel entry
            WagonWheelData.Add(new WagonWheelEntry
            {
                Runs = runs,
                Direction = direction,
                Distance = distance,
                ShotType = shotType,
                BallNumber = PlayerBallsFaced
            });

            // Update shot distribution
            if (ShotDistribution.ContainsKey(shotType))
            {
                ShotDistribution[shotType].TimesPlayed++;
                ShotDistribution[shotType].TotalRuns += runs;
                if (runs == 4) ShotDistribution[shotType].Fours++;
                if (runs == 6) ShotDistribution[shotType].Sixes++;
                if (runs == 0) ShotDistribution[shotType].DotBalls++;
            }

            // Check milestones
            CheckMilestones();
        }

        /// <summary>
        /// Record a dot ball (no runs scored).
        /// </summary>
        public void RecordDotBall()
        {
            PlayerBallsFaced++;
            UpdateStrikeRate();
        }

        /// <summary>
        /// Record the player's dismissal.
        /// </summary>
        public void RecordDismissal(DismissalType dismissalType)
        {
            PlayerDismissal = dismissalType;
            PlayerNotOut = false;
        }

        /// <summary>
        /// Record an over summary.
        /// </summary>
        public void RecordOverSummary(int overNumber, int runsInOver, int wicketsInOver, List<int> ballByBall)
        {
            OverByOver.Add(new OverSummary
            {
                OverNumber = overNumber,
                RunsScored = runsInOver,
                WicketsLost = wicketsInOver,
                BallByBall = ballByBall != null ? new List<int>(ballByBall) : new List<int>()
            });
        }

        /// <summary>
        /// Update the player's strike rate.
        /// </summary>
        private void UpdateStrikeRate()
        {
            PlayerStrikeRate = PlayerBallsFaced > 0 ? (PlayerRuns * 100f) / PlayerBallsFaced : 0f;
        }

        /// <summary>
        /// Check if the player has reached a milestone.
        /// </summary>
        private void CheckMilestones()
        {
            int[] milestones = { 50, 100, 150, 200, 250, 300 };
            foreach (int milestone in milestones)
            {
                if (PlayerRuns >= milestone && !MilestonesReached.Contains(milestone))
                {
                    MilestonesReached.Add(milestone);

                    EventBus.Publish(new MilestoneEvent
                    {
                        Milestone = milestone,
                        BallsFaced = PlayerBallsFaced,
                        StrikeRate = PlayerStrikeRate,
                        Fours = PlayerFours,
                        Sixes = PlayerSixes,
                        Description = milestone >= 100 ? "CENTURY!" : "HALF CENTURY!"
                    });
                }
            }
        }

        /// <summary>
        /// Get the match result description.
        /// </summary>
        public string GetResultDescription()
        {
            if (PlayerTeamTotal > OpponentTeamTotal)
            {
                int wicketsRemaining = 10 - PlayerTeamWickets;
                return $"{PlayerTeam} won by {wicketsRemaining} wickets";
            }
            else if (OpponentTeamTotal > PlayerTeamTotal)
            {
                int runDifference = OpponentTeamTotal - PlayerTeamTotal;
                return $"{OpponentTeam} won by {runDifference} runs";
            }
            else
            {
                return "Match tied!";
            }
        }

        /// <summary>
        /// Get the player's best shot type (most runs scored).
        /// </summary>
        public ShotType GetBestShot()
        {
            ShotType bestShot = ShotType.DefensiveBlock;
            int maxRuns = 0;

            foreach (var kvp in ShotDistribution)
            {
                if (kvp.Value.TotalRuns > maxRuns)
                {
                    maxRuns = kvp.Value.TotalRuns;
                    bestShot = kvp.Key;
                }
            }

            return bestShot;
        }
    }

    /// <summary>
    /// Entry in the wagon wheel showing where each shot went.
    /// </summary>
    [Serializable]
    public class WagonWheelEntry
    {
        public int Runs;
        public Vector3 Direction;
        public float Distance;
        public ShotType ShotType;
        public int BallNumber;
    }

    /// <summary>
    /// Statistics for a specific shot type.
    /// </summary>
    [Serializable]
    public class ShotStats
    {
        public int TimesPlayed;
        public int TotalRuns;
        public int Fours;
        public int Sixes;
        public int DotBalls;

        public float AverageRuns => TimesPlayed > 0 ? (float)TotalRuns / TimesPlayed : 0f;
        public float BoundaryPercentage => TimesPlayed > 0 ? (Fours + Sixes) * 100f / TimesPlayed : 0f;
    }

    /// <summary>
    /// Summary of a single over.
    /// </summary>
    [Serializable]
    public class OverSummary
    {
        public int OverNumber;
        public int RunsScored;
        public int WicketsLost;
        public List<int> BallByBall;

        public OverSummary()
        {
            BallByBall = new List<int>();
        }
    }
}
