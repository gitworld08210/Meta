using System;
using System.Threading.Tasks;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.Backend
{
    /// <summary>
    /// Singleton managing Supabase connection: initialization with project URL and anon key,
    /// authentication (anonymous + email), session management, and error handling with retry logic.
    /// Central entry point for all backend operations.
    /// </summary>
    public class SupabaseManager : MonoBehaviour
    {
        private static SupabaseManager _instance;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static SupabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[SupabaseManager] Instance is null. Ensure SupabaseManager exists in the scene.");
                }
                return _instance;
            }
        }

        [Header("Configuration")]
        [SerializeField] private SupabaseConfig _config;

        [Header("Settings")]
        [SerializeField] private bool _autoSignInAnonymous = true;
        [SerializeField] private float _autoSaveInterval = 60f;

        private SupabaseAuth _auth;
        private LeaderboardService _leaderboardService;
        private PlayerDataService _playerDataService;
        private bool _isInitialized;
        private int _retryCount;

        /// <summary>Authentication service.</summary>
        public SupabaseAuth Auth => _auth;

        /// <summary>Leaderboard service.</summary>
        public LeaderboardService Leaderboard => _leaderboardService;

        /// <summary>Player data sync service.</summary>
        public PlayerDataService PlayerData => _playerDataService;

        /// <summary>Whether the backend is initialized and ready.</summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>Whether the user is currently authenticated.</summary>
        public bool IsAuthenticated => _auth?.IsAuthenticated ?? false;

        /// <summary>Current user ID.</summary>
        public string UserId => _auth?.UserId;

        /// <summary>Event fired when backend initialization is complete.</summary>
        public event Action OnInitialized;

        /// <summary>Event fired when a backend error occurs.</summary>
        public event Action<string> OnError;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            // Check for auto-save
            if (_playerDataService != null && _playerDataService.ShouldAutoSave(Time.deltaTime))
            {
                _ = AutoSavePlayerData();
            }
        }

        private async void Initialize()
        {
            if (_config == null)
            {
                Debug.LogError("[SupabaseManager] No SupabaseConfig assigned!");
                OnError?.Invoke("Backend configuration missing.");
                return;
            }

            if (!_config.IsValid())
            {
                Debug.LogError("[SupabaseManager] Invalid Supabase configuration. Check project URL and anon key.");
                OnError?.Invoke("Invalid backend configuration.");
                return;
            }

            // Create services
            _auth = new SupabaseAuth(_config);
            _leaderboardService = new LeaderboardService(_config, _auth);
            _playerDataService = new PlayerDataService(_config, _auth);
            _playerDataService.SetAutoSaveInterval(_autoSaveInterval);

            // Subscribe to auth events
            _auth.OnAuthSuccess += OnAuthSuccess;
            _auth.OnAuthError += OnAuthError;

            // Register with ServiceLocator
            ServiceLocator.Register(this);

            _isInitialized = true;
            OnInitialized?.Invoke();

            if (_config.DebugLogs)
                Debug.Log("[SupabaseManager] Backend initialized successfully.");

            // Auto sign-in
            if (_autoSignInAnonymous && !_auth.IsAuthenticated)
            {
                await AutoSignIn();
            }
        }

        /// <summary>
        /// Attempt automatic sign-in (restore session or sign in anonymously).
        /// </summary>
        public async Task AutoSignIn()
        {
            // Try to restore existing session
            if (_auth.IsAuthenticated)
            {
                if (_config.DebugLogs)
                    Debug.Log("[SupabaseManager] Session restored from cache.");
                return;
            }

            // Try token refresh
            bool refreshed = await _auth.RefreshSession();
            if (refreshed)
            {
                if (_config.DebugLogs)
                    Debug.Log("[SupabaseManager] Session refreshed successfully.");
                return;
            }

            // Sign in anonymously
            if (_autoSignInAnonymous)
            {
                bool success = await RetryOperation(() => _auth.SignInAnonymously());
                if (!success)
                {
                    Debug.LogWarning("[SupabaseManager] Anonymous sign-in failed. Offline mode.");
                }
            }
        }

        /// <summary>
        /// Sign in with email and password.
        /// </summary>
        public async Task<bool> SignInWithEmail(string email, string password)
        {
            return await RetryOperation(() => _auth.SignInWithEmail(email, password));
        }

        /// <summary>
        /// Register a new account with email and password.
        /// </summary>
        public async Task<bool> SignUpWithEmail(string email, string password)
        {
            return await RetryOperation(() => _auth.SignUpWithEmail(email, password));
        }

        /// <summary>
        /// Sign out the current user.
        /// </summary>
        public async Task SignOut()
        {
            await _auth.SignOut();
            if (_config.DebugLogs)
                Debug.Log("[SupabaseManager] User signed out.");
        }

        /// <summary>
        /// Submit a match score to the leaderboard.
        /// </summary>
        public async Task<bool> SubmitMatchScore(string playerName, int score, CareerStage stage, MatchType matchType)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[SupabaseManager] Backend not initialized.");
                return false;
            }

            return await RetryOperation(() =>
                _leaderboardService.SubmitScore(playerName, score, stage, matchType));
        }

        /// <summary>
        /// Sync player data with the server.
        /// </summary>
        public async Task<SaveData> SyncPlayerData(SaveData localData)
        {
            if (!_isInitialized || !IsAuthenticated)
            {
                return localData;
            }

            return await _playerDataService.SyncPlayerData(localData);
        }

        private async Task AutoSavePlayerData()
        {
            if (!IsAuthenticated) return;

            // Get current save data from SaveSystem
            if (ServiceLocator.TryGet<SaveSystem>(out var saveSystem))
            {
                if (_config.DebugLogs)
                    Debug.Log("[SupabaseManager] Auto-save triggered.");
            }
        }

        /// <summary>
        /// Retry an async operation with exponential backoff.
        /// </summary>
        private async Task<bool> RetryOperation(Func<Task<bool>> operation)
        {
            _retryCount = 0;

            while (_retryCount < _config.MaxRetries)
            {
                try
                {
                    bool result = await operation();
                    if (result)
                    {
                        _retryCount = 0;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SupabaseManager] Operation failed (attempt {_retryCount + 1}): {ex.Message}");
                }

                _retryCount++;

                if (_retryCount < _config.MaxRetries)
                {
                    float delay = _config.RetryDelay * Mathf.Pow(2f, _retryCount - 1);
                    await Task.Delay((int)(delay * 1000));
                }
            }

            Debug.LogError($"[SupabaseManager] Operation failed after {_config.MaxRetries} retries.");
            OnError?.Invoke("Operation failed after multiple retries.");
            return false;
        }

        private void OnAuthSuccess(string userId)
        {
            if (_config.DebugLogs)
                Debug.Log($"[SupabaseManager] Auth success. User: {userId}");
        }

        private void OnAuthError(string error)
        {
            Debug.LogWarning($"[SupabaseManager] Auth error: {error}");
            OnError?.Invoke($"Authentication error: {error}");
        }
    }
}
