using NUnit.Framework;
using UnityEngine;
using MetaCricket.Core;
using MetaCricket.MatchEngine;

namespace MetaCricket.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the match scoring system (Scorecard).
    /// Tests run counting, strike rate calculation, boundary tracking,
    /// milestone detection, and shot distribution recording.
    /// </summary>
    [TestFixture]
    public class ScorecardTests
    {
        private Scorecard _scorecard;

        [SetUp]
        public void SetUp()
        {
            _scorecard = new Scorecard();
            _scorecard.MatchFormat = MatchType.T20;
            _scorecard.PlayerTeam = "Test XI";
            _scorecard.OpponentTeam = "Opponent XI";
        }

        [Test]
        public void NewScorecard_HasZeroRuns()
        {
            Assert.AreEqual(0, _scorecard.PlayerRuns);
            Assert.AreEqual(0, _scorecard.PlayerBallsFaced);
            Assert.AreEqual(0, _scorecard.PlayerFours);
            Assert.AreEqual(0, _scorecard.PlayerSixes);
        }

        [Test]
        public void RecordShot_SingleRun_IncrementsCorrectly()
        {
            _scorecard.RecordShot(1, ShotType.Flick, Vector3.forward, 10f);

            Assert.AreEqual(1, _scorecard.PlayerRuns);
            Assert.AreEqual(1, _scorecard.PlayerBallsFaced);
            Assert.AreEqual(0, _scorecard.PlayerFours);
            Assert.AreEqual(0, _scorecard.PlayerSixes);
        }

        [Test]
        public void RecordShot_Four_IncrementsFoursCounter()
        {
            _scorecard.RecordShot(4, ShotType.CoverDrive, new Vector3(1f, 0f, 1f), 60f);

            Assert.AreEqual(4, _scorecard.PlayerRuns);
            Assert.AreEqual(1, _scorecard.PlayerFours);
            Assert.AreEqual(0, _scorecard.PlayerSixes);
        }

        [Test]
        public void RecordShot_Six_IncrementsSixesCounter()
        {
            _scorecard.RecordShot(6, ShotType.HelicopterShot, new Vector3(0f, 1f, 1f), 80f);

            Assert.AreEqual(6, _scorecard.PlayerRuns);
            Assert.AreEqual(0, _scorecard.PlayerFours);
            Assert.AreEqual(1, _scorecard.PlayerSixes);
        }

        [Test]
        public void StrikeRate_AfterMultipleShots_CalculatedCorrectly()
        {
            // 10 runs off 5 balls = 200 strike rate
            _scorecard.RecordShot(4, ShotType.CoverDrive, Vector3.forward, 50f);
            _scorecard.RecordShot(2, ShotType.Flick, Vector3.left, 30f);
            _scorecard.RecordShot(0, ShotType.DefensiveBlock, Vector3.forward, 5f);
            _scorecard.RecordShot(1, ShotType.StraightDrive, Vector3.forward, 20f);
            _scorecard.RecordShot(6, ShotType.HelicopterShot, Vector3.up, 85f);

            // Total: 4+2+0+1+6 = 13 runs, 5 balls
            Assert.AreEqual(13, _scorecard.PlayerRuns);
            Assert.AreEqual(5, _scorecard.PlayerBallsFaced);

            float expectedStrikeRate = (13f * 100f) / 5f; // 260
            Assert.AreEqual(expectedStrikeRate, _scorecard.PlayerStrikeRate, 0.01f);
        }

        [Test]
        public void RecordDotBall_IncrementsBallsFacedOnly()
        {
            _scorecard.RecordDotBall();

            Assert.AreEqual(0, _scorecard.PlayerRuns);
            Assert.AreEqual(1, _scorecard.PlayerBallsFaced);
        }

        [Test]
        public void StrikeRate_ZeroBalls_ReturnsZero()
        {
            Assert.AreEqual(0f, _scorecard.PlayerStrikeRate);
        }

        [Test]
        public void RecordDismissal_SetsDismissalType()
        {
            _scorecard.RecordDismissal(DismissalType.Bowled);

            Assert.AreEqual(DismissalType.Bowled, _scorecard.PlayerDismissal);
            Assert.IsFalse(_scorecard.PlayerNotOut);
        }

        [Test]
        public void NewScorecard_PlayerIsNotOut()
        {
            Assert.IsTrue(_scorecard.PlayerNotOut);
            Assert.AreEqual(DismissalType.NotOut, _scorecard.PlayerDismissal);
        }

        [Test]
        public void ShotDistribution_TracksEachShotType()
        {
            _scorecard.RecordShot(4, ShotType.CoverDrive, Vector3.forward, 50f);
            _scorecard.RecordShot(2, ShotType.CoverDrive, Vector3.forward, 30f);
            _scorecard.RecordShot(6, ShotType.PullShot, Vector3.left, 80f);

            Assert.AreEqual(2, _scorecard.ShotDistribution[ShotType.CoverDrive].TimesPlayed);
            Assert.AreEqual(6, _scorecard.ShotDistribution[ShotType.CoverDrive].TotalRuns);
            Assert.AreEqual(1, _scorecard.ShotDistribution[ShotType.CoverDrive].Fours);
            Assert.AreEqual(1, _scorecard.ShotDistribution[ShotType.PullShot].TimesPlayed);
            Assert.AreEqual(1, _scorecard.ShotDistribution[ShotType.PullShot].Sixes);
        }

        [Test]
        public void ShotStats_AverageRuns_CalculatedCorrectly()
        {
            _scorecard.RecordShot(4, ShotType.CoverDrive, Vector3.forward, 50f);
            _scorecard.RecordShot(2, ShotType.CoverDrive, Vector3.forward, 30f);
            _scorecard.RecordShot(0, ShotType.CoverDrive, Vector3.forward, 5f);

            ShotStats coverDriveStats = _scorecard.ShotDistribution[ShotType.CoverDrive];
            Assert.AreEqual(3, coverDriveStats.TimesPlayed);
            Assert.AreEqual(6, coverDriveStats.TotalRuns);
            Assert.AreEqual(2f, coverDriveStats.AverageRuns, 0.01f);
            Assert.AreEqual(1, coverDriveStats.DotBalls);
        }

        [Test]
        public void ShotStats_BoundaryPercentage_CalculatedCorrectly()
        {
            _scorecard.RecordShot(4, ShotType.CoverDrive, Vector3.forward, 50f);
            _scorecard.RecordShot(1, ShotType.CoverDrive, Vector3.forward, 15f);
            _scorecard.RecordShot(6, ShotType.CoverDrive, Vector3.forward, 85f);
            _scorecard.RecordShot(0, ShotType.CoverDrive, Vector3.forward, 5f);

            ShotStats stats = _scorecard.ShotDistribution[ShotType.CoverDrive];
            // 2 boundaries out of 4 = 50%
            float expectedPercentage = (1 + 1) * 100f / 4f; // fours + sixes / total
            Assert.AreEqual(expectedPercentage, stats.BoundaryPercentage, 0.01f);
        }

        [Test]
        public void WagonWheel_RecordsDirectionAndDistance()
        {
            Vector3 direction = new Vector3(0.7f, 0f, 0.7f).normalized;
            _scorecard.RecordShot(4, ShotType.CoverDrive, direction, 55f);

            Assert.AreEqual(1, _scorecard.WagonWheelData.Count);
            Assert.AreEqual(4, _scorecard.WagonWheelData[0].Runs);
            Assert.AreEqual(55f, _scorecard.WagonWheelData[0].Distance, 0.01f);
            Assert.AreEqual(ShotType.CoverDrive, _scorecard.WagonWheelData[0].ShotType);
        }

        [Test]
        public void RecordOverSummary_StoresOverData()
        {
            var ballByBall = new System.Collections.Generic.List<int> { 1, 0, 4, 2, 6, 1 };
            _scorecard.RecordOverSummary(1, 14, 0, ballByBall);

            Assert.AreEqual(1, _scorecard.OverByOver.Count);
            Assert.AreEqual(1, _scorecard.OverByOver[0].OverNumber);
            Assert.AreEqual(14, _scorecard.OverByOver[0].RunsScored);
            Assert.AreEqual(0, _scorecard.OverByOver[0].WicketsLost);
            Assert.AreEqual(6, _scorecard.OverByOver[0].BallByBall.Count);
        }

        [Test]
        public void GetBestShot_ReturnsShotWithMostRuns()
        {
            _scorecard.RecordShot(4, ShotType.CoverDrive, Vector3.forward, 50f);
            _scorecard.RecordShot(6, ShotType.PullShot, Vector3.left, 80f);
            _scorecard.RecordShot(6, ShotType.PullShot, Vector3.left, 80f);
            _scorecard.RecordShot(1, ShotType.Flick, Vector3.forward, 10f);

            Assert.AreEqual(ShotType.PullShot, _scorecard.GetBestShot());
        }

        [Test]
        public void GetResultDescription_PlayerWins_ShowsWicketMargin()
        {
            _scorecard.PlayerTeamTotal = 180;
            _scorecard.PlayerTeamWickets = 4;
            _scorecard.OpponentTeamTotal = 160;

            string result = _scorecard.GetResultDescription();
            Assert.IsTrue(result.Contains("won by 6 wickets"));
        }

        [Test]
        public void GetResultDescription_OpponentWins_ShowsRunMargin()
        {
            _scorecard.PlayerTeamTotal = 140;
            _scorecard.OpponentTeamTotal = 180;

            string result = _scorecard.GetResultDescription();
            Assert.IsTrue(result.Contains("won by 40 runs"));
        }

        [Test]
        public void GetResultDescription_Tie_ShowsTieMessage()
        {
            _scorecard.PlayerTeamTotal = 165;
            _scorecard.OpponentTeamTotal = 165;

            string result = _scorecard.GetResultDescription();
            Assert.AreEqual("Match tied!", result);
        }

        [Test]
        public void MultipleOvers_AccumulatesCorrectly()
        {
            // Simulate 2 overs of batting
            // Over 1: 4, 1, 0, 2, 6, 1 = 14 runs
            _scorecard.RecordShot(4, ShotType.CoverDrive, Vector3.forward, 50f);
            _scorecard.RecordShot(1, ShotType.Flick, Vector3.left, 15f);
            _scorecard.RecordDotBall();
            _scorecard.RecordShot(2, ShotType.StraightDrive, Vector3.forward, 25f);
            _scorecard.RecordShot(6, ShotType.HelicopterShot, Vector3.up, 85f);
            _scorecard.RecordShot(1, ShotType.Flick, Vector3.left, 12f);

            Assert.AreEqual(14, _scorecard.PlayerRuns);
            Assert.AreEqual(6, _scorecard.PlayerBallsFaced);
            Assert.AreEqual(1, _scorecard.PlayerFours);
            Assert.AreEqual(1, _scorecard.PlayerSixes);
        }
    }
}
