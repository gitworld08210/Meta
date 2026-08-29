using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.Camera
{
    /// <summary>
    /// Camera states for different game situations.
    /// </summary>
    public enum CameraState
    {
        BattingView,
        BallFollow,
        Celebration,
        Replay,
        ARMotionDetection,
        Overview,
        Transition
    }

    /// <summary>
    /// Cinemachine-based camera system managing multiple virtual cameras
    /// for batting view, ball follow, celebration, replay, and AR camera integration.
    /// Provides smooth transitions between states.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        private static CameraManager _instance;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static CameraManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[CameraManager] Instance is null. Ensure CameraManager exists in the scene.");
                }
                return _instance;
            }
        }

        [Header("Virtual Cameras (Cinemachine)")]
        [SerializeField] private GameObject _battingViewCamera;
        [SerializeField] private GameObject _ballFollowCamera;
        [SerializeField] private GameObject _celebrationCamera;
        [SerializeField] private GameObject _replayCamera;
        [SerializeField] private GameObject _arCamera;
        [SerializeField] private GameObject _overviewCamera;

        [Header("Camera Priority (Cinemachine)")]
        [SerializeField] private int _activePriority = 20;
        [SerializeField] private int _inactivePriority = 10;

        [Header("Transition Settings")]
        [SerializeField] private float _defaultBlendTime = 0.5f;
        [SerializeField] private float _fastBlendTime = 0.2f;
        [SerializeField] private float _slowBlendTime = 1.5f;

        [Header("Batting Camera Settings")]
        [SerializeField] private Vector3 _battingViewOffset = new Vector3(0f, 2f, -5f);
        [SerializeField] private float _battingViewFOV = 60f;

        [Header("Ball Follow Settings")]
        [SerializeField] private Transform _ballTransform;
        [SerializeField] private float _followDamping = 0.3f;
        [SerializeField] private float _followLookahead = 2f;

        [Header("Celebration Settings")]
        [SerializeField] private float _celebrationOrbitSpeed = 30f;
        [SerializeField] private float _celebrationDuration = 3f;

        [Header("Camera Shake")]
        [SerializeField] private CameraShake _cameraShake;

        [Header("Replay System")]
        [SerializeField] private ReplayCamera _replayCameraSystem;

        private CameraState _currentState = CameraState.BattingView;
        private CameraState _previousState;
        private float _celebrationTimer;
        private bool _isInCelebration;

        /// <summary>
        /// Current active camera state.
        /// </summary>
        public CameraState CurrentState => _currentState;

        /// <summary>
        /// Whether the camera is currently in replay mode.
        /// </summary>
        public bool IsInReplay => _currentState == CameraState.Replay;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (_isInCelebration)
            {
                UpdateCelebration();
            }
        }

        private void Initialize()
        {
            // Set initial camera state
            SwitchToState(CameraState.BattingView);
            ServiceLocator.Register(this);
            Debug.Log("[CameraManager] Camera system initialized with Cinemachine virtual cameras.");
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<BoundaryEvent>(OnBoundary);
            EventBus.Subscribe<WicketEvent>(OnWicket);
            EventBus.Subscribe<BallBowledEvent>(OnBallBowled);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<VFX.CameraShakeEvent>(OnCameraShakeRequest);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<BoundaryEvent>(OnBoundary);
            EventBus.Unsubscribe<WicketEvent>(OnWicket);
            EventBus.Unsubscribe<BallBowledEvent>(OnBallBowled);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<VFX.CameraShakeEvent>(OnCameraShakeRequest);
        }

        private void OnBoundary(BoundaryEvent evt)
        {
            // Switch to celebration camera for boundaries
            SwitchToState(CameraState.Celebration);
            _isInCelebration = true;
            _celebrationTimer = 0f;

            // Trigger replay for sixes
            if (evt.IsSix && _replayCameraSystem != null)
            {
                Invoke(nameof(TriggerReplay), _celebrationDuration);
            }
            else
            {
                // Return to batting view after celebration
                Invoke(nameof(ReturnToBattingView), _celebrationDuration);
            }
        }

        private void OnWicket(WicketEvent evt)
        {
            // Trigger replay for wickets
            if (_replayCameraSystem != null)
            {
                Invoke(nameof(TriggerReplay), 0.5f);
            }
        }

        private void OnBallBowled(BallBowledEvent evt)
        {
            // Switch to ball follow mode when ball is delivered
            SwitchToState(CameraState.BallFollow);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case GameState.Playing:
                    SwitchToState(CameraState.BattingView);
                    break;
                case GameState.Calibrating:
                    SwitchToState(CameraState.ARMotionDetection);
                    break;
                case GameState.MainMenu:
                    SwitchToState(CameraState.Overview);
                    break;
            }
        }

        private void OnCameraShakeRequest(VFX.CameraShakeEvent evt)
        {
            if (_cameraShake != null)
            {
                _cameraShake.Shake(evt.Intensity, evt.Duration);
            }
        }

        /// <summary>
        /// Switch to a specific camera state with smooth Cinemachine blend.
        /// </summary>
        /// <param name="newState">Target camera state.</param>
        public void SwitchToState(CameraState newState)
        {
            if (_currentState == newState) return;

            _previousState = _currentState;
            _currentState = newState;

            DeactivateAllCameras();
            ActivateCamera(newState);

            Debug.Log($"[CameraManager] Camera state: {_previousState} -> {_currentState}");
        }

        /// <summary>
        /// Switch to AR camera for motion detection view.
        /// </summary>
        public void SwitchToARView()
        {
            SwitchToState(CameraState.ARMotionDetection);
        }

        /// <summary>
        /// Return to default batting view.
        /// </summary>
        public void ReturnToBattingView()
        {
            _isInCelebration = false;
            CancelInvoke(nameof(ReturnToBattingView));
            SwitchToState(CameraState.BattingView);
        }

        /// <summary>
        /// Set the ball transform for ball-following camera.
        /// </summary>
        public void SetBallTarget(Transform ball)
        {
            _ballTransform = ball;
        }

        /// <summary>
        /// Trigger camera shake effect.
        /// </summary>
        public void TriggerShake(float intensity, float duration)
        {
            if (_cameraShake != null)
            {
                _cameraShake.Shake(intensity, duration);
            }
        }

        private void TriggerReplay()
        {
            SwitchToState(CameraState.Replay);
            if (_replayCameraSystem != null)
            {
                _replayCameraSystem.PlayReplay(() =>
                {
                    ReturnToBattingView();
                });
            }
        }

        private void UpdateCelebration()
        {
            _celebrationTimer += Time.deltaTime;

            // Orbit camera around the action
            if (_celebrationCamera != null)
            {
                _celebrationCamera.transform.RotateAround(
                    Vector3.zero,
                    Vector3.up,
                    _celebrationOrbitSpeed * Time.deltaTime
                );
            }
        }

        private void DeactivateAllCameras()
        {
            SetCameraPriority(_battingViewCamera, _inactivePriority);
            SetCameraPriority(_ballFollowCamera, _inactivePriority);
            SetCameraPriority(_celebrationCamera, _inactivePriority);
            SetCameraPriority(_replayCamera, _inactivePriority);
            SetCameraPriority(_arCamera, _inactivePriority);
            SetCameraPriority(_overviewCamera, _inactivePriority);
        }

        private void ActivateCamera(CameraState state)
        {
            GameObject targetCamera = GetCameraForState(state);
            SetCameraPriority(targetCamera, _activePriority);
        }

        private GameObject GetCameraForState(CameraState state)
        {
            switch (state)
            {
                case CameraState.BattingView: return _battingViewCamera;
                case CameraState.BallFollow: return _ballFollowCamera;
                case CameraState.Celebration: return _celebrationCamera;
                case CameraState.Replay: return _replayCamera;
                case CameraState.ARMotionDetection: return _arCamera;
                case CameraState.Overview: return _overviewCamera;
                default: return _battingViewCamera;
            }
        }

        private void SetCameraPriority(GameObject cameraObj, int priority)
        {
            if (cameraObj == null) return;

            // Cinemachine VirtualCamera priority is set via the Priority property
            // We use a generic approach that works whether Cinemachine is imported or not
            var vcam = cameraObj.GetComponent<MonoBehaviour>();
            if (vcam != null)
            {
                // Use reflection to set Priority if Cinemachine is available
                var priorityField = vcam.GetType().GetProperty("Priority");
                if (priorityField != null)
                {
                    priorityField.SetValue(vcam, priority);
                }
            }

            // Fallback: enable/disable the camera object
            cameraObj.SetActive(priority >= _activePriority);
        }
    }
}
