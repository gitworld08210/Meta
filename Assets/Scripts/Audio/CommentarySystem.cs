using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.Audio
{
    /// <summary>
    /// Commentary event types that trigger specific commentary clips.
    /// </summary>
    public enum CommentaryEventType
    {
        BoundaryFour,
        BoundarySix,
        Wicket,
        MilestoneHalfCentury,
        MilestoneCentury,
        GoodShot,
        DotBall,
        MatchStart,
        MatchEnd,
        OverComplete,
        BigHit,
        CloseCall,
        FinalOvers
    }

    /// <summary>
    /// Commentary manager that triggers contextual commentary clips based on match events.
    /// Supports Hindi and English language switching, queue system to prevent overlap,
    /// and excitement level scaling for intensity variation.
    /// </summary>
    public class CommentarySystem : MonoBehaviour
    {
        [Header("Audio Data Reference")]
        [SerializeField] private AudioData _audioData;

        [Header("Commentary Audio Source")]
        [SerializeField] private AudioSource _commentarySource;

        [Header("Language")]
        [SerializeField] private CommentaryLanguage _currentLanguage = CommentaryLanguage.English;

        [Header("Settings")]
        [SerializeField] private float _minDelayBetweenClips = 1f;
        [SerializeField] private float _maxQueueSize = 3;
        [SerializeField] [Range(0f, 1f)] private float _commentaryVolume = 0.8f;

        [Header("Excitement")]
        [SerializeField] private float _baseExcitement = 0.5f;
        [SerializeField] private float _excitementDecayRate = 0.1f;
        [SerializeField] private float _maxExcitement = 1.5f;

        private Queue<CommentaryClipRequest> _commentaryQueue = new Queue<CommentaryClipRequest>();
        private float _lastClipEndTime;
        private float _currentExcitement;
        private bool _isPlaying;

        /// <summary>
        /// Current commentary language.
        /// </summary>
        public CommentaryLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                Debug.Log($"[CommentarySystem] Language changed to: {value}");
            }
        }

        /// <summary>
        /// Current excitement level affecting pitch and clip selection.
        /// </summary>
        public float ExcitementLevel => _currentExcitement;

        private struct CommentaryClipRequest
        {
            public CommentaryEventType EventType;
            public float ExcitementBoost;
            public float Priority;
        }

        private void Awake()
        {
            if (_commentarySource == null)
            {
                GameObject commentaryObj = new GameObject("Commentary_Source");
                commentaryObj.transform.SetParent(transform);
                _commentarySource = commentaryObj.AddComponent<AudioSource>();
                _commentarySource.playOnAwake = false;
                _commentarySource.loop = false;
            }

            _currentExcitement = _baseExcitement;
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Decay excitement over time
            if (_currentExcitement > _baseExcitement)
            {
                _currentExcitement -= _excitementDecayRate * Time.deltaTime;
                _currentExcitement = Mathf.Max(_currentExcitement, _baseExcitement);
            }

            // Process queue
            if (!_isPlaying && _commentaryQueue.Count > 0 &&
                Time.time >= _lastClipEndTime + _minDelayBetweenClips)
            {
                ProcessNextInQueue();
            }

            // Update playing state
            if (_isPlaying && !_commentarySource.isPlaying)
            {
                _isPlaying = false;
                _lastClipEndTime = Time.time;
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<BoundaryEvent>(OnBoundary);
            EventBus.Subscribe<WicketEvent>(OnWicket);
            EventBus.Subscribe<ShotPlayedEvent>(OnShotPlayed);
            EventBus.Subscribe<MatchEndEvent>(OnMatchEnd);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<BoundaryEvent>(OnBoundary);
            EventBus.Unsubscribe<WicketEvent>(OnWicket);
            EventBus.Unsubscribe<ShotPlayedEvent>(OnShotPlayed);
            EventBus.Unsubscribe<MatchEndEvent>(OnMatchEnd);
        }

        private void OnBoundary(BoundaryEvent evt)
        {
            CommentaryEventType eventType = evt.IsSix
                ? CommentaryEventType.BoundarySix
                : CommentaryEventType.BoundaryFour;

            float excitementBoost = evt.IsSix ? 0.4f : 0.25f;
            QueueCommentary(eventType, excitementBoost, priority: 0.9f);
        }

        private void OnWicket(WicketEvent evt)
        {
            QueueCommentary(CommentaryEventType.Wicket, 0.3f, priority: 1.0f);
        }

        private void OnShotPlayed(ShotPlayedEvent evt)
        {
            if (evt.TimingAccuracy >= 0.9f)
            {
                QueueCommentary(CommentaryEventType.GoodShot, 0.15f, priority: 0.5f);
            }
            else if (evt.Power >= 0.8f)
            {
                QueueCommentary(CommentaryEventType.BigHit, 0.2f, priority: 0.6f);
            }
        }

        private void OnMatchEnd(MatchEndEvent evt)
        {
            QueueCommentary(CommentaryEventType.MatchEnd, 0.5f, priority: 1.0f);
        }

        /// <summary>
        /// Trigger commentary for a specific event with excitement boost.
        /// </summary>
        /// <param name="eventType">The type of commentary event.</param>
        /// <param name="excitementBoost">How much to increase excitement level.</param>
        /// <param name="priority">Priority for queue ordering (higher = more important).</param>
        public void QueueCommentary(CommentaryEventType eventType, float excitementBoost = 0f, float priority = 0.5f)
        {
            // Boost excitement
            _currentExcitement = Mathf.Min(_currentExcitement + excitementBoost, _maxExcitement);

            // Add to queue (limit queue size)
            if (_commentaryQueue.Count >= _maxQueueSize)
            {
                return; // Drop low-priority clips when queue is full
            }

            _commentaryQueue.Enqueue(new CommentaryClipRequest
            {
                EventType = eventType,
                ExcitementBoost = excitementBoost,
                Priority = priority
            });
        }

        /// <summary>
        /// Trigger a milestone commentary (50, 100 etc).
        /// </summary>
        public void TriggerMilestone(int runs)
        {
            if (runs >= 100)
            {
                QueueCommentary(CommentaryEventType.MilestoneCentury, 0.5f, priority: 1.0f);
            }
            else if (runs >= 50)
            {
                QueueCommentary(CommentaryEventType.MilestoneHalfCentury, 0.3f, priority: 0.9f);
            }
        }

        /// <summary>
        /// Switch between Hindi and English commentary.
        /// </summary>
        public void SwitchLanguage(CommentaryLanguage language)
        {
            _currentLanguage = language;
            Debug.Log($"[CommentarySystem] Commentary language switched to: {language}");
        }

        /// <summary>
        /// Stop all current commentary and clear queue.
        /// </summary>
        public void StopAllCommentary()
        {
            _commentaryQueue.Clear();
            if (_commentarySource != null && _commentarySource.isPlaying)
            {
                _commentarySource.Stop();
            }
            _isPlaying = false;
        }

        private void ProcessNextInQueue()
        {
            if (_commentaryQueue.Count == 0) return;

            CommentaryClipRequest request = _commentaryQueue.Dequeue();
            AudioClip clip = GetCommentaryClip(request.EventType);

            if (clip != null)
            {
                PlayCommentaryClip(clip);
            }
        }

        private void PlayCommentaryClip(AudioClip clip)
        {
            if (_commentarySource == null || clip == null) return;

            _commentarySource.clip = clip;
            _commentarySource.volume = _commentaryVolume *
                (AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f);

            // Adjust pitch slightly based on excitement for variety
            _commentarySource.pitch = Mathf.Lerp(1f, 1.05f, (_currentExcitement - _baseExcitement) / (_maxExcitement - _baseExcitement));

            _commentarySource.Play();
            _isPlaying = true;
        }

        private AudioClip GetCommentaryClip(CommentaryEventType eventType)
        {
            if (_audioData == null) return null;

            return _audioData.GetCommentaryClip(eventType, _currentLanguage);
        }

        /// <summary>
        /// Set commentary volume (0 to 1).
        /// </summary>
        public void SetVolume(float volume)
        {
            _commentaryVolume = Mathf.Clamp01(volume);
            if (_commentarySource != null)
            {
                _commentarySource.volume = _commentaryVolume *
                    (AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f);
            }
        }
    }
}
