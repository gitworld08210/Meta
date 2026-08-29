using System;
using UnityEngine;

namespace MetaCricket.Core
{
    /// <summary>
    /// Singleton GameManager responsible for managing game state, scene transitions,
    /// and the overall initialization sequence of the game.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[GameManager] Instance is null. Ensure GameManager exists in the scene.");
                }
                return _instance;
            }
        }

        /// <summary>
        /// Current game state.
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        /// <summary>
        /// Previous game state for transition tracking.
        /// </summary>
        public GameState PreviousState { get; private set; } = GameState.MainMenu;

        /// <summary>
        /// Whether the game has been fully initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Whether the game is currently in a transition between states.
        /// </summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Event fired when the game state changes.
        /// </summary>
        public event Action<GameState, GameState> OnStateChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Initialize all game systems in the correct order.
        /// </summary>
        private void InitializeGame()
        {
            Debug.Log("[GameManager] Initializing game systems...");

            // Register self with ServiceLocator
            ServiceLocator.Register(this);

            // Set target frame rate for mobile
            Application.targetFrameRate = 60;

            // Prevent screen dimming during gameplay
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            IsInitialized = true;
            Debug.Log("[GameManager] Game initialization complete.");
        }

        /// <summary>
        /// Transition to a new game state.
        /// </summary>
        /// <param name="newState">The target state to transition to.</param>
        public void ChangeState(GameState newState)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning($"[GameManager] Cannot change state while transitioning. Current: {CurrentState}, Requested: {newState}");
                return;
            }

            if (CurrentState == newState)
            {
                Debug.LogWarning($"[GameManager] Already in state: {newState}");
                return;
            }

            IsTransitioning = true;
            PreviousState = CurrentState;

            Debug.Log($"[GameManager] State change: {CurrentState} -> {newState}");

            ExitState(CurrentState);
            CurrentState = newState;
            EnterState(newState);

            IsTransitioning = false;

            // Notify listeners
            OnStateChanged?.Invoke(PreviousState, CurrentState);

            // Publish event via EventBus
            EventBus.Publish(new GameStateChangedEvent
            {
                PreviousState = PreviousState,
                NewState = CurrentState
            });
        }

        /// <summary>
        /// Handle exiting the current state.
        /// </summary>
        private void ExitState(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    break;
                case GameState.Calibrating:
                    break;
                case GameState.MainMenu:
                    break;
                case GameState.GameOver:
                    break;
            }
        }

        /// <summary>
        /// Handle entering a new state.
        /// </summary>
        private void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
                case GameState.Calibrating:
                    Time.timeScale = 1f;
                    break;
            }
        }

        /// <summary>
        /// Pause the game (transitions to Paused state).
        /// </summary>
        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        /// <summary>
        /// Resume the game from Paused state.
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }

        /// <summary>
        /// Start a new match (transitions to Playing state).
        /// </summary>
        public void StartMatch()
        {
            ChangeState(GameState.Playing);
        }

        /// <summary>
        /// End the current match (transitions to GameOver state).
        /// </summary>
        public void EndMatch()
        {
            ChangeState(GameState.GameOver);
        }

        /// <summary>
        /// Return to main menu from any state.
        /// </summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            ChangeState(GameState.MainMenu);
        }

        /// <summary>
        /// Start AR calibration sequence.
        /// </summary>
        public void StartCalibration()
        {
            ChangeState(GameState.Calibrating);
        }

        /// <summary>
        /// Handle application pause/resume (Android lifecycle).
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && CurrentState == GameState.Playing)
            {
                PauseGame();
            }
        }

        /// <summary>
        /// Handle application focus change.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && CurrentState == GameState.Playing)
            {
                PauseGame();
            }
        }
    }
}
