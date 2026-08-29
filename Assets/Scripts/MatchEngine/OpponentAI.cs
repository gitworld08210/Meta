using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.MatchEngine
{
    /// <summary>
    /// Simulates opponent team batting (for when the player bowls or watches).
    /// Generates realistic scoring patterns, wicket falls, and run rate based on
    /// difficulty and match situation.
    /// </summary>
    public class OpponentAI : MonoBehaviour
    {
        [Header("Difficulty Settings")]
        [SerializeField] private DifficultyLevel _difficulty = DifficultyLevel.Medium;

        [Header("Scoring Probabilities - Easy")]
        [SerializeField] private float _easyDotBallChance = 0.45f;
        [SerializeField] private float _easySingleChance = 0.25f;
        [SerializeField] private float _easyTwoChance = 0.1f;
        [SerializeField] private float _easyThreeChance = 0.02f;
        [SerializeField] private float _easyFourChance = 0.1f;
        [SerializeField] private float _easySixChance = 0.03f;
        [SerializeField] private float _easyWicketChance = 0.05f;

        [Header("Scoring Probabilities - Legend")]
        [SerializeField] private float _legendDotBallChance = 0.3f;
        [SerializeField] private float _legendSingleChance = 0.25f;
        [SerializeField] private float _legendTwoChance = 0.12f;
        [SerializeField] private float _legendThreeChance = 0.03f;
        [SerializeField] private float _legendFourChance = 0.15f;
        [SerializeField] private float _legendSixChance = 0.08f;
        [SerializeField] private float _legendWicketChance = 0.07f;

        // Internal state
        private int _targetScore;
        private int _oversRemaining;
        private int _wicketsInHand;
        private float _currentRunRate;

        /// <summary>
        /// Set the difficulty for opponent simulation.
        /// </summary>
        public void SetDifficulty(DifficultyLevel difficulty)
        {
            _difficulty = difficulty;
        }

        /// <summary>
        /// Simulate a complete opponent innings.
        /// </summary>
        /// <param name="config">Match configuration.</param>
        /// <param name="targetScore">Target to chase (0 if batting first).</param>
        /// <returns>Complete innings data for the opponent.</returns>
        public InningsData SimulateInnings(MatchConfig config, int targetScore = 0)
        {
            InningsData innings = new InningsData();
            _targetScore = targetScore;
            _oversRemaining = config.GetOversLimit();
            _wicketsInHand = config.GetMaxWickets();
            _currentRunRate = 0f;

            int maxOvers = config.GetOversLimit();
            int maxWickets = config.GetMaxWickets();

            // Generate batsman list
            List<BatsmanInningsData> batsmen = GenerateBattingOrder();
            innings.BatsmanScores = batsmen;

            int currentBatsman1 = 0;
            int currentBatsman2 = 1;
            int onsStrike = currentBatsman1;
            int offStrike = currentBatsman2;

            for (int over = 0; over < maxOvers; over++)
            {
                if (innings.Wickets >= maxWickets)
                    break;

                // Check if target reached (if chasing)
                if (_targetScore > 0 && innings.TotalRuns > _targetScore)
                    break;

                int runsInOver = 0;
                int wicketsInOver = 0;

                for (int ball = 0; ball < 6; ball++)
                {
                    if (innings.Wickets >= maxWickets)
                        break;

                    if (_targetScore > 0 && innings.TotalRuns > _targetScore)
                        break;

                    _oversRemaining = maxOvers - over;
                    int result = SimulateBall(innings, over, ball);

                    if (result == -1)
                    {
                        // Wicket
                        wicketsInOver++;
                        innings.RecordWicket(GetRandomDismissal(),
                            batsmen[onsStrike].Runs, batsmen[onsStrike].BallsFaced);

                        batsmen[onsStrike].Dismissal = GetRandomDismissal();
                        batsmen[onsStrike].IsNotOut = false;
                        batsmen[onsStrike].BallsFaced++;
                        batsmen[onsStrike].UpdateStrikeRate();

                        // New batsman comes in
                        int nextBatsman = innings.Wickets + 1;
                        if (nextBatsman < batsmen.Count)
                        {
                            onsStrike = nextBatsman;
                        }

                        innings.RecordBall();
                    }
                    else
                    {
                        // Runs scored
                        runsInOver += result;
                        innings.AddRuns(result);
                        innings.RecordBall();

                        batsmen[onsStrike].Runs += result;
                        batsmen[onsStrike].BallsFaced++;
                        if (result == 4) batsmen[onsStrike].Fours++;
                        if (result == 6) batsmen[onsStrike].Sixes++;
                        batsmen[onsStrike].UpdateStrikeRate();

                        // Rotate strike on odd runs
                        if (result % 2 == 1)
                        {
                            int temp = onsStrike;
                            onsStrike = offStrike;
                            offStrike = temp;
                        }
                    }
                }

                // Rotate strike at end of over
                int swap = onsStrike;
                onsStrike = offStrike;
                offStrike = swap;

                innings.UpdateRunRate();
            }

            return innings;
        }

        /// <summary>
        /// Simulate a single ball delivery result.
        /// Returns -1 for wicket, or runs scored (0-6).
        /// </summary>
        private int SimulateBall(InningsData innings, int overNumber, int ballNumber)
        {
            // Get probabilities based on difficulty
            float dotChance, singleChance, twoChance, threeChance, fourChance, sixChance, wicketChance;
            GetScoringProbabilities(out dotChance, out singleChance, out twoChance,
                                     out threeChance, out fourChance, out sixChance, out wicketChance);

            // Adjust based on match situation
            AdjustProbabilitiesForSituation(innings, overNumber, ref dotChance, ref singleChance,
                                             ref fourChance, ref sixChance, ref wicketChance);

            // Roll the dice
            float roll = Random.value;
            float cumulative = 0f;

            cumulative += wicketChance;
            if (roll < cumulative) return -1;

            cumulative += dotChance;
            if (roll < cumulative) return 0;

            cumulative += singleChance;
            if (roll < cumulative) return 1;

            cumulative += twoChance;
            if (roll < cumulative) return 2;

            cumulative += threeChance;
            if (roll < cumulative) return 3;

            cumulative += fourChance;
            if (roll < cumulative) return 4;

            return 6; // Six
        }

        /// <summary>
        /// Get base scoring probabilities based on difficulty level.
        /// </summary>
        private void GetScoringProbabilities(out float dot, out float single, out float two,
                                              out float three, out float four, out float six, out float wicket)
        {
            switch (_difficulty)
            {
                case DifficultyLevel.Easy:
                    dot = _easyDotBallChance;
                    single = _easySingleChance;
                    two = _easyTwoChance;
                    three = _easyThreeChance;
                    four = _easyFourChance;
                    six = _easySixChance;
                    wicket = _easyWicketChance;
                    break;

                case DifficultyLevel.Medium:
                    dot = 0.38f;
                    single = 0.25f;
                    two = 0.1f;
                    three = 0.02f;
                    four = 0.13f;
                    six = 0.05f;
                    wicket = 0.07f;
                    break;

                case DifficultyLevel.Hard:
                    dot = 0.33f;
                    single = 0.24f;
                    two = 0.11f;
                    three = 0.03f;
                    four = 0.14f;
                    six = 0.07f;
                    wicket = 0.08f;
                    break;

                case DifficultyLevel.Legend:
                default:
                    dot = _legendDotBallChance;
                    single = _legendSingleChance;
                    two = _legendTwoChance;
                    three = _legendThreeChance;
                    four = _legendFourChance;
                    six = _legendSixChance;
                    wicket = _legendWicketChance;
                    break;
            }
        }

        /// <summary>
        /// Adjust scoring probabilities based on match situation (chasing, death overs, etc.).
        /// </summary>
        private void AdjustProbabilitiesForSituation(InningsData innings, int overNumber,
            ref float dotChance, ref float singleChance,
            ref float fourChance, ref float sixChance, ref float wicketChance)
        {
            // If chasing and behind required rate, increase aggression
            if (_targetScore > 0)
            {
                int runsNeeded = _targetScore - innings.TotalRuns;
                float requiredRate = _oversRemaining > 0 ? (float)runsNeeded / _oversRemaining : 99f;

                if (requiredRate > 10f)
                {
                    // Very aggressive
                    dotChance -= 0.1f;
                    fourChance += 0.05f;
                    sixChance += 0.05f;
                    wicketChance += 0.03f; // More risk = more wickets
                }
                else if (requiredRate > 7f)
                {
                    // Slightly aggressive
                    dotChance -= 0.05f;
                    fourChance += 0.03f;
                    sixChance += 0.02f;
                }
            }

            // Death overs (last 4 overs) - more aggressive
            int totalOvers = innings.OversCompleted + 1;
            if (_oversRemaining <= 4)
            {
                dotChance -= 0.05f;
                fourChance += 0.03f;
                sixChance += 0.02f;
                wicketChance += 0.02f;
            }

            // Powerplay - slightly more boundaries
            if (overNumber < 6)
            {
                fourChance += 0.02f;
                dotChance -= 0.02f;
            }

            // Ensure no negative probabilities
            dotChance = Mathf.Max(dotChance, 0.1f);
            singleChance = Mathf.Max(singleChance, 0.1f);
            fourChance = Mathf.Max(fourChance, 0.02f);
            sixChance = Mathf.Max(sixChance, 0.01f);
            wicketChance = Mathf.Max(wicketChance, 0.02f);
        }

        /// <summary>
        /// Generate a batting order with named opponents.
        /// </summary>
        private List<BatsmanInningsData> GenerateBattingOrder()
        {
            List<BatsmanInningsData> batsmen = new List<BatsmanInningsData>();
            string[] names = {
                "A. Sharma", "V. Kumar", "R. Patel", "S. Singh",
                "M. Reddy", "K. Nair", "D. Gupta", "P. Joshi",
                "N. Chauhan", "T. Yadav", "B. Malik"
            };

            for (int i = 0; i < 11; i++)
            {
                batsmen.Add(new BatsmanInningsData { BatsmanName = names[i] });
            }

            return batsmen;
        }

        /// <summary>
        /// Get a random dismissal type based on realistic probabilities.
        /// </summary>
        private DismissalType GetRandomDismissal()
        {
            float roll = Random.value;
            if (roll < 0.35f) return DismissalType.Bowled;
            if (roll < 0.70f) return DismissalType.Caught;
            if (roll < 0.85f) return DismissalType.LBW;
            if (roll < 0.92f) return DismissalType.RunOut;
            if (roll < 0.97f) return DismissalType.Stumped;
            return DismissalType.HitWicket;
        }
    }
}
