using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetaCricket.MotionDetection
{
    /// <summary>
    /// Enum representing upper-body joint types tracked for motion detection.
    /// </summary>
    public enum JointType
    {
        Nose,
        LeftShoulder,
        RightShoulder,
        LeftElbow,
        RightElbow,
        LeftWrist,
        RightWrist,
        LeftHip,
        RightHip
    }

    /// <summary>
    /// Represents a single tracked joint with position and confidence.
    /// </summary>
    [Serializable]
    public struct PoseJoint
    {
        /// <summary>
        /// Normalized position of the joint (0-1 range in screen space).
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// 3D world position when depth estimation is available.
        /// </summary>
        public Vector3 WorldPosition;

        /// <summary>
        /// Confidence score for this joint detection (0-1).
        /// </summary>
        public float Confidence;

        /// <summary>
        /// The type of joint this represents.
        /// </summary>
        public JointType JointType;

        /// <summary>
        /// Whether the joint detection meets the minimum confidence threshold.
        /// </summary>
        public bool IsValid => Confidence >= MinConfidenceThreshold;

        /// <summary>
        /// Minimum confidence threshold for a joint to be considered valid.
        /// </summary>
        public const float MinConfidenceThreshold = 0.3f;

        public PoseJoint(JointType jointType, Vector2 position, float confidence)
        {
            JointType = jointType;
            Position = position;
            WorldPosition = Vector3.zero;
            Confidence = confidence;
        }

        public PoseJoint(JointType jointType, Vector2 position, Vector3 worldPosition, float confidence)
        {
            JointType = jointType;
            Position = position;
            WorldPosition = worldPosition;
            Confidence = confidence;
        }
    }

    /// <summary>
    /// Represents a complete upper-body pose skeleton at a single point in time.
    /// </summary>
    [Serializable]
    public class PoseSkeleton
    {
        /// <summary>
        /// Dictionary mapping joint types to their detected pose joints.
        /// </summary>
        public Dictionary<JointType, PoseJoint> Joints { get; private set; }

        /// <summary>
        /// Timestamp when this pose was captured.
        /// </summary>
        public float Timestamp { get; set; }

        /// <summary>
        /// Overall confidence of the full pose detection.
        /// </summary>
        public float OverallConfidence { get; set; }

        /// <summary>
        /// Whether the pose has sufficient valid joints for processing.
        /// </summary>
        public bool IsValid => GetValidJointCount() >= MinValidJoints;

        /// <summary>
        /// Minimum number of valid joints required for a pose to be usable.
        /// </summary>
        public const int MinValidJoints = 5;

        public PoseSkeleton()
        {
            Joints = new Dictionary<JointType, PoseJoint>();
            Timestamp = 0f;
            OverallConfidence = 0f;
        }

        /// <summary>
        /// Sets a joint in the skeleton.
        /// </summary>
        public void SetJoint(PoseJoint joint)
        {
            Joints[joint.JointType] = joint;
        }

        /// <summary>
        /// Gets a joint by type. Returns default if not found.
        /// </summary>
        public PoseJoint GetJoint(JointType jointType)
        {
            if (Joints.TryGetValue(jointType, out PoseJoint joint))
            {
                return joint;
            }
            return default;
        }

        /// <summary>
        /// Checks if a specific joint exists and is valid.
        /// </summary>
        public bool HasValidJoint(JointType jointType)
        {
            return Joints.TryGetValue(jointType, out PoseJoint joint) && joint.IsValid;
        }

        /// <summary>
        /// Returns the count of valid joints in this pose.
        /// </summary>
        public int GetValidJointCount()
        {
            int count = 0;
            foreach (var kvp in Joints)
            {
                if (kvp.Value.IsValid)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Calculates the overall confidence as the average of all joint confidences.
        /// </summary>
        public void RecalculateOverallConfidence()
        {
            if (Joints.Count == 0)
            {
                OverallConfidence = 0f;
                return;
            }

            float totalConfidence = 0f;
            foreach (var kvp in Joints)
            {
                totalConfidence += kvp.Value.Confidence;
            }
            OverallConfidence = totalConfidence / Joints.Count;
        }
    }

    /// <summary>
    /// Complete pose data including skeleton, metadata, and frame information.
    /// </summary>
    [Serializable]
    public class PoseData
    {
        /// <summary>
        /// The detected pose skeleton.
        /// </summary>
        public PoseSkeleton Skeleton;

        /// <summary>
        /// Frame number this pose was captured on.
        /// </summary>
        public int FrameNumber;

        /// <summary>
        /// Time since the last pose update in seconds.
        /// </summary>
        public float DeltaTime;

        /// <summary>
        /// Whether this pose data represents a successfully detected pose.
        /// </summary>
        public bool IsDetected;

        public PoseData()
        {
            Skeleton = new PoseSkeleton();
            FrameNumber = 0;
            DeltaTime = 0f;
            IsDetected = false;
        }

        public PoseData(PoseSkeleton skeleton, int frameNumber, float deltaTime)
        {
            Skeleton = skeleton;
            FrameNumber = frameNumber;
            DeltaTime = deltaTime;
            IsDetected = skeleton != null && skeleton.IsValid;
        }
    }
}
