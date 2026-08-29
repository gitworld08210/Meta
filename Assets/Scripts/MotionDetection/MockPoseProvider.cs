using System;
using UnityEngine;

namespace MetaCricket.MotionDetection
{
    /// <summary>
    /// Development mock pose provider that generates synthetic pose data
    /// for testing the shot pipeline without a real camera or ML model.
    /// Produces configurable motion patterns (idle, cover drive swing, pull shot, etc.)
    /// that exercise the downstream shot detection and classification systems.
    /// </summary>
    public class MockPoseProvider : PoseProvider
    {
        [Header("Mock Configuration")]
        [SerializeField]
        [Tooltip("The type of mock motion pattern to generate.")]
        private MockMotionPattern _motionPattern = MockMotionPattern.IdleSway;

        [SerializeField]
        [Tooltip("Speed multiplier for the mock motion.")]
        [Range(0.1f, 5f)]
        private float _motionSpeed = 1.0f;

        [SerializeField]
        [Tooltip("How often to emit poses per second.")]
        [Range(1, 60)]
        private int _emitRate = 30;

        [SerializeField]
        [Tooltip("Base confidence for generated joints (simulates detection quality).")]
        [Range(0.3f, 1.0f)]
        private float _baseConfidence = 0.85f;

        [SerializeField]
        [Tooltip("Whether to add noise to joint positions (simulates jitter).")]
        private bool _addNoise = true;

        [SerializeField]
        [Tooltip("Amount of positional noise to add.")]
        [Range(0f, 0.05f)]
        private float _noiseAmount = 0.01f;

        [SerializeField]
        [Tooltip("If true, auto-triggers a swing pattern at intervals.")]
        private bool _autoTriggerSwing = false;

        [SerializeField]
        [Tooltip("Interval between auto-triggered swings in seconds.")]
        [Range(2f, 10f)]
        private float _swingInterval = 4f;

        // Internal state
        private float _elapsedTime;
        private float _lastEmitTime;
        private float _lastSwingTriggerTime;
        private int _frameCount;
        private bool _isSwinging;
        private float _swingStartTime;
        private float _swingDuration = 0.4f;
        private MockMotionPattern _activePattern;

        // Base skeleton positions (normalized 0-1 screen space, representing a
        // right-handed batsman in batting stance)
        private static readonly Vector2 BaseNose = new Vector2(0.5f, 0.2f);
        private static readonly Vector2 BaseLeftShoulder = new Vector2(0.42f, 0.3f);
        private static readonly Vector2 BaseRightShoulder = new Vector2(0.58f, 0.3f);
        private static readonly Vector2 BaseLeftElbow = new Vector2(0.38f, 0.42f);
        private static readonly Vector2 BaseRightElbow = new Vector2(0.62f, 0.42f);
        private static readonly Vector2 BaseLeftWrist = new Vector2(0.4f, 0.52f);
        private static readonly Vector2 BaseRightWrist = new Vector2(0.6f, 0.52f);
        private static readonly Vector2 BaseLeftHip = new Vector2(0.45f, 0.55f);
        private static readonly Vector2 BaseRightHip = new Vector2(0.55f, 0.55f);

        /// <summary>
        /// Manually trigger a swing motion of the specified pattern.
        /// </summary>
        public void TriggerSwing(MockMotionPattern pattern)
        {
            _isSwinging = true;
            _swingStartTime = Time.time;
            _activePattern = pattern;
            Debug.Log($"[MockPoseProvider] Swing triggered: {pattern}");
        }

        /// <summary>
        /// Initialize the mock provider (always succeeds).
        /// </summary>
        public override bool Initialize()
        {
            IsInitialized = true;
            Debug.Log("[MockPoseProvider] Initialized (mock mode - synthetic pose data).");
            return true;
        }

        /// <summary>
        /// Start generating mock poses.
        /// </summary>
        public override void StartTracking()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[MockPoseProvider] Cannot start tracking - not initialized.");
                return;
            }

            IsTracking = true;
            _elapsedTime = 0f;
            _frameCount = 0;
            _lastEmitTime = 0f;
            _lastSwingTriggerTime = Time.time;
            _activePattern = _motionPattern;
            Debug.Log("[MockPoseProvider] Mock tracking started.");
        }

        /// <summary>
        /// Stop generating mock poses.
        /// </summary>
        public override void StopTracking()
        {
            IsTracking = false;
            _isSwinging = false;
            Debug.Log("[MockPoseProvider] Mock tracking stopped.");
        }

        /// <summary>
        /// Get the current mock pose data.
        /// </summary>
        public override PoseData GetCurrentPose()
        {
            return CurrentPose;
        }

        /// <summary>
        /// Dispose (nothing to clean up for mock).
        /// </summary>
        public override void Dispose()
        {
            IsInitialized = false;
        }

        private void Update()
        {
            if (!IsTracking)
                return;

            // Throttle emission rate
            float emitInterval = 1f / _emitRate;
            if (Time.time - _lastEmitTime < emitInterval)
                return;

            _lastEmitTime = Time.time;
            _elapsedTime += emitInterval * _motionSpeed;
            _frameCount++;

            // Auto-trigger swing if enabled
            if (_autoTriggerSwing && !_isSwinging)
            {
                if (Time.time - _lastSwingTriggerTime >= _swingInterval)
                {
                    _lastSwingTriggerTime = Time.time;
                    TriggerSwing(_motionPattern);
                }
            }

            // Check if swing is complete
            if (_isSwinging && Time.time - _swingStartTime > _swingDuration)
            {
                _isSwinging = false;
            }

            // Generate pose
            PoseData poseData = GeneratePose();

            if (poseData.IsDetected)
            {
                RaisePoseUpdated(poseData);
            }
        }

        /// <summary>
        /// Generate a complete pose frame based on the current motion pattern.
        /// </summary>
        private PoseData GeneratePose()
        {
            PoseSkeleton skeleton = new PoseSkeleton();
            skeleton.Timestamp = Time.time;

            Vector2[] positions = GetJointPositions();
            JointType[] jointTypes = {
                JointType.Nose,
                JointType.LeftShoulder,
                JointType.RightShoulder,
                JointType.LeftElbow,
                JointType.RightElbow,
                JointType.LeftWrist,
                JointType.RightWrist,
                JointType.LeftHip,
                JointType.RightHip
            };

            for (int i = 0; i < jointTypes.Length && i < positions.Length; i++)
            {
                Vector2 pos = positions[i];

                // Add noise if enabled
                if (_addNoise)
                {
                    pos += new Vector2(
                        UnityEngine.Random.Range(-_noiseAmount, _noiseAmount),
                        UnityEngine.Random.Range(-_noiseAmount, _noiseAmount)
                    );
                }

                // Vary confidence slightly per joint
                float confidence = _baseConfidence + UnityEngine.Random.Range(-0.05f, 0.05f);
                confidence = Mathf.Clamp01(confidence);

                PoseJoint joint = new PoseJoint(jointTypes[i], pos, confidence);
                skeleton.SetJoint(joint);
            }

            skeleton.RecalculateOverallConfidence();

            PoseData poseData = new PoseData(skeleton, _frameCount, 1f / _emitRate);
            return poseData;
        }

        /// <summary>
        /// Get joint positions based on current motion state.
        /// </summary>
        private Vector2[] GetJointPositions()
        {
            if (_isSwinging)
            {
                float swingProgress = Mathf.Clamp01((Time.time - _swingStartTime) / _swingDuration);
                return GetSwingPositions(_activePattern, swingProgress);
            }
            else
            {
                return GetIdlePositions();
            }
        }

        /// <summary>
        /// Generate idle stance positions with subtle sway.
        /// </summary>
        private Vector2[] GetIdlePositions()
        {
            float sway = Mathf.Sin(_elapsedTime * 2f) * 0.005f;
            float breathe = Mathf.Sin(_elapsedTime * 1.2f) * 0.003f;

            return new Vector2[]
            {
                BaseNose + new Vector2(sway, breathe),
                BaseLeftShoulder + new Vector2(sway, breathe),
                BaseRightShoulder + new Vector2(sway, breathe),
                BaseLeftElbow + new Vector2(sway * 0.8f, breathe * 0.5f),
                BaseRightElbow + new Vector2(sway * 0.8f, breathe * 0.5f),
                BaseLeftWrist + new Vector2(sway * 0.6f, 0f),
                BaseRightWrist + new Vector2(sway * 0.6f, 0f),
                BaseLeftHip + new Vector2(sway * 0.3f, 0f),
                BaseRightHip + new Vector2(sway * 0.3f, 0f)
            };
        }

        /// <summary>
        /// Generate swing positions based on pattern and progress.
        /// </summary>
        private Vector2[] GetSwingPositions(MockMotionPattern pattern, float progress)
        {
            switch (pattern)
            {
                case MockMotionPattern.CoverDriveSwing:
                    return GetCoverDrivePositions(progress);
                case MockMotionPattern.PullShotSwing:
                    return GetPullShotPositions(progress);
                case MockMotionPattern.StraightDriveSwing:
                    return GetStraightDrivePositions(progress);
                case MockMotionPattern.DefensiveBlockSwing:
                    return GetDefensiveBlockPositions(progress);
                default:
                    return GetCoverDrivePositions(progress);
            }
        }

        /// <summary>
        /// Cover drive: front foot movement, bat sweeps through off-side at 30-60 degree angle.
        /// </summary>
        private Vector2[] GetCoverDrivePositions(float t)
        {
            // Smooth ease-in-out curve
            float smoothT = t * t * (3f - 2f * t);

            // Right wrist sweeps from backlift to follow-through
            Vector2 wristStart = new Vector2(0.55f, 0.35f); // Backlift position
            Vector2 wristMid = new Vector2(0.6f, 0.5f);     // Impact
            Vector2 wristEnd = new Vector2(0.7f, 0.45f);    // Follow-through

            Vector2 rightWrist;
            if (t < 0.5f)
                rightWrist = Vector2.Lerp(wristStart, wristMid, smoothT * 2f);
            else
                rightWrist = Vector2.Lerp(wristMid, wristEnd, (smoothT - 0.5f) * 2f);

            // Elbow follows wrist
            Vector2 rightElbow = rightWrist + new Vector2(-0.06f, -0.1f);

            // Left hip moves forward (front foot stride)
            float hipShift = smoothT * 0.03f;

            return new Vector2[]
            {
                BaseNose + new Vector2(smoothT * 0.02f, 0f),
                BaseLeftShoulder + new Vector2(smoothT * 0.02f, 0f),
                BaseRightShoulder + new Vector2(smoothT * 0.03f, 0f),
                BaseLeftElbow + new Vector2(smoothT * 0.01f, -smoothT * 0.02f),
                rightElbow,
                BaseLeftWrist + new Vector2(0f, -smoothT * 0.03f),
                rightWrist,
                BaseLeftHip + new Vector2(hipShift, smoothT * 0.02f),
                BaseRightHip
            };
        }

        /// <summary>
        /// Pull shot: back foot, horizontal cross-body swing.
        /// </summary>
        private Vector2[] GetPullShotPositions(float t)
        {
            float smoothT = t * t * (3f - 2f * t);

            // Right wrist sweeps across the body (cross-body horizontal)
            Vector2 wristStart = new Vector2(0.6f, 0.38f);
            Vector2 wristMid = new Vector2(0.5f, 0.42f);
            Vector2 wristEnd = new Vector2(0.35f, 0.4f);

            Vector2 rightWrist;
            if (t < 0.4f)
                rightWrist = Vector2.Lerp(wristStart, wristMid, smoothT * 2.5f);
            else
                rightWrist = Vector2.Lerp(wristMid, wristEnd, (smoothT - 0.4f) * 1.67f);

            Vector2 rightElbow = rightWrist + new Vector2(0.04f, -0.1f);

            // Body rotates with the pull
            float bodyRotation = smoothT * 0.04f;

            return new Vector2[]
            {
                BaseNose + new Vector2(-bodyRotation, 0f),
                BaseLeftShoulder + new Vector2(-bodyRotation, 0f),
                BaseRightShoulder + new Vector2(-bodyRotation * 0.8f, 0f),
                BaseLeftElbow + new Vector2(-bodyRotation * 0.6f, 0f),
                rightElbow,
                BaseLeftWrist + new Vector2(-bodyRotation * 0.4f, 0f),
                rightWrist,
                BaseLeftHip + new Vector2(0f, -smoothT * 0.01f), // Back foot
                BaseRightHip + new Vector2(0f, -smoothT * 0.01f)
            };
        }

        /// <summary>
        /// Straight drive: vertical swing plane, bat straight down the ground.
        /// </summary>
        private Vector2[] GetStraightDrivePositions(float t)
        {
            float smoothT = t * t * (3f - 2f * t);

            // Wrist moves vertically (downswing to follow-through)
            Vector2 wristStart = new Vector2(0.58f, 0.3f);  // High backlift
            Vector2 wristMid = new Vector2(0.57f, 0.5f);    // Impact (straight)
            Vector2 wristEnd = new Vector2(0.56f, 0.35f);   // High follow-through

            Vector2 rightWrist;
            if (t < 0.5f)
                rightWrist = Vector2.Lerp(wristStart, wristMid, smoothT * 2f);
            else
                rightWrist = Vector2.Lerp(wristMid, wristEnd, (smoothT - 0.5f) * 2f);

            Vector2 rightElbow = rightWrist + new Vector2(-0.02f, -0.1f);

            float hipShift = smoothT * 0.02f;

            return new Vector2[]
            {
                BaseNose + new Vector2(0f, smoothT * 0.01f),
                BaseLeftShoulder,
                BaseRightShoulder,
                BaseLeftElbow + new Vector2(0f, -smoothT * 0.02f),
                rightElbow,
                BaseLeftWrist + new Vector2(0f, -smoothT * 0.03f),
                rightWrist,
                BaseLeftHip + new Vector2(hipShift, smoothT * 0.015f),
                BaseRightHip
            };
        }

        /// <summary>
        /// Defensive block: minimal movement, bat comes forward slightly.
        /// </summary>
        private Vector2[] GetDefensiveBlockPositions(float t)
        {
            float smoothT = t * t * (3f - 2f * t);

            // Minimal wrist movement
            Vector2 rightWrist = Vector2.Lerp(
                BaseRightWrist,
                BaseRightWrist + new Vector2(0.01f, 0.03f),
                smoothT
            );

            Vector2 rightElbow = Vector2.Lerp(
                BaseRightElbow,
                BaseRightElbow + new Vector2(0.005f, 0.02f),
                smoothT
            );

            return new Vector2[]
            {
                BaseNose,
                BaseLeftShoulder,
                BaseRightShoulder,
                BaseLeftElbow,
                rightElbow,
                BaseLeftWrist + new Vector2(0f, smoothT * 0.01f),
                rightWrist,
                BaseLeftHip + new Vector2(smoothT * 0.005f, 0f),
                BaseRightHip
            };
        }
    }

    /// <summary>
    /// Types of mock motion patterns available for testing.
    /// </summary>
    public enum MockMotionPattern
    {
        /// <summary>Subtle idle sway, no swing.</summary>
        IdleSway,
        /// <summary>Cover drive: front foot, off-side horizontal swing.</summary>
        CoverDriveSwing,
        /// <summary>Pull shot: back foot, cross-body horizontal swing.</summary>
        PullShotSwing,
        /// <summary>Straight drive: vertical swing plane.</summary>
        StraightDriveSwing,
        /// <summary>Defensive block: minimal movement.</summary>
        DefensiveBlockSwing
    }
}
