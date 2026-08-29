using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MetaCricket.Core
{
    /// <summary>
    /// Async scene loading utility with progress callbacks and loading screen support.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static SceneLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[SceneLoader] Instance is null. Ensure SceneLoader exists in the scene.");
                }
                return _instance;
            }
        }

        /// <summary>
        /// Whether a scene is currently loading.
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Current loading progress (0 to 1).
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// Event fired when loading progress updates.
        /// </summary>
        public event Action<float> OnProgressUpdated;

        /// <summary>
        /// Event fired when scene loading completes.
        /// </summary>
        public event Action<string> OnSceneLoaded;

        /// <summary>
        /// Event fired when scene loading starts.
        /// </summary>
        public event Action<string> OnSceneLoadStarted;

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

            // Subscribe to scene unload events to clear stale references
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            if (_instance == this)
            {
                ServiceLocator.Unregister<SceneLoader>();
                _instance = null;
            }
        }

        /// <summary>
        /// Called when any scene is unloaded. Cleans up destroyed subscribers and services
        /// while preserving subscriptions and registrations from persistent DontDestroyOnLoad singletons.
        /// </summary>
        private void OnSceneUnloaded(Scene unloadedScene)
        {
            // Remove only subscriptions from destroyed objects, preserving persistent singletons
            EventBus.CleanupDestroyedSubscribers();

            // Remove only services whose instances have been destroyed, preserving persistent singletons
            ServiceLocator.CleanupDestroyedServices();

            Debug.Log($"[SceneLoader] Scene unloaded: {unloadedScene.name}. Destroyed subscribers and services cleaned up.");
        }

        /// <summary>
        /// Load a scene asynchronously by name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="showLoadingScreen">Whether to show the loading screen during transition.</param>
        /// <param name="onProgress">Optional progress callback (0 to 1).</param>
        /// <param name="onComplete">Optional completion callback.</param>
        public async Task LoadSceneAsync(string sceneName, bool showLoadingScreen = true, Action<float> onProgress = null, Action onComplete = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene. Ignoring request to load: {sceneName}");
                return;
            }

            IsLoading = true;
            Progress = 0f;

            Debug.Log($"[SceneLoader] Loading scene: {sceneName}");
            OnSceneLoadStarted?.Invoke(sceneName);

            // Load loading screen first if requested
            if (showLoadingScreen)
            {
                AsyncOperation loadingOp = SceneManager.LoadSceneAsync(Constants.Scenes.Loading, LoadSceneMode.Additive);
                if (loadingOp != null)
                {
                    while (!loadingOp.isDone)
                    {
                        await Task.Yield();
                    }
                }
            }

            // Load the target scene
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene: {sceneName}");
                IsLoading = false;
                return;
            }

            operation.allowSceneActivation = false;

            // Track progress
            while (operation.progress < 0.9f)
            {
                Progress = Mathf.Clamp01(operation.progress / 0.9f);
                onProgress?.Invoke(Progress);
                OnProgressUpdated?.Invoke(Progress);
                await Task.Yield();
            }

            // Activation
            Progress = 1f;
            onProgress?.Invoke(Progress);
            OnProgressUpdated?.Invoke(Progress);

            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            // Unload loading screen if it was shown
            if (showLoadingScreen)
            {
                Scene loadingScene = SceneManager.GetSceneByName(Constants.Scenes.Loading);
                if (loadingScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadingScene);
                }
            }

            IsLoading = false;

            Debug.Log($"[SceneLoader] Scene loaded: {sceneName}");
            OnSceneLoaded?.Invoke(sceneName);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Load a scene additively (for layered scenes like UI overlays).
        /// </summary>
        /// <param name="sceneName">The name of the scene to load additively.</param>
        public async Task LoadSceneAdditiveAsync(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load additive scene: {sceneName}");
                return;
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            Debug.Log($"[SceneLoader] Additive scene loaded: {sceneName}");
        }

        /// <summary>
        /// Unload a scene asynchronously.
        /// </summary>
        /// <param name="sceneName">The name of the scene to unload.</param>
        public async Task UnloadSceneAsync(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"[SceneLoader] Scene is not loaded: {sceneName}");
                return;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to unload scene: {sceneName}");
                return;
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            Debug.Log($"[SceneLoader] Scene unloaded: {sceneName}");
        }

        /// <summary>
        /// Reload the currently active scene.
        /// </summary>
        public async Task ReloadCurrentScene()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            await LoadSceneAsync(currentSceneName, true);
        }

        /// <summary>
        /// Get the name of the currently active scene.
        /// </summary>
        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}
