using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.ShotDetection
{
    /// <summary>
    /// Classifies swing data into ShotType using rule-based analysis:
    /// arm angle at impact, swing plane (horizontal/vertical/diagonal),
    /// wrist trajectory shape, and relative positions to calibrated baseline.
    /// Implements rules for all 8 shot types.
    /// </summary>
    public class ShotClassifier : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Shot rules ScriptableObject defining classification criteria.")]
        private ShotRules _shotRules;

        [SerializeField]
        [Tooltip("Minimum confidence threshold to accept a classification.")]
        [Range(0f, 1f)]
        private float _minimumConfidence = 0.3f;

        [SerializeField]
        [Tooltip("Default shot type when no classification meets the threshold.")]
        private ShotType _defaultShotType = ShotType.DefensiveBlock;

        /// <summary>
        /// The currently loaded classification rules.
        /// </summary>
        private ShotClassificationRule[] _rules;

        private void Awake()
        {
            LoadRules();
        }

        /// <summary>
        /// Load classification rules from ScriptableObject or create defaults.
        /// </summary>
        private void LoadRules()
        {
            if (_shotRules != null && _shotRules.Rules != null)
            {
                _rules = _shotRules.Rules;
            }
            else
            {
                // Use default rules if none assigned
                _rules = GetDefaultRules();
            }
        }

        /// <summary>
        /// Classify a swing into a shot type based on the captured swing data.
        /// </summary>
        /// <param name="swingData">The complete swing data to classify.</param>
        /// <returns>A ShotResult with the detected shot type and confidence.</returns>
        public ShotResult ClassifyShot(SwingData swingData)
        {
            if (swingData == null || _rules == null || _rules.Length == 0)
            {
                return CreateDefaultResult();
            }

            // Ensure swing metrics are calculated
            swingData.CalculateMetrics();

            // Evaluate each rule and collect candidates
            List<ClassificationCandidate> candidates = new List<ClassificationCandidate>();

            for (int i = 0; i < _rules.Length; i++)
            {
                float confidence = EvaluateRule(_rules[i], swingData);
                if (confidence > 0f)
                {
                    candidates.Add(new ClassificationCandidate
                    {
                        Rule = _rules[i],
                        Confidence = confidence,
                        WeightedScore = confidence * _rules[i].Weight
                    });
                }
            }

            // Sort candidates by weighted score then priority
            candidates.Sort((a, b) =>
            {
                int scoreCompare = b.WeightedScore.CompareTo(a.WeightedScore);
                if (scoreCompare != 0) return scoreCompare;
                return b.Rule.Priority.CompareTo(a.Rule.Priority);
            });

            // Select the best candidate
            if (candidates.Count > 0 && candidates[0].Confidence >= _minimumConfidence)
            {
                ClassificationCandidate best = candidates[0];
                return CreateResult(best.Rule.ShotType, best.Confidence, swingData);
            }

            return CreateDefaultResult();
        }

        /// <summary>
        /// Evaluate a single classification rule against swing data.
        /// Returns a confidence score (0-1) indicating how well the swing matches the rule.
        /// </summary>
        private float EvaluateRule(ShotClassificationRule rule, SwingData swingData)
        {
            float totalScore = 0f;
            float totalWeight = 0f;

            // Check arm angle at impact
            float armAngleScore = EvaluateRange(
                swingData.ArmAngleAtImpact, rule.MinArmAngle, rule.MaxArmAngle);
            totalScore += armAngleScore * 1.5f;
            totalWeight += 1.5f;

            // Check swing plane angle
            float swingPlaneScore = EvaluateRange(
                swingData.SwingPlaneAngle, rule.MinSwingPlaneAngle, rule.MaxSwingPlaneAngle);
            totalScore += swingPlaneScore * 1.5f;
            totalWeight += 1.5f;

            // Check velocity
            float velocityScore = EvaluateRange(
                swingData.PeakVelocity, rule.MinVelocity, rule.MaxVelocity);
            totalScore += velocityScore * 1.0f;
            totalWeight += 1.0f;

            // Check wrist rotation
            float wristRotationScore = EvaluateRange(
                swingData.WristRotation, rule.MinWristRotation, rule.MaxWristRotation);
            totalScore += wristRotationScore * 1.2f;
            totalWeight += 1.2f;

            // Check backswing ratio
            float backswingScore = EvaluateRange(
                swingData.BackswingRatio, rule.MinBackswingRatio, rule.MaxBackswingRatio);
            totalScore += backswingScore * 0.8f;
            totalWeight += 0.8f;

            // Check follow-through height
            float followThroughScore = EvaluateRange(
                swingData.FollowThroughHeight, rule.MinFollowThroughHeight, rule.MaxFollowThroughHeight);
            totalScore += followThroughScore * 1.0f;
            totalWeight += 1.0f;

            // Check body rotation
            float bodyRotationScore = EvaluateRange(
                swingData.BodyRotation, rule.MinBodyRotation, rule.MaxBodyRotation);
            totalScore += bodyRotationScore * 0.7f;
            totalWeight += 0.7f;

            // Check boolean requirements (these are hard constraints)
            // Stance reversal requirement
            if (rule.RequiresStanceReversal)
            {
                if (swingData.StanceReversalDetected)
                {
                    totalScore += 2.0f; // Strong bonus for matching unique constraint
                    totalWeight += 2.0f;
                }
                else
                {
                    return 0f; // Hard failure - stance reversal required but not detected
                }
            }
            else if (swingData.StanceReversalDetected)
            {
                // Penalize non-switch-hit rules if stance reversal is detected
                totalScore -= 0.5f;
            }

            // Cross-body requirement
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

            // Front foot requirement
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

            // Back foot requirement
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

            // Calculate final confidence
            if (totalWeight <= 0f)
                return 0f;

            float confidence = Mathf.Clamp01(totalScore / totalWeight);
            return confidence;
        }

        /// <summary>
        /// Evaluate how well a value fits within a range.
        /// Returns 1.0 if within range, diminishing score as it moves outside.
        /// </summary>
        private float EvaluateRange(float value, float min, float max)
        {
            if (value >= min && value <= max)
            {
                // Within range - calculate how centered the value is
                float center = (min + max) / 2f;
                float halfRange = (max - min) / 2f;
                if (halfRange <= 0f) return 1f;

                float distFromCenter = Mathf.Abs(value - center);
                return 1f - (distFromCenter / halfRange) * 0.3f; // Slight reduction toward edges
            }
            else
            {
                // Outside range - diminishing score
                float rangeSize = max - min;
                float tolerance = Mathf.Max(rangeSize * 0.3f, 5f); // 30% tolerance

                float distOutside;
                if (value < min)
                    distOutside = min - value;
                else
                    distOutside = value - max;

                return Mathf.Max(0f, 1f - (distOutside / tolerance));
            }
        }

        /// <summary>
        /// Create a ShotResult from classification data and swing metrics.
        /// </summary>
        private ShotResult CreateResult(ShotType shotType, float confidence, SwingData swingData)
        {
            // Calculate power from velocity and follow-through
            float power = CalculatePower(swingData);

            // Calculate direction from swing plane and arm angle
            Vector3 direction = CalculateDirection(shotType, swingData);

            ShotResult result = new ShotResult
            {
                ShotType = shotType,
                Confidence = confidence,
                Timing = TimingQuality.Good, // Timing is set externally by TimingWindow
                Power = power,
                Direction = direction,
                SwingSpeed = swingData.PeakVelocity,
                ArmAngle = swingData.ArmAngleAtImpact,
                Timestamp = Time.time,
                IsValid = true,
                SourceSwingData = swingData
            };

            return result;
        }

        /// <summary>
        /// Create a default result for unclassified swings.
        /// </summary>
        private ShotResult CreateDefaultResult()
        {
            return new ShotResult
            {
                ShotType = _defaultShotType,
                Confidence = 0.1f,
                Timing = TimingQuality.Good,
                Power = 0.2f,
                Direction = Vector3.forward,
                Timestamp = Time.time,
                IsValid = false
            };
        }

        /// <summary>
        /// Calculate shot power from swing velocity and follow-through.
        /// </summary>
        private float CalculatePower(SwingData swingData)
        {
            // Combine velocity and follow-through for power estimation
            // Normalize velocity to 0-1 range (assuming max velocity of ~2.5)
            float velocityFactor = Mathf.Clamp01(swingData.PeakVelocity / 2.5f);
            float followThroughFactor = Mathf.Clamp01(swingData.FollowThroughHeight);

            // Power is weighted combination
            float power = velocityFactor * 0.7f + followThroughFactor * 0.3f;
            return Mathf.Clamp01(power);
        }

        /// <summary>
        /// Calculate shot direction based on shot type and swing characteristics.
        /// </summary>
        private Vector3 CalculateDirection(ShotType shotType, SwingData swingData)
        {
            Vector3 baseDirection;

            // Assign base direction per shot type
            switch (shotType)
            {
                case ShotType.CoverDrive:
                    baseDirection = new Vector3(0.5f, 0.1f, 0.85f); // Off-side, slightly elevated
                    break;
                case ShotType.PullShot:
                    baseDirection = new Vector3(-0.6f, 0.3f, 0.7f); // Leg-side, elevated
                    break;
                case ShotType.StraightDrive:
                    baseDirection = new Vector3(0f, 0.15f, 1f); // Straight down the ground
                    break;
                case ShotType.HelicopterShot:
                    baseDirection = new Vector3(-0.3f, 0.5f, 0.8f); // Slightly leg-side, high
                    break;
                case ShotType.Uppercut:
                    baseDirection = new Vector3(0.4f, 0.7f, 0.5f); // Over slips, high
                    break;
                case ShotType.SwitchHit:
                    baseDirection = new Vector3(-0.7f, 0.2f, 0.7f); // Reverse direction
                    break;
                case ShotType.Flick:
                    baseDirection = new Vector3(-0.4f, 0.1f, 0.9f); // Leg-side, along ground
                    break;
                case ShotType.DefensiveBlock:
                    baseDirection = new Vector3(0f, -0.1f, 0.3f); // Into the ground
                    break;
                default:
                    baseDirection = Vector3.forward;
                    break;
            }

            // Modify direction based on swing characteristics
            if (swingData.SwingDirection.magnitude > 0.01f)
            {
                float swingInfluence = 0.2f;
                baseDirection.x += swingData.SwingDirection.x * swingInfluence;
            }

            return baseDirection.normalized;
        }

        /// <summary>
        /// Get default rules when no ScriptableObject is assigned.
        /// Creates the rules array directly without relying on a temporary ScriptableObject.
        /// </summary>
        private ShotClassificationRule[] GetDefaultRules()
        {
            ShotRules defaultRules = ShotRules.CreateDefaultRules();
            // Copy the rules array before destroying the temporary SO
            ShotClassificationRule[] rules = new ShotClassificationRule[defaultRules.Rules.Length];
            System.Array.Copy(defaultRules.Rules, rules, defaultRules.Rules.Length);
            DestroyImmediate(defaultRules);
            return rules;
        }

        /// <summary>
        /// Internal candidate for classification ranking.
        /// </summary>
        private struct ClassificationCandidate
        {
            public ShotClassificationRule Rule;
            public float Confidence;
            public float WeightedScore;
        }
    }
}
