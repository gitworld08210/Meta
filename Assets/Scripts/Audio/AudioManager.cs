using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.Audio
{
    /// <summary>
    /// Singleton AudioManager handling all game audio: BGM tracks per scene,
    /// SFX playback (bat hit, crowd roar, boundary, wicket, UI clicks),
    /// volume control, crossfade between tracks, and pool-based SFX playback.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[AudioManager] Instance is null. Ensure AudioManager exists in the scene.");
                }
                return _instance;
            }
        }

        [Header("Audio Data")]
        [SerializeField] private AudioData _audioData;

        [Header("BGM Settings")]
        [SerializeField] private AudioSource _bgmSourceA;
        [SerializeField] private AudioSource _bgmSourceB;
        [SerializeField] private float _crossfadeDuration = 1.5f;

        [Header("SFX Pool")]
        [SerializeField] private int _sfxPoolSize = 10;
        [SerializeField] private Transform _sfxPoolContainer;

        [Header("Volume Settings")]
        [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.7f;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 1f;

        private List<AudioSource> _sfxPool = new List<AudioSource>();
        private int _currentSfxIndex;
        private AudioSource _currentBgmSource;
        private bool _isCrossfading;
        private float _crossfadeTimer;
        private AudioSource _fadingOutSource;
        private AudioSource _fadingInSource;

        /// <summary>Master volume (0 to 1).</summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        /// <summary>Music volume (0 to 1).</summary>
        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        /// <summary>SFX volume (0 to 1).</summary>
        public float SFXVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudio();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (_isCrossfading)
            {
                UpdateCrossfade();
            }
        }

        private void InitializeAudio()
        {
            // Create BGM sources if not assigned
            if (_bgmSourceA == null)
            {
                GameObject bgmObjA = new GameObject("BGM_A");
                bgmObjA.transform.SetParent(transform);
                _bgmSourceA = bgmObjA.AddComponent<AudioSource>();
                _bgmSourceA.loop = true;
                _bgmSourceA.playOnAwake = false;
            }

            if (_bgmSourceB == null)
            {
                GameObject bgmObjB = new GameObject("BGM_B");
                bgmObjB.transform.SetParent(transform);
                _bgmSourceB = bgmObjB.AddComponent<AudioSource>();
                _bgmSourceB.loop = true;
                _bgmSourceB.playOnAwake = false;
            }

            _currentBgmSource = _bgmSourceA;

            // Create SFX pool
            InitializeSFXPool();

            // Register with ServiceLocator
            ServiceLocator.Register(this);

            Debug.Log("[AudioManager] Audio system initialized with pool-based SFX playback.");
        }

        private void InitializeSFXPool()
        {
            if (_sfxPoolContainer == null)
            {
                GameObject poolObj = new GameObject("SFX_Pool");
                poolObj.transform.SetParent(transform);
                _sfxPoolContainer = poolObj.transform;
            }

            for (int i = 0; i < _sfxPoolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFX_{i}");
                sfxObj.transform.SetParent(_sfxPoolContainer);
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                _sfxPool.Add(source);
            }
        }

        /// <summary>
        /// Play background music with optional crossfade from current track.
        /// </summary>
        /// <param name="clip">The BGM audio clip to play.</param>
        /// <param name="crossfade">Whether to crossfade from current track.</param>
        public void PlayBGM(AudioClip clip, bool crossfade = true)
        {
            if (clip == null) return;

            if (crossfade && _currentBgmSource.isPlaying)
            {
                StartCrossfade(clip);
            }
            else
            {
                _currentBgmSource.clip = clip;
                _currentBgmSource.volume = _musicVolume * _masterVolume;
                _currentBgmSource.Play();
            }
        }

        /// <summary>
        /// Play a specific BGM track for a game state.
        /// </summary>
        public void PlayBGMForState(GameState state)
        {
            if (_audioData == null) return;

            AudioClip clip = _audioData.GetBGMForState(state);
            if (clip != null)
            {
                PlayBGM(clip);
            }
        }

        /// <summary>
        /// Stop the current BGM with optional fade out.
        /// </summary>
        public void StopBGM(bool fadeOut = true)
        {
            if (fadeOut)
            {
                _fadingOutSource = _currentBgmSource;
                _isCrossfading = true;
                _crossfadeTimer = 0f;
                _fadingInSource = null;
            }
            else
            {
                _currentBgmSource.Stop();
            }
        }

        /// <summary>
        /// Play a sound effect from the pool.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="volumeScale">Additional volume multiplier (0 to 1).</param>
        /// <param name="pitch">Pitch variation (default 1).</param>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume * volumeScale;
            source.pitch = pitch;
            source.Play();
        }

        /// <summary>
        /// Play a bat hit sound effect.
        /// </summary>
        public void PlayBatHit(float power)
        {
            if (_audioData == null) return;

            AudioClip clip = _audioData.GetBatHitClip(power);
            float pitch = Mathf.Lerp(0.9f, 1.1f, power);
            PlaySFX(clip, 1f, pitch);
        }

        /// <summary>
        /// Play a boundary celebration sound.
        /// </summary>
        public void PlayBoundarySound(bool isSix)
        {
            if (_audioData == null) return;

            AudioClip clip = isSix ? _audioData.SixBoundaryClip : _audioData.FourBoundaryClip;
            PlaySFX(clip);

            // Also trigger crowd roar
            PlayCrowdRoar(isSix ? 1f : 0.7f);
        }

        /// <summary>
        /// Play crowd roar with intensity scaling.
        /// </summary>
        public void PlayCrowdRoar(float intensity)
        {
            if (_audioData == null || _audioData.CrowdRoarClip == null) return;

            PlaySFX(_audioData.CrowdRoarClip, intensity);
        }

        /// <summary>
        /// Play wicket fall sound.
        /// </summary>
        public void PlayWicketSound()
        {
            if (_audioData == null || _audioData.WicketClip == null) return;

            PlaySFX(_audioData.WicketClip);
        }

        /// <summary>
        /// Play UI click sound.
        /// </summary>
        public void PlayUIClick()
        {
            if (_audioData == null || _audioData.UIClickClip == null) return;

            PlaySFX(_audioData.UIClickClip, 0.5f);
        }

        private AudioSource GetAvailableSFXSource()
        {
            // Round-robin through the pool
            AudioSource source = _sfxPool[_currentSfxIndex];
            _currentSfxIndex = (_currentSfxIndex + 1) % _sfxPool.Count;

            // If source is playing, find a free one
            if (source.isPlaying)
            {
                for (int i = 0; i < _sfxPool.Count; i++)
                {
                    if (!_sfxPool[i].isPlaying)
                    {
                        return _sfxPool[i];
                    }
                }
            }

            return source;
        }

        private void StartCrossfade(AudioClip newClip)
        {
            AudioSource newSource = (_currentBgmSource == _bgmSourceA) ? _bgmSourceB : _bgmSourceA;
            newSource.clip = newClip;
            newSource.volume = 0f;
            newSource.Play();

            _fadingOutSource = _currentBgmSource;
            _fadingInSource = newSource;
            _currentBgmSource = newSource;
            _isCrossfading = true;
            _crossfadeTimer = 0f;
        }

        private void UpdateCrossfade()
        {
            _crossfadeTimer += Time.deltaTime;
            float t = _crossfadeTimer / _crossfadeDuration;

            if (t >= 1f)
            {
                // Crossfade complete
                if (_fadingOutSource != null)
                {
                    _fadingOutSource.Stop();
                    _fadingOutSource.volume = 0f;
                }
                if (_fadingInSource != null)
                {
                    _fadingInSource.volume = _musicVolume * _masterVolume;
                }
                _isCrossfading = false;
                return;
            }

            // Smooth crossfade
            float effectiveVolume = _musicVolume * _masterVolume;
            if (_fadingOutSource != null)
            {
                _fadingOutSource.volume = Mathf.Lerp(effectiveVolume, 0f, t);
            }
            if (_fadingInSource != null)
            {
                _fadingInSource.volume = Mathf.Lerp(0f, effectiveVolume, t);
            }
        }

        private void UpdateVolumes()
        {
            float effectiveMusic = _musicVolume * _masterVolume;

            if (_currentBgmSource != null && _currentBgmSource.isPlaying && !_isCrossfading)
            {
                _currentBgmSource.volume = effectiveMusic;
            }
        }
    }
}
