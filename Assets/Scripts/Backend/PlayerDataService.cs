using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using MetaCricket.Core;

namespace MetaCricket.Backend
{
    /// <summary>
    /// Player profile synchronization service.
    /// Handles upload/download of career progress, conflict resolution (latest timestamp wins),
    /// and periodic auto-save.
    /// </summary>
    public class PlayerDataService
    {
        private readonly SupabaseConfig _config;
        private readonly SupabaseAuth _auth;

        private float _autoSaveInterval = 60f;
        private float _autoSaveTimer;
        private bool _hasPendingChanges;
        private DateTime _lastSyncTime;

        /// <summary>
        /// Event fired when player data is successfully synced.
        /// </summary>
        public event Action OnDataSynced;

        /// <summary>
        /// Event fired when a sync conflict is detected and resolved.
        /// </summary>
        public event Action<string> OnConflictResolved;

        public PlayerDataService(SupabaseConfig config, SupabaseAuth auth)
        {
            _config = config;
            _auth = auth;
            _lastSyncTime = DateTime.MinValue;
        }

        /// <summary>
        /// Upload player career progress to the server.
        /// </summary>
        /// <param name="profile">Player profile to sync.</param>
        /// <param name="career">Career progress to sync.</param>
        /// <returns>True if upload was successful.</returns>
        public async Task<bool> UploadPlayerData(PlayerProfile profile, CareerProgress career)
        {
            if (!await _auth.EnsureValidToken())
            {
                Debug.LogError("[PlayerDataService] Cannot upload - not authenticated.");
                return false;
            }

            string url = $"{_config.RestUrl}/{_config.PlayersTable}?player_id=eq.{_auth.UserId}";

            string body = BuildPlayerDataJson(profile, career);

            try
            {
                // Use UPSERT (insert or update)
                await UpsertRequest(url, body);

                _lastSyncTime = DateTime.UtcNow;
                _hasPendingChanges = false;
                OnDataSynced?.Invoke();

                if (_config.DebugLogs)
                    Debug.Log("[PlayerDataService] Player data uploaded successfully.");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDataService] Upload failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Download player data from the server (e.g., on new device).
        /// </summary>
        /// <returns>Downloaded save data, or null if not found.</returns>
        public async Task<SaveData> DownloadPlayerData()
        {
            if (!await _auth.EnsureValidToken())
            {
                Debug.LogError("[PlayerDataService] Cannot download - not authenticated.");
                return null;
            }

            string url = $"{_config.RestUrl}/{_config.PlayersTable}" +
                         $"?player_id=eq.{_auth.UserId}&select=*&limit=1";

            try
            {
                string response = await GetRequest(url);

                if (string.IsNullOrEmpty(response) || response == "[]")
                {
                    if (_config.DebugLogs)
                        Debug.Log("[PlayerDataService] No remote data found for player.");
                    return null;
                }

                SaveData remoteData = ParsePlayerData(response);
                _lastSyncTime = DateTime.UtcNow;

                if (_config.DebugLogs)
                    Debug.Log("[PlayerDataService] Player data downloaded successfully.");

                return remoteData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDataService] Download failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resolve conflict between local and remote data.
        /// Uses latest timestamp wins strategy.
        /// </summary>
        /// <param name="localData">Local save data.</param>
        /// <param name="remoteData">Remote save data.</param>
        /// <returns>The winning data set.</returns>
        public SaveData ResolveConflict(SaveData localData, SaveData remoteData)
        {
            if (localData == null) return remoteData;
            if (remoteData == null) return localData;

            // Latest timestamp wins
            if (localData.LastSaved > remoteData.LastSaved)
            {
                OnConflictResolved?.Invoke("Local data is newer - keeping local.");
                if (_config.DebugLogs)
                    Debug.Log("[PlayerDataService] Conflict resolved: Local data wins (newer timestamp).");
                return localData;
            }
            else
            {
                OnConflictResolved?.Invoke("Remote data is newer - using remote.");
                if (_config.DebugLogs)
                    Debug.Log("[PlayerDataService] Conflict resolved: Remote data wins (newer timestamp).");
                return remoteData;
            }
        }

        /// <summary>
        /// Sync player data with conflict resolution.
        /// Compares local and remote timestamps and uploads/downloads accordingly.
        /// </summary>
        public async Task<SaveData> SyncPlayerData(SaveData localData)
        {
            if (!await _auth.EnsureValidToken())
            {
                return localData;
            }

            SaveData remoteData = await DownloadPlayerData();

            if (remoteData == null)
            {
                // No remote data - upload local
                if (localData != null)
                {
                    await UploadPlayerData(localData.Profile, localData.Career);
                }
                return localData;
            }

            if (localData == null)
            {
                return remoteData;
            }

            // Resolve conflict
            SaveData resolvedData = ResolveConflict(localData, remoteData);

            // If local wins, upload it
            if (resolvedData == localData && localData.LastSaved > remoteData.LastSaved)
            {
                await UploadPlayerData(localData.Profile, localData.Career);
            }

            return resolvedData;
        }

        /// <summary>
        /// Mark that local data has changed and needs syncing.
        /// </summary>
        public void MarkDirty()
        {
            _hasPendingChanges = true;
        }

        /// <summary>
        /// Check if auto-save should trigger based on timer and pending changes.
        /// Call this from a MonoBehaviour Update loop.
        /// </summary>
        public bool ShouldAutoSave(float deltaTime)
        {
            if (!_hasPendingChanges) return false;

            _autoSaveTimer += deltaTime;
            if (_autoSaveTimer >= _autoSaveInterval)
            {
                _autoSaveTimer = 0f;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set the auto-save interval in seconds.
        /// </summary>
        public void SetAutoSaveInterval(float seconds)
        {
            _autoSaveInterval = Mathf.Max(10f, seconds);
        }

        /// <summary>
        /// Upload career progress data specifically.
        /// </summary>
        public async Task<bool> UploadCareerProgress(CareerProgress career)
        {
            if (!await _auth.EnsureValidToken())
            {
                return false;
            }

            string url = $"{_config.RestUrl}/{_config.CareerProgressTable}?player_id=eq.{_auth.UserId}";

            CareerProgressUpload uploadData = new CareerProgressUpload
            {
                player_id = _auth.UserId,
                current_stage = career.CurrentStage.ToString(),
                stage_progress = career.StageProgress,
                matches_won = career.MatchesWonInStage,
                matches_played = career.MatchesPlayedInStage,
                total_coins = career.TotalCoins,
                updated_at = DateTime.UtcNow.ToString("O")
            };

            string body = JsonUtility.ToJson(uploadData);

            try
            {
                await UpsertRequest(url, body);
                if (_config.DebugLogs)
                    Debug.Log("[PlayerDataService] Career progress uploaded.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDataService] Career upload failed: {ex.Message}");
                return false;
            }
        }

        private string BuildPlayerDataJson(PlayerProfile profile, CareerProgress career)
        {
            PlayerDataUpload uploadData = new PlayerDataUpload
            {
                player_id = _auth.UserId,
                player_name = profile.PlayerName ?? "",
                level = profile.Level,
                experience_points = profile.ExperiencePoints,
                total_runs = profile.TotalRuns,
                matches_played = profile.MatchesPlayed,
                matches_won = profile.MatchesWon,
                batting_average = profile.BattingAverage,
                strike_rate = profile.StrikeRate,
                high_score = profile.HighScore,
                current_career_stage = career.CurrentStage.ToString(),
                updated_at = DateTime.UtcNow.ToString("O")
            };

            return JsonUtility.ToJson(uploadData);
        }

        private SaveData ParsePlayerData(string json)
        {
            SaveData data = new SaveData
            {
                Profile = new PlayerProfile(),
                Career = new CareerProgress(),
                Settings = GameSettings.CreateDefault(),
                LastSaved = DateTime.UtcNow
            };

            try
            {
                // Supabase returns an array; strip array brackets for single object
                string trimmedJson = json.Trim();
                if (trimmedJson.StartsWith("[") && trimmedJson.EndsWith("]"))
                {
                    trimmedJson = trimmedJson.Substring(1, trimmedJson.Length - 2).Trim();
                }

                if (string.IsNullOrEmpty(trimmedJson))
                    return data;

                // Parse with JsonUtility using DTO
                PlayerDataResponse response = JsonUtility.FromJson<PlayerDataResponse>(trimmedJson);

                if (response != null)
                {
                    data.Profile.PlayerName = response.player_name ?? "";
                    data.Profile.Level = response.level;
                    data.Profile.ExperiencePoints = response.experience_points;
                    data.Profile.TotalRuns = response.total_runs;
                    data.Profile.MatchesPlayed = response.matches_played;
                    data.Profile.MatchesWon = response.matches_won;
                    data.Profile.BattingAverage = response.batting_average;
                    data.Profile.StrikeRate = response.strike_rate;
                    data.Profile.HighScore = response.high_score;

                    if (!string.IsNullOrEmpty(response.updated_at))
                    {
                        if (DateTime.TryParse(response.updated_at, out DateTime updatedAt))
                        {
                            data.LastSaved = updatedAt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDataService] Failed to parse player data: {ex.Message}");
            }

            return data;
        }

        private async Task<string> GetRequest(string url)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("apikey", _config.AnonKey);
                request.SetRequestHeader("Authorization", $"Bearer {_auth.AccessToken}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)_config.RequestTimeout;

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception($"HTTP {request.responseCode}: {request.error}");
                }

                return request.downloadHandler.text;
            }
        }

        private async Task<string> UpsertRequest(string url, string body)
        {
            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", _config.AnonKey);
                request.SetRequestHeader("Authorization", $"Bearer {_auth.AccessToken}");
                request.SetRequestHeader("Prefer", "resolution=merge-duplicates");
                request.timeout = (int)_config.RequestTimeout;

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception($"HTTP {request.responseCode}: {request.error}");
                }

                return request.downloadHandler?.text ?? "";
            }
        }

        #region JSON DTO Classes

        /// <summary>
        /// DTO for uploading player data to Supabase.
        /// </summary>
        [Serializable]
        private class PlayerDataUpload
        {
            public string player_id;
            public string player_name;
            public int level;
            public int experience_points;
            public int total_runs;
            public int matches_played;
            public int matches_won;
            public float batting_average;
            public float strike_rate;
            public int high_score;
            public string current_career_stage;
            public string updated_at;
        }

        /// <summary>
        /// DTO for uploading career progress to Supabase.
        /// </summary>
        [Serializable]
        private class CareerProgressUpload
        {
            public string player_id;
            public string current_stage;
            public int stage_progress;
            public int matches_won;
            public int matches_played;
            public int total_coins;
            public string updated_at;
        }

        /// <summary>
        /// DTO for receiving player data from Supabase.
        /// </summary>
        [Serializable]
        private class PlayerDataResponse
        {
            public string player_id;
            public string player_name;
            public int level;
            public int experience_points;
            public int total_runs;
            public int matches_played;
            public int matches_won;
            public float batting_average;
            public float strike_rate;
            public int high_score;
            public string current_career_stage;
            public string updated_at;
        }

        #endregion
    }
}
