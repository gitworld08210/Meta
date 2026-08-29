using System;
using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.Camera
{
    /// <summary>
    /// Replay system that records the last delivery for slow-motion playback
    /// with cinematic camera angles. Triggered on boundaries and wickets.
    /// </summary>
    public class ReplayCamera : MonoBehaviour
    {
        /// <summary>
        /// Stores a single frame of replay data.
        /// </summary>
        [Serializable]
        private struct ReplayFrame
        {
            public Vector3 BallPosition;
            public Quaternion BallRotation;
            public Vector3 BatPosition;
            public Quaternion BatRotation;
            public float Timestamp;
        }

        [Header("Recording")]
        [SerializeField] private Transform _ballTransform;
        [SerializeField] private Transform _batTransform;
        [SerializeField] private float _recordingRate = 60f;
        [SerializeField] private float _maxRecordingDuration = 5f;

        [Header("Playback")]
        [SerializeField] private float _slowMotionScale = 0.3f;
        [SerializeField] private float _replayDuration = 4f;
        [SerializeField] private float _transitionInDuration = 0.5f;
        [SerializeField] private float _transitionOutDuration = 0.5f;

        [Header("Camera Angles")]
        [SerializeField] private Transform[] _cinematicAngles;
        [SerializeField] private float _angleSwitchInterval = 1.5f;
        [SerializeField] private float _cameraMovementSpeed = 2f;

        [Header("Visual Settings")]
        [SerializeField] private float _replayVignette = 0.5f;
        [SerializeField] private bool _showReplayBanner = true;

        private List<ReplayFrame> _frameBuffer = new List<ReplayFrame>();
        private bool _isRecording;
        private bool _isPlayingReplay;
        private float _recordTimer;
        private float _playbackTimer;
        private int _currentAngleIndex;
        private float _angleSwitchTimer;
        private Action _onReplayComplete;

        /// <summary>
        /// Whether the replay system is currently playing back.
        /// </summary>
        public bool IsPlaying => _isPlayingReplay;

        /// <summary>
        /// Whether the system is actively recording frames.
        /// </summary>
        public bool IsRecording => _isRecording;

        private void Update()
        {
            if (_isRecording)
            {
                RecordFrame();
            }

            if (_isPlayingReplay)
            {
                UpdatePlayback();
            }
        }

        /// <summary>
        /// Start recording frames for replay.
        /// </summary>
        public void StartRecording()
        {
            _frameBuffer.Clear();
            _isRecording = true;
            _recordTimer = 0f;
            Debug.Log("[ReplayCamera] Recording started");
        }

        /// <summary>
        /// Stop recording frames.
        /// </summary>
        public void StopRecording()
        {
            _isRecording = false;
            Debug.Log($"[ReplayCamera] Recording stopped. {_frameBuffer.Count} frames captured.");
        }

        /// <summary>
        /// Play the recorded replay with slow-motion and cinematic angles.
        /// </summary>
        /// <param name="onComplete">Callback when replay finishes.</param>
        public void PlayReplay(Action onComplete = null)
        {
            if (_frameBuffer.Count == 0)
            {
                Debug.LogWarning("[ReplayCamera] No frames recorded for replay.");
                onComplete?.Invoke();
                return;
            }

            _onReplayComplete = onComplete;
            _isPlayingReplay = true;
            _playbackTimer = 0f;
            _currentAngleIndex = 0;
            _angleSwitchTimer = 0f;

            // Apply slow motion
            Time.timeScale = _slowMotionScale;

            Debug.Log($"[ReplayCamera] Playing replay: {_frameBuffer.Count} frames at {_slowMotionScale}x speed");
        }

        /// <summary>
        /// Stop the replay and restore normal time.
        /// </summary>
        public void StopReplay()
        {
            _isPlayingReplay = false;
            Time.timeScale = 1f;

            _onReplayComplete?.Invoke();
            _onReplayComplete = null;

            Debug.Log("[ReplayCamera] Replay stopped");
        }

        private void RecordFrame()
        {
            _recordTimer += Time.deltaTime;

            // Cap recording duration
            if (_recordTimer > _maxRecordingDuration)
            {
                // Remove oldest frames to keep buffer manageable
                if (_frameBuffer.Count > 0)
                {
                    _frameBuffer.RemoveAt(0);
                }
            }

            // Record at specified rate
            if (_recordTimer >= 1f / _recordingRate)
            {
                _recordTimer = 0f;

                ReplayFrame frame = new ReplayFrame
                {
                    BallPosition = _ballTransform != null ? _ballTransform.position : Vector3.zero,
                    BallRotation = _ballTransform != null ? _ballTransform.rotation : Quaternion.identity,
                    BatPosition = _batTransform != null ? _batTransform.position : Vector3.zero,
                    BatRotation = _batTransform != null ? _batTransform.rotation : Quaternion.identity,
                    Timestamp = Time.time
                };

                _frameBuffer.Add(frame);
            }
        }

        private void UpdatePlayback()
        {
            _playbackTimer += Time.unscaledDeltaTime;

            float totalDuration = _replayDuration / _slowMotionScale;

            if (_playbackTimer >= totalDuration)
            {
                StopReplay();
                return;
            }

            // Calculate which frame to show
            float normalizedTime = _playbackTimer / totalDuration;
            int frameIndex = Mathf.FloorToInt(normalizedTime * (_frameBuffer.Count - 1));
            frameIndex = Mathf.Clamp(frameIndex, 0, _frameBuffer.Count - 1);

            // Apply recorded positions
            ApplyFrame(_frameBuffer[frameIndex]);

            // Switch camera angles periodically
            UpdateCameraAngle();
        }

        private void ApplyFrame(ReplayFrame frame)
        {
            if (_ballTransform != null)
            {
                _ballTransform.position = frame.BallPosition;
                _ballTransform.rotation = frame.BallRotation;
            }

            if (_batTransform != null)
            {
                _batTransform.position = frame.BatPosition;
                _batTransform.rotation = frame.BatRotation;
            }
        }

        private void UpdateCameraAngle()
        {
            if (_cinematicAngles == null || _cinematicAngles.Length == 0) return;

            _angleSwitchTimer += Time.unscaledDeltaTime;

            if (_angleSwitchTimer >= _angleSwitchInterval)
            {
                _angleSwitchTimer = 0f;
                _currentAngleIndex = (_currentAngleIndex + 1) % _cinematicAngles.Length;
            }

            // Smoothly move camera towards current angle
            Transform targetAngle = _cinematicAngles[_currentAngleIndex];
            if (targetAngle != null)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetAngle.position,
                    _cameraMovementSpeed * Time.unscaledDeltaTime
                );
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetAngle.rotation,
                    _cameraMovementSpeed * Time.unscaledDeltaTime
                );
            }
        }

        /// <summary>
        /// Check if there is a valid replay available.
        /// </summary>
        public bool HasReplay()
        {
            return _frameBuffer.Count > 0;
        }

        /// <summary>
        /// Clear the replay buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _frameBuffer.Clear();
        }
    }
}
