using System.Collections.Generic;
using UnityEngine;

namespace MetaCricket.MotionDetection
{
    /// <summary>
    /// Applies exponential moving average (EMA) and Kalman-like filtering
    /// to reduce jitter in joint positions. Configurable smoothing factor per joint.
    /// </summary>
    public class JointSmoother : MonoBehaviour
    {
        [Header("Smoothing Configuration")]
        [SerializeField]
        [Tooltip("Default EMA smoothing factor (0 = no smoothing, 1 = infinite smoothing).")]
        [Range(0f, 0.99f)]
        private float _defaultSmoothingFactor = 0.5f;

        [SerializeField]
        [Tooltip("Kalman filter process noise (Q). Higher values trust measurements more.")]
        private float _processNoise = 0.01f;

        [SerializeField]
        [Tooltip("Kalman filter measurement noise (R). Higher values trust predictions more.")]
        private float _measurementNoise = 0.1f;

        [SerializeField]
        [Tooltip("Enable velocity-based prediction for smoother tracking.")]
        private bool _enableVelocityPrediction = true;

        [SerializeField]
        [Tooltip("Maximum velocity threshold. Movements exceeding this are considered teleports.")]
        private float _maxVelocityThreshold = 2.0f;

        /// <summary>
        /// Per-joint smoothing factor overrides.
        /// </summary>
        private Dictionary<JointType, float> _perJointSmoothingFactors;

        /// <summary>
        /// Previous smoothed positions for EMA calculation.
        /// </summary>
        private Dictionary<JointType, Vector2> _previousPositions;

        /// <summary>
        /// Previous velocities for prediction.
        /// </summary>
        private Dictionary<JointType, Vector2> _velocities;

        /// <summary>
        /// Kalman filter state per joint.
        /// </summary>
        private Dictionary<JointType, KalmanState> _kalmanStates;

        private void Awake()
        {
            _previousPositions = new Dictionary<JointType, Vector2>();
            _velocities = new Dictionary<JointType, Vector2>();
            _kalmanStates = new Dictionary<JointType, KalmanState>();
            _perJointSmoothingFactors = new Dictionary<JointType, float>();
        }

        /// <summary>
        /// Set a custom smoothing factor for a specific joint type.
        /// </summary>
        /// <param name="jointType">The joint to configure.</param>
        /// <param name="smoothingFactor">Smoothing factor between 0 and 1.</param>
        public void SetJointSmoothingFactor(JointType jointType, float smoothingFactor)
        {
            _perJointSmoothingFactors[jointType] = Mathf.Clamp01(smoothingFactor);
        }

        /// <summary>
        /// Get the smoothing factor for a specific joint (uses default if not overridden).
        /// </summary>
        private float GetSmoothingFactor(JointType jointType)
        {
            if (_perJointSmoothingFactors.TryGetValue(jointType, out float factor))
            {
                return factor;
            }
            return _defaultSmoothingFactor;
        }

        /// <summary>
        /// Apply smoothing to a complete PoseData and return a smoothed copy.
        /// </summary>
        /// <param name="rawPose">The raw, unsmoothed pose data.</param>
        /// <returns>A new PoseData with smoothed joint positions.</returns>
        public PoseData SmoothPose(PoseData rawPose)
        {
            if (rawPose == null || !rawPose.IsDetected)
                return rawPose;

            PoseSkeleton smoothedSkeleton = new PoseSkeleton();
            smoothedSkeleton.Timestamp = rawPose.Skeleton.Timestamp;

            foreach (var kvp in rawPose.Skeleton.Joints)
            {
                JointType jointType = kvp.Key;
                PoseJoint rawJoint = kvp.Value;

                Vector2 smoothedPosition = SmoothJointPosition(jointType, rawJoint.Position, rawPose.DeltaTime);

                PoseJoint smoothedJoint = new PoseJoint(
                    jointType,
                    smoothedPosition,
                    rawJoint.Confidence
                );

                smoothedSkeleton.SetJoint(smoothedJoint);
            }

            smoothedSkeleton.RecalculateOverallConfidence();
            return new PoseData(smoothedSkeleton, rawPose.FrameNumber, rawPose.DeltaTime);
        }

        /// <summary>
        /// Smooth a single joint position using combined EMA and Kalman filtering.
        /// </summary>
        /// <param name="jointType">The type of joint being smoothed.</param>
        /// <param name="rawPosition">The raw measured position.</param>
        /// <param name="deltaTime">Time since last update.</param>
        /// <returns>The smoothed position.</returns>
        public Vector2 SmoothJointPosition(JointType jointType, Vector2 rawPosition, float deltaTime)
        {
            // First frame for this joint - initialize and return raw position
            if (!_previousPositions.ContainsKey(jointType))
            {
                _previousPositions[jointType] = rawPosition;
                _velocities[jointType] = Vector2.zero;
                _kalmanStates[jointType] = new KalmanState(rawPosition);
                return rawPosition;
            }

            Vector2 previousPosition = _previousPositions[jointType];

            // Check for teleport (sudden large movement) - skip smoothing
            Vector2 delta = rawPosition - previousPosition;
            float velocity = deltaTime > 0 ? delta.magnitude / deltaTime : 0f;

            if (velocity > _maxVelocityThreshold)
            {
                // Teleport detected - reset smoothing state
                _previousPositions[jointType] = rawPosition;
                _velocities[jointType] = Vector2.zero;
                _kalmanStates[jointType] = new KalmanState(rawPosition);
                return rawPosition;
            }

            // Apply Kalman filtering
            Vector2 kalmanFiltered = ApplyKalmanFilter(jointType, rawPosition);

            // Apply EMA smoothing on top of Kalman output
            float smoothingFactor = GetSmoothingFactor(jointType);
            Vector2 emaSmoothed = Vector2.Lerp(kalmanFiltered, previousPosition, smoothingFactor);

            // Apply velocity prediction for smoother tracking
            if (_enableVelocityPrediction && deltaTime > 0)
            {
                Vector2 currentVelocity = (emaSmoothed - previousPosition) / deltaTime;
                _velocities[jointType] = Vector2.Lerp(_velocities[jointType], currentVelocity, 0.3f);
            }

            _previousPositions[jointType] = emaSmoothed;
            return emaSmoothed;
        }

        /// <summary>
        /// Apply Kalman filter to a joint position measurement.
        /// </summary>
        private Vector2 ApplyKalmanFilter(JointType jointType, Vector2 measurement)
        {
            KalmanState state = _kalmanStates[jointType];

            // Predict step
            // State prediction (assume constant position model)
            Vector2 predictedState = state.Estimate;
            float predictedCovariance = state.ErrorCovariance + _processNoise;

            // Update step
            float kalmanGain = predictedCovariance / (predictedCovariance + _measurementNoise);
            Vector2 updatedEstimate = predictedState + kalmanGain * (measurement - predictedState);
            float updatedCovariance = (1f - kalmanGain) * predictedCovariance;

            // Store updated state
            state.Estimate = updatedEstimate;
            state.ErrorCovariance = updatedCovariance;
            _kalmanStates[jointType] = state;

            return updatedEstimate;
        }

        /// <summary>
        /// Get the velocity of a specific joint (useful for motion analysis).
        /// </summary>
        /// <param name="jointType">The joint to get velocity for.</param>
        /// <returns>The velocity vector of the joint.</returns>
        public Vector2 GetJointVelocity(JointType jointType)
        {
            if (_velocities.TryGetValue(jointType, out Vector2 velocity))
            {
                return velocity;
            }
            return Vector2.zero;
        }

        /// <summary>
        /// Reset all smoothing state. Call when tracking is lost and reacquired.
        /// </summary>
        public void Reset()
        {
            _previousPositions.Clear();
            _velocities.Clear();
            _kalmanStates.Clear();
        }

        /// <summary>
        /// Internal Kalman filter state per joint.
        /// </summary>
        private struct KalmanState
        {
            public Vector2 Estimate;
            public float ErrorCovariance;

            public KalmanState(Vector2 initialEstimate)
            {
                Estimate = initialEstimate;
                ErrorCovariance = 1f;
            }
        }
    }
}
