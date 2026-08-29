using System;
using System.Collections.Generic;
using UnityEngine;
using MetaCricket.MotionDetection;

namespace MetaCricket.Calibration
{
    /// <summary>
    /// Serializable class storing baseline calibration measurements including
    /// arm lengths, shoulder width, neutral stance joint positions, and detection zone boundaries.
    /// </summary>
    [Serializable]
    public class CalibrationData
    {
        /// <summary>
        /// Measured left arm length (shoulder to wrist) in normalized coordinates.
        /// </summary>
        public float LeftArmLength;

        /// <summary>
        /// Measured right arm length (shoulder to wrist) in normalized coordinates.
        /// </summary>
        public float RightArmLength;

        /// <summary>
        /// Distance between shoulders in normalized coordinates.
        /// </summary>
        public float ShoulderWidth;

        /// <summary>
        /// Distance from shoulder to hip in normalized coordinates.
        /// </summary>
        public float TorsoLength;

        /// <summary>
        /// Neutral standing position of the player's nose/head.
        /// </summary>
        public Vector2 NeutralHeadPosition;

        /// <summary>
        /// Neutral position for left shoulder.
        /// </summary>
        public Vector2 NeutralLeftShoulderPosition;

        /// <summary>
        /// Neutral position for right shoulder.
        /// </summary>
        public Vector2 NeutralRightShoulderPosition;

        /// <summary>
        /// Neutral position for left hip.
        /// </summary>
        public Vector2 NeutralLeftHipPosition;

        /// <summary>
        /// Neutral position for right hip.
        /// </summary>
        public Vector2 NeutralRightHipPosition;

        /// <summary>
        /// All neutral joint positions captured during calibration.
        /// </summary>
        public SerializableJointPositions NeutralStancePositions;

        /// <summary>
        /// Upper boundary of the detection zone (top of frame usable area).
        /// </summary>
        public float DetectionZoneTop;

        /// <summary>
        /// Lower boundary of the detection zone.
        /// </summary>
        public float DetectionZoneBottom;

        /// <summary>
        /// Left boundary of the detection zone.
        /// </summary>
        public float DetectionZoneLeft;

        /// <summary>
        /// Right boundary of the detection zone.
        /// </summary>
        public float DetectionZoneRight;

        /// <summary>
        /// Scale factor to normalize movements relative to body size.
        /// </summary>
        public float BodyScaleFactor;

        /// <summary>
        /// Timestamp when calibration was performed.
        /// </summary>
        public long CalibrationTimestamp;

        /// <summary>
        /// Whether this calibration data is valid and usable.
        /// </summary>
        public bool IsValid;

        public CalibrationData()
        {
            LeftArmLength = 0f;
            RightArmLength = 0f;
            ShoulderWidth = 0f;
            TorsoLength = 0f;
            NeutralHeadPosition = Vector2.zero;
            NeutralLeftShoulderPosition = Vector2.zero;
            NeutralRightShoulderPosition = Vector2.zero;
            NeutralLeftHipPosition = Vector2.zero;
            NeutralRightHipPosition = Vector2.zero;
            NeutralStancePositions = new SerializableJointPositions();
            DetectionZoneTop = 0f;
            DetectionZoneBottom = 1f;
            DetectionZoneLeft = 0f;
            DetectionZoneRight = 1f;
            BodyScaleFactor = 1f;
            CalibrationTimestamp = 0;
            IsValid = false;
        }

        /// <summary>
        /// Calculate body scale factor based on shoulder width.
        /// This normalizes movement detection relative to the player's body size.
        /// </summary>
        public void CalculateBodyScaleFactor()
        {
            // Use shoulder width as the reference measurement
            // A typical shoulder width in normalized coords is around 0.2-0.4
            const float referenceShoulderWidth = 0.3f;

            if (ShoulderWidth > 0.01f)
            {
                BodyScaleFactor = referenceShoulderWidth / ShoulderWidth;
            }
            else
            {
                BodyScaleFactor = 1f;
            }
        }

        /// <summary>
        /// Calculate detection zone boundaries based on the calibrated pose.
        /// Provides margins around the player for comfortable movement detection.
        /// </summary>
        public void CalculateDetectionZone()
        {
            float margin = ShoulderWidth * 1.5f;

            DetectionZoneLeft = Mathf.Max(0f, NeutralLeftShoulderPosition.x - LeftArmLength - margin);
            DetectionZoneRight = Mathf.Min(1f, NeutralRightShoulderPosition.x + RightArmLength + margin);
            DetectionZoneTop = Mathf.Max(0f, NeutralHeadPosition.y - margin);
            DetectionZoneBottom = Mathf.Min(1f, NeutralLeftHipPosition.y + margin);
        }

        /// <summary>
        /// Get the average arm length.
        /// </summary>
        public float GetAverageArmLength()
        {
            return (LeftArmLength + RightArmLength) / 2f;
        }
    }

    /// <summary>
    /// Serializable wrapper for joint positions since Unity cannot
    /// serialize Dictionaries directly.
    /// </summary>
    [Serializable]
    public class SerializableJointPositions
    {
        public List<JointPositionEntry> Entries;

        public SerializableJointPositions()
        {
            Entries = new List<JointPositionEntry>();
        }

        public void SetPosition(JointType jointType, Vector2 position)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].JointType == jointType)
                {
                    Entries[i] = new JointPositionEntry(jointType, position);
                    return;
                }
            }
            Entries.Add(new JointPositionEntry(jointType, position));
        }

        public Vector2 GetPosition(JointType jointType)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].JointType == jointType)
                {
                    return Entries[i].Position;
                }
            }
            return Vector2.zero;
        }

        public bool HasPosition(JointType jointType)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].JointType == jointType)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Single entry mapping a joint type to a position.
    /// </summary>
    [Serializable]
    public struct JointPositionEntry
    {
        public JointType JointType;
        public Vector2 Position;

        public JointPositionEntry(JointType jointType, Vector2 position)
        {
            JointType = jointType;
            Position = position;
        }
    }
}
