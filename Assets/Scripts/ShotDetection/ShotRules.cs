using System;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.ShotDetection
{
    /// <summary>
    /// ScriptableObject-based shot classification rules.
    /// Each rule defines required arm angle range, swing direction, velocity range,
    /// and body rotation criteria for a specific shot type.
    /// </summary>
    [CreateAssetMenu(fileName = "ShotRules", menuName = "MetaCricket/Shot Detection/Shot Rules")]
    public class ShotRules : ScriptableObject
    {
        /// <summary>
        /// Array of individual shot classification rules.
        /// </summary>
        public ShotClassificationRule[] Rules;

        /// <summary>
        /// Create a default set of rules covering all 8 shot types.
        /// </summary>
        public static ShotRules CreateDefaultRules()
        {
            ShotRules rules = CreateInstance<ShotRules>();
            rules.Rules = new ShotClassificationRule[]
            {
                CreateCoverDriveRule(),
                CreatePullShotRule(),
                CreateStraightDriveRule(),
                CreateHelicopterShotRule(),
                CreateUppercutRule(),
                CreateSwitchHitRule(),
                CreateFlickRule(),
                CreateDefensiveBlockRule()
            };
            return rules;
        }

        /// <summary>
        /// CoverDrive: front foot, bat angle 30-60 degrees, horizontal swing.
        /// </summary>
        private static ShotClassificationRule CreateCoverDriveRule()
        {
            return new ShotClassificationRule
            {
                ShotType = ShotType.CoverDrive,
                DisplayName = "Cover Drive",
                MinArmAngle = 30f,
                MaxArmAngle = 60f,
                MinSwingPlaneAngle = 0f,
                MaxSwingPlaneAngle = 30f, // Horizontal swing
                MinVelocity = 0.3f,
                MaxVelocity = 1.5f,
                RequiresFrontFoot = true,
                RequiresBackFoot = false,
                MinWristRotation = 0f,
                MaxWristRotation = 90f,
                RequiresStanceReversal = false,
                MinBackswingRatio = 0.2f,
                MaxBackswingRatio = 0.8f,
                MinFollowThroughHeight = -0.2f,
                MaxFollowThroughHeight = 0.5f,
                RequiresCrossBody = false,
                MinBodyRotation = 10f,
                MaxBodyRotation = 60f,
                Priority = 5,
                Weight = 1.0f
            };
        }

        /// <summary>
        /// PullShot: back foot, horizontal cross-body swing.
        /// </summary>
        private static ShotClassificationRule CreatePullShotRule()
        {
            return new ShotClassificationRule
            {
                ShotType = ShotType.PullShot,
                DisplayName = "Pull Shot",
                MinArmAngle = 0f,
                MaxArmAngle = 45f,
                MinSwingPlaneAngle = 0f,
                MaxSwingPlaneAngle = 35f, // Horizontal
                MinVelocity = 0.5f,
                MaxVelocity = 2.0f,
                RequiresFrontFoot = false,
                RequiresBackFoot = true,
                MinWristRotation = 0f,
                MaxWristRotation = 120f,
                RequiresStanceReversal = false,
                MinBackswingRatio = 0.3f,
                MaxBackswingRatio = 1.0f,
                MinFollowThroughHeight = -0.1f,
                MaxFollowThroughHeight = 0.7f,
                RequiresCrossBody = true,
                MinBodyRotation = 20f,
                MaxBodyRotation = 90f,
                Priority = 5,
                Weight = 1.0f
            };
        }

        /// <summary>
        /// StraightDrive: vertical swing plane, bat straight.
        /// </summary>
        private static ShotClassificationRule CreateStraightDriveRule()
        {
            return new ShotClassificationRule
            {
                ShotType = ShotType.StraightDrive,
                DisplayName = "Straight Drive",
                MinArmAngle = 60f,
                MaxArmAngle = 100f, // Bat relatively straight/vertical
                MinSwingPlaneAngle = 50f,
                MaxSwingPlaneAngle = 90f, // Vertical swing plane
                MinVelocity = 0.3f,
                MaxVelocity = 1.5f,
                RequiresFrontFoot = true,
                RequiresBackFoot = false,
                MinWristRotation = 0f,
                MaxWristRotation = 60f,
                RequiresStanceReversal = false,
                MinBackswingRatio = 0.3f,
                MaxBackswingRatio = 0.9f,
                MinFollowThroughHeight = 0.2f,
                MaxFollowThroughHeight = 1.0f,
                RequiresCrossBody = false,
                MinBodyRotation = 5f,
                MaxBodyRotation = 40f,
                Priority = 5,
                Weight = 1.0f
            };
        }

        /// <summary>
        /// HelicopterShot: wrist rotation >270 degrees, follow-through over shoulder.
        /// </summary>
        private static ShotClassificationRule CreateHelicopterShotRule()
        {
            return new ShotClassificationRule
            {
                ShotType = ShotType.HelicopterShot,
                DisplayName = "Helicopter Shot",
                MinArmAngle = 20f,
                MaxArmAngle = 90f,
                MinSwingPlaneAngle = 30f,
                MaxSwingPlaneAngle = 80f,
                MinVelocity = 0.8f,
                MaxVelocity = 3.0f,
                RequiresFrontFoot = false,
                RequiresBackFoot = false,
                MinWristRotation = 270f, // Key identifier: massive wrist rotation
                MaxWristRotation = 450f,
                RequiresStanceReversal = false,
                MinBackswingRatio = 0.2f,
                MaxBackswingRatio = 1.0f,
                MinFollowThroughHeight = 0.8f, // Follow-through over shoulder
                MaxFollowThroughHeight = 2.0f,
                RequiresCrossBody = false,
                MinBodyRotation = 30f,
                MaxBodyRotation = 180f,
                Priority = 8, // Higher priority due to unique signature
                Weight = 1.2f
            };
        }

        /// <summary>
        /// Uppercut: back and up motion, high follow-through.
        /// </summary>
        private static ShotClassificationRule CreateUppercutRule()
        {
            return new ShotClassificationRule
            {
                ShotType = ShotType.Uppercut,
                DisplayName = "Uppercut",
                MinArmAngle = 45f,
                MaxArmAngle = 120f,
                MinSwingPlaneAngle = 50f,
                MaxSwingPlaneAngle = 90f, // Upward swing
                MinVelocity = 0.5f,
                MaxVelocity = 2.0f,
                RequiresFrontFoot = false,
                RequiresBackFoot = true, // Back and up motion
                MinWristRotation = 0f,
                MaxWristRotation = 180f,
                RequiresStanceReversal = false,
                MinBackswingRatio = 0.1f,
                MaxBackswingRatio = 0.5f,
                MinFollowThroughHeight = 0.7f, // High follow-through
                MaxFollowThroughHeight = 2.0f,
                RequiresCrossBody = false,
                MinBodyRotation = 10f,
                MaxBodyRotation = 60f,
                Priority = 6,
                Weight = 1.0f
            };
        }

        /// <summary>
        /// SwitchHit: stance reversal detected before swing.
        /// </summary>
        private static ShotClassificationRule CreateSwitchHitRule()
        {
            return new ShotClassificationRule
            {
                ShotType = ShotType.SwitchHit,
                DisplayName = "Switch Hit",
                MinArmAngle = 10f,
                MaxArmAngle = 90f,
                MinSwingPlaneAngle = 0f,
                MaxSwingPlaneAngle = 60f,
                MinVelocity = 0.4f,
                MaxVelocity = 2.0f,
                RequiresFrontFoot = false,
                RequiresBackFoot = false,
                MinWristRotation = 0f,
                MaxWristRotation = 200f,
                RequiresStanceReversal = true, // Key identifier: stance reversal
                MinBackswingRatio = 0.2f,
                MaxBackswingRatio = 1.0f,
                MinFollowThroughHeight = -0.5f,
                MaxFollowThroughHeight = 1.5f,
                RequiresCrossBody = false,
                MinBodyRotation = 30f,
                MaxBodyRotation = 180f,
                Priority = 9, // Highest priority due to unique identifier
                Weight = 1.3f
            };
        }

        /// <summary>
        /// Flick: minimal backswing, wrist-dominant flick motion.
        /// </summary>
        private static ShotClassificationRule CreateFlickRule()
        {
            return new ShotClassificationRule
            {
                ShotType = ShotType.Flick,
                DisplayName = "Flick",
                MinArmAngle = 20f,
                MaxArmAngle = 70f,
                MinSwingPlaneAngle = 10f,
                MaxSwingPlaneAngle = 50f,
                MinVelocity = 0.3f,
                MaxVelocity = 1.5f,
                RequiresFrontFoot = false,
                RequiresBackFoot = false,
                MinWristRotation = 60f,
                MaxWristRotation = 200f,
                RequiresStanceReversal = false,
                MinBackswingRatio = 0f,
                MaxBackswingRatio = 0.2f, // Key identifier: minimal backswing
                MinFollowThroughHeight = -0.2f,
                MaxFollowThroughHeight = 0.6f,
                RequiresCrossBody = false,
                MinBodyRotation = 5f,
                MaxBodyRotation = 45f,
                Priority = 6,
                Weight = 1.0f
            };
        }

        /// <summary>
        /// DefensiveBlock: minimal swing, bat vertical, no follow-through.
        /// </summary>
        private static ShotClassificationRule CreateDefensiveBlockRule()
        {
            return new ShotClassificationRule
            {
                ShotType = ShotType.DefensiveBlock,
                DisplayName = "Defensive Block",
                MinArmAngle = 70f,
                MaxArmAngle = 110f, // Bat nearly vertical
                MinSwingPlaneAngle = 50f,
                MaxSwingPlaneAngle = 90f,
                MinVelocity = 0f,
                MaxVelocity = 0.3f, // Minimal swing speed
                RequiresFrontFoot = false,
                RequiresBackFoot = false,
                MinWristRotation = 0f,
                MaxWristRotation = 30f, // Minimal wrist rotation
                RequiresStanceReversal = false,
                MinBackswingRatio = 0f,
                MaxBackswingRatio = 0.15f,
                MinFollowThroughHeight = -0.5f,
                MaxFollowThroughHeight = 0.1f, // No follow-through
                RequiresCrossBody = false,
                MinBodyRotation = 0f,
                MaxBodyRotation = 15f, // Minimal body rotation
                Priority = 3, // Low priority, acts as fallback
                Weight = 0.8f
            };
        }
    }

    /// <summary>
    /// Individual classification rule for a single shot type.
    /// Defines the expected ranges for various swing metrics.
    /// </summary>
    [Serializable]
    public struct ShotClassificationRule
    {
        [Header("Shot Identity")]
        public ShotType ShotType;
        public string DisplayName;

        [Header("Arm Angle (degrees)")]
        [Tooltip("Minimum arm angle at impact.")]
        public float MinArmAngle;
        [Tooltip("Maximum arm angle at impact.")]
        public float MaxArmAngle;

        [Header("Swing Plane (degrees, 0=horizontal, 90=vertical)")]
        [Tooltip("Minimum swing plane angle.")]
        public float MinSwingPlaneAngle;
        [Tooltip("Maximum swing plane angle.")]
        public float MaxSwingPlaneAngle;

        [Header("Velocity (normalized units/second)")]
        [Tooltip("Minimum swing velocity.")]
        public float MinVelocity;
        [Tooltip("Maximum swing velocity.")]
        public float MaxVelocity;

        [Header("Footwork")]
        [Tooltip("Whether this shot requires front foot movement.")]
        public bool RequiresFrontFoot;
        [Tooltip("Whether this shot requires back foot movement.")]
        public bool RequiresBackFoot;

        [Header("Wrist Rotation (degrees)")]
        [Tooltip("Minimum wrist rotation during swing.")]
        public float MinWristRotation;
        [Tooltip("Maximum wrist rotation during swing.")]
        public float MaxWristRotation;

        [Header("Special Requirements")]
        [Tooltip("Whether this shot requires a stance reversal (switch hit).")]
        public bool RequiresStanceReversal;
        [Tooltip("Whether this shot requires cross-body motion.")]
        public bool RequiresCrossBody;

        [Header("Backswing")]
        [Tooltip("Minimum backswing-to-forward ratio.")]
        public float MinBackswingRatio;
        [Tooltip("Maximum backswing-to-forward ratio.")]
        public float MaxBackswingRatio;

        [Header("Follow-Through")]
        [Tooltip("Minimum follow-through height relative to shoulder.")]
        public float MinFollowThroughHeight;
        [Tooltip("Maximum follow-through height relative to shoulder.")]
        public float MaxFollowThroughHeight;

        [Header("Body Rotation (degrees)")]
        [Tooltip("Minimum body rotation during swing.")]
        public float MinBodyRotation;
        [Tooltip("Maximum body rotation during swing.")]
        public float MaxBodyRotation;

        [Header("Classification")]
        [Tooltip("Priority for resolving conflicts (higher wins).")]
        public int Priority;
        [Tooltip("Weight multiplier for confidence calculation.")]
        public float Weight;
    }
}
