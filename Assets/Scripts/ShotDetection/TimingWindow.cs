using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.ShotDetection
{
    /// <summary>
    /// Manages the timing system: ball delivery triggers a timing window,
    /// and shot execution time relative to ball position determines timing quality
    /// (Early/Good/Perfect/Late).
    /// </summary>
    public class TimingWindow : MonoBehaviour
    {
        [Header("Timing Configuration")]
        [SerializeField]
        [Tooltip("Duration of the perfect timing window (seconds).")]
        private float _perfectWindowDuration = 0.08f;

        [SerializeField]
        [Tooltip("Duration of the good timing window on each side of perfect (seconds).")]
        private float _goodWindowDuration = 0.12f;

        [SerializeField]
        [Tooltip("Duration of the early window before good (seconds).")]
        private float _earlyWindowDuration = 0.15f;

        [SerializeField]
        [Tooltip("Duration of the late window after good (seconds).")]
        private float _lateWindowDuration = 0.15f;

        [SerializeField]
        [Tooltip("How much difficulty affects the timing windows (0-1). Higher = tighter windows.")]
        [Range(0f, 1f)]
        private float _difficultyTighteningFactor = 0f;

        /// <summary>
        /// Whether a timing window is currently active.
        /// </summary>
        public bool IsWindowActive { get; private set; }

        /// <summary>
        /// Time remaining in the current timing window.
        /// </summary>
        public float TimeRemaining { get; private set; }

        /// <summary>
        /// The ideal time to hit the ball (center of perfect window).
        /// </summary>
        public float IdealHitTime { get; private set; }

        /// <summary>
        /// Current progress through the timing window (0-1).
        /// </summary>
        public float WindowProgress { get; private set; }

        private float _windowStartTime;
        private float _windowEndTime;
        private float _totalWindowDuration;
        private bool _shotRegistered;

        private void Awake()
        {
            IsWindowActive = false;
            _shotRegistered = false;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BallBowledEvent>(OnBallBowled);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BallBowledEvent>(OnBallBowled);
        }

        /// <summary>
        /// Open a timing window when a ball is bowled.
        /// </summary>
        private void OnBallBowled(BallBowledEvent ballEvent)
        {
            StartTimingWindow(ballEvent.Speed);
        }

        /// <summary>
        /// Start a timing window based on ball travel time.
        /// The ideal hit time is calculated based on ball speed.
        /// </summary>
        /// <param name="ballSpeed">Speed of the bowled ball.</param>
        public void StartTimingWindow(float ballSpeed)
        {
            _totalWindowDuration = _earlyWindowDuration + _goodWindowDuration +
                                   _perfectWindowDuration + _goodWindowDuration + _lateWindowDuration;

            // Apply difficulty tightening
            float tightenFactor = 1f - (_difficultyTighteningFactor * 0.5f);
            float adjustedDuration = _totalWindowDuration * tightenFactor;

            // Calculate when the ball arrives at the batsman
            float ballTravelTime = Constants.GameBalance.BallTravelTime;
            if (ballSpeed > 0)
            {
                // Adjust based on ball speed relative to a reference speed
                const float referenceSpeed = 140f; // km/h reference
                ballTravelTime = Constants.GameBalance.BallTravelTime * (referenceSpeed / ballSpeed);
            }

            // The ideal hit time is when the ball is in the hitting zone
            IdealHitTime = Time.time + ballTravelTime;

            // Center the window around the ideal hit time
            _windowStartTime = IdealHitTime - (adjustedDuration / 2f);
            _windowEndTime = IdealHitTime + (adjustedDuration / 2f);

            IsWindowActive = true;
            _shotRegistered = false;
            TimeRemaining = adjustedDuration;

            Debug.Log($"[TimingWindow] Window opened. Ideal hit time in {ballTravelTime:F3}s. " +
                     $"Window duration: {adjustedDuration:F3}s");
        }

        /// <summary>
        /// Manually close the timing window.
        /// </summary>
        public void CloseWindow()
        {
            IsWindowActive = false;
            TimeRemaining = 0f;
            WindowProgress = 1f;
        }

        /// <summary>
        /// Evaluate the timing quality of a shot executed at the current time.
        /// </summary>
        /// <returns>The timing quality based on current time relative to the window.</returns>
        public TimingQuality EvaluateTiming()
        {
            return EvaluateTimingAt(Time.time);
        }

        /// <summary>
        /// Evaluate the timing quality of a shot executed at a specific time.
        /// </summary>
        /// <param name="shotTime">The time the shot was executed.</param>
        /// <returns>The timing quality based on relative position in the window.</returns>
        public TimingQuality EvaluateTimingAt(float shotTime)
        {
            if (!IsWindowActive)
            {
                // If no window is active, default to Late
                return TimingQuality.Late;
            }

            _shotRegistered = true;

            float timeDiff = shotTime - IdealHitTime;
            float absDiff = Mathf.Abs(timeDiff);

            // Apply difficulty tightening
            float tightenFactor = 1f - (_difficultyTighteningFactor * 0.5f);
            float perfectHalf = (_perfectWindowDuration / 2f) * tightenFactor;
            float goodHalf = (perfectHalf + _goodWindowDuration) * tightenFactor;

            // Perfect timing: within the perfect window
            if (absDiff <= perfectHalf)
            {
                return TimingQuality.Perfect;
            }

            // Good timing: within the good window
            if (absDiff <= goodHalf)
            {
                return TimingQuality.Good;
            }

            // Early or Late based on direction
            if (timeDiff < 0)
            {
                return TimingQuality.Early;
            }
            else
            {
                return TimingQuality.Late;
            }
        }

        /// <summary>
        /// Get a normalized timing accuracy value (0-1, where 1 is perfect).
        /// </summary>
        /// <param name="shotTime">The time the shot was executed.</param>
        /// <returns>Normalized timing accuracy.</returns>
        public float GetTimingAccuracy(float shotTime)
        {
            if (!IsWindowActive)
                return 0f;

            float timeDiff = Mathf.Abs(shotTime - IdealHitTime);
            float maxWindow = _earlyWindowDuration + _goodWindowDuration + _perfectWindowDuration;

            // Normalize: 1.0 at ideal time, approaching 0 at window edges
            return Mathf.Clamp01(1f - (timeDiff / maxWindow));
        }

        /// <summary>
        /// Set difficulty level which affects timing window sizes.
        /// </summary>
        /// <param name="difficulty">The difficulty level to apply.</param>
        public void SetDifficulty(DifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case DifficultyLevel.Easy:
                    _difficultyTighteningFactor = 0f;
                    break;
                case DifficultyLevel.Medium:
                    _difficultyTighteningFactor = 0.2f;
                    break;
                case DifficultyLevel.Hard:
                    _difficultyTighteningFactor = 0.5f;
                    break;
                case DifficultyLevel.Legend:
                    _difficultyTighteningFactor = 0.8f;
                    break;
            }
        }

        private void Update()
        {
            if (!IsWindowActive)
                return;

            // Update time remaining
            TimeRemaining = _windowEndTime - Time.time;
            WindowProgress = Mathf.Clamp01((Time.time - _windowStartTime) /
                            (_windowEndTime - _windowStartTime));

            // Check if window has expired
            if (Time.time > _windowEndTime)
            {
                if (!_shotRegistered)
                {
                    Debug.Log("[TimingWindow] Window expired without a shot.");
                }
                CloseWindow();
            }
        }
    }
}
