using System;
using UnityEngine;

namespace MetaCricket.MotionDetection
{
    /// <summary>
    /// Concrete pose provider implementation for Google ML Kit Pose Detection
    /// via Android native plugin bridge. Defines the JNI interface methods
    /// for communication with the native Android pose detection library.
    /// </summary>
    public class MLKitPoseProvider : PoseProvider
    {
        [Header("ML Kit Configuration")]
        [SerializeField]
        [Tooltip("Use accurate mode (slower but more precise) vs. fast mode.")]
        private bool _useAccurateMode = false;

        [SerializeField]
        [Tooltip("Enable pose detection streaming mode for video input.")]
        private bool _useStreamingMode = true;

        // JNI class and method references
        private const string JavaClassName = "com.metacricket.mlkit.PoseDetectorBridge";
        private const string MethodInitialize = "initialize";
        private const string MethodStartDetection = "startDetection";
        private const string MethodStopDetection = "stopDetection";
        private const string MethodGetLatestPose = "getLatestPose";
        private const string MethodDispose = "dispose";
        private const string MethodSetAccurateMode = "setAccurateMode";
        private const string MethodIsAvailable = "isAvailable";

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _poseDetectorBridge;
        private AndroidJavaObject _unityActivity;
#endif

        private int _frameCount;

        /// <summary>
        /// Initialize the ML Kit pose detection native bridge.
        /// </summary>
        public override bool Initialize()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                _unityActivity = GetUnityActivity();
                _poseDetectorBridge = new AndroidJavaObject(JavaClassName, _unityActivity);
                
                bool available = _poseDetectorBridge.Call<bool>(MethodIsAvailable);
                if (!available)
                {
                    Debug.LogError("[MLKitPoseProvider] ML Kit Pose Detection is not available on this device.");
                    return false;
                }

                _poseDetectorBridge.Call(MethodSetAccurateMode, _useAccurateMode);
                bool result = _poseDetectorBridge.Call<bool>(MethodInitialize);
                
                if (result)
                {
                    IsInitialized = true;
                    Debug.Log("[MLKitPoseProvider] Initialized successfully via JNI.");
                }
                return result;
#else
                Debug.LogWarning("[MLKitPoseProvider] ML Kit is only available on Android. Using stub mode.");
                IsInitialized = true;
                return true;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MLKitPoseProvider] Initialization failed: {ex.Message}");
                IsInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// Start ML Kit pose detection from the camera feed.
        /// </summary>
        public override void StartTracking()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[MLKitPoseProvider] Cannot start tracking - not initialized.");
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            _poseDetectorBridge?.Call(MethodStartDetection, _useStreamingMode);
#endif

            IsTracking = true;
            _frameCount = 0;
            Debug.Log("[MLKitPoseProvider] Tracking started.");
        }

        /// <summary>
        /// Stop ML Kit pose detection.
        /// </summary>
        public override void StopTracking()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _poseDetectorBridge?.Call(MethodStopDetection);
#endif

            IsTracking = false;
            Debug.Log("[MLKitPoseProvider] Tracking stopped.");
        }

        /// <summary>
        /// Get the current pose from the native bridge.
        /// </summary>
        public override PoseData GetCurrentPose()
        {
            return CurrentPose;
        }

        /// <summary>
        /// Dispose native plugin resources.
        /// </summary>
        public override void Dispose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _poseDetectorBridge?.Call(MethodDispose);
            _poseDetectorBridge?.Dispose();
            _poseDetectorBridge = null;
            _unityActivity?.Dispose();
            _unityActivity = null;
#endif

            IsInitialized = false;
            Debug.Log("[MLKitPoseProvider] Resources disposed.");
        }

        private void Update()
        {
            if (!IsTracking)
                return;

            _frameCount++;
            FetchPoseFromNative();
        }

        /// <summary>
        /// Fetch the latest pose data from the native ML Kit bridge.
        /// The native side returns a float array with joint positions and confidences.
        /// Format: [y0, x0, conf0, y1, x1, conf1, ...] for 17 keypoints.
        /// </summary>
        private void FetchPoseFromNative()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            float[] rawPoseData = _poseDetectorBridge?.Call<float[]>(MethodGetLatestPose);
            
            if (rawPoseData == null || rawPoseData.Length == 0)
            {
                RaiseTrackingLost();
                return;
            }

            PoseData poseData = ParseNativePoseData(rawPoseData);
            
            if (poseData.IsDetected)
            {
                RaisePoseUpdated(poseData);
            }
            else
            {
                RaiseTrackingLost();
            }
#endif
        }

        /// <summary>
        /// Parse the raw float array from native ML Kit into PoseData.
        /// ML Kit provides 33 landmarks; we extract the upper body subset.
        /// </summary>
        private PoseData ParseNativePoseData(float[] rawData)
        {
            PoseSkeleton skeleton = new PoseSkeleton();
            skeleton.Timestamp = Time.time;

            // ML Kit landmark indices for upper body:
            // 0-Nose, 11-LeftShoulder, 12-RightShoulder,
            // 13-LeftElbow, 14-RightElbow, 15-LeftWrist, 16-RightWrist,
            // 23-LeftHip, 24-RightHip
            int[] mlKitIndices = { 0, 11, 12, 13, 14, 15, 16, 23, 24 };
            JointType[] mappedTypes =
            {
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

            for (int i = 0; i < mlKitIndices.Length; i++)
            {
                int dataIndex = mlKitIndices[i] * 3;

                if (dataIndex + 2 >= rawData.Length)
                    continue;

                float y = rawData[dataIndex];
                float x = rawData[dataIndex + 1];
                float confidence = rawData[dataIndex + 2];

                PoseJoint joint = new PoseJoint(mappedTypes[i], new Vector2(x, y), confidence);
                skeleton.SetJoint(joint);
            }

            skeleton.RecalculateOverallConfidence();
            return new PoseData(skeleton, _frameCount, Time.deltaTime);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Get the current Unity activity via JNI.
        /// </summary>
        private AndroidJavaObject GetUnityActivity()
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }
#endif
    }
}
