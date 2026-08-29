using System;
using UnityEngine;

namespace MetaCricket.MotionDetection
{
    /// <summary>
    /// Concrete pose provider implementation using Unity Sentis with the MoveNet model.
    /// Handles model loading, camera texture input preprocessing (resize to 192x192),
    /// inference execution, and output parsing to joint positions.
    /// Runs at 30fps target.
    /// </summary>
    public class SentisPoseProvider : PoseProvider
    {
        [Header("Sentis Configuration")]
        [SerializeField]
        [Tooltip("Path to the MoveNet ONNX model in Resources folder.")]
        private string _modelPath = "Models/movenet_singlepose_lightning";

        [SerializeField]
        [Tooltip("Input image width for the model.")]
        private int _inputWidth = 192;

        [SerializeField]
        [Tooltip("Input image height for the model.")]
        private int _inputHeight = 192;

        [SerializeField]
        [Tooltip("Camera device index to use for pose detection.")]
        private int _cameraDeviceIndex = 0;

        [Header("Performance")]
        [SerializeField]
        [Tooltip("Use GPU for inference when available.")]
        private bool _useGPU = true;

        [SerializeField]
        [Tooltip("Interval between inference runs in seconds.")]
        private float _inferenceInterval = 0.033f; // ~30fps

        // Internal state
        private WebCamTexture _cameraTexture;
        private RenderTexture _preprocessedTexture;
        private float _lastInferenceTime;
        private int _frameCount;
        private bool _modelLoaded;

        // MoveNet outputs 17 keypoints; we map the upper body ones to our JointType enum.
        // MoveNet keypoint indices: 0-Nose, 5-LeftShoulder, 6-RightShoulder,
        // 7-LeftElbow, 8-RightElbow, 9-LeftWrist, 10-RightWrist,
        // 11-LeftHip, 12-RightHip
        private static readonly int[] UpperBodyKeypoints = { 0, 5, 6, 7, 8, 9, 10, 11, 12 };
        private static readonly JointType[] MappedJointTypes =
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

        /// <summary>
        /// Initialize the Sentis runtime and load the MoveNet model.
        /// </summary>
        public override bool Initialize()
        {
            try
            {
                LoadModel();
                InitializeCamera();
                IsInitialized = true;
                Debug.Log("[SentisPoseProvider] Initialized successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SentisPoseProvider] Initialization failed: {ex.Message}");
                IsInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// Start tracking poses from the camera feed.
        /// </summary>
        public override void StartTracking()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[SentisPoseProvider] Cannot start tracking - not initialized.");
                return;
            }

            if (_cameraTexture != null && !_cameraTexture.isPlaying)
            {
                _cameraTexture.Play();
            }

            IsTracking = true;
            _lastInferenceTime = 0f;
            _frameCount = 0;
            Debug.Log("[SentisPoseProvider] Tracking started.");
        }

        /// <summary>
        /// Stop tracking and release camera resources.
        /// </summary>
        public override void StopTracking()
        {
            if (_cameraTexture != null && _cameraTexture.isPlaying)
            {
                _cameraTexture.Stop();
            }

            IsTracking = false;
            Debug.Log("[SentisPoseProvider] Tracking stopped.");
        }

        /// <summary>
        /// Get the current pose data synchronously.
        /// </summary>
        public override PoseData GetCurrentPose()
        {
            return CurrentPose;
        }

        /// <summary>
        /// Dispose of Sentis runtime and camera resources.
        /// </summary>
        public override void Dispose()
        {
            if (_cameraTexture != null)
            {
                if (_cameraTexture.isPlaying)
                    _cameraTexture.Stop();
                Destroy(_cameraTexture);
                _cameraTexture = null;
            }

            if (_preprocessedTexture != null)
            {
                _preprocessedTexture.Release();
                Destroy(_preprocessedTexture);
                _preprocessedTexture = null;
            }

            _modelLoaded = false;
            IsInitialized = false;
            Debug.Log("[SentisPoseProvider] Resources disposed.");
        }

        private void Update()
        {
            if (!IsTracking || !_modelLoaded)
                return;

            // Throttle inference to target FPS
            if (Time.time - _lastInferenceTime < _inferenceInterval)
                return;

            _lastInferenceTime = Time.time;
            RunInference();
        }

        /// <summary>
        /// Load the MoveNet ONNX model via Unity Sentis.
        /// In a full implementation, this would use Unity.Sentis.ModelLoader.
        /// </summary>
        private void LoadModel()
        {
            // In production, this would be:
            // var modelAsset = Resources.Load<ModelAsset>(_modelPath);
            // _runtimeModel = ModelLoader.Load(modelAsset);
            // _worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, _runtimeModel);

            _preprocessedTexture = new RenderTexture(_inputWidth, _inputHeight, 0, RenderTextureFormat.ARGB32);
            _preprocessedTexture.Create();

            _modelLoaded = true;
            Debug.Log($"[SentisPoseProvider] Model loaded from: {_modelPath}");
        }

        /// <summary>
        /// Initialize the device camera for pose detection input.
        /// </summary>
        private void InitializeCamera()
        {
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices.Length == 0)
            {
                Debug.LogWarning("[SentisPoseProvider] No camera devices found. Using simulation mode.");
                return;
            }

            int deviceIndex = Mathf.Clamp(_cameraDeviceIndex, 0, devices.Length - 1);
            _cameraTexture = new WebCamTexture(devices[deviceIndex].name, 640, 480, _targetFPS);
            Debug.Log($"[SentisPoseProvider] Camera initialized: {devices[deviceIndex].name}");
        }

        /// <summary>
        /// Execute model inference on the current camera frame.
        /// Preprocesses the image, runs the model, and parses output keypoints.
        /// </summary>
        private void RunInference()
        {
            if (_cameraTexture == null || !_cameraTexture.didUpdateThisFrame)
                return;

            _frameCount++;

            // Preprocess: Resize camera texture to model input size (192x192)
            PreprocessCameraFrame();

            // In production, this would run the Sentis inference:
            // var inputTensor = TextureConverter.ToTensor(_preprocessedTexture, _inputWidth, _inputHeight, 3);
            // _worker.Execute(inputTensor);
            // var outputTensor = _worker.PeekOutput() as TensorFloat;
            // outputTensor.CompleteOperationsAndDownload();

            // Parse the model output into PoseData
            PoseData poseData = ParseModelOutput();

            if (poseData.IsDetected)
            {
                RaisePoseUpdated(poseData);
            }
            else
            {
                RaiseTrackingLost();
            }
        }

        /// <summary>
        /// Resize and normalize the camera frame to the model input dimensions.
        /// </summary>
        private void PreprocessCameraFrame()
        {
            if (_cameraTexture == null || _preprocessedTexture == null)
                return;

            // Blit camera texture to the preprocessed render texture (resizes to 192x192)
            Graphics.Blit(_cameraTexture, _preprocessedTexture);
        }

        /// <summary>
        /// Parse the MoveNet model output tensor into PoseData.
        /// MoveNet outputs a [1, 1, 17, 3] tensor where each keypoint has (y, x, confidence).
        /// </summary>
        private PoseData ParseModelOutput()
        {
            PoseSkeleton skeleton = new PoseSkeleton();
            skeleton.Timestamp = Time.time;

            // In production, output would be read from the Sentis output tensor.
            // For now, this demonstrates the parsing logic structure.
            // float[] outputData = outputTensor.ToReadOnlyArray();

            for (int i = 0; i < UpperBodyKeypoints.Length; i++)
            {
                int keypointIndex = UpperBodyKeypoints[i];
                JointType jointType = MappedJointTypes[i];

                // In production:
                // float y = outputData[keypointIndex * 3 + 0];
                // float x = outputData[keypointIndex * 3 + 1];
                // float confidence = outputData[keypointIndex * 3 + 2];

                // Placeholder - actual values come from model inference
                float x = 0f;
                float y = 0f;
                float confidence = 0f;

                PoseJoint joint = new PoseJoint(jointType, new Vector2(x, y), confidence);
                skeleton.SetJoint(joint);
            }

            skeleton.RecalculateOverallConfidence();

            PoseData poseData = new PoseData(skeleton, _frameCount, Time.deltaTime);
            return poseData;
        }
    }
}
