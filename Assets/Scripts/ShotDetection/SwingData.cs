using System;
using System.Collections.Generic;
using UnityEngine;
using MetaCricket.MotionDetection;

namespace MetaCricket.ShotDetection
{
    /// <summary>
    /// Data class capturing a complete swing: list of PoseData frames during the swing,
    /// start/end timestamps, peak velocity, swing arc angle, and impact point estimation.
    /// </summary>
    [Serializable]
    public class SwingData
    {
        /// <summary>
        /// List of pose data frames captured during the swing.
        /// </summary>
        public List<PoseData> Frames;

        /// <summary>
        /// Timestamp when the swing was initiated.
        /// </summary>
        public float StartTime;

        /// <summary>
        /// Timestamp when the swing ended.
        /// </summary>
        public float EndTime;

        /// <summary>
        /// Duration of the swing in seconds.
        /// </summary>
        public float Duration => EndTime - StartTime;

        /// <summary>
        /// Peak velocity of the dominant wrist during the swing (normalized units/second).
        /// </summary>
        public float PeakVelocity;

        /// <summary>
        /// Average velocity of the swing.
        /// </summary>
        public float AverageVelocity;

        /// <summary>
        /// The total arc angle of the swing in degrees.
        /// </summary>
        public float SwingArcAngle;

        /// <summary>
        /// Estimated point of impact (where bat meets ball) in normalized coordinates.
        /// </summary>
        public Vector2 ImpactPoint;

        /// <summary>
        /// Direction vector of the swing at impact (normalized).
        /// </summary>
        public Vector2 SwingDirection;

        /// <summary>
        /// The dominant hand's wrist trajectory during the swing.
        /// </summary>
        public List<Vector2> WristTrajectory;

        /// <summary>
        /// Angle of the arm at the estimated point of impact.
        /// </summary>
        public float ArmAngleAtImpact;

        /// <summary>
        /// The swing plane angle: 0 = horizontal, 90 = vertical.
        /// </summary>
        public float SwingPlaneAngle;

        /// <summary>
        /// Amount of wrist rotation during the swing (in degrees).
        /// </summary>
        public float WristRotation;

        /// <summary>
        /// Whether a stance reversal was detected before the swing.
        /// </summary>
        public bool StanceReversalDetected;

        /// <summary>
        /// Ratio of backswing distance to forward swing distance.
        /// Low values indicate minimal backswing (flick-type shots).
        /// </summary>
        public float BackswingRatio;

        /// <summary>
        /// Whether the shot was played off the front foot or back foot.
        /// True = front foot, False = back foot.
        /// </summary>
        public bool IsFrontFoot;

        /// <summary>
        /// Height of the follow-through relative to shoulder height.
        /// Values > 1 indicate high follow-through (above shoulder).
        /// </summary>
        public float FollowThroughHeight;

        /// <summary>
        /// Whether cross-body motion was detected (arm crossing body center).
        /// </summary>
        public bool IsCrossBody;

        /// <summary>
        /// The body rotation angle during the swing (degrees).
        /// </summary>
        public float BodyRotation;

        public SwingData()
        {
            Frames = new List<PoseData>();
            WristTrajectory = new List<Vector2>();
            StartTime = 0f;
            EndTime = 0f;
            PeakVelocity = 0f;
            AverageVelocity = 0f;
            SwingArcAngle = 0f;
            ImpactPoint = Vector2.zero;
            SwingDirection = Vector2.zero;
            ArmAngleAtImpact = 0f;
            SwingPlaneAngle = 0f;
            WristRotation = 0f;
            StanceReversalDetected = false;
            BackswingRatio = 0f;
            IsFrontFoot = true;
            FollowThroughHeight = 0f;
            IsCrossBody = false;
            BodyRotation = 0f;
        }

        /// <summary>
        /// Calculate swing metrics from the captured frames.
        /// Should be called after all frames have been added.
        /// </summary>
        public void CalculateMetrics()
        {
            if (Frames == null || Frames.Count < 2)
                return;

            CalculateWristTrajectory();
            CalculateVelocities();
            CalculateSwingPlane();
            CalculateWristRotation();
            DetectStanceReversal();
            CalculateBackswingRatio();
            CalculateFollowThroughHeight();
            DetectCrossBodyMotion();
            CalculateBodyRotation();
        }

        /// <summary>
        /// Extract the wrist trajectory from pose frames.
        /// </summary>
        private void CalculateWristTrajectory()
        {
            WristTrajectory.Clear();

            foreach (var frame in Frames)
            {
                if (frame == null || !frame.IsDetected)
                    continue;

                // Use dominant (right) wrist for trajectory
                PoseJoint rightWrist = frame.Skeleton.GetJoint(JointType.RightWrist);
                if (rightWrist.IsValid)
                {
                    WristTrajectory.Add(rightWrist.Position);
                }
            }
        }

        /// <summary>
        /// Calculate peak and average velocities from wrist trajectory.
        /// </summary>
        private void CalculateVelocities()
        {
            if (WristTrajectory.Count < 2)
                return;

            float totalVelocity = 0f;
            float maxVelocity = 0f;
            int velocityCount = 0;

            for (int i = 1; i < WristTrajectory.Count; i++)
            {
                float distance = Vector2.Distance(WristTrajectory[i], WristTrajectory[i - 1]);
                float deltaTime = Duration / WristTrajectory.Count;

                if (deltaTime > 0)
                {
                    float velocity = distance / deltaTime;
                    totalVelocity += velocity;
                    velocityCount++;

                    if (velocity > maxVelocity)
                        maxVelocity = velocity;
                }
            }

            PeakVelocity = maxVelocity;
            AverageVelocity = velocityCount > 0 ? totalVelocity / velocityCount : 0f;
        }

        /// <summary>
        /// Calculate the swing plane angle from the wrist trajectory.
        /// </summary>
        private void CalculateSwingPlane()
        {
            if (WristTrajectory.Count < 2)
                return;

            Vector2 start = WristTrajectory[0];
            Vector2 end = WristTrajectory[WristTrajectory.Count - 1];
            Vector2 swingVector = end - start;

            // Calculate angle from horizontal (0 = horizontal, 90 = vertical)
            SwingPlaneAngle = Mathf.Abs(Mathf.Atan2(swingVector.y, swingVector.x) * Mathf.Rad2Deg);
            if (SwingPlaneAngle > 90f)
                SwingPlaneAngle = 180f - SwingPlaneAngle;

            SwingDirection = swingVector.normalized;

            // Calculate total arc angle
            float totalAngle = 0f;
            for (int i = 2; i < WristTrajectory.Count; i++)
            {
                Vector2 v1 = WristTrajectory[i - 1] - WristTrajectory[i - 2];
                Vector2 v2 = WristTrajectory[i] - WristTrajectory[i - 1];

                if (v1.magnitude > 0.001f && v2.magnitude > 0.001f)
                {
                    totalAngle += Vector2.Angle(v1, v2);
                }
            }
            SwingArcAngle = totalAngle;
        }

        /// <summary>
        /// Calculate wrist rotation during the swing.
        /// </summary>
        private void CalculateWristRotation()
        {
            if (Frames.Count < 3)
                return;

            float totalRotation = 0f;

            for (int i = 1; i < Frames.Count; i++)
            {
                if (Frames[i] == null || !Frames[i].IsDetected ||
                    Frames[i - 1] == null || !Frames[i - 1].IsDetected)
                    continue;

                PoseJoint currentElbow = Frames[i].Skeleton.GetJoint(JointType.RightElbow);
                PoseJoint currentWrist = Frames[i].Skeleton.GetJoint(JointType.RightWrist);
                PoseJoint prevElbow = Frames[i - 1].Skeleton.GetJoint(JointType.RightElbow);
                PoseJoint prevWrist = Frames[i - 1].Skeleton.GetJoint(JointType.RightWrist);

                if (!currentElbow.IsValid || !currentWrist.IsValid ||
                    !prevElbow.IsValid || !prevWrist.IsValid)
                    continue;

                Vector2 currentForearm = currentWrist.Position - currentElbow.Position;
                Vector2 prevForearm = prevWrist.Position - prevElbow.Position;

                float frameRotation = Vector2.SignedAngle(prevForearm, currentForearm);
                totalRotation += frameRotation;
            }

            WristRotation = Mathf.Abs(totalRotation);
        }

        /// <summary>
        /// Detect if a stance reversal occurred before the swing.
        /// </summary>
        private void DetectStanceReversal()
        {
            if (Frames.Count < 5)
            {
                StanceReversalDetected = false;
                return;
            }

            // Check early frames for hip position reversal
            PoseData firstFrame = Frames[0];
            int midIndex = Frames.Count / 3;
            PoseData midFrame = Frames[midIndex];

            if (firstFrame == null || !firstFrame.IsDetected ||
                midFrame == null || !midFrame.IsDetected)
            {
                StanceReversalDetected = false;
                return;
            }

            PoseJoint firstLeftHip = firstFrame.Skeleton.GetJoint(JointType.LeftHip);
            PoseJoint firstRightHip = firstFrame.Skeleton.GetJoint(JointType.RightHip);
            PoseJoint midLeftHip = midFrame.Skeleton.GetJoint(JointType.LeftHip);
            PoseJoint midRightHip = midFrame.Skeleton.GetJoint(JointType.RightHip);

            if (!firstLeftHip.IsValid || !firstRightHip.IsValid ||
                !midLeftHip.IsValid || !midRightHip.IsValid)
            {
                StanceReversalDetected = false;
                return;
            }

            // Check if left and right hip positions swapped (stance reversal)
            float firstHipDiff = firstLeftHip.Position.x - firstRightHip.Position.x;
            float midHipDiff = midLeftHip.Position.x - midRightHip.Position.x;

            StanceReversalDetected = (firstHipDiff > 0 && midHipDiff < 0) ||
                                     (firstHipDiff < 0 && midHipDiff > 0);
        }

        /// <summary>
        /// Calculate the ratio of backswing to forward swing distance.
        /// </summary>
        private void CalculateBackswingRatio()
        {
            if (WristTrajectory.Count < 3)
            {
                BackswingRatio = 0f;
                return;
            }

            // Find the peak velocity frame (swing initiation point)
            int peakIndex = 0;
            float maxDist = 0f;

            for (int i = 1; i < WristTrajectory.Count; i++)
            {
                float dist = Vector2.Distance(WristTrajectory[i], WristTrajectory[i - 1]);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    peakIndex = i;
                }
            }

            // Calculate backswing distance (before peak)
            float backswingDist = 0f;
            for (int i = 1; i < peakIndex && i < WristTrajectory.Count; i++)
            {
                backswingDist += Vector2.Distance(WristTrajectory[i], WristTrajectory[i - 1]);
            }

            // Calculate forward swing distance (after peak)
            float forwardDist = 0f;
            for (int i = peakIndex + 1; i < WristTrajectory.Count; i++)
            {
                forwardDist += Vector2.Distance(WristTrajectory[i], WristTrajectory[i - 1]);
            }

            BackswingRatio = forwardDist > 0.001f ? backswingDist / forwardDist : 0f;
        }

        /// <summary>
        /// Calculate the follow-through height relative to shoulder height.
        /// </summary>
        private void CalculateFollowThroughHeight()
        {
            if (Frames.Count < 2)
            {
                FollowThroughHeight = 0f;
                return;
            }

            PoseData lastFrame = Frames[Frames.Count - 1];
            if (lastFrame == null || !lastFrame.IsDetected)
            {
                FollowThroughHeight = 0f;
                return;
            }

            PoseJoint shoulder = lastFrame.Skeleton.GetJoint(JointType.RightShoulder);
            PoseJoint wrist = lastFrame.Skeleton.GetJoint(JointType.RightWrist);

            if (!shoulder.IsValid || !wrist.IsValid)
            {
                FollowThroughHeight = 0f;
                return;
            }

            // In screen space, lower Y = higher on screen
            float shoulderY = shoulder.Position.y;
            float wristY = wrist.Position.y;

            // If wrist is above shoulder (lower Y value), follow-through is high
            if (shoulderY > 0.001f)
            {
                FollowThroughHeight = (shoulderY - wristY) / shoulderY;
            }
        }

        /// <summary>
        /// Detect if the swing crosses the body center line.
        /// </summary>
        private void DetectCrossBodyMotion()
        {
            if (Frames.Count < 2 || WristTrajectory.Count < 2)
            {
                IsCrossBody = false;
                return;
            }

            // Get body center from shoulder positions
            PoseData referenceFrame = Frames[0];
            if (referenceFrame == null || !referenceFrame.IsDetected)
            {
                IsCrossBody = false;
                return;
            }

            PoseJoint leftShoulder = referenceFrame.Skeleton.GetJoint(JointType.LeftShoulder);
            PoseJoint rightShoulder = referenceFrame.Skeleton.GetJoint(JointType.RightShoulder);

            if (!leftShoulder.IsValid || !rightShoulder.IsValid)
            {
                IsCrossBody = false;
                return;
            }

            float bodyCenter = (leftShoulder.Position.x + rightShoulder.Position.x) / 2f;

            // Check if wrist starts on one side and ends on the other
            Vector2 startPos = WristTrajectory[0];
            Vector2 endPos = WristTrajectory[WristTrajectory.Count - 1];

            IsCrossBody = (startPos.x > bodyCenter && endPos.x < bodyCenter) ||
                         (startPos.x < bodyCenter && endPos.x > bodyCenter);
        }

        /// <summary>
        /// Calculate body rotation during the swing.
        /// </summary>
        private void CalculateBodyRotation()
        {
            if (Frames.Count < 2)
            {
                BodyRotation = 0f;
                return;
            }

            PoseData firstFrame = Frames[0];
            PoseData lastFrame = Frames[Frames.Count - 1];

            if (firstFrame == null || !firstFrame.IsDetected ||
                lastFrame == null || !lastFrame.IsDetected)
            {
                BodyRotation = 0f;
                return;
            }

            PoseJoint firstLeftShoulder = firstFrame.Skeleton.GetJoint(JointType.LeftShoulder);
            PoseJoint firstRightShoulder = firstFrame.Skeleton.GetJoint(JointType.RightShoulder);
            PoseJoint lastLeftShoulder = lastFrame.Skeleton.GetJoint(JointType.LeftShoulder);
            PoseJoint lastRightShoulder = lastFrame.Skeleton.GetJoint(JointType.RightShoulder);

            if (!firstLeftShoulder.IsValid || !firstRightShoulder.IsValid ||
                !lastLeftShoulder.IsValid || !lastRightShoulder.IsValid)
            {
                BodyRotation = 0f;
                return;
            }

            // Calculate shoulder line angle change
            Vector2 firstShoulderLine = firstRightShoulder.Position - firstLeftShoulder.Position;
            Vector2 lastShoulderLine = lastRightShoulder.Position - lastLeftShoulder.Position;

            BodyRotation = Vector2.Angle(firstShoulderLine, lastShoulderLine);
        }
    }
}
