using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.VFX
{
    /// <summary>
    /// Bat-ball contact visual effect with spark particles at impact point.
    /// Intensity scales based on shot power with directional trail following the ball.
    /// </summary>
    public class BatImpactVFX : MonoBehaviour
    {
        [Header("Impact Sparks")]
        [SerializeField] private ParticleSystem _sparkParticles;
        [SerializeField] private int _minParticleCount = 5;
        [SerializeField] private int _maxParticleCount = 30;
        [SerializeField] private float _minSpeed = 2f;
        [SerializeField] private float _maxSpeed = 10f;

        [Header("Impact Colors")]
        [SerializeField] private Color _lightHitColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color _mediumHitColor = new Color(1f, 0.878f, 0.4f, 1f);
        [SerializeField] private Color _heavyHitColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _perfectHitColor = new Color(1f, 0.6f, 0f, 1f);

        [Header("Ball Trail")]
        [SerializeField] private TrailRenderer _ballTrail;
        [SerializeField] private float _trailDuration = 1f;
        [SerializeField] private float _trailWidthMultiplier = 1f;
        [SerializeField] private Gradient _trailGradient;

        [Header("Impact Light")]
        [SerializeField] private Light _impactLight;
        [SerializeField] private float _lightIntensity = 2f;
        [SerializeField] private float _lightFadeDuration = 0.3f;

        [Header("Settings")]
        [SerializeField] private float _effectLifetime = 1.5f;

        private float _currentPower;
        private bool _isActive;
        private float _lightTimer;

        /// <summary>
        /// Trigger the bat impact effect at the specified position.
        /// </summary>
        /// <param name="position">World position of the impact.</param>
        /// <param name="direction">Direction the ball travels after impact.</param>
        /// <param name="power">Shot power (0 to 1) affecting intensity.</param>
        public void TriggerImpact(Vector3 position, Vector3 direction, float power)
        {
            _currentPower = Mathf.Clamp01(power);
            _isActive = true;
            transform.position = position;

            PlaySparkEffect(direction, power);
            ActivateTrail(direction, power);
            FlashImpactLight(power);

            // Auto-deactivate after lifetime
            CancelInvoke(nameof(Deactivate));
            Invoke(nameof(Deactivate), _effectLifetime);
        }

        private void Update()
        {
            if (_isActive && _impactLight != null && _impactLight.enabled)
            {
                _lightTimer += Time.deltaTime;
                float t = _lightTimer / _lightFadeDuration;
                _impactLight.intensity = Mathf.Lerp(_lightIntensity * _currentPower, 0f, t);

                if (t >= 1f)
                {
                    _impactLight.enabled = false;
                }
            }
        }

        private void PlaySparkEffect(Vector3 direction, float power)
        {
            if (_sparkParticles == null) return;

            // Configure particle count based on power
            var emission = _sparkParticles.emission;
            int particleCount = Mathf.RoundToInt(Mathf.Lerp(_minParticleCount, _maxParticleCount, power));
            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)particleCount));

            // Configure speed based on power
            var main = _sparkParticles.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(_minSpeed, _maxSpeed * 0.7f, power),
                Mathf.Lerp(_minSpeed * 1.5f, _maxSpeed, power)
            );

            // Set color based on power level
            main.startColor = GetImpactColor(power);

            // Set direction (shape cone direction)
            var shape = _sparkParticles.shape;
            shape.rotation = Quaternion.LookRotation(direction).eulerAngles;

            _sparkParticles.Play();
        }

        private void ActivateTrail(Vector3 direction, float power)
        {
            if (_ballTrail == null) return;

            _ballTrail.time = _trailDuration * power;
            _ballTrail.widthMultiplier = _trailWidthMultiplier * Mathf.Lerp(0.5f, 1.5f, power);

            // Set trail color based on power
            if (_trailGradient == null)
            {
                Color startColor = GetImpactColor(power);
                Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(startColor, 0f),
                        new GradientColorKey(endColor, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                _ballTrail.colorGradient = gradient;
            }

            _ballTrail.emitting = true;
        }

        private void FlashImpactLight(float power)
        {
            if (_impactLight == null) return;

            _impactLight.enabled = true;
            _impactLight.color = GetImpactColor(power);
            _impactLight.intensity = _lightIntensity * power;
            _lightTimer = 0f;
        }

        private Color GetImpactColor(float power)
        {
            if (power >= 0.9f) return _perfectHitColor;
            if (power >= 0.7f) return _heavyHitColor;
            if (power >= 0.4f) return _mediumHitColor;
            return _lightHitColor;
        }

        private void Deactivate()
        {
            _isActive = false;

            if (_sparkParticles != null) _sparkParticles.Stop();
            if (_ballTrail != null) _ballTrail.emitting = false;
            if (_impactLight != null) _impactLight.enabled = false;
        }

        /// <summary>
        /// Immediately stop all effects.
        /// </summary>
        public void StopImmediate()
        {
            CancelInvoke();
            Deactivate();

            if (_ballTrail != null) _ballTrail.Clear();
        }
    }
}
