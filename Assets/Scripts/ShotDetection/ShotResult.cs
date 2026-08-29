using System;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.ShotDetection
{
    /// <summary>
    /// Quality of shot timing relative to ball delivery.
    /// </summary>
    public enum TimingQuality
    {
        Early,
        Good,
        Perfect,
        Late
    }

    /// <summary>
    /// Result data from shot detection: type detected, confidence score,
    /// timing quality, power level, and direction vector.
    /// </summary>
    [Serializable]
    public class ShotResult
    {
        /// <summary>
        /// The classified shot type.
        /// </summary>
        public ShotType ShotType;

        /// <summary>
        /// Confidence score for the classification (0-1).
        /// Higher values indicate more certainty in the detected shot type.
        /// </summary>
        public float Confidence;

        /// <summary>
        /// The quality of the shot timing relative to ball position.
        /// </summary>
        public TimingQuality Timing;

        /// <summary>
        /// Power level of the shot (0-1).
        /// Based on swing velocity and follow-through.
        /// </summary>
        public float Power;

        /// <summary>
        /// Direction vector of the shot in world space.
        /// Normalized direction indicating where the ball will travel.
        /// </summary>
        public Vector3 Direction;

        /// <summary>
        /// Swing speed at the point of impact (normalized units/second).
        /// </summary>
        public float SwingSpeed;

        /// <summary>
        /// The arm angle at the point of impact (degrees).
        /// </summary>
        public float ArmAngle;

        /// <summary>
        /// Timestamp when the shot was detected.
        /// </summary>
        public float Timestamp;

        /// <summary>
        /// Whether this result represents a valid shot detection.
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// The raw swing data that produced this result.
        /// </summary>
        [NonSerialized]
        public SwingData SourceSwingData;

        public ShotResult()
        {
            ShotType = ShotType.DefensiveBlock;
            Confidence = 0f;
            Timing = TimingQuality.Good;
            Power = 0f;
            Direction = Vector3.forward;
            SwingSpeed = 0f;
            ArmAngle = 0f;
            Timestamp = 0f;
            IsValid = false;
        }

        public ShotResult(ShotType shotType, float confidence, TimingQuality timing,
                          float power, Vector3 direction)
        {
            ShotType = shotType;
            Confidence = confidence;
            Timing = timing;
            Power = power;
            Direction = direction;
            SwingSpeed = 0f;
            ArmAngle = 0f;
            Timestamp = Time.time;
            IsValid = confidence > 0f;
        }

        /// <summary>
        /// Get a multiplier based on timing quality for gameplay purposes.
        /// </summary>
        public float GetTimingMultiplier()
        {
            switch (Timing)
            {
                case TimingQuality.Perfect:
                    return 1.5f;
                case TimingQuality.Good:
                    return 1.0f;
                case TimingQuality.Early:
                    return 0.6f;
                case TimingQuality.Late:
                    return 0.5f;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Calculate the final shot effectiveness combining power, timing, and confidence.
        /// </summary>
        public float GetEffectiveness()
        {
            return Power * GetTimingMultiplier() * Confidence;
        }

        public override string ToString()
        {
            return $"ShotResult: {ShotType} (Conf: {Confidence:F2}, Timing: {Timing}, Power: {Power:F2})";
        }
    }
}
