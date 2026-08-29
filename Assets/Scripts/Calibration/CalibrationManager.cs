using UnityEngine;
using MetaCricket.Core;
using MetaCricket.MotionDetection;

namespace MetaCricket.Calibration
{
    /// <summary>
    /// MonoBehaviour managing the full calibration flow: instructs the player to stand in T-pose,
    /// captures baseline measurements (arm length, shoulder width, standing position),
    /// stores in CalibrationData, and publishes CalibrationCompleteEvent.
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        /// <summary>
        /// Calibration flow states.
        /// </summary>
        public enum CalibrationPhase
        {
            Idle,
            Positioning,
            WaitingForTPose,
            HoldingTPose,
            Processing,
            Complete,
            Failed
        }

        [Header("References")]
        [SerializeField]
        [Tooltip("The pose provider to use for detection.")]
        private PoseProvider _poseProvider;

        [SerializeField]
        [Tooltip("The T-pose detector component.")]
        private TPoseDetector _tposeDetector;

        [SerializeField]
        [Tooltip("The calibration UI controller.")]
        private CalibrationUI _calibrationUI;

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Maximum time allowed for calibration before timeout (seconds).")]
        private float _calibrationTimeout = 30f;

        [SerializeField]
        [Tooltip("Number of frames to average for stable calibration.")]
        private int _averagingFrameCount = 10;

        [SerializeField]
        [Tooltip("Minimum overall pose confidence required for calibration.")]
        private float _minimumPoseConfidence = 0.6f;

        /// <summary>
        /// Current phase of the calibration process.
        /// </summary>
        public CalibrationPhase CurrentPhase { get; private set; }

        /// <summary>
        /// The calibration data captured during the process.
        /// </summary>
        public CalibrationData CapturedData { get; private set; }

        /// <summary>
        /// Whether calibration has been completed successfully.
        /// </summary>
        public bool IsCalibrated { get; private set; }

        /// <summary>
        /// Progress of the current calibration phase (0-1).
        /// </summary>
        public float Progress { get; private set; }

        private float _calibrationStartTime;
        private int _capturedFrameCount;
        private CalibrationData _accumulatedData;
        private JointSmoother _jointSmoother;

        private void Awake()
        {
            CurrentPhase = CalibrationPhase.Idle;
            _jointSmoother = GetComponent<JointSmoother>();
        }

        private void OnEnable()
        {
            if (_poseProvider != null)
            {
                _poseProvider.OnPoseUpdated += OnPoseUpdated;
                _poseProvider.OnTrackingLost += OnTrackingLost;
            }
        }

        private void OnDisable()
        {
            if (_poseProvider != null)
            {
                _poseProvider.OnPoseUpdated -= OnPoseUpdated;
                _poseProvider.OnTrackingLost -= OnTrackingLost;
            }
        }

        /// <summary>
        /// Start the calibration process.
        /// </summary>
        public void StartCalibration()
        {
            Debug.Log("[CalibrationManager] Starting calibration...");

            CurrentPhase = CalibrationPhase.Positioning;
            _calibrationStartTime = Time.time;
            _capturedFrameCount = 0;
            _accumulatedData = new CalibrationData();
            Progress = 0f;

            // Initialize pose provider if needed
            if (_poseProvider != null && !_poseProvider.IsInitialized)
            {
                _poseProvider.Initialize();
            }

            if (_poseProvider != null && !_poseProvider.IsTracking)
            {
                _poseProvider.StartTracking();
            }

            // Reset T-pose detector
            if (_tposeDetector != null)
            {
                _tposeDetector.ResetDetection();
            }

            // Update UI
            if (_calibrationUI != null)
            {
                _calibrationUI.ShowPositioningInstructions();
            }
        }

        /// <summary>
        /// Cancel the current calibration.
        /// </summary>
        public void CancelCalibration()
        {
            Debug.Log("[CalibrationManager] Calibration cancelled.");
            CurrentPhase = CalibrationPhase.Idle;
            Progress = 0f;

            if (_tposeDetector != null)
            {
                _tposeDetector.ResetDetection();
            }

            if (_calibrationUI != null)
            {
                _calibrationUI.HideAll();
            }
        }

        /// <summary>
        /// Reset calibration data and state.
        /// </summary>
        public void ResetCalibration()
        {
            IsCalibrated = false;
            CapturedData = null;
            CurrentPhase = CalibrationPhase.Idle;
            Progress = 0f;

            if (_tposeDetector != null)
            {
                _tposeDetector.ResetDetection();
            }
        }

        private void Update()
        {
            if (CurrentPhase == CalibrationPhase.Idle || CurrentPhase == CalibrationPhase.Complete)
                return;

            // Check for timeout
            if (Time.time - _calibrationStartTime > _calibrationTimeout)
            {
                OnCalibrationFailed("Calibration timed out. Please try again.");
                return;
            }
        }

        /// <summary>
        /// Handle incoming pose data during calibration.
        /// </summary>
        private void OnPoseUpdated(PoseData poseData)
        {
            if (CurrentPhase == CalibrationPhase.Idle || CurrentPhase == CalibrationPhase.Complete)
                return;

            // Apply smoothing if available
            PoseData smoothedPose = poseData;
            if (_jointSmoother != null)
            {
                smoothedPose = _jointSmoother.SmoothPose(poseData);
            }

            switch (CurrentPhase)
            {
                case CalibrationPhase.Positioning:
                    HandlePositioningPhase(smoothedPose);
                    break;

                case CalibrationPhase.WaitingForTPose:
                    HandleWaitingForTPose(smoothedPose);
                    break;

                case CalibrationPhase.HoldingTPose:
                    HandleHoldingTPose(smoothedPose);
                    break;

                case CalibrationPhase.Processing:
                    HandleProcessingPhase(smoothedPose);
                    break;
            }
        }

        /// <summary>
        /// Handle the positioning phase: ensure the player is visible in frame.
        /// </summary>
        private void HandlePositioningPhase(PoseData poseData)
        {
            if (poseData == null || !poseData.IsDetected)
                return;

            // Check if we have enough valid joints visible
            if (poseData.Skeleton.GetValidJointCount() >= PoseSkeleton.MinValidJoints &&
                poseData.Skeleton.OverallConfidence >= _minimumPoseConfidence)
            {
                // Player is visible - transition to waiting for T-pose
                CurrentPhase = CalibrationPhase.WaitingForTPose;

                if (_calibrationUI != null)
                {
                    _calibrationUI.ShowTPoseInstructions();
                }

                Debug.Log("[CalibrationManager] Player detected. Waiting for T-pose...");
            }
        }

        /// <summary>
        /// Handle waiting for the T-pose to be detected.
        /// </summary>
        private void HandleWaitingForTPose(PoseData poseData)
        {
            if (_tposeDetector == null)
                return;

            bool tposeDetected = _tposeDetector.AnalyzePose(poseData);

            if (_tposeDetector.IsTPoseDetected && !_tposeDetector.IsTPoseHeld)
            {
                // T-pose started but not held long enough yet
                CurrentPhase = CalibrationPhase.HoldingTPose;
                Progress = _tposeDetector.HoldProgress;

                if (_calibrationUI != null)
                {
                    _calibrationUI.ShowHoldProgress(_tposeDetector.HoldProgress);
                }
            }

            if (tposeDetected)
            {
                OnTPoseConfirmed(poseData);
            }
        }

        /// <summary>
        /// Handle the holding phase while the T-pose is being maintained.
        /// </summary>
        private void HandleHoldingTPose(PoseData poseData)
        {
            if (_tposeDetector == null)
                return;

            bool tposeHeld = _tposeDetector.AnalyzePose(poseData);
            Progress = _tposeDetector.HoldProgress;

            if (_calibrationUI != null)
            {
                _calibrationUI.ShowHoldProgress(_tposeDetector.HoldProgress);
            }

            if (!_tposeDetector.IsTPoseDetected)
            {
                // T-pose lost - go back to waiting
                CurrentPhase = CalibrationPhase.WaitingForTPose;
                Progress = 0f;

                if (_calibrationUI != null)
                {
                    _calibrationUI.ShowTPoseInstructions();
                }

                Debug.Log("[CalibrationManager] T-pose lost. Waiting for T-pose again...");
                return;
            }

            if (tposeHeld)
            {
                OnTPoseConfirmed(poseData);
            }
        }

        /// <summary>
        /// Process the confirmed T-pose and capture calibration data.
        /// </summary>
        private void HandleProcessingPhase(PoseData poseData)
        {
            // Accumulate additional frames for averaging
            _capturedFrameCount++;

            if (_capturedFrameCount >= _averagingFrameCount)
            {
                FinalizeCalibration();
            }
        }

        /// <summary>
        /// Called when T-pose has been detected and held for the required duration.
        /// </summary>
        private void OnTPoseConfirmed(PoseData poseData)
        {
            Debug.Log("[CalibrationManager] T-pose confirmed! Processing calibration data...");
            CurrentPhase = CalibrationPhase.Processing;

            // Extract calibration data from the T-pose
            if (_tposeDetector != null)
            {
                CapturedData = _tposeDetector.ExtractCalibrationData(poseData);
            }

            if (_calibrationUI != null)
            {
                _calibrationUI.ShowProcessing();
            }

            // Start averaging frames
            _capturedFrameCount = 0;

            // If we got good data on the first pass, finalize immediately
            if (CapturedData != null && CapturedData.IsValid)
            {
                FinalizeCalibration();
            }
        }

        /// <summary>
        /// Finalize the calibration process and publish completion event.
        /// </summary>
        private void FinalizeCalibration()
        {
            if (CapturedData == null || !CapturedData.IsValid)
            {
                OnCalibrationFailed("Failed to capture valid calibration data.");
                return;
            }

            IsCalibrated = true;
            CurrentPhase = CalibrationPhase.Complete;
            Progress = 1f;

            // Publish calibration complete event
            EventBus.Publish(new CalibrationCompleteEvent
            {
                Success = true,
                PitchPosition = Vector3.zero,
                PitchRotation = Quaternion.identity,
                PitchScale = CapturedData.BodyScaleFactor
            });

            if (_calibrationUI != null)
            {
                _calibrationUI.ShowComplete();
            }

            Debug.Log($"[CalibrationManager] Calibration complete! " +
                     $"Shoulder width: {CapturedData.ShoulderWidth:F3}, " +
                     $"Avg arm length: {CapturedData.GetAverageArmLength():F3}, " +
                     $"Scale factor: {CapturedData.BodyScaleFactor:F3}");
        }

        /// <summary>
        /// Handle calibration failure.
        /// </summary>
        private void OnCalibrationFailed(string reason)
        {
            Debug.LogWarning($"[CalibrationManager] Calibration failed: {reason}");
            CurrentPhase = CalibrationPhase.Failed;
            Progress = 0f;

            EventBus.Publish(new CalibrationCompleteEvent
            {
                Success = false,
                PitchPosition = Vector3.zero,
                PitchRotation = Quaternion.identity,
                PitchScale = 1f
            });

            if (_calibrationUI != null)
            {
                _calibrationUI.ShowFailed(reason);
            }
        }

        /// <summary>
        /// Handle tracking lost during calibration.
        /// </summary>
        private void OnTrackingLost()
        {
            if (CurrentPhase == CalibrationPhase.HoldingTPose ||
                CurrentPhase == CalibrationPhase.WaitingForTPose)
            {
                CurrentPhase = CalibrationPhase.Positioning;
                Progress = 0f;

                if (_tposeDetector != null)
                {
                    _tposeDetector.ResetDetection();
                }

                if (_calibrationUI != null)
                {
                    _calibrationUI.ShowPositioningInstructions();
                }

                Debug.Log("[CalibrationManager] Tracking lost. Waiting for player to be visible again...");
            }
        }
    }
}
