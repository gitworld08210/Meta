using UnityEngine;
using MetaCricket.MotionDetection;

namespace MetaCricket.Calibration
{
    /// <summary>
    /// Analyzes PoseData to detect a T-pose: both arms extended horizontally
    /// with shoulder-elbow-wrist roughly collinear and horizontal.
    /// Requires the pose to be held for a configurable duration (default 2 seconds).
    /// </summary>
    public class TPoseDetector : MonoBehaviour
    {
        [Header("Detection Parameters")]
        [SerializeField]
        [Tooltip("Maximum allowed angle deviation from horizontal for arms (in degrees).")]
        private float _maxArmAngleDeviation = 20f;

        [SerializeField]
        [Tooltip("Maximum allowed angle deviation for arm collinearity (in degrees).")]
        private float _maxCollinearityDeviation = 25f;

        [SerializeField]
        [Tooltip("Minimum confidence threshold for joints to be considered valid.")]
        private float _minimumJointConfidence = 0.5f;

        [SerializeField]
        [Tooltip("Duration the T-pose must be held (in seconds).")]
        private float _holdDuration = 2.0f;

        [SerializeField]
        [Tooltip("Maximum movement allowed while holding the pose (normalized units).")]
        private float _maxMovementDuringHold = 0.03f;

        /// <summary>
        /// Whether a T-pose is currently being detected.
        /// </summary>
        public bool IsTPoseDetected { get; private set; }

        /// <summary>
        /// Progress toward the hold duration requirement (0-1).
        /// </summary>
        public float HoldProgress { get; private set; }

        /// <summary>
        /// Whether the T-pose has been held for the required duration.
        /// </summary>
        public bool IsTPoseHeld { get; private set; }

        private float _tposeStartTime;
        private bool _wasTPoseLastFrame;
        private PoseSkeleton _holdStartSkeleton;

        /// <summary>
        /// Analyze a pose frame to determine if the player is in a T-pose.
        /// Returns true if the T-pose has been held for the required duration.
        /// </summary>
        /// <param name="poseData">The current pose data to analyze.</param>
        /// <returns>True if T-pose has been held for the full required duration.</returns>
        public bool AnalyzePose(PoseData poseData)
        {
            if (poseData == null || !poseData.IsDetected)
            {
                ResetDetection();
                return false;
            }

            PoseSkeleton skeleton = poseData.Skeleton;

            // Check all required joints are present and valid
            if (!HasRequiredJoints(skeleton))
            {
                ResetDetection();
                return false;
            }

            // Check if current pose matches T-pose criteria
            bool isTPose = CheckTPoseCriteria(skeleton);
            IsTPoseDetected = isTPose;

            if (isTPose)
            {
                if (!_wasTPoseLastFrame)
                {
                    // T-pose just started
                    _tposeStartTime = Time.time;
                    _holdStartSkeleton = skeleton;
                }
                else
                {
                    // T-pose continuing - check for excessive movement
                    if (HasExcessiveMovement(skeleton))
                    {
                        // Movement detected, reset hold timer
                        _tposeStartTime = Time.time;
                        _holdStartSkeleton = skeleton;
                    }
                }

                float holdTime = Time.time - _tposeStartTime;
                HoldProgress = Mathf.Clamp01(holdTime / _holdDuration);
                IsTPoseHeld = holdTime >= _holdDuration;

                _wasTPoseLastFrame = true;
                return IsTPoseHeld;
            }
            else
            {
                ResetDetection();
                return false;
            }
        }

        /// <summary>
        /// Check if the skeleton has all joints required for T-pose detection.
        /// </summary>
        private bool HasRequiredJoints(PoseSkeleton skeleton)
        {
            return skeleton.HasValidJoint(JointType.LeftShoulder) &&
                   skeleton.HasValidJoint(JointType.RightShoulder) &&
                   skeleton.HasValidJoint(JointType.LeftElbow) &&
                   skeleton.HasValidJoint(JointType.RightElbow) &&
                   skeleton.HasValidJoint(JointType.LeftWrist) &&
                   skeleton.HasValidJoint(JointType.RightWrist);
        }

        /// <summary>
        /// Check if the pose meets T-pose criteria:
        /// 1. Both arms roughly horizontal
        /// 2. Arms are straight (shoulder-elbow-wrist collinear)
        /// </summary>
        private bool CheckTPoseCriteria(PoseSkeleton skeleton)
        {
            PoseJoint leftShoulder = skeleton.GetJoint(JointType.LeftShoulder);
            PoseJoint rightShoulder = skeleton.GetJoint(JointType.RightShoulder);
            PoseJoint leftElbow = skeleton.GetJoint(JointType.LeftElbow);
            PoseJoint rightElbow = skeleton.GetJoint(JointType.RightElbow);
            PoseJoint leftWrist = skeleton.GetJoint(JointType.LeftWrist);
            PoseJoint rightWrist = skeleton.GetJoint(JointType.RightWrist);

            // Check confidence thresholds
            if (leftShoulder.Confidence < _minimumJointConfidence ||
                rightShoulder.Confidence < _minimumJointConfidence ||
                leftElbow.Confidence < _minimumJointConfidence ||
                rightElbow.Confidence < _minimumJointConfidence ||
                leftWrist.Confidence < _minimumJointConfidence ||
                rightWrist.Confidence < _minimumJointConfidence)
            {
                return false;
            }

            // Check left arm is horizontal
            if (!IsArmHorizontal(leftShoulder.Position, leftElbow.Position, leftWrist.Position))
                return false;

            // Check right arm is horizontal
            if (!IsArmHorizontal(rightShoulder.Position, rightElbow.Position, rightWrist.Position))
                return false;

            // Check left arm collinearity (shoulder-elbow-wrist roughly in a line)
            if (!IsArmStraight(leftShoulder.Position, leftElbow.Position, leftWrist.Position))
                return false;

            // Check right arm collinearity
            if (!IsArmStraight(rightShoulder.Position, rightElbow.Position, rightWrist.Position))
                return false;

            return true;
        }

        /// <summary>
        /// Check if an arm (shoulder to wrist) is approximately horizontal.
        /// </summary>
        private bool IsArmHorizontal(Vector2 shoulder, Vector2 elbow, Vector2 wrist)
        {
            // Calculate angle of shoulder-to-wrist vector from horizontal
            Vector2 armDirection = wrist - shoulder;
            float angle = Mathf.Abs(Mathf.Atan2(armDirection.y, armDirection.x) * Mathf.Rad2Deg);

            // Normalize to 0-180 range, horizontal is 0 or 180 degrees
            float deviationFromHorizontal = Mathf.Min(angle, 180f - angle);

            return deviationFromHorizontal <= _maxArmAngleDeviation;
        }

        /// <summary>
        /// Check if an arm is roughly straight (shoulder, elbow, wrist are collinear).
        /// </summary>
        private bool IsArmStraight(Vector2 shoulder, Vector2 elbow, Vector2 wrist)
        {
            // Calculate angle at the elbow
            Vector2 shoulderToElbow = elbow - shoulder;
            Vector2 elbowToWrist = wrist - elbow;

            float angle = Vector2.Angle(shoulderToElbow, elbowToWrist);

            // A straight arm has an angle close to 0 (vectors point in same direction)
            return angle <= _maxCollinearityDeviation;
        }

        /// <summary>
        /// Check if the player has moved too much since the T-pose hold started.
        /// </summary>
        private bool HasExcessiveMovement(PoseSkeleton currentSkeleton)
        {
            if (_holdStartSkeleton == null)
                return false;

            // Check shoulder positions for stability
            PoseJoint currentLeftShoulder = currentSkeleton.GetJoint(JointType.LeftShoulder);
            PoseJoint currentRightShoulder = currentSkeleton.GetJoint(JointType.RightShoulder);
            PoseJoint startLeftShoulder = _holdStartSkeleton.GetJoint(JointType.LeftShoulder);
            PoseJoint startRightShoulder = _holdStartSkeleton.GetJoint(JointType.RightShoulder);

            float leftShoulderMovement = Vector2.Distance(
                currentLeftShoulder.Position, startLeftShoulder.Position);
            float rightShoulderMovement = Vector2.Distance(
                currentRightShoulder.Position, startRightShoulder.Position);

            return leftShoulderMovement > _maxMovementDuringHold ||
                   rightShoulderMovement > _maxMovementDuringHold;
        }

        /// <summary>
        /// Reset all detection state.
        /// </summary>
        public void ResetDetection()
        {
            IsTPoseDetected = false;
            IsTPoseHeld = false;
            HoldProgress = 0f;
            _wasTPoseLastFrame = false;
            _holdStartSkeleton = null;
        }

        /// <summary>
        /// Get the captured pose data when T-pose is confirmed held.
        /// Used by CalibrationManager to extract baseline measurements.
        /// </summary>
        /// <param name="poseData">The current T-pose data.</param>
        /// <returns>CalibrationData with baseline measurements.</returns>
        public CalibrationData ExtractCalibrationData(PoseData poseData)
        {
            if (poseData == null || !poseData.IsDetected)
                return null;

            PoseSkeleton skeleton = poseData.Skeleton;
            CalibrationData data = new CalibrationData();

            PoseJoint leftShoulder = skeleton.GetJoint(JointType.LeftShoulder);
            PoseJoint rightShoulder = skeleton.GetJoint(JointType.RightShoulder);
            PoseJoint leftElbow = skeleton.GetJoint(JointType.LeftElbow);
            PoseJoint rightElbow = skeleton.GetJoint(JointType.RightElbow);
            PoseJoint leftWrist = skeleton.GetJoint(JointType.LeftWrist);
            PoseJoint rightWrist = skeleton.GetJoint(JointType.RightWrist);
            PoseJoint leftHip = skeleton.GetJoint(JointType.LeftHip);
            PoseJoint rightHip = skeleton.GetJoint(JointType.RightHip);
            PoseJoint nose = skeleton.GetJoint(JointType.Nose);

            // Calculate arm lengths
            data.LeftArmLength = Vector2.Distance(leftShoulder.Position, leftElbow.Position)
                               + Vector2.Distance(leftElbow.Position, leftWrist.Position);
            data.RightArmLength = Vector2.Distance(rightShoulder.Position, rightElbow.Position)
                                + Vector2.Distance(rightElbow.Position, rightWrist.Position);

            // Calculate shoulder width
            data.ShoulderWidth = Vector2.Distance(leftShoulder.Position, rightShoulder.Position);

            // Calculate torso length
            Vector2 shoulderCenter = (leftShoulder.Position + rightShoulder.Position) / 2f;
            Vector2 hipCenter = (leftHip.Position + rightHip.Position) / 2f;
            data.TorsoLength = Vector2.Distance(shoulderCenter, hipCenter);

            // Store neutral positions
            data.NeutralHeadPosition = nose.Position;
            data.NeutralLeftShoulderPosition = leftShoulder.Position;
            data.NeutralRightShoulderPosition = rightShoulder.Position;
            data.NeutralLeftHipPosition = leftHip.Position;
            data.NeutralRightHipPosition = rightHip.Position;

            // Store all joint positions
            foreach (var kvp in skeleton.Joints)
            {
                data.NeutralStancePositions.SetPosition(kvp.Key, kvp.Value.Position);
            }

            // Calculate derived values
            data.CalculateBodyScaleFactor();
            data.CalculateDetectionZone();
            data.CalibrationTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            data.IsValid = true;

            return data;
        }
    }
}
