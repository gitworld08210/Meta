using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.VFX
{
    /// <summary>
    /// Specific boundary celebration effects.
    /// Six = gold fireworks + screen flash + camera shake.
    /// Four = blue streak + crowd animation trigger.
    /// </summary>
    public class BoundaryEffect : MonoBehaviour
    {
        [Header("Six Effect - Gold Fireworks")]
        [SerializeField] private ParticleSystem _goldFireworks;
        [SerializeField] private ParticleSystem _goldSparkles;
        [SerializeField] private float _fireworksDuration = 3f;
        [SerializeField] private int _fireworksBurstCount = 50;
        [SerializeField] private Color _fireworksColor = new Color(1f, 0.843f, 0f, 1f);

        [Header("Six Effect - Screen Flash")]
        [SerializeField] private float _flashIntensity = 0.8f;
        [SerializeField] private float _flashDuration = 0.3f;
        [SerializeField] private Color _sixFlashColor = new Color(1f, 0.878f, 0.4f, 0.5f);

        [Header("Six Effect - Camera Shake")]
        [SerializeField] private float _sixShakeIntensity = 0.5f;
        [SerializeField] private float _sixShakeDuration = 0.4f;

        [Header("Four Effect - Blue Streak")]
        [SerializeField] private ParticleSystem _blueStreak;
        [SerializeField] private TrailRenderer _boundaryTrail;
        [SerializeField] private float _streakDuration = 2f;
        [SerializeField] private Color _streakColor = new Color(0.129f, 0.588f, 0.953f, 1f);

        [Header("Four Effect - Crowd")]
        [SerializeField] private float _fourShakeIntensity = 0.2f;
        [SerializeField] private float _fourShakeDuration = 0.2f;

        private bool _isPlaying;

        /// <summary>
        /// Play the six boundary celebration (gold fireworks + screen flash + camera shake).
        /// </summary>
        /// <param name="position">World position where the ball crossed the boundary.</param>
        public void PlaySixCelebration(Vector3 position)
        {
            if (_isPlaying) return;
            _isPlaying = true;

            transform.position = position;

            // Gold fireworks burst
            if (_goldFireworks != null)
            {
                var main = _goldFireworks.main;
                main.startColor = _fireworksColor;
                main.duration = _fireworksDuration;

                var emission = _goldFireworks.emission;
                emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)_fireworksBurstCount));

                _goldFireworks.Play();
            }

            // Gold sparkles
            if (_goldSparkles != null)
            {
                var main = _goldSparkles.main;
                main.startColor = new Color(1f, 0.878f, 0.4f, 1f);
                _goldSparkles.Play();
            }

            // Request screen flash via event
            EventBus.Publish(new ScreenFlashEvent
            {
                Color = _sixFlashColor,
                Intensity = _flashIntensity,
                Duration = _flashDuration
            });

            // Request camera shake
            EventBus.Publish(new CameraShakeEvent
            {
                Intensity = _sixShakeIntensity,
                Duration = _sixShakeDuration
            });

            // Auto-stop after duration
            Invoke(nameof(StopEffect), _fireworksDuration);

            Debug.Log("[BoundaryEffect] Six celebration: Gold fireworks + screen flash + camera shake");
        }

        /// <summary>
        /// Play the four boundary effect (blue streak + crowd animation trigger).
        /// </summary>
        /// <param name="startPosition">Start position of the ball trajectory.</param>
        /// <param name="endPosition">Where the ball crossed the boundary.</param>
        public void PlayFourCelebration(Vector3 startPosition, Vector3 endPosition)
        {
            if (_isPlaying) return;
            _isPlaying = true;

            transform.position = endPosition;

            // Blue streak trail
            if (_blueStreak != null)
            {
                var main = _blueStreak.main;
                main.startColor = _streakColor;
                _blueStreak.transform.position = startPosition;
                _blueStreak.Play();
            }

            // Activate trail renderer
            if (_boundaryTrail != null)
            {
                _boundaryTrail.startColor = _streakColor;
                _boundaryTrail.endColor = new Color(_streakColor.r, _streakColor.g, _streakColor.b, 0f);
                _boundaryTrail.emitting = true;
            }

            // Lighter camera shake for four
            EventBus.Publish(new CameraShakeEvent
            {
                Intensity = _fourShakeIntensity,
                Duration = _fourShakeDuration
            });

            // Trigger crowd animation
            EventBus.Publish(new CrowdAnimationEvent
            {
                AnimationType = CrowdAnimationType.Wave,
                Intensity = 0.7f
            });

            // Auto-stop
            Invoke(nameof(StopEffect), _streakDuration);

            Debug.Log("[BoundaryEffect] Four celebration: Blue streak + crowd animation");
        }

        /// <summary>
        /// Stop all active effects.
        /// </summary>
        public void StopEffect()
        {
            _isPlaying = false;

            if (_goldFireworks != null) _goldFireworks.Stop();
            if (_goldSparkles != null) _goldSparkles.Stop();
            if (_blueStreak != null) _blueStreak.Stop();
            if (_boundaryTrail != null) _boundaryTrail.emitting = false;
        }
    }

    /// <summary>
    /// Event requesting a screen flash effect.
    /// </summary>
    public struct ScreenFlashEvent
    {
        public Color Color;
        public float Intensity;
        public float Duration;
    }

    /// <summary>
    /// Event requesting a camera shake.
    /// </summary>
    public struct CameraShakeEvent
    {
        public float Intensity;
        public float Duration;
    }

    /// <summary>
    /// Types of crowd animations that can be triggered.
    /// </summary>
    public enum CrowdAnimationType
    {
        Wave,
        Cheer,
        Groan,
        Celebrate
    }

    /// <summary>
    /// Event requesting crowd animation playback.
    /// </summary>
    public struct CrowdAnimationEvent
    {
        public CrowdAnimationType AnimationType;
        public float Intensity;
    }
}
