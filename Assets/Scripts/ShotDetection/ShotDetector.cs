using System.Collections.Generic;
using UnityEngine;
using MetaCricket.MotionDetection;
using MetaCricket.Calibration;

namespace MetaCricket.ShotDetection
{
    /// <summary>
    /// Main MonoBehaviour for shot detection: buffers recent pose frames (last 30 frames),
    /// detects swing initiation (wrist velocity exceeds threshold), captures the swing window,
    /// and passes data to ShotClassifier for classification.
    /// </summary>
    public class ShotDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("The pose provider for motion input.")]
        private PoseProvider _poseProvider;

        [SerializeField]
        [Tooltip("The joint smoother for noise reduction.")]
        private JointSmoother _jointSmoother;

        [SerializeField]
        [Tooltip("The shot classifier for classification.")]
        private ShotClassifier _shotClassifier;

        [SerializeField]
        [Tooltip("The timing window manager.")]
        private TimingWindow _timingWindow;

        [Header("Detection Parameters")]
        [SerializeField]
        [Tooltip("Number of frames to buffer for analysis.")]
        private int _frameBufferSize = 30;

        [SerializeField]
        [Tooltip("Wrist velocity threshold to initiate swing detection (normalized units/second).")]
        private float _swingInitiationThreshold = 0.4f;

        [SerializeField]
        [Tooltip("Wrist velocity threshold to end swing detection.")]
        private float _swingEndThreshold = 0.1f;

        [SerializeField]
        [Tooltip("Maximum swing duration before forced termination (seconds).")]
        private float _maxSwingDuration = 1.0f;

        [SerializeField]
        [Tooltip("Minimum swing duration for a valid shot (seconds).")]
        private float _minSwingDuration = 0.1f;

        [SerializeField]
        [Tooltip("Cooldown between shot detections (seconds).")]
        private float _detectionCooldown = 0.5f;

        /// <summary>
        /// Whether shot detection is currently active.
        /// </summary>
        public bool IsDetectionActive { get; private set; }

        /// <summary>
        /// Whether a swing is currently being captured.
        /// </summary>
        public bool IsCapturingSwing { get; private set; }

        /// <summary>
        /// The most recently detected shot result.
        /// </summary>
        public ShotResult LastShotResult { get; private set; }

        /// <summary>
        /// Reference to the calibration data for normalization.
        /// </summary>
        public CalibrationData CalibrationData { get; set; }

        // Internal state
        private Queue<PoseData> _frameBuffer;
        private List<PoseData> _swingFrames;
        private float _swingStartTime;
        private float _lastDetectionTime;
        private Vector2 _previousWristPosition;
        private bool _hasPreviousWristPosition;

        private void Awake()
        {
            _frameBuffer = new Queue<PoseData>();
            _swingFrames = new List<PoseData>();
            IsDetectionActive = false;
            IsCapturingSwing = false;
            _hasPreviousWristPosition = false;
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
        /// Enable shot detection.
        /// </summary>
        public void EnableDetection()
        {
            IsDetectionActive = true;
            _frameBuffer.Clear();
            _swingFrames.Clear();
            _hasPreviousWristPosition = false;
            IsCapturingSwing = false;
            Debug.Log("[ShotDetector] Detection enabled.");
        }

        /// <summary>
        /// Disable shot detection.
        /// </summary>
        public void DisableDetection()
        {
            IsDetectionActive = false;
            IsCapturingSwing = false;
            Debug.Log("[ShotDetector] Detection disabled.");
        }

        /// <summary>
        /// Set the pose provider at runtime.
        /// </summary>
        public void SetPoseProvider(PoseProvider provider)
        {
            if (_poseProvider != null)
            {
                _poseProvider.OnPoseUpdated -= OnPoseUpdated;
                _poseProvider.OnTrackingLost -= OnTrackingLost;
            }

            _poseProvider = provider;

            if (_poseProvider != null)
            {
                _poseProvider.OnPoseUpdated += OnPoseUpdated;
                _poseProvider.OnTrackingLost += OnTrackingLost;
            }
        }

        /// <summary>
        /// Handle new pose data: buffer frames and detect swing initiation.
        /// </summary>
        private void OnPoseUpdated(PoseData poseData)
        {
            if (!IsDetectionActive)
                return;

            // Apply smoothing if available
            PoseData smoothedPose = poseData;
            if (_jointSmoother != null)
            {
                smoothedPose = _jointSmoother.SmoothPose(poseData);
            }

            // Add to frame buffer
            AddToBuffer(smoothedPose);

            // Process swing detection
            if (IsCapturingSwing)
            {
                ProcessSwingFrame(smoothedPose);
            }
            else
            {
                DetectSwingInitiation(smoothedPose);
            }
        }

        /// <summary>
        /// Handle tracking lost events.
        /// </summary>
        private void OnTrackingLost()
        {
            if (IsCapturingSwing)
            {
                // Abort current swing capture
                IsCapturingSwing = false;
                _swingFrames.Clear();
                Debug.Log("[ShotDetector] Swing capture aborted - tracking lost.");
            }

            _hasPreviousWristPosition = false;
        }

        /// <summary>
        /// Add a frame to the circular buffer.
        /// </summary>
        private void AddToBuffer(PoseData poseData)
        {
            _frameBuffer.Enqueue(poseData);

            while (_frameBuffer.Count > _frameBufferSize)
            {
                _frameBuffer.Dequeue();
            }
        }

        /// <summary>
        /// Detect if a swing is being initiated by checking wrist velocity.
        /// </summary>
        private void DetectSwingInitiation(PoseData poseData)
        {
            if (poseData == null || !poseData.IsDetected)
                return;

            // Check cooldown
            if (Time.time - _lastDetectionTime < _detectionCooldown)
                return;

            // Get dominant wrist position (right wrist)
            PoseJoint rightWrist = poseData.Skeleton.GetJoint(JointType.RightWrist);
            if (!rightWrist.IsValid)
                return;

            Vector2 currentWristPosition = rightWrist.Position;

            if (!_hasPreviousWristPosition)
            {
                _previousWristPosition = currentWristPosition;
                _hasPreviousWristPosition = true;
                return;
            }

            // Calculate wrist velocity
            float wristVelocity = 0f;
            if (poseData.DeltaTime > 0)
            {
                float distance = Vector2.Distance(currentWristPosition, _previousWristPosition);
                wristVelocity = distance / poseData.DeltaTime;
            }

            // Apply calibration scaling if available
            if (CalibrationData != null && CalibrationData.IsValid)
            {
                wristVelocity *= CalibrationData.BodyScaleFactor;
            }

            // Check if velocity exceeds initiation threshold
            if (wristVelocity >= _swingInitiationThreshold)
            {
                StartSwingCapture(poseData);
            }

            _previousWristPosition = currentWristPosition;
        }

        /// <summary>
        /// Start capturing swing frames.
        /// </summary>
        private void StartSwingCapture(PoseData initiationFrame)
        {
            IsCapturingSwing = true;
            _swingStartTime = Time.time;
            _swingFrames.Clear();

            // Include a few buffered frames before initiation for context
            PoseData[] bufferedFrames = _frameBuffer.ToArray();
            int preSwingFrames = (int)Mathf.Min(5, bufferedFrames.Length);
            for (int i = bufferedFrames.Length - preSwingFrames; i < bufferedFrames.Length; i++)
            {
                _swingFrames.Add(bufferedFrames[i]);
            }

            Debug.Log("[ShotDetector] Swing initiation detected. Capturing...");
        }

        /// <summary>
        /// Process a frame during active swing capture.
        /// </summary>
        private void ProcessSwingFrame(PoseData poseData)
        {
            _swingFrames.Add(poseData);

            // Check termination conditions
            float swingDuration = Time.time - _swingStartTime;

            // Max duration exceeded
            if (swingDuration >= _maxSwingDuration)
            {
                EndSwingCapture();
                return;
            }

            // Check if swing has ended (velocity dropped below threshold)
            if (swingDuration >= _minSwingDuration)
            {
                PoseJoint rightWrist = poseData.Skeleton.GetJoint(JointType.RightWrist);
                if (rightWrist.IsValid && _hasPreviousWristPosition)
                {
                    float distance = Vector2.Distance(rightWrist.Position, _previousWristPosition);
                    float velocity = poseData.DeltaTime > 0 ? distance / poseData.DeltaTime : 0f;

                    if (velocity < _swingEndThreshold)
                    {
                        EndSwingCapture();
                        return;
                    }

                    _previousWristPosition = rightWrist.Position;
                }
            }
        }

        /// <summary>
        /// End swing capture and classify the shot.
        /// </summary>
        private void EndSwingCapture()
        {
            IsCapturingSwing = false;
            _lastDetectionTime = Time.time;

            float swingDuration = Time.time - _swingStartTime;

            // Validate minimum duration
            if (swingDuration < _minSwingDuration || _swingFrames.Count < 3)
            {
                Debug.Log("[ShotDetector] Swing too short, discarding.");
                _swingFrames.Clear();
                return;
            }

            // Create SwingData from captured frames
            SwingData swingData = CreateSwingData();

            // Classify the shot
            if (_shotClassifier != null)
            {
                ShotResult result = _shotClassifier.ClassifyShot(swingData);

                // Apply timing from TimingWindow
                if (_timingWindow != null && _timingWindow.IsWindowActive)
                {
                    result.Timing = _timingWindow.EvaluateTiming();
                }

                LastShotResult = result;

                Debug.Log($"[ShotDetector] Shot classified: {result}");
            }

            _swingFrames.Clear();
        }

        /// <summary>
        /// Create SwingData from the captured frames.
        /// </summary>
        private SwingData CreateSwingData()
        {
            SwingData swingData = new SwingData
            {
                Frames = new List<PoseData>(_swingFrames),
                StartTime = _swingStartTime,
                EndTime = Time.time
            };

            // Calculate arm angle at the estimated impact point (middle of swing)
            int impactIndex = _swingFrames.Count / 2;
            if (impactIndex < _swingFrames.Count && _swingFrames[impactIndex] != null
                && _swingFrames[impactIndex].IsDetected)
            {
                PoseData impactFrame = _swingFrames[impactIndex];
                PoseJoint shoulder = impactFrame.Skeleton.GetJoint(JointType.RightShoulder);
                PoseJoint elbow = impactFrame.Skeleton.GetJoint(JointType.RightElbow);
                PoseJoint wrist = impactFrame.Skeleton.GetJoint(JointType.RightWrist);

                if (shoulder.IsValid && elbow.IsValid && wrist.IsValid)
                {
                    // Calculate arm angle (angle of upper arm from vertical)
                    Vector2 upperArm = elbow.Position - shoulder.Position;
                    float armAngle = Mathf.Abs(Mathf.Atan2(upperArm.x, upperArm.y) * Mathf.Rad2Deg);
                    swingData.ArmAngleAtImpact = armAngle;
                    swingData.ImpactPoint = wrist.Position;
                }
            }

            // Determine front foot/back foot from hip positions
            if (_swingFrames.Count > 0 && _swingFrames[0] != null && _swingFrames[0].IsDetected)
            {
                PoseJoint leftHip = _swingFrames[0].Skeleton.GetJoint(JointType.LeftHip);
                PoseJoint rightHip = _swingFrames[0].Skeleton.GetJoint(JointType.RightHip);

                if (leftHip.IsValid && rightHip.IsValid)
                {
                    // If left hip moved forward (lower Y in screen space), front foot
                    // This is a simplified heuristic
                    float hipMovement = 0f;
                    if (_swingFrames.Count > 2)
                    {
                        PoseData laterFrame = _swingFrames[_swingFrames.Count / 3];
                        if (laterFrame != null && laterFrame.IsDetected)
                        {
                            PoseJoint laterLeftHip = laterFrame.Skeleton.GetJoint(JointType.LeftHip);
                            if (laterLeftHip.IsValid)
                            {
                                hipMovement = leftHip.Position.y - laterLeftHip.Position.y;
                            }
                        }
                    }
                    swingData.IsFrontFoot = hipMovement > 0.01f;
                }
            }

            return swingData;
        }

        private void Update()
        {
            // Timeout active swing captures
            if (IsCapturingSwing)
            {
                float swingDuration = Time.time - _swingStartTime;
                if (swingDuration >= _maxSwingDuration)
                {
                    EndSwingCapture();
                }
            }
        }
    }
}
