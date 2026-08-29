using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MetaCricket.Backend
{
    /// <summary>
    /// Authentication flows for Supabase:
    /// - Anonymous sign-in for quick play
    /// - Email/password registration and login
    /// - Session persistence via PlayerPrefs
    /// - Token refresh for expired sessions
    /// </summary>
    public class SupabaseAuth
    {
        private readonly SupabaseConfig _config;

        private string _accessToken;
        private string _refreshToken;
        private string _userId;
        private DateTime _tokenExpiry;

        private const string PREFS_ACCESS_TOKEN = "supabase_access_token";
        private const string PREFS_REFRESH_TOKEN = "supabase_refresh_token";
        private const string PREFS_USER_ID = "supabase_user_id";
        private const string PREFS_TOKEN_EXPIRY = "supabase_token_expiry";

        /// <summary>
        /// Current access token for API requests.
        /// </summary>
        public string AccessToken => _accessToken;

        /// <summary>
        /// Current authenticated user ID.
        /// </summary>
        public string UserId => _userId;

        /// <summary>
        /// Whether the user is currently authenticated.
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;

        /// <summary>
        /// Event fired on successful authentication.
        /// </summary>
        public event Action<string> OnAuthSuccess;

        /// <summary>
        /// Event fired on authentication failure.
        /// </summary>
        public event Action<string> OnAuthError;

        public SupabaseAuth(SupabaseConfig config)
        {
            _config = config;
            LoadSession();
        }

        /// <summary>
        /// Sign in anonymously for quick play without account creation.
        /// </summary>
        public async Task<bool> SignInAnonymously()
        {
            string url = $"{_config.AuthUrl}/signup";
            string body = "{\"data\":{}}";

            try
            {
                string response = await PostRequest(url, body, useAnonKey: true);
                return ProcessAuthResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseAuth] Anonymous sign-in failed: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Register a new user with email and password.
        /// </summary>
        public async Task<bool> SignUpWithEmail(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                OnAuthError?.Invoke("Email and password are required.");
                return false;
            }

            string url = $"{_config.AuthUrl}/signup";
            AuthRequestBody requestBody = new AuthRequestBody { email = email, password = password };
            string body = JsonUtility.ToJson(requestBody);

            try
            {
                string response = await PostRequest(url, body, useAnonKey: true);
                return ProcessAuthResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseAuth] Sign up failed: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Log in with email and password.
        /// </summary>
        public async Task<bool> SignInWithEmail(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                OnAuthError?.Invoke("Email and password are required.");
                return false;
            }

            string url = $"{_config.AuthUrl}/token?grant_type=password";
            AuthRequestBody requestBody = new AuthRequestBody { email = email, password = password };
            string body = JsonUtility.ToJson(requestBody);

            try
            {
                string response = await PostRequest(url, body, useAnonKey: true);
                return ProcessAuthResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseAuth] Sign in failed: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Refresh the access token using the stored refresh token.
        /// </summary>
        public async Task<bool> RefreshSession()
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                Debug.LogWarning("[SupabaseAuth] No refresh token available.");
                return false;
            }

            string url = $"{_config.AuthUrl}/token?grant_type=refresh_token";
            RefreshRequestBody requestBody = new RefreshRequestBody { refresh_token = _refreshToken };
            string body = JsonUtility.ToJson(requestBody);

            try
            {
                string response = await PostRequest(url, body, useAnonKey: true);
                return ProcessAuthResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseAuth] Token refresh failed: {ex.Message}");
                ClearSession();
                return false;
            }
        }

        /// <summary>
        /// Sign out and clear stored session.
        /// </summary>
        public async Task SignOut()
        {
            if (!string.IsNullOrEmpty(_accessToken))
            {
                string url = $"{_config.AuthUrl}/logout";
                try
                {
                    await PostRequest(url, "", useAnonKey: false);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SupabaseAuth] Logout request failed: {ex.Message}");
                }
            }

            ClearSession();
            Debug.Log("[SupabaseAuth] User signed out.");
        }

        /// <summary>
        /// Ensure the current token is valid, refreshing if needed.
        /// </summary>
        public async Task<bool> EnsureValidToken()
        {
            if (IsAuthenticated) return true;

            if (!string.IsNullOrEmpty(_refreshToken))
            {
                return await RefreshSession();
            }

            return false;
        }

        private bool ProcessAuthResponse(string responseJson)
        {
            if (string.IsNullOrEmpty(responseJson))
            {
                OnAuthError?.Invoke("Empty response from server.");
                return false;
            }

            try
            {
                // Parse using JsonUtility with DTO classes for safe JSON handling
                AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(responseJson);

                if (authResponse == null || string.IsNullOrEmpty(authResponse.access_token))
                {
                    // Try parsing as error response
                    AuthErrorResponse errorResponse = JsonUtility.FromJson<AuthErrorResponse>(responseJson);
                    string errorMsg = errorResponse != null
                        ? (!string.IsNullOrEmpty(errorResponse.error_description)
                            ? errorResponse.error_description
                            : errorResponse.msg)
                        : "Authentication failed.";
                    OnAuthError?.Invoke(errorMsg ?? "Authentication failed.");
                    return false;
                }

                _accessToken = authResponse.access_token;

                if (!string.IsNullOrEmpty(authResponse.refresh_token))
                    _refreshToken = authResponse.refresh_token;

                // Extract user ID from nested user object or top-level id
                if (authResponse.user != null && !string.IsNullOrEmpty(authResponse.user.id))
                    _userId = authResponse.user.id;
                else if (!string.IsNullOrEmpty(authResponse.id))
                    _userId = authResponse.id;

                int expirySeconds = authResponse.expires_in > 0 ? authResponse.expires_in : 3600;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expirySeconds);

                SaveSession();
                OnAuthSuccess?.Invoke(_userId);

                if (_config.DebugLogs)
                    Debug.Log($"[SupabaseAuth] Authenticated successfully. User ID: {_userId}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseAuth] Failed to parse auth response: {ex.Message}");
                OnAuthError?.Invoke("Failed to parse server response.");
                return false;
            }
        }

        private void SaveSession()
        {
            SecureStorage.SetString(PREFS_ACCESS_TOKEN, _accessToken ?? "");
            SecureStorage.SetString(PREFS_REFRESH_TOKEN, _refreshToken ?? "");
            SecureStorage.SetString(PREFS_USER_ID, _userId ?? "");
            SecureStorage.SetString(PREFS_TOKEN_EXPIRY, _tokenExpiry.ToBinary().ToString());
            SecureStorage.Save();
        }

        private void LoadSession()
        {
            _accessToken = SecureStorage.GetString(PREFS_ACCESS_TOKEN, "");
            _refreshToken = SecureStorage.GetString(PREFS_REFRESH_TOKEN, "");
            _userId = SecureStorage.GetString(PREFS_USER_ID, "");

            string expiryStr = SecureStorage.GetString(PREFS_TOKEN_EXPIRY, "");
            if (!string.IsNullOrEmpty(expiryStr) && long.TryParse(expiryStr, out long binary))
            {
                _tokenExpiry = DateTime.FromBinary(binary);
            }

            if (_config.DebugLogs && !string.IsNullOrEmpty(_userId))
            {
                Debug.Log($"[SupabaseAuth] Loaded session for user: {_userId}");
            }
        }

        private void ClearSession()
        {
            _accessToken = null;
            _refreshToken = null;
            _userId = null;
            _tokenExpiry = DateTime.MinValue;

            SecureStorage.DeleteKey(PREFS_ACCESS_TOKEN);
            SecureStorage.DeleteKey(PREFS_REFRESH_TOKEN);
            SecureStorage.DeleteKey(PREFS_USER_ID);
            SecureStorage.DeleteKey(PREFS_TOKEN_EXPIRY);
            SecureStorage.Save();
        }

        private async Task<string> PostRequest(string url, string body, bool useAnonKey)
        {
            using (var request = new UnityWebRequest(url, "POST"))
            {
                if (!string.IsNullOrEmpty(body))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }

                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", _config.AnonKey);

                if (!useAnonKey && !string.IsNullOrEmpty(_accessToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
                }

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

        #region JSON DTO Classes

        /// <summary>
        /// Request body for email/password authentication.
        /// </summary>
        [Serializable]
        private class AuthRequestBody
        {
            public string email;
            public string password;
        }

        /// <summary>
        /// Request body for token refresh.
        /// </summary>
        [Serializable]
        private class RefreshRequestBody
        {
            public string refresh_token;
        }

        /// <summary>
        /// Supabase auth success response DTO.
        /// </summary>
        [Serializable]
        private class AuthResponse
        {
            public string access_token;
            public string refresh_token;
            public string token_type;
            public int expires_in;
            public string id;
            public AuthUser user;
        }

        /// <summary>
        /// Nested user object in auth response.
        /// </summary>
        [Serializable]
        private class AuthUser
        {
            public string id;
            public string email;
            public string role;
        }

        /// <summary>
        /// Supabase auth error response DTO.
        /// </summary>
        [Serializable]
        private class AuthErrorResponse
        {
            public string error;
            public string error_description;
            public string msg;
            public int status_code;
        }

        #endregion
    }
}
