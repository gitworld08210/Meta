using UnityEngine;

namespace MetaCricket.Camera
{
    /// <summary>
    /// Screen shake effect with configurable intensity, duration, and frequency.
    /// Triggered on big hits and wickets for impactful feedback.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Header("Default Settings")]
        [SerializeField] private float _defaultIntensity = 0.3f;
        [SerializeField] private float _defaultDuration = 0.3f;
        [SerializeField] private float _defaultFrequency = 25f;

        [Header("Constraints")]
        [SerializeField] private float _maxIntensity = 1f;
        [SerializeField] private float _maxDuration = 2f;
        [SerializeField] private bool _shakePosition = true;
        [SerializeField] private bool _shakeRotation = true;
        [SerializeField] private float _rotationMultiplier = 0.5f;

        [Header("Dampening")]
        [SerializeField] private AnimationCurve _shakeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private bool _useUnscaledTime = true;

        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private float _currentIntensity;
        private float _currentDuration;
        private float _currentFrequency;
        private float _shakeTimer;
        private bool _isShaking;
        private float _seed;

        /// <summary>
        /// Whether the camera is currently shaking.
        /// </summary>
        public bool IsShaking => _isShaking;

        private void Awake()
        {
            _originalPosition = transform.localPosition;
            _originalRotation = transform.localRotation;
            _seed = Random.Range(0f, 1000f);
        }

        private void Update()
        {
            if (!_isShaking) return;

            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _shakeTimer += deltaTime;

            if (_shakeTimer >= _currentDuration)
            {
                StopShake();
                return;
            }

            float progress = _shakeTimer / _currentDuration;
            float dampening = _shakeCurve.Evaluate(progress);
            float currentShakeIntensity = _currentIntensity * dampening;

            ApplyShake(currentShakeIntensity);
        }

        /// <summary>
        /// Start a camera shake with specified parameters.
        /// </summary>
        /// <param name="intensity">Shake intensity (0 to maxIntensity).</param>
        /// <param name="duration">Duration of the shake in seconds.</param>
        /// <param name="frequency">Oscillation frequency (higher = faster shaking).</param>
        public void Shake(float intensity, float duration, float frequency = 0f)
        {
            _currentIntensity = Mathf.Min(intensity, _maxIntensity);
            _currentDuration = Mathf.Min(duration, _maxDuration);
            _currentFrequency = frequency > 0f ? frequency : _defaultFrequency;
            _shakeTimer = 0f;
            _isShaking = true;

            // Store original transform if not already shaking
            if (!_isShaking)
            {
                _originalPosition = transform.localPosition;
                _originalRotation = transform.localRotation;
            }

            // New seed for varied shake pattern
            _seed = Random.Range(0f, 1000f);
        }

        /// <summary>
        /// Start a shake with default parameters.
        /// </summary>
        public void ShakeDefault()
        {
            Shake(_defaultIntensity, _defaultDuration, _defaultFrequency);
        }

        /// <summary>
        /// Trigger a light shake (e.g., for four boundaries).
        /// </summary>
        public void ShakeLight()
        {
            Shake(_defaultIntensity * 0.5f, _defaultDuration * 0.5f);
        }

        /// <summary>
        /// Trigger a heavy shake (e.g., for sixes, wickets).
        /// </summary>
        public void ShakeHeavy()
        {
            Shake(_defaultIntensity * 2f, _defaultDuration * 1.5f);
        }

        /// <summary>
        /// Immediately stop the shake and reset transform.
        /// </summary>
        public void StopShake()
        {
            _isShaking = false;
            transform.localPosition = _originalPosition;
            transform.localRotation = _originalRotation;
        }

        private void ApplyShake(float intensity)
        {
            float time = _useUnscaledTime ? Time.unscaledTime : Time.time;
            float t = time * _currentFrequency;

            if (_shakePosition)
            {
                float offsetX = (Mathf.PerlinNoise(_seed, t) * 2f - 1f) * intensity;
                float offsetY = (Mathf.PerlinNoise(_seed + 100f, t) * 2f - 1f) * intensity;
                float offsetZ = (Mathf.PerlinNoise(_seed + 200f, t) * 2f - 1f) * intensity * 0.5f;

                transform.localPosition = _originalPosition + new Vector3(offsetX, offsetY, offsetZ);
            }

            if (_shakeRotation)
            {
                float rotX = (Mathf.PerlinNoise(_seed + 300f, t) * 2f - 1f) * intensity * _rotationMultiplier;
                float rotY = (Mathf.PerlinNoise(_seed + 400f, t) * 2f - 1f) * intensity * _rotationMultiplier;
                float rotZ = (Mathf.PerlinNoise(_seed + 500f, t) * 2f - 1f) * intensity * _rotationMultiplier;

                transform.localRotation = _originalRotation * Quaternion.Euler(rotX, rotY, rotZ);
            }
        }

        private void OnDisable()
        {
            if (_isShaking)
            {
                StopShake();
            }
        }
    }
}
