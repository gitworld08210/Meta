using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MetaCricket.Core
{
    /// <summary>
    /// JSON-based save system handling persistent game data storage.
    /// Uses Application.persistentDataPath for cross-device compatibility.
    /// Supports async file I/O for non-blocking save/load operations.
    /// Implements a save queue so rapid save calls are queued instead of dropped.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        private static SaveSystem _instance;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static SaveSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[SaveSystem] Instance is null. Ensure SaveSystem exists in the scene.");
                }
                return _instance;
            }
        }

        /// <summary>
        /// The currently loaded save data.
        /// </summary>
        public SaveData CurrentSaveData { get; private set; }

        /// <summary>
        /// Whether a save/load operation is currently in progress.
        /// </summary>
        public bool IsOperationInProgress { get; private set; }

        /// <summary>
        /// Number of operations currently waiting in the queue.
        /// </summary>
        public int QueuedOperationCount => _saveQueue.Count;

        /// <summary>
        /// Event fired when save completes.
        /// </summary>
        public event Action<bool> OnSaveCompleted;

        /// <summary>
        /// Event fired when load completes.
        /// </summary>
        public event Action<bool> OnLoadCompleted;

        // Save queue to handle concurrent operations
        private Queue<SaveOperation> _saveQueue = new Queue<SaveOperation>();
        private bool _isProcessingQueue;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ServiceLocator.Unregister<SaveSystem>();
                _instance = null;
            }
        }

        /// <summary>
        /// Get the full file path for a save file.
        /// </summary>
        private string GetFilePath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        /// <summary>
        /// Save data asynchronously to a JSON file.
        /// If another operation is in progress, the save is queued and will execute
        /// after the current operation completes.
        /// </summary>
        /// <typeparam name="T">The type of data to save.</typeparam>
        /// <param name="data">The data object to serialize.</param>
        /// <param name="fileName">The file name to save to.</param>
        /// <returns>True if save was successful.</returns>
        public async Task<bool> SaveAsync<T>(T data, string fileName) where T : class
        {
            string json = JsonUtility.ToJson(data, true);

            if (IsOperationInProgress)
            {
                // Queue the save operation instead of dropping it
                var operation = new SaveOperation(fileName, json);
                _saveQueue.Enqueue(operation);
                Debug.Log($"[SaveSystem] Save queued for: {fileName} (queue size: {_saveQueue.Count})");

                // Wait for the queued operation to complete
                while (!operation.IsCompleted)
                {
                    await Task.Yield();
                }

                return operation.Success;
            }

            return await ExecuteSave(fileName, json);
        }

        /// <summary>
        /// Execute a save operation directly.
        /// </summary>
        private async Task<bool> ExecuteSave(string fileName, string json)
        {
            IsOperationInProgress = true;

            try
            {
                string filePath = GetFilePath(fileName);

                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write to a temp file first, then rename for atomic write
                string tempPath = filePath + ".tmp";

                using (StreamWriter writer = new StreamWriter(tempPath))
                {
                    await writer.WriteAsync(json);
                }

                // Replace original with temp file
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempPath, filePath);

                Debug.Log($"[SaveSystem] Data saved successfully to: {fileName}");
                OnSaveCompleted?.Invoke(true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save data to {fileName}: {ex.Message}");
                OnSaveCompleted?.Invoke(false);
                return false;
            }
            finally
            {
                IsOperationInProgress = false;
                ProcessQueue();
            }
        }

        /// <summary>
        /// Load data asynchronously from a JSON file.
        /// If another operation is in progress, the load waits in the queue.
        /// </summary>
        /// <typeparam name="T">The type of data to load.</typeparam>
        /// <param name="fileName">The file name to load from.</param>
        /// <returns>The deserialized data, or null if load failed.</returns>
        public async Task<T> LoadAsync<T>(string fileName) where T : class
        {
            // Wait for any in-progress operations
            while (IsOperationInProgress)
            {
                await Task.Yield();
            }

            IsOperationInProgress = true;

            try
            {
                string filePath = GetFilePath(fileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[SaveSystem] Save file not found: {fileName}");
                    OnLoadCompleted?.Invoke(false);
                    return null;
                }

                string json;
                using (StreamReader reader = new StreamReader(filePath))
                {
                    json = await reader.ReadToEndAsync();
                }

                T data = JsonUtility.FromJson<T>(json);
                Debug.Log($"[SaveSystem] Data loaded successfully from: {fileName}");
                OnLoadCompleted?.Invoke(true);
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load data from {fileName}: {ex.Message}");
                OnLoadCompleted?.Invoke(false);
                return null;
            }
            finally
            {
                IsOperationInProgress = false;
                ProcessQueue();
            }
        }

        /// <summary>
        /// Process queued save operations sequentially.
        /// Deduplicates saves to the same file (only the latest data is written).
        /// </summary>
        private async void ProcessQueue()
        {
            if (_isProcessingQueue || _saveQueue.Count == 0)
                return;

            _isProcessingQueue = true;

            while (_saveQueue.Count > 0)
            {
                // Deduplicate: if multiple saves to the same file are queued,
                // skip all but the last one for that file
                SaveOperation nextOp = _saveQueue.Dequeue();

                // Check if there are newer saves for the same file in the queue
                SaveOperation latestForFile = nextOp;
                Queue<SaveOperation> remaining = new Queue<SaveOperation>();

                while (_saveQueue.Count > 0)
                {
                    SaveOperation op = _saveQueue.Dequeue();
                    if (op.FileName == nextOp.FileName)
                    {
                        // Mark the older operation as completed (superseded)
                        latestForFile.MarkCompleted(true);
                        latestForFile = op;
                    }
                    else
                    {
                        remaining.Enqueue(op);
                    }
                }

                // Restore non-matching operations
                while (remaining.Count > 0)
                {
                    _saveQueue.Enqueue(remaining.Dequeue());
                }

                // Execute the latest save for this file
                bool result = await ExecuteSave(latestForFile.FileName, latestForFile.JsonData);
                latestForFile.MarkCompleted(result);
            }

            _isProcessingQueue = false;
        }

        /// <summary>
        /// Save the complete game data (profile, career, stats, settings).
        /// </summary>
        public async Task<bool> SaveGameData(SaveData saveData)
        {
            saveData.LastSaved = DateTime.Now;
            saveData.SaveVersion = 1;
            CurrentSaveData = saveData;
            return await SaveAsync(saveData, Constants.FilePaths.SaveFileName);
        }

        /// <summary>
        /// Load the complete game data.
        /// </summary>
        public async Task<SaveData> LoadGameData()
        {
            SaveData data = await LoadAsync<SaveData>(Constants.FilePaths.SaveFileName);

            if (data != null)
            {
                CurrentSaveData = data;
            }

            return data;
        }

        /// <summary>
        /// Save player profile separately for quick access.
        /// </summary>
        public async Task<bool> SaveProfile(PlayerProfile profile)
        {
            return await SaveAsync(profile, Constants.FilePaths.ProfileFileName);
        }

        /// <summary>
        /// Load player profile.
        /// </summary>
        public async Task<PlayerProfile> LoadProfile()
        {
            return await LoadAsync<PlayerProfile>(Constants.FilePaths.ProfileFileName);
        }

        /// <summary>
        /// Save game settings.
        /// </summary>
        public async Task<bool> SaveSettings(GameSettings settings)
        {
            return await SaveAsync(settings, Constants.FilePaths.SettingsFileName);
        }

        /// <summary>
        /// Load game settings.
        /// </summary>
        public async Task<GameSettings> LoadSettings()
        {
            return await LoadAsync<GameSettings>(Constants.FilePaths.SettingsFileName);
        }

        /// <summary>
        /// Check if a save file exists.
        /// </summary>
        public bool SaveFileExists(string fileName)
        {
            return File.Exists(GetFilePath(fileName));
        }

        /// <summary>
        /// Check if main save data exists.
        /// </summary>
        public bool HasSaveData()
        {
            return SaveFileExists(Constants.FilePaths.SaveFileName);
        }

        /// <summary>
        /// Delete a save file.
        /// </summary>
        public bool DeleteSaveFile(string fileName)
        {
            try
            {
                string filePath = GetFilePath(fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[SaveSystem] Deleted save file: {fileName}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to delete save file {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete all save data (for reset/new game).
        /// </summary>
        public void DeleteAllSaveData()
        {
            DeleteSaveFile(Constants.FilePaths.SaveFileName);
            DeleteSaveFile(Constants.FilePaths.ProfileFileName);
            DeleteSaveFile(Constants.FilePaths.SettingsFileName);
            CurrentSaveData = null;
            Debug.Log("[SaveSystem] All save data deleted.");
        }

        /// <summary>
        /// Internal class representing a queued save operation.
        /// </summary>
        private class SaveOperation
        {
            public string FileName { get; private set; }
            public string JsonData { get; private set; }
            public bool IsCompleted { get; private set; }
            public bool Success { get; private set; }

            public SaveOperation(string fileName, string jsonData)
            {
                FileName = fileName;
                JsonData = jsonData;
                IsCompleted = false;
                Success = false;
            }

            public void MarkCompleted(bool success)
            {
                Success = success;
                IsCompleted = true;
            }
        }
    }
}
