using UnityEngine;

namespace MetaCricket.Backend
{
    /// <summary>
    /// ScriptableObject storing Supabase project configuration.
    /// Allows easy configuration of backend connection without code changes.
    /// Create via Assets > Create > MetaCricket > Backend > Supabase Config.
    /// </summary>
    [CreateAssetMenu(fileName = "SupabaseConfig", menuName = "MetaCricket/Backend/Supabase Config")]
    public class SupabaseConfig : ScriptableObject
    {
        [Header("Supabase Project")]
        [Tooltip("Your Supabase project URL (e.g., https://your-project.supabase.co)")]
        [SerializeField] private string _projectUrl = "";

        [Tooltip("Supabase anonymous (public) key for client-side access")]
        [SerializeField] private string _anonKey = "";

        [Header("Table Names")]
        [SerializeField] private string _playersTable = "players";
        [SerializeField] private string _leaderboardTable = "leaderboard";
        [SerializeField] private string _matchHistoryTable = "match_history";
        [SerializeField] private string _careerProgressTable = "career_progress";

        [Header("API Endpoints")]
        [SerializeField] private string _authEndpoint = "/auth/v1";
        [SerializeField] private string _restEndpoint = "/rest/v1";
        [SerializeField] private string _storageEndpoint = "/storage/v1";

        [Header("Settings")]
        [SerializeField] private float _requestTimeout = 10f;
        [SerializeField] private int _maxRetries = 3;
        [SerializeField] private float _retryDelay = 1f;
        [SerializeField] private bool _enableDebugLogs = true;

        /// <summary>Supabase project URL.</summary>
        public string ProjectUrl => _projectUrl;

        /// <summary>Supabase anonymous key.</summary>
        public string AnonKey => _anonKey;

        /// <summary>Players table name.</summary>
        public string PlayersTable => _playersTable;

        /// <summary>Leaderboard table name.</summary>
        public string LeaderboardTable => _leaderboardTable;

        /// <summary>Match history table name.</summary>
        public string MatchHistoryTable => _matchHistoryTable;

        /// <summary>Career progress table name.</summary>
        public string CareerProgressTable => _careerProgressTable;

        /// <summary>Full auth endpoint URL.</summary>
        public string AuthUrl => $"{_projectUrl}{_authEndpoint}";

        /// <summary>Full REST endpoint URL.</summary>
        public string RestUrl => $"{_projectUrl}{_restEndpoint}";

        /// <summary>Full storage endpoint URL.</summary>
        public string StorageUrl => $"{_projectUrl}{_storageEndpoint}";

        /// <summary>Request timeout in seconds.</summary>
        public float RequestTimeout => _requestTimeout;

        /// <summary>Maximum retry attempts for failed requests.</summary>
        public int MaxRetries => _maxRetries;

        /// <summary>Delay between retries in seconds.</summary>
        public float RetryDelay => _retryDelay;

        /// <summary>Whether debug logging is enabled.</summary>
        public bool DebugLogs => _enableDebugLogs;

        /// <summary>
        /// Validate that required configuration is set.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_projectUrl) && !string.IsNullOrEmpty(_anonKey);
        }
    }
}
