using System;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.BallPhysics
{
    /// <summary>
    /// Data class defining the parameters of a single ball delivery.
    /// Contains speed, spin type, swing type, line, length, bounce height, and target position.
    /// This is a plain serializable class (not a ScriptableObject) to avoid memory leaks
    /// when creating delivery data per ball.
    /// </summary>
    [Serializable]
    public class BallDeliveryData
    {
        [Header("Speed Settings")]
        [Tooltip("Ball speed in kilometers per hour (70-150 kph).")]
        [Range(70f, 150f)]
        public float Speed = 120f;

        [Header("Ball Type & Spin")]
        [Tooltip("The type of delivery being bowled.")]
        public BallType DeliveryType = BallType.Pace;

        [Tooltip("Spin rate in revolutions per second. Higher = more deviation.")]
        [Range(0f, 40f)]
        public float SpinRate = 0f;

        [Tooltip("Spin axis direction - determines which way the ball deviates after pitching.")]
        public Vector3 SpinAxis = Vector3.up;

        [Header("Swing Settings")]
        [Tooltip("Amount of lateral swing in the air (0 = no swing, 1 = maximum swing).")]
        [Range(0f, 1f)]
        public float SwingAmount = 0f;

        [Tooltip("Direction of swing: positive = inswing (towards batsman), negative = outswing (away).")]
        [Range(-1f, 1f)]
        public float SwingDirection = 0f;

        [Header("Line & Length")]
        [Tooltip("Ball line: -1 = outside off, 0 = middle stump, 1 = leg stump.")]
        [Range(-1f, 1f)]
        public float Line = 0f;

        [Tooltip("Ball length: 0 = yorker, 0.25 = full, 0.5 = good length, 0.75 = short, 1 = bouncer.")]
        [Range(0f, 1f)]
        public float Length = 0.5f;

        [Header("Bounce")]
        [Tooltip("Bounce height multiplier. Higher values = more bounce off the pitch.")]
        [Range(0.5f, 2.0f)]
        public float BounceHeight = 1.0f;

        [Tooltip("Seam movement after pitching (0 = none, 1 = maximum).")]
        [Range(0f, 1f)]
        public float SeamMovement = 0f;

        [Header("Target")]
        [Tooltip("Target position where the ball aims to arrive at the batsman's crease.")]
        public Vector3 TargetPosition = new Vector3(0f, 0.7f, 0f);

        [Tooltip("Pitch landing position (calculated from length).")]
        public Vector3 PitchPosition = new Vector3(0f, 0f, 5f);

        /// <summary>
        /// Gets the speed in meters per second for physics calculations.
        /// </summary>
        public float SpeedInMetersPerSecond => Speed / 3.6f;

        /// <summary>
        /// Whether this delivery has spin applied.
        /// </summary>
        public bool HasSpin => SpinRate > 0.1f;

        /// <summary>
        /// Whether this delivery has swing applied.
        /// </summary>
        public bool HasSwing => Mathf.Abs(SwingAmount) > 0.05f;

        /// <summary>
        /// Gets the ball length as a descriptive category.
        /// </summary>
        public BallLengthCategory GetLengthCategory()
        {
            if (Length < 0.15f) return BallLengthCategory.Yorker;
            if (Length < 0.35f) return BallLengthCategory.Full;
            if (Length < 0.6f) return BallLengthCategory.GoodLength;
            if (Length < 0.8f) return BallLengthCategory.Short;
            return BallLengthCategory.Bouncer;
        }

        /// <summary>
        /// Gets the ball line as a descriptive category.
        /// </summary>
        public BallLineCategory GetLineCategory()
        {
            if (Line < -0.3f) return BallLineCategory.OutsideOff;
            if (Line < 0.3f) return BallLineCategory.Middle;
            return BallLineCategory.LegStump;
        }

        /// <summary>
        /// Reset all values to defaults for reuse in pooling scenarios.
        /// </summary>
        public void Reset()
        {
            Speed = 120f;
            DeliveryType = BallType.Pace;
            SpinRate = 0f;
            SpinAxis = Vector3.up;
            SwingAmount = 0f;
            SwingDirection = 0f;
            Line = 0f;
            Length = 0.5f;
            BounceHeight = 1.0f;
            SeamMovement = 0f;
            TargetPosition = new Vector3(0f, 0.7f, 0f);
            PitchPosition = new Vector3(0f, 0f, 5f);
        }
    }

    /// <summary>
    /// Categories for ball length description.
    /// </summary>
    public enum BallLengthCategory
    {
        Yorker,
        Full,
        GoodLength,
        Short,
        Bouncer
    }

    /// <summary>
    /// Categories for ball line description.
    /// </summary>
    public enum BallLineCategory
    {
        OutsideOff,
        Middle,
        LegStump
    }
}
