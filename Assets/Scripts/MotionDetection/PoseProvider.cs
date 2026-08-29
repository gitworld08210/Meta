using System;
using UnityEngine;

namespace MetaCricket.MotionDetection
{
    /// <summary>
    /// Abstract base class defining the pose detection interface.
    /// Supports swapping between ML Kit and Sentis backends.
    /// </summary>
    public abstract class PoseProvider : MonoBehaviour
    {
        /// <summary>
        /// Event fired when a new pose is detected and available.
        /// </summary>
        public event Action<PoseData> OnPoseUpdated;

        /// <summary>
        /// Event fired when tracking is lost.
        /// </summary>
        public event Action OnTrackingLost;

        /// <summary>
        /// Whether the provider has been initialized successfully.
        /// </summary>
        public bool IsInitialized { get; protected set; }

        /// <summary>
        /// Whether the provider is currently tracking.
        /// </summary>
        public bool IsTracking { get; protected set; }

        /// <summary>
        /// The most recently detected pose data.
        /// </summary>
        public PoseData CurrentPose { get; protected set; }

        /// <summary>
        /// Target frames per second for pose detection.
        /// </summary>
        [SerializeField]
        protected int _targetFPS = 30;

        /// <summary>
        /// Minimum confidence threshold to consider a pose valid.
        /// </summary>
        [SerializeField]
        protected float _confidenceThreshold = 0.5f;

        /// <summary>
        /// Initialize the pose detection backend.
        /// </summary>
        /// <returns>True if initialization was successful.</returns>
        public abstract bool Initialize();

        /// <summary>
        /// Start tracking poses from the camera feed.
        /// </summary>
        public abstract void StartTracking();

        /// <summary>
        /// Stop tracking and release camera resources.
        /// </summary>
        public abstract void StopTracking();

        /// <summary>
        /// Get the current pose data synchronously.
        /// </summary>
        /// <returns>The most recent PoseData, or null if not available.</returns>
        public abstract PoseData GetCurrentPose();

        /// <summary>
        /// Dispose of resources used by the provider.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Invokes the OnPoseUpdated event for subscribers.
        /// </summary>
        protected void RaisePoseUpdated(PoseData poseData)
        {
            CurrentPose = poseData;
            OnPoseUpdated?.Invoke(poseData);
        }

        /// <summary>
        /// Invokes the OnTrackingLost event for subscribers.
        /// </summary>
        protected void RaiseTrackingLost()
        {
            IsTracking = false;
            OnTrackingLost?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            StopTracking();
            Dispose();
        }
    }
}
