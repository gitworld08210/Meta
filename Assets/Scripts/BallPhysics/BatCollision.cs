using UnityEngine;
using MetaCricket.Core;
using MetaCricket.ShotDetection;

namespace MetaCricket.BallPhysics
{
    /// <summary>
    /// Handles bat-ball collision physics. Uses the ShotResult (timing, power, direction)
    /// to determine ball exit velocity and angle. Implements sweet spot mechanics
    /// where perfect timing yields maximum power transfer.
    /// </summary>
    public class BatCollision : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallController _ballController;

        [Header("Exit Speed Settings")]
        [Tooltip("Maximum exit speed multiplier relative to incoming ball speed.")]
        [SerializeField] private float _maxExitSpeedMultiplier = 2.5f;

        [Tooltip("Minimum exit speed even on a mis-hit (fraction of incoming).")]
        [SerializeField] private float _minExitSpeedMultiplier = 0.3f;

        [Header("Sweet Spot Mechanics")]
        [Tooltip("Exit speed bonus for perfect timing (multiplier).")]
        [SerializeField] private float _perfectTimingBonus = 1.5f;

        [Tooltip("Exit speed penalty for early timing (multiplier).")]
        [SerializeField] private float _earlyTimingPenalty = 0.6f;

        [Tooltip("Exit speed penalty for late timing (multiplier).")]
        [SerializeField] private float _lateTimingPenalty = 0.5f;

        [Header("Elevation Settings")]
        [Tooltip("Max elevation angle for aerial shots (degrees).")]
        [SerializeField] private float _maxElevation = 60f;

        [Tooltip("Min elevation angle for grounded shots (degrees).")]
        [SerializeField] private float _minElevation = -5f;

        /// <summary>
        /// Process a bat-ball collision using the detected shot result.
        /// Calculates and applies the exit velocity to the ball.
        /// </summary>
        /// <param name="shotResult">The detected shot parameters.</param>
        /// <param name="incomingVelocity">Ball velocity at the moment of contact.</param>
        /// <returns>The calculated exit velocity vector.</returns>
        public Vector3 ProcessCollision(ShotResult shotResult, Vector3 incomingVelocity)
        {
            if (shotResult == null || !shotResult.IsValid)
            {
                return HandleMissedShot(incomingVelocity);
            }

            // Calculate base exit speed
            float incomingSpeed = incomingVelocity.magnitude;
            float exitSpeed = CalculateExitSpeed(shotResult, incomingSpeed);

            // Calculate exit direction based on shot type and player direction
            Vector3 exitDirection = CalculateExitDirection(shotResult, incomingVelocity);

            // Calculate elevation based on shot type and timing
            float elevation = CalculateElevation(shotResult);
            exitDirection = ApplyElevation(exitDirection, elevation);

            // Apply sweet spot bonus/penalty based on timing
            float timingMultiplier = GetTimingMultiplier(shotResult.Timing);
            exitSpeed *= timingMultiplier;

            // Final exit velocity
            Vector3 exitVelocity = exitDirection.normalized * exitSpeed;

            // Apply to ball
            if (_ballController != null)
            {
                _ballController.OnBatContact(exitVelocity);
            }

            // Publish shot played event
            EventBus.Publish(new ShotPlayedEvent
            {
                Type = shotResult.ShotType,
                Power = shotResult.Power,
                Direction = exitDirection,
                TimingAccuracy = timingMultiplier
            });

            return exitVelocity;
        }

        /// <summary>
        /// Calculate exit speed based on shot power, timing, and incoming speed.
        /// </summary>
        private float CalculateExitSpeed(ShotResult shotResult, float incomingSpeed)
        {
            // Base exit speed from power and incoming ball speed
            float powerFactor = Mathf.Lerp(_minExitSpeedMultiplier, _maxExitSpeedMultiplier, shotResult.Power);
            float exitSpeed = incomingSpeed * powerFactor;

            // Shot type modifiers
            float shotTypeMultiplier = GetShotTypeSpeedMultiplier(shotResult.ShotType);
            exitSpeed *= shotTypeMultiplier;

            // Confidence affects consistency
            float confidenceFactor = Mathf.Lerp(0.7f, 1.0f, shotResult.Confidence);
            exitSpeed *= confidenceFactor;

            return exitSpeed;
        }

        /// <summary>
        /// Get speed multiplier based on shot type.
        /// Power shots get higher multiplier, defensive shots lower.
        /// </summary>
        private float GetShotTypeSpeedMultiplier(ShotType shotType)
        {
            switch (shotType)
            {
                case ShotType.HelicopterShot:
                    return 1.3f;
                case ShotType.Uppercut:
                    return 1.2f;
                case ShotType.SwitchHit:
                    return 1.15f;
                case ShotType.PullShot:
                    return 1.1f;
                case ShotType.CoverDrive:
                    return 1.05f;
                case ShotType.StraightDrive:
                    return 1.0f;
                case ShotType.Flick:
                    return 0.95f;
                case ShotType.DefensiveBlock:
                    return 0.3f;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Calculate the exit direction based on shot type and player input direction.
        /// </summary>
        private Vector3 CalculateExitDirection(ShotResult shotResult, Vector3 incomingVelocity)
        {
            Vector3 baseDirection = shotResult.Direction;

            // If no direction data, derive from shot type
            if (baseDirection.sqrMagnitude < 0.01f)
            {
                baseDirection = GetDefaultDirectionForShot(shotResult.ShotType, incomingVelocity);
            }

            // Add some randomness based on confidence (lower confidence = more deviation)
            float deviationAngle = (1f - shotResult.Confidence) * 15f;
            Vector3 deviation = new Vector3(
                Random.Range(-deviationAngle, deviationAngle),
                0f,
                Random.Range(-deviationAngle, deviationAngle)
            );

            baseDirection = Quaternion.Euler(deviation) * baseDirection;

            return baseDirection.normalized;
        }

        /// <summary>
        /// Get the default ball direction for a given shot type.
        /// </summary>
        private Vector3 GetDefaultDirectionForShot(ShotType shotType, Vector3 incomingVelocity)
        {
            switch (shotType)
            {
                case ShotType.CoverDrive:
                    return new Vector3(0.7f, 0f, 0.7f).normalized; // Between point and mid-off
                case ShotType.PullShot:
                    return new Vector3(-0.7f, 0f, 0.3f).normalized; // Square leg area
                case ShotType.StraightDrive:
                    return Vector3.forward; // Straight down the ground
                case ShotType.HelicopterShot:
                    return new Vector3(-0.3f, 0f, 0.8f).normalized; // Between mid-on and long-on
                case ShotType.Uppercut:
                    return new Vector3(0.8f, 0f, 0.3f).normalized; // Over point/third man
                case ShotType.SwitchHit:
                    return new Vector3(-0.5f, 0f, 0.6f).normalized; // Over mid-wicket
                case ShotType.Flick:
                    return new Vector3(-0.5f, 0f, 0.5f).normalized; // Through mid-wicket
                case ShotType.DefensiveBlock:
                    return -incomingVelocity.normalized * 0.3f + Vector3.down * 0.7f; // Into the ground
                default:
                    return Vector3.forward;
            }
        }

        /// <summary>
        /// Calculate elevation angle based on shot type and timing.
        /// </summary>
        private float CalculateElevation(ShotResult shotResult)
        {
            float baseElevation = GetBaseElevationForShot(shotResult.ShotType);

            // Perfect timing gives controlled elevation
            // Early timing results in more elevation (ball goes in the air)
            // Late timing results in less elevation (ball stays low)
            switch (shotResult.Timing)
            {
                case TimingQuality.Perfect:
                    // Player gets the elevation they intended
                    break;
                case TimingQuality.Good:
                    baseElevation += Random.Range(-5f, 5f);
                    break;
                case TimingQuality.Early:
                    baseElevation += Random.Range(10f, 25f); // Ball pops up
                    break;
                case TimingQuality.Late:
                    baseElevation -= Random.Range(5f, 15f); // Ball stays low
                    break;
            }

            // Power affects elevation for attacking shots
            if (shotResult.Power > 0.8f && baseElevation > 0f)
            {
                baseElevation += shotResult.Power * 10f;
            }

            return Mathf.Clamp(baseElevation, _minElevation, _maxElevation);
        }

        /// <summary>
        /// Get the base elevation angle for each shot type (in degrees).
        /// </summary>
        private float GetBaseElevationForShot(ShotType shotType)
        {
            switch (shotType)
            {
                case ShotType.HelicopterShot:
                    return 35f;
                case ShotType.Uppercut:
                    return 45f;
                case ShotType.SwitchHit:
                    return 20f;
                case ShotType.PullShot:
                    return 15f;
                case ShotType.CoverDrive:
                    return 5f;
                case ShotType.StraightDrive:
                    return 3f;
                case ShotType.Flick:
                    return 10f;
                case ShotType.DefensiveBlock:
                    return -3f;
                default:
                    return 5f;
            }
        }

        /// <summary>
        /// Apply elevation angle to direction vector.
        /// </summary>
        private Vector3 ApplyElevation(Vector3 direction, float elevationDegrees)
        {
            Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z).normalized;
            float horizontalMag = new Vector2(direction.x, direction.z).magnitude;

            float elevationRad = elevationDegrees * Mathf.Deg2Rad;
            float verticalComponent = Mathf.Tan(elevationRad) * horizontalMag;

            return new Vector3(horizontalDir.x, verticalComponent, horizontalDir.z).normalized;
        }

        /// <summary>
        /// Get timing multiplier for exit speed.
        /// </summary>
        private float GetTimingMultiplier(TimingQuality timing)
        {
            switch (timing)
            {
                case TimingQuality.Perfect:
                    return _perfectTimingBonus;
                case TimingQuality.Good:
                    return 1.0f;
                case TimingQuality.Early:
                    return _earlyTimingPenalty;
                case TimingQuality.Late:
                    return _lateTimingPenalty;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Handle a missed shot (ball passes through without clean contact).
        /// </summary>
        private Vector3 HandleMissedShot(Vector3 incomingVelocity)
        {
            // Ball continues mostly in same direction with slight deflection
            Vector3 deflection = incomingVelocity;
            deflection += Random.insideUnitSphere * incomingVelocity.magnitude * 0.1f;
            return deflection;
        }
    }
}
