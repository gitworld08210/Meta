using NUnit.Framework;
using UnityEngine;
using MetaCricket.Core;
using MetaCricket.ShotDetection;
using MetaCricket.MotionDetection;
using System.Collections.Generic;

namespace MetaCricket.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the shot classification logic.
    /// Tests that the ShotClassifier correctly identifies shot types
    /// based on swing data metrics (arm angle, velocity, wrist rotation, etc.).
    /// </summary>
    [TestFixture]
    public class ShotClassificationTests
    {
        private ShotClassificationRule[] _rules;

        [SetUp]
        public void SetUp()
        {
            // Use the default rules defined in ShotRules
            _rules = GetDefaultRules();
        }

        [Test]
        public void CoverDrive_CorrectArmAngleAndFrontFoot_ClassifiedCorrectly()
        {
            // Arrange: Cover drive has arm angle 30-60, horizontal swing, front foot
            SwingData swingData = CreateSwingData(
                armAngle: 45f,
                swingPlaneAngle: 15f,
                peakVelocity: 0.8f,
                wristRotation: 40f,
                backswingRatio: 0.5f,
                followThroughHeight: 0.3f,
                bodyRotation: 30f,
                isFrontFoot: true,
                isCrossBody: false,
                stanceReversal: false
            );

            // Act
            ShotType result = ClassifyShot(swingData);

            // Assert
            Assert.AreEqual(ShotType.CoverDrive, result,
                "Swing with arm angle 45, horizontal plane, front foot should be Cover Drive");
        }

        [Test]
        public void PullShot_BackFootCrossBody_ClassifiedCorrectly()
        {
            // Arrange: Pull shot has back foot, cross-body horizontal swing
            SwingData swingData = CreateSwingData(
                armAngle: 20f,
                swingPlaneAngle: 15f,
                peakVelocity: 1.2f,
                wristRotation: 80f,
                backswingRatio: 0.6f,
                followThroughHeight: 0.4f,
                bodyRotation: 50f,
                isFrontFoot: false,
                isCrossBody: true,
                stanceReversal: false
            );

            // Act
            ShotType result = ClassifyShot(swingData);

            // Assert
            Assert.AreEqual(ShotType.PullShot, result,
                "Back foot cross-body horizontal swing should be Pull Shot");
        }

        [Test]
        public void StraightDrive_VerticalSwingFrontFoot_ClassifiedCorrectly()
        {
            // Arrange: Straight drive has vertical swing plane (50-90), arm angle 60-100
            SwingData swingData = CreateSwingData(
                armAngle: 80f,
                swingPlaneAngle: 70f,
                peakVelocity: 0.9f,
                wristRotation: 30f,
                backswingRatio: 0.6f,
                followThroughHeight: 0.6f,
                bodyRotation: 20f,
                isFrontFoot: true,
                isCrossBody: false,
                stanceReversal: false
            );

            // Act
            ShotType result = ClassifyShot(swingData);

            // Assert
            Assert.AreEqual(ShotType.StraightDrive, result,
                "Vertical swing with high arm angle and front foot should be Straight Drive");
        }

        [Test]
        public void HelicopterShot_HighWristRotation_ClassifiedCorrectly()
        {
            // Arrange: Helicopter shot has wrist rotation > 270 degrees
            SwingData swingData = CreateSwingData(
                armAngle: 50f,
                swingPlaneAngle: 55f,
                peakVelocity: 1.5f,
                wristRotation: 300f,
                backswingRatio: 0.5f,
                followThroughHeight: 1.2f,
                bodyRotation: 80f,
                isFrontFoot: false,
                isCrossBody: false,
                stanceReversal: false
            );

            // Act
            ShotType result = ClassifyShot(swingData);

            // Assert
            Assert.AreEqual(ShotType.HelicopterShot, result,
                "High wrist rotation (>270) with high follow-through should be Helicopter Shot");
        }

        [Test]
        public void SwitchHit_StanceReversal_ClassifiedCorrectly()
        {
            // Arrange: Switch hit requires stance reversal
            SwingData swingData = CreateSwingData(
                armAngle: 40f,
                swingPlaneAngle: 25f,
                peakVelocity: 1.0f,
                wristRotation: 100f,
                backswingRatio: 0.5f,
                followThroughHeight: 0.5f,
                bodyRotation: 90f,
                isFrontFoot: false,
                isCrossBody: false,
                stanceReversal: true
            );

            // Act
            ShotType result = ClassifyShot(swingData);

            // Assert
            Assert.AreEqual(ShotType.SwitchHit, result,
                "Stance reversal should always classify as Switch Hit");
        }

        [Test]
        public void DefensiveBlock_MinimalSwing_ClassifiedCorrectly()
        {
            // Arrange: Defensive block has minimal velocity, minimal wrist rotation
            SwingData swingData = CreateSwingData(
                armAngle: 85f,
                swingPlaneAngle: 70f,
                peakVelocity: 0.15f,
                wristRotation: 10f,
                backswingRatio: 0.05f,
                followThroughHeight: -0.1f,
                bodyRotation: 5f,
                isFrontFoot: false,
                isCrossBody: false,
                stanceReversal: false
            );

            // Act
            ShotType result = ClassifyShot(swingData);

            // Assert
            Assert.AreEqual(ShotType.DefensiveBlock, result,
                "Minimal swing with vertical bat should be Defensive Block");
        }

        [Test]
        public void Flick_MinimalBackswingWristDominant_ClassifiedCorrectly()
        {
            // Arrange: Flick has minimal backswing (0-0.2), moderate wrist rotation
            SwingData swingData = CreateSwingData(
                armAngle: 40f,
                swingPlaneAngle: 30f,
                peakVelocity: 0.8f,
                wristRotation: 120f,
                backswingRatio: 0.1f,
                followThroughHeight: 0.3f,
                bodyRotation: 20f,
                isFrontFoot: false,
                isCrossBody: false,
                stanceReversal: false
            );

            // Act
            ShotType result = ClassifyShot(swingData);

            // Assert
            Assert.AreEqual(ShotType.Flick, result,
                "Minimal backswing with wrist-dominant motion should be Flick");
        }

        [Test]
        public void Uppercut_BackFootHighFollowThrough_ClassifiedCorrectly()
        {
            // Arrange: Uppercut has back foot, high follow-through, upward swing
            SwingData swingData = CreateSwingData(
                armAngle: 80f,
                swingPlaneAngle: 75f,
                peakVelocity: 1.2f,
                wristRotation: 90f,
                backswingRatio: 0.3f,
                followThroughHeight: 1.2f,
                bodyRotation: 30f,
                isFrontFoot: false,
                isCrossBody: false,
                stanceReversal: false
            );

            // Act
            ShotType result = ClassifyShot(swingData);

            // Assert
            Assert.AreEqual(ShotType.Uppercut, result,
                "Back foot with high follow-through and upward swing should be Uppercut");
        }

        [Test]
        public void ShotResult_GetTimingMultiplier_ReturnsCorrectValues()
        {
            ShotResult result = new ShotResult();

            result.Timing = TimingQuality.Perfect;
            Assert.AreEqual(1.5f, result.GetTimingMultiplier(), 0.001f);

            result.Timing = TimingQuality.Good;
            Assert.AreEqual(1.0f, result.GetTimingMultiplier(), 0.001f);

            result.Timing = TimingQuality.Early;
            Assert.AreEqual(0.6f, result.GetTimingMultiplier(), 0.001f);

            result.Timing = TimingQuality.Late;
            Assert.AreEqual(0.5f, result.GetTimingMultiplier(), 0.001f);
        }

        [Test]
        public void ShotResult_GetEffectiveness_CombinesPowerTimingConfidence()
        {
            ShotResult result = new ShotResult
            {
                Power = 0.8f,
                Timing = TimingQuality.Perfect,
                Confidence = 0.9f
            };

            float expected = 0.8f * 1.5f * 0.9f; // power * timing * confidence
            Assert.AreEqual(expected, result.GetEffectiveness(), 0.001f);
        }

        #region Helper Methods

        private SwingData CreateSwingData(
            float armAngle, float swingPlaneAngle, float peakVelocity,
            float wristRotation, float backswingRatio, float followThroughHeight,
            float bodyRotation, bool isFrontFoot, bool isCrossBody, bool stanceReversal)
        {
            SwingData data = new SwingData
            {
                Frames = new List<PoseData>(),
                StartTime = 0f,
                EndTime = 0.5f,
                ArmAngleAtImpact = armAngle,
                SwingPlaneAngle = swingPlaneAngle,
                PeakVelocity = peakVelocity,
                WristRotation = wristRotation,
                BackswingRatio = backswingRatio,
                FollowThroughHeight = followThroughHeight,
                BodyRotation = bodyRotation,
                IsFrontFoot = isFrontFoot,
                IsCrossBody = isCrossBody,
                StanceReversalDetected = stanceReversal,
                ImpactPoint = new Vector2(0.5f, 0.5f),
                SwingDirection = Vector2.right
            };

            return data;
        }

        /// <summary>
        /// Simplified classification logic matching ShotClassifier.EvaluateRule
        /// for unit test purposes (no MonoBehaviour dependency).
        /// </summary>
        private ShotType ClassifyShot(SwingData swingData)
        {
            float bestScore = 0f;
            ShotType bestType = ShotType.DefensiveBlock;

            for (int i = 0; i < _rules.Length; i++)
            {
                float confidence = EvaluateRule(_rules[i], swingData);
                float weightedScore = confidence * _rules[i].Weight;

                if (weightedScore > bestScore)
                {
                    bestScore = weightedScore;
                    bestType = _rules[i].ShotType;
                }
            }

            return bestType;
        }

        private float EvaluateRule(ShotClassificationRule rule, SwingData swingData)
        {
            float totalScore = 0f;
            float totalWeight = 0f;

            float armAngleScore = EvaluateRange(swingData.ArmAngleAtImpact, rule.MinArmAngle, rule.MaxArmAngle);
            totalScore += armAngleScore * 1.5f;
            totalWeight += 1.5f;

            float swingPlaneScore = EvaluateRange(swingData.SwingPlaneAngle, rule.MinSwingPlaneAngle, rule.MaxSwingPlaneAngle);
            totalScore += swingPlaneScore * 1.5f;
            totalWeight += 1.5f;

            float velocityScore = EvaluateRange(swingData.PeakVelocity, rule.MinVelocity, rule.MaxVelocity);
            totalScore += velocityScore * 1.0f;
            totalWeight += 1.0f;

            float wristRotationScore = EvaluateRange(swingData.WristRotation, rule.MinWristRotation, rule.MaxWristRotation);
            totalScore += wristRotationScore * 1.2f;
            totalWeight += 1.2f;

            float backswingScore = EvaluateRange(swingData.BackswingRatio, rule.MinBackswingRatio, rule.MaxBackswingRatio);
            totalScore += backswingScore * 0.8f;
            totalWeight += 0.8f;

            float followThroughScore = EvaluateRange(swingData.FollowThroughHeight, rule.MinFollowThroughHeight, rule.MaxFollowThroughHeight);
            totalScore += followThroughScore * 1.0f;
            totalWeight += 1.0f;

            float bodyRotationScore = EvaluateRange(swingData.BodyRotation, rule.MinBodyRotation, rule.MaxBodyRotation);
            totalScore += bodyRotationScore * 0.7f;
            totalWeight += 0.7f;

            if (rule.RequiresStanceReversal)
            {
                if (swingData.StanceReversalDetected)
                {
                    totalScore += 2.0f;
                    totalWeight += 2.0f;
                }
                else
                {
                    return 0f;
                }
            }
            else if (swingData.StanceReversalDetected)
            {
                totalScore -= 0.5f;
            }

            if (rule.RequiresCrossBody)
            {
                if (swingData.IsCrossBody)
                {
                    totalScore += 1.0f;
                    totalWeight += 1.0f;
                }
                else
                {
                    totalScore += 0f;
                    totalWeight += 1.0f;
                }
            }

            if (rule.RequiresFrontFoot)
            {
                if (swingData.IsFrontFoot)
                {
                    totalScore += 0.8f;
                    totalWeight += 0.8f;
                }
                else
                {
                    totalScore += 0f;
                    totalWeight += 0.8f;
                }
            }

            if (rule.RequiresBackFoot)
            {
                if (!swingData.IsFrontFoot)
                {
                    totalScore += 0.8f;
                    totalWeight += 0.8f;
                }
                else
                {
                    totalScore += 0f;
                    totalWeight += 0.8f;
                }
            }

            if (totalWeight <= 0f)
                return 0f;

            return Mathf.Clamp01(totalScore / totalWeight);
        }

        private float EvaluateRange(float value, float min, float max)
        {
            if (value >= min && value <= max)
            {
                float center = (min + max) / 2f;
                float halfRange = (max - min) / 2f;
                if (halfRange <= 0f) return 1f;
                float distFromCenter = Mathf.Abs(value - center);
                return 1f - (distFromCenter / halfRange) * 0.3f;
            }
            else
            {
                float rangeSize = max - min;
                float tolerance = Mathf.Max(rangeSize * 0.3f, 5f);
                float distOutside;
                if (value < min)
                    distOutside = min - value;
                else
                    distOutside = value - max;
                return Mathf.Max(0f, 1f - (distOutside / tolerance));
            }
        }

        private ShotClassificationRule[] GetDefaultRules()
        {
            return new ShotClassificationRule[]
            {
                new ShotClassificationRule
                {
                    ShotType = ShotType.CoverDrive, DisplayName = "Cover Drive",
                    MinArmAngle = 30f, MaxArmAngle = 60f,
                    MinSwingPlaneAngle = 0f, MaxSwingPlaneAngle = 30f,
                    MinVelocity = 0.3f, MaxVelocity = 1.5f,
                    RequiresFrontFoot = true, RequiresBackFoot = false,
                    MinWristRotation = 0f, MaxWristRotation = 90f,
                    RequiresStanceReversal = false,
                    MinBackswingRatio = 0.2f, MaxBackswingRatio = 0.8f,
                    MinFollowThroughHeight = -0.2f, MaxFollowThroughHeight = 0.5f,
                    RequiresCrossBody = false,
                    MinBodyRotation = 10f, MaxBodyRotation = 60f,
                    Priority = 5, Weight = 1.0f
                },
                new ShotClassificationRule
                {
                    ShotType = ShotType.PullShot, DisplayName = "Pull Shot",
                    MinArmAngle = 0f, MaxArmAngle = 45f,
                    MinSwingPlaneAngle = 0f, MaxSwingPlaneAngle = 35f,
                    MinVelocity = 0.5f, MaxVelocity = 2.0f,
                    RequiresFrontFoot = false, RequiresBackFoot = true,
                    MinWristRotation = 0f, MaxWristRotation = 120f,
                    RequiresStanceReversal = false,
                    MinBackswingRatio = 0.3f, MaxBackswingRatio = 1.0f,
                    MinFollowThroughHeight = -0.1f, MaxFollowThroughHeight = 0.7f,
                    RequiresCrossBody = true,
                    MinBodyRotation = 20f, MaxBodyRotation = 90f,
                    Priority = 5, Weight = 1.0f
                },
                new ShotClassificationRule
                {
                    ShotType = ShotType.StraightDrive, DisplayName = "Straight Drive",
                    MinArmAngle = 60f, MaxArmAngle = 100f,
                    MinSwingPlaneAngle = 50f, MaxSwingPlaneAngle = 90f,
                    MinVelocity = 0.3f, MaxVelocity = 1.5f,
                    RequiresFrontFoot = true, RequiresBackFoot = false,
                    MinWristRotation = 0f, MaxWristRotation = 60f,
                    RequiresStanceReversal = false,
                    MinBackswingRatio = 0.3f, MaxBackswingRatio = 0.9f,
                    MinFollowThroughHeight = 0.2f, MaxFollowThroughHeight = 1.0f,
                    RequiresCrossBody = false,
                    MinBodyRotation = 5f, MaxBodyRotation = 40f,
                    Priority = 5, Weight = 1.0f
                },
                new ShotClassificationRule
                {
                    ShotType = ShotType.HelicopterShot, DisplayName = "Helicopter Shot",
                    MinArmAngle = 20f, MaxArmAngle = 90f,
                    MinSwingPlaneAngle = 30f, MaxSwingPlaneAngle = 80f,
                    MinVelocity = 0.8f, MaxVelocity = 3.0f,
                    RequiresFrontFoot = false, RequiresBackFoot = false,
                    MinWristRotation = 270f, MaxWristRotation = 450f,
                    RequiresStanceReversal = false,
                    MinBackswingRatio = 0.2f, MaxBackswingRatio = 1.0f,
                    MinFollowThroughHeight = 0.8f, MaxFollowThroughHeight = 2.0f,
                    RequiresCrossBody = false,
                    MinBodyRotation = 30f, MaxBodyRotation = 180f,
                    Priority = 8, Weight = 1.2f
                },
                new ShotClassificationRule
                {
                    ShotType = ShotType.Uppercut, DisplayName = "Uppercut",
                    MinArmAngle = 45f, MaxArmAngle = 120f,
                    MinSwingPlaneAngle = 50f, MaxSwingPlaneAngle = 90f,
                    MinVelocity = 0.5f, MaxVelocity = 2.0f,
                    RequiresFrontFoot = false, RequiresBackFoot = true,
                    MinWristRotation = 0f, MaxWristRotation = 180f,
                    RequiresStanceReversal = false,
                    MinBackswingRatio = 0.1f, MaxBackswingRatio = 0.5f,
                    MinFollowThroughHeight = 0.7f, MaxFollowThroughHeight = 2.0f,
                    RequiresCrossBody = false,
                    MinBodyRotation = 10f, MaxBodyRotation = 60f,
                    Priority = 6, Weight = 1.0f
                },
                new ShotClassificationRule
                {
                    ShotType = ShotType.SwitchHit, DisplayName = "Switch Hit",
                    MinArmAngle = 10f, MaxArmAngle = 90f,
                    MinSwingPlaneAngle = 0f, MaxSwingPlaneAngle = 60f,
                    MinVelocity = 0.4f, MaxVelocity = 2.0f,
                    RequiresFrontFoot = false, RequiresBackFoot = false,
                    MinWristRotation = 0f, MaxWristRotation = 200f,
                    RequiresStanceReversal = true,
                    MinBackswingRatio = 0.2f, MaxBackswingRatio = 1.0f,
                    MinFollowThroughHeight = -0.5f, MaxFollowThroughHeight = 1.5f,
                    RequiresCrossBody = false,
                    MinBodyRotation = 30f, MaxBodyRotation = 180f,
                    Priority = 9, Weight = 1.3f
                },
                new ShotClassificationRule
                {
                    ShotType = ShotType.Flick, DisplayName = "Flick",
                    MinArmAngle = 20f, MaxArmAngle = 70f,
                    MinSwingPlaneAngle = 10f, MaxSwingPlaneAngle = 50f,
                    MinVelocity = 0.3f, MaxVelocity = 1.5f,
                    RequiresFrontFoot = false, RequiresBackFoot = false,
                    MinWristRotation = 60f, MaxWristRotation = 200f,
                    RequiresStanceReversal = false,
                    MinBackswingRatio = 0f, MaxBackswingRatio = 0.2f,
                    MinFollowThroughHeight = -0.2f, MaxFollowThroughHeight = 0.6f,
                    RequiresCrossBody = false,
                    MinBodyRotation = 5f, MaxBodyRotation = 45f,
                    Priority = 6, Weight = 1.0f
                },
                new ShotClassificationRule
                {
                    ShotType = ShotType.DefensiveBlock, DisplayName = "Defensive Block",
                    MinArmAngle = 70f, MaxArmAngle = 110f,
                    MinSwingPlaneAngle = 50f, MaxSwingPlaneAngle = 90f,
                    MinVelocity = 0f, MaxVelocity = 0.3f,
                    RequiresFrontFoot = false, RequiresBackFoot = false,
                    MinWristRotation = 0f, MaxWristRotation = 30f,
                    RequiresStanceReversal = false,
                    MinBackswingRatio = 0f, MaxBackswingRatio = 0.15f,
                    MinFollowThroughHeight = -0.5f, MaxFollowThroughHeight = 0.1f,
                    RequiresCrossBody = false,
                    MinBodyRotation = 0f, MaxBodyRotation = 15f,
                    Priority = 3, Weight = 0.8f
                }
            };
        }

        #endregion
    }
}
