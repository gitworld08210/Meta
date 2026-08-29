using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using MetaCricket.Core;

namespace MetaCricket.Backend
{
    /// <summary>
    /// Leaderboard data entry structure.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public string id;
        public string player_id;
        public string player_name;
        public int score;
        public string career_stage;
        public string match_type;
        public string created_at;
    }

    /// <summary>
    /// Leaderboard CRUD operations using Supabase REST API.
    /// Supports submitting scores, fetching global top 100, player rank, and nearby ranks.
    /// </summary>
    public class LeaderboardService
    {
        private readonly SupabaseConfig _config;
        private readonly SupabaseAuth _auth;

        public LeaderboardService(SupabaseConfig config, SupabaseAuth auth)
        {
            _config = config;
            _auth = auth;
        }

        /// <summary>
        /// Submit a new score to the leaderboard.
        /// </summary>
        /// <param name="playerName">Display name of the player.</param>
        /// <param name="score">Score achieved.</param>
        /// <param name="careerStage">Current career stage.</param>
        /// <param name="matchType">Type of match played.</param>
        /// <returns>True if submission was successful.</returns>
        public async Task<bool> SubmitScore(string playerName, int score, CareerStage careerStage, MatchType matchType)
        {
            if (!await _auth.EnsureValidToken())
            {
                Debug.LogError("[LeaderboardService] Cannot submit score - not authenticated.");
                return false;
            }

            string url = $"{_config.RestUrl}/{_config.LeaderboardTable}";
            string body = $"{{" +
                          $"\"player_id\":\"{_auth.UserId}\"," +
                          $"\"player_name\":\"{EscapeJson(playerName)}\"," +
                          $"\"score\":{score}," +
                          $"\"career_stage\":\"{careerStage}\"," +
                          $"\"match_type\":\"{matchType}\"" +
                          $"}}";

            try
            {
                await PostRequest(url, body);
                if (_config.DebugLogs)
                    Debug.Log($"[LeaderboardService] Score submitted: {score} by {playerName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardService] Failed to submit score: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fetch the global top 100 leaderboard entries.
        /// </summary>
        /// <returns>List of leaderboard entries sorted by score descending.</returns>
        public async Task<List<LeaderboardEntry>> FetchGlobalTop100()
        {
            if (!await _auth.EnsureValidToken())
            {
                Debug.LogError("[LeaderboardService] Cannot fetch leaderboard - not authenticated.");
                return new List<LeaderboardEntry>();
            }

            string url = $"{_config.RestUrl}/{_config.LeaderboardTable}" +
                         $"?select=*&order=score.desc&limit=100";

            try
            {
                string response = await GetRequest(url);
                return ParseLeaderboardEntries(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardService] Failed to fetch leaderboard: {ex.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        /// <summary>
        /// Fetch the current player's rank on the leaderboard.
        /// </summary>
        /// <returns>Player's rank (1-based), or -1 if not found.</returns>
        public async Task<int> FetchPlayerRank()
        {
            if (!await _auth.EnsureValidToken())
            {
                return -1;
            }

            // Fetch all scores higher than the player's best score
            string playerScoreUrl = $"{_config.RestUrl}/{_config.LeaderboardTable}" +
                                    $"?select=score&player_id=eq.{_auth.UserId}&order=score.desc&limit=1";

            try
            {
                string playerResponse = await GetRequest(playerScoreUrl);
                List<LeaderboardEntry> playerEntries = ParseLeaderboardEntries(playerResponse);

                if (playerEntries.Count == 0) return -1;

                int playerBestScore = playerEntries[0].score;

                // Count entries with higher scores
                string rankUrl = $"{_config.RestUrl}/{_config.LeaderboardTable}" +
                                 $"?select=count&score=gt.{playerBestScore}";

                string rankResponse = await GetRequest(rankUrl);

                // Parse count from response
                int higherCount = ParseCount(rankResponse);
                return higherCount + 1;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardService] Failed to fetch player rank: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Fetch leaderboard entries near the player's rank (5 above and 5 below).
        /// </summary>
        /// <returns>List of nearby leaderboard entries.</returns>
        public async Task<List<LeaderboardEntry>> FetchNearbyRanks()
        {
            if (!await _auth.EnsureValidToken())
            {
                return new List<LeaderboardEntry>();
            }

            int playerRank = await FetchPlayerRank();
            if (playerRank <= 0) return new List<LeaderboardEntry>();

            int offset = Mathf.Max(0, playerRank - 6);
            int limit = 11;

            string url = $"{_config.RestUrl}/{_config.LeaderboardTable}" +
                         $"?select=*&order=score.desc&offset={offset}&limit={limit}";

            try
            {
                string response = await GetRequest(url);
                return ParseLeaderboardEntries(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardService] Failed to fetch nearby ranks: {ex.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        /// <summary>
        /// Fetch leaderboard entries filtered by career stage.
        /// </summary>
        public async Task<List<LeaderboardEntry>> FetchByCareerStage(CareerStage stage, int limit = 50)
        {
            if (!await _auth.EnsureValidToken())
            {
                return new List<LeaderboardEntry>();
            }

            string url = $"{_config.RestUrl}/{_config.LeaderboardTable}" +
                         $"?select=*&career_stage=eq.{stage}&order=score.desc&limit={limit}";

            try
            {
                string response = await GetRequest(url);
                return ParseLeaderboardEntries(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardService] Failed to fetch career leaderboard: {ex.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        private List<LeaderboardEntry> ParseLeaderboardEntries(string json)
        {
            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

            if (string.IsNullOrEmpty(json) || json == "[]") return entries;

            // Parse JSON array (simplified parsing without external dependency)
            try
            {
                // Wrap in utility object for Unity JSON parsing
                string wrappedJson = $"{{\"items\":{json}}}";
                var wrapper = JsonUtility.FromJson<LeaderboardListWrapper>(wrappedJson);
                if (wrapper != null && wrapper.items != null)
                {
                    entries.AddRange(wrapper.items);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LeaderboardService] JSON parse fallback: {ex.Message}");
            }

            return entries;
        }

        private int ParseCount(string json)
        {
            // Simple count extraction from response like [{"count": 42}]
            try
            {
                int countIndex = json.IndexOf("count", StringComparison.Ordinal);
                if (countIndex == -1) return 0;

                int colonIndex = json.IndexOf(':', countIndex);
                if (colonIndex == -1) return 0;

                int valueStart = colonIndex + 1;
                while (valueStart < json.Length && !char.IsDigit(json[valueStart]))
                    valueStart++;

                int valueEnd = valueStart;
                while (valueEnd < json.Length && char.IsDigit(json[valueEnd]))
                    valueEnd++;

                string countStr = json.Substring(valueStart, valueEnd - valueStart);
                return int.TryParse(countStr, out int count) ? count : 0;
            }
            catch
            {
                return 0;
            }
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

        private async Task<string> PostRequest(string url, string body)
        {
            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", _config.AnonKey);
                request.SetRequestHeader("Authorization", $"Bearer {_auth.AccessToken}");
                request.SetRequestHeader("Prefer", "return=minimal");
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

        private string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    /// <summary>
    /// Wrapper for JSON array deserialization.
    /// </summary>
    [Serializable]
    internal class LeaderboardListWrapper
    {
        public LeaderboardEntry[] items;
    }
}
