using System;
using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.CareerMode
{
    /// <summary>
    /// Manages unlockable rewards: new shots, stadiums, cosmetics, and team upgrades.
    /// Uses XP-based progression with stage milestones for gating content.
    /// </summary>
    public class RewardSystem : MonoBehaviour
    {
        [Header("XP Rewards")]
        [SerializeField] private int _xpPerRun = 10;
        [SerializeField] private int _xpPerFour = 50;
        [SerializeField] private int _xpPerSix = 100;
        [SerializeField] private int _xpPerWin = 500;
        [SerializeField] private int _xpPerHalfCentury = 500;
        [SerializeField] private int _xpPerCentury = 1000;

        [Header("Coin Rewards")]
        [SerializeField] private int _coinsPerWin = 100;
        [SerializeField] private int _coinsPerHalfCentury = 200;
        [SerializeField] private int _coinsPerCentury = 500;
        [SerializeField] private int _coinsPerStageComplete = 1000;

        [Header("Unlockable Definitions")]
        [SerializeField] private List<UnlockableReward> _allRewards;

        /// <summary>
        /// Event fired when a reward is unlocked.
        /// </summary>
        public event Action<UnlockableReward> OnRewardUnlocked;

        private void Awake()
        {
            if (_allRewards == null)
            {
                _allRewards = CreateDefaultRewards();
            }

            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<RewardSystem>();
        }

        /// <summary>
        /// Calculate XP earned from a match result.
        /// </summary>
        /// <param name="runs">Runs scored.</param>
        /// <param name="fours">Fours hit.</param>
        /// <param name="sixes">Sixes hit.</param>
        /// <param name="isWin">Whether the match was won.</param>
        /// <param name="xpMultiplier">Stage-based XP multiplier.</param>
        /// <returns>Total XP earned.</returns>
        public int CalculateMatchXP(int runs, int fours, int sixes, bool isWin, float xpMultiplier = 1.0f)
        {
            int xp = 0;

            xp += runs * _xpPerRun;
            xp += fours * _xpPerFour;
            xp += sixes * _xpPerSix;

            if (isWin) xp += _xpPerWin;
            if (runs >= 100) xp += _xpPerCentury;
            else if (runs >= 50) xp += _xpPerHalfCentury;

            return Mathf.RoundToInt(xp * xpMultiplier);
        }

        /// <summary>
        /// Calculate coins earned from a match result.
        /// </summary>
        public int CalculateMatchCoins(int runs, bool isWin, int stageCoinsPerWin)
        {
            int coins = 0;

            if (isWin) coins += stageCoinsPerWin;
            if (runs >= 100) coins += _coinsPerCentury;
            else if (runs >= 50) coins += _coinsPerHalfCentury;

            return coins;
        }

        /// <summary>
        /// Check and process any new rewards the player has unlocked.
        /// </summary>
        /// <param name="progress">Current career progress.</param>
        /// <returns>List of newly unlocked rewards.</returns>
        public List<UnlockableReward> CheckNewUnlocks(CareerProgress progress)
        {
            List<UnlockableReward> newUnlocks = new List<UnlockableReward>();

            foreach (UnlockableReward reward in _allRewards)
            {
                if (reward.IsUnlocked) continue;

                if (MeetsUnlockRequirements(reward, progress))
                {
                    reward.IsUnlocked = true;
                    reward.UnlockDate = DateTime.Now;
                    newUnlocks.Add(reward);
                    OnRewardUnlocked?.Invoke(reward);

                    // Apply the reward
                    ApplyReward(reward, progress);
                }
            }

            return newUnlocks;
        }

        /// <summary>
        /// Check if the player meets the requirements for a specific reward.
        /// </summary>
        private bool MeetsUnlockRequirements(UnlockableReward reward, CareerProgress progress)
        {
            // Check XP requirement
            if (progress.TotalXP < reward.RequiredXP) return false;

            // Check stage requirement
            if ((int)progress.CurrentStage < (int)reward.RequiredStage) return false;

            // Check matches won requirement
            if (progress.TotalMatchesWon < reward.RequiredWins) return false;

            // Check total runs requirement
            if (progress.TotalRunsScored < reward.RequiredRuns) return false;

            // Check achievement requirement
            if (!string.IsNullOrEmpty(reward.RequiredAchievement))
            {
                if (!progress.HasAchievement(reward.RequiredAchievement)) return false;
            }

            return true;
        }

        /// <summary>
        /// Apply a reward to the player's progress.
        /// </summary>
        private void ApplyReward(UnlockableReward reward, CareerProgress progress)
        {
            switch (reward.Type)
            {
                case RewardType.Shot:
                    if (reward.ShotReward != ShotType.DefensiveBlock &&
                        !progress.UnlockedShots.Contains(reward.ShotReward))
                    {
                        progress.UnlockedShots.Add(reward.ShotReward);
                    }
                    break;

                case RewardType.Stadium:
                    if (!string.IsNullOrEmpty(reward.StadiumReward) &&
                        !progress.UnlockedStadiums.Contains(reward.StadiumReward))
                    {
                        progress.UnlockedStadiums.Add(reward.StadiumReward);
                    }
                    break;

                case RewardType.Coins:
                    progress.TotalCoins += reward.CoinAmount;
                    break;

                case RewardType.XPBoost:
                    progress.TotalXP += reward.XPBoostAmount;
                    break;

                case RewardType.Cosmetic:
                    // Cosmetics tracked separately - stored as unlocked achievement
                    if (!progress.UnlockedAchievements.Contains($"cosmetic_{reward.RewardId}"))
                    {
                        progress.UnlockedAchievements.Add($"cosmetic_{reward.RewardId}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Get all available (but not yet unlocked) rewards.
        /// </summary>
        public List<UnlockableReward> GetAvailableRewards(CareerProgress progress)
        {
            List<UnlockableReward> available = new List<UnlockableReward>();
            foreach (UnlockableReward reward in _allRewards)
            {
                if (!reward.IsUnlocked)
                {
                    available.Add(reward);
                }
            }
            return available;
        }

        /// <summary>
        /// Get all unlocked rewards.
        /// </summary>
        public List<UnlockableReward> GetUnlockedRewards()
        {
            List<UnlockableReward> unlocked = new List<UnlockableReward>();
            foreach (UnlockableReward reward in _allRewards)
            {
                if (reward.IsUnlocked)
                {
                    unlocked.Add(reward);
                }
            }
            return unlocked;
        }

        /// <summary>
        /// Create the default set of rewards available in the game.
        /// </summary>
        private List<UnlockableReward> CreateDefaultRewards()
        {
            return new List<UnlockableReward>
            {
                // Shot unlocks
                new UnlockableReward
                {
                    RewardId = "shot_cover_drive",
                    Name = "Cover Drive",
                    Description = "Unlock the elegant cover drive shot",
                    Type = RewardType.Shot,
                    ShotReward = ShotType.CoverDrive,
                    RequiredStage = CareerStage.TennisBallTournament,
                    RequiredXP = 500
                },
                new UnlockableReward
                {
                    RewardId = "shot_pull",
                    Name = "Pull Shot",
                    Description = "Unlock the powerful pull shot",
                    Type = RewardType.Shot,
                    ShotReward = ShotType.PullShot,
                    RequiredStage = CareerStage.TennisBallTournament,
                    RequiredXP = 800
                },
                new UnlockableReward
                {
                    RewardId = "shot_uppercut",
                    Name = "Uppercut",
                    Description = "Unlock the daring uppercut over third man",
                    Type = RewardType.Shot,
                    ShotReward = ShotType.Uppercut,
                    RequiredStage = CareerStage.District,
                    RequiredXP = 3000
                },
                new UnlockableReward
                {
                    RewardId = "shot_switch_hit",
                    Name = "Switch Hit",
                    Description = "Unlock the audacious switch hit",
                    Type = RewardType.Shot,
                    ShotReward = ShotType.SwitchHit,
                    RequiredStage = CareerStage.State,
                    RequiredXP = 7000
                },
                new UnlockableReward
                {
                    RewardId = "shot_helicopter",
                    Name = "Helicopter Shot",
                    Description = "Unlock the iconic helicopter shot",
                    Type = RewardType.Shot,
                    ShotReward = ShotType.HelicopterShot,
                    RequiredStage = CareerStage.RanjiTrophy,
                    RequiredXP = 15000
                },

                // Stadium unlocks
                new UnlockableReward
                {
                    RewardId = "stadium_district",
                    Name = "District Stadium",
                    Description = "Unlock the District Stadium venue",
                    Type = RewardType.Stadium,
                    StadiumReward = "District Stadium",
                    RequiredStage = CareerStage.District,
                    RequiredXP = 2000
                },
                new UnlockableReward
                {
                    RewardId = "stadium_wankhede",
                    Name = "Wankhede Stadium",
                    Description = "Unlock the iconic Wankhede Stadium",
                    Type = RewardType.Stadium,
                    StadiumReward = "Wankhede",
                    RequiredStage = CareerStage.RanjiTrophy,
                    RequiredXP = 12000
                },
                new UnlockableReward
                {
                    RewardId = "stadium_eden",
                    Name = "Eden Gardens",
                    Description = "Unlock the legendary Eden Gardens",
                    Type = RewardType.Stadium,
                    StadiumReward = "Eden Gardens",
                    RequiredStage = CareerStage.RanjiTrophy,
                    RequiredXP = 14000
                },
                new UnlockableReward
                {
                    RewardId = "stadium_lords",
                    Name = "Lords Cricket Ground",
                    Description = "Unlock the Home of Cricket - Lords",
                    Type = RewardType.Stadium,
                    StadiumReward = "Lords",
                    RequiredStage = CareerStage.International,
                    RequiredXP = 45000
                },
                new UnlockableReward
                {
                    RewardId = "stadium_mcg",
                    Name = "Melbourne Cricket Ground",
                    Description = "Unlock the mighty MCG",
                    Type = RewardType.Stadium,
                    StadiumReward = "MCG",
                    RequiredStage = CareerStage.International,
                    RequiredXP = 50000
                },

                // Milestone rewards
                new UnlockableReward
                {
                    RewardId = "milestone_1000_runs",
                    Name = "1000 Career Runs",
                    Description = "Earn bonus coins for scoring 1000 career runs",
                    Type = RewardType.Coins,
                    CoinAmount = 2000,
                    RequiredRuns = 1000
                },
                new UnlockableReward
                {
                    RewardId = "milestone_50_wins",
                    Name = "50 Match Wins",
                    Description = "Earn XP boost for winning 50 matches",
                    Type = RewardType.XPBoost,
                    XPBoostAmount = 5000,
                    RequiredWins = 50
                }
            };
        }
    }

    /// <summary>
    /// Defines a single unlockable reward in the career mode.
    /// </summary>
    [Serializable]
    public class UnlockableReward
    {
        public string RewardId;
        public string Name;
        public string Description;
        public RewardType Type;

        // Requirements
        public CareerStage RequiredStage;
        public int RequiredXP;
        public int RequiredWins;
        public int RequiredRuns;
        public string RequiredAchievement;

        // Reward contents
        public ShotType ShotReward;
        public string StadiumReward;
        public int CoinAmount;
        public int XPBoostAmount;

        // State
        public bool IsUnlocked;
        public DateTime UnlockDate;
    }

    /// <summary>
    /// Types of rewards that can be unlocked.
    /// </summary>
    public enum RewardType
    {
        Shot,
        Stadium,
        Cosmetic,
        Coins,
        XPBoost
    }
}
