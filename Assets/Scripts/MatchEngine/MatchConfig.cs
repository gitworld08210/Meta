using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.MatchEngine
{
    /// <summary>
    /// ScriptableObject defining match parameters: match type (T20/ODI/Test),
    /// overs limit, target score (if chasing), difficulty, opponent team, and venue.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMatchConfig", menuName = "MetaCricket/Match Engine/Match Config")]
    public class MatchConfig : ScriptableObject
    {
        [Header("Match Format")]
        [Tooltip("The format of the match.")]
        public MatchType MatchFormat = MatchType.T20;

        [Tooltip("Number of overs per innings. Auto-set based on format if 0.")]
        public int OversLimit = 20;

        [Tooltip("Target score to chase (0 if batting first).")]
        public int TargetScore = 0;

        [Tooltip("Whether the player is chasing a target.")]
        public bool IsChasing = false;

        [Header("Difficulty")]
        [Tooltip("Overall match difficulty.")]
        public DifficultyLevel Difficulty = DifficultyLevel.Medium;

        [Header("Teams")]
        [Tooltip("Name of the opponent team.")]
        public string OpponentTeamName = "Opposition XI";

        [Tooltip("Player's team name.")]
        public string PlayerTeamName = "Player XI";

        [Header("Venue")]
        [Tooltip("Name of the stadium/venue.")]
        public string VenueName = "Local Ground";

        [Tooltip("Pitch type affects ball behavior.")]
        public PitchType PitchCondition = PitchType.Balanced;

        [Header("Career Integration")]
        [Tooltip("Whether this is a career mode match.")]
        public bool IsCareerMatch = false;

        [Tooltip("Career stage for this match.")]
        public CareerStage CareerMatchStage = CareerStage.GullyCricket;

        [Header("Match Rules")]
        [Tooltip("Maximum wickets before innings ends.")]
        public int MaxWickets = 10;

        [Tooltip("Whether to allow DRS (Decision Review System).")]
        public bool AllowDRS = false;

        [Tooltip("Whether powerplay rules apply.")]
        public bool HasPowerplay = true;

        /// <summary>
        /// Get the correct overs limit for this match format.
        /// </summary>
        public int GetOversLimit()
        {
            if (OversLimit > 0) return OversLimit;

            switch (MatchFormat)
            {
                case MatchType.T20:
                    return Constants.GameBalance.MaxOversT20;
                case MatchType.ODI:
                    return Constants.GameBalance.MaxOversODI;
                case MatchType.Test:
                    return 90; // Overs per day in Test cricket
                default:
                    return Constants.GameBalance.MaxOversT20;
            }
        }

        /// <summary>
        /// Get the maximum wickets for this match.
        /// </summary>
        public int GetMaxWickets()
        {
            return MaxWickets > 0 ? MaxWickets : Constants.GameBalance.MaxWickets;
        }

        /// <summary>
        /// Whether this match format uses powerplay overs.
        /// </summary>
        public bool UsesPowerplay()
        {
            return HasPowerplay && (MatchFormat == MatchType.T20 || MatchFormat == MatchType.ODI);
        }

        /// <summary>
        /// Create a match config from match settings data.
        /// </summary>
        public static MatchConfig CreateFromSettings(MatchSettings settings)
        {
            MatchConfig config = CreateInstance<MatchConfig>();
            config.MatchFormat = settings.MatchFormat;
            config.OversLimit = settings.Overs;
            config.TargetScore = settings.TargetScore;
            config.IsChasing = settings.TargetScore > 0;
            config.Difficulty = settings.Difficulty;
            config.OpponentTeamName = settings.OpponentTeamName;
            config.VenueName = settings.VenueName;
            config.IsCareerMatch = settings.IsCareerMatch;
            config.CareerMatchStage = settings.CareerMatchStage;
            return config;
        }
    }

    /// <summary>
    /// Types of pitch conditions affecting gameplay.
    /// </summary>
    public enum PitchType
    {
        Fast,       // Extra pace and bounce
        Spin,       // Assists spin bowlers
        Balanced,   // Even for all types
        Green,      // Seam movement
        Dead        // Low bounce, slow
    }
}
