using System;
using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.Audio
{
    /// <summary>
    /// ScriptableObject storing all audio clip references organized by category.
    /// Includes BGM tracks, SFX clips, and commentary mappings per event type and language.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioData", menuName = "MetaCricket/Audio/Audio Data")]
    public class AudioData : ScriptableObject
    {
        [Header("Background Music")]
        [SerializeField] private AudioClip _mainMenuBGM;
        [SerializeField] private AudioClip _matchBGM;
        [SerializeField] private AudioClip _victoryBGM;
        [SerializeField] private AudioClip _defeatBGM;
        [SerializeField] private AudioClip _careerHubBGM;
        [SerializeField] private AudioClip _splashBGM;

        [Header("Bat Hit SFX")]
        [SerializeField] private AudioClip _batHitLight;
        [SerializeField] private AudioClip _batHitMedium;
        [SerializeField] private AudioClip _batHitHeavy;
        [SerializeField] private AudioClip _batHitEdge;

        [Header("Boundary SFX")]
        public AudioClip FourBoundaryClip;
        public AudioClip SixBoundaryClip;

        [Header("Match SFX")]
        public AudioClip CrowdRoarClip;
        public AudioClip WicketClip;
        [SerializeField] private AudioClip _bowlingClip;
        [SerializeField] private AudioClip _crowdCheerClip;
        [SerializeField] private AudioClip _crowdGroanClip;

        [Header("UI SFX")]
        public AudioClip UIClickClip;
        [SerializeField] private AudioClip _uiBackClip;
        [SerializeField] private AudioClip _uiSuccessClip;
        [SerializeField] private AudioClip _uiErrorClip;
        [SerializeField] private AudioClip _levelUpClip;

        [Header("Commentary - English")]
        [SerializeField] private CommentaryClipSet _englishCommentary;

        [Header("Commentary - Hindi")]
        [SerializeField] private CommentaryClipSet _hindiCommentary;

        /// <summary>
        /// Get BGM clip for a specific game state.
        /// </summary>
        public AudioClip GetBGMForState(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu: return _mainMenuBGM;
                case GameState.Playing: return _matchBGM;
                case GameState.GameOver: return _victoryBGM;
                case GameState.Calibrating: return _splashBGM;
                default: return _mainMenuBGM;
            }
        }

        /// <summary>
        /// Get appropriate bat hit clip based on shot power.
        /// </summary>
        public AudioClip GetBatHitClip(float power)
        {
            if (power >= 0.8f) return _batHitHeavy;
            if (power >= 0.5f) return _batHitMedium;
            if (power >= 0.2f) return _batHitLight;
            return _batHitEdge;
        }

        /// <summary>
        /// Get a commentary clip for a specific event type and language.
        /// </summary>
        public AudioClip GetCommentaryClip(CommentaryEventType eventType, CommentaryLanguage language)
        {
            CommentaryClipSet clipSet = GetClipSetForLanguage(language);
            if (clipSet == null) return null;

            AudioClip[] clips = clipSet.GetClipsForEvent(eventType);
            if (clips == null || clips.Length == 0) return null;

            // Return random clip from available options for variety
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        private CommentaryClipSet GetClipSetForLanguage(CommentaryLanguage language)
        {
            switch (language)
            {
                case CommentaryLanguage.English: return _englishCommentary;
                case CommentaryLanguage.Hindi: return _hindiCommentary;
                default: return _englishCommentary;
            }
        }
    }

    /// <summary>
    /// Serializable set of commentary clips organized by event type for a specific language.
    /// </summary>
    [Serializable]
    public class CommentaryClipSet
    {
        [Header("Boundary Commentary")]
        public AudioClip[] BoundaryFourClips;
        public AudioClip[] BoundarySixClips;

        [Header("Wicket Commentary")]
        public AudioClip[] WicketClips;

        [Header("Milestone Commentary")]
        public AudioClip[] HalfCenturyClips;
        public AudioClip[] CenturyClips;

        [Header("Shot Commentary")]
        public AudioClip[] GoodShotClips;
        public AudioClip[] BigHitClips;

        [Header("Ball Commentary")]
        public AudioClip[] DotBallClips;
        public AudioClip[] CloseCallClips;

        [Header("Match Commentary")]
        public AudioClip[] MatchStartClips;
        public AudioClip[] MatchEndClips;
        public AudioClip[] OverCompleteClips;
        public AudioClip[] FinalOversClips;

        /// <summary>
        /// Get clips array for a given event type.
        /// </summary>
        public AudioClip[] GetClipsForEvent(CommentaryEventType eventType)
        {
            switch (eventType)
            {
                case CommentaryEventType.BoundaryFour: return BoundaryFourClips;
                case CommentaryEventType.BoundarySix: return BoundarySixClips;
                case CommentaryEventType.Wicket: return WicketClips;
                case CommentaryEventType.MilestoneHalfCentury: return HalfCenturyClips;
                case CommentaryEventType.MilestoneCentury: return CenturyClips;
                case CommentaryEventType.GoodShot: return GoodShotClips;
                case CommentaryEventType.DotBall: return DotBallClips;
                case CommentaryEventType.MatchStart: return MatchStartClips;
                case CommentaryEventType.MatchEnd: return MatchEndClips;
                case CommentaryEventType.OverComplete: return OverCompleteClips;
                case CommentaryEventType.BigHit: return BigHitClips;
                case CommentaryEventType.CloseCall: return CloseCallClips;
                case CommentaryEventType.FinalOvers: return FinalOversClips;
                default: return null;
            }
        }
    }
}
