using UnityEngine;

namespace MetaCricket.BallPhysics
{
    /// <summary>
    /// Calculates ball flight path using physics equations:
    /// projectile motion with air resistance, Magnus effect for spin,
    /// and lateral movement for swing. Provides predicted positions along trajectory.
    /// </summary>
    public class BallTrajectory
    {
        // Physics constants
        private const float Gravity = 9.81f;
        private const float AirDensity = 1.225f; // kg/m^3 at sea level
        private const float BallMass = 0.156f; // Cricket ball mass in kg
        private const float BallRadius = 0.036f; // Cricket ball radius in meters
        private const float BallCrossSectionArea = Mathf.PI * BallRadius * BallRadius;
        private const float DragCoefficient = 0.45f; // Sphere drag coefficient
        private const float MagnusCoefficient = 0.33f; // Magnus lift coefficient
        private const float SwingCoefficient = 0.15f; // Lateral swing coefficient

        private BallDeliveryData _deliveryData;
        private Vector3 _startPosition;
        private Vector3 _initialVelocity;

        /// <summary>
        /// Initialize trajectory calculation with delivery data and start position.
        /// </summary>
        /// <param name="deliveryData">The delivery configuration.</param>
        /// <param name="startPosition">The bowler's release point.</param>
        /// <param name="targetDirection">Direction towards the batsman.</param>
        public BallTrajectory(BallDeliveryData deliveryData, Vector3 startPosition, Vector3 targetDirection)
        {
            _deliveryData = deliveryData;
            _startPosition = startPosition;

            float speed = deliveryData.SpeedInMetersPerSecond;
            _initialVelocity = targetDirection.normalized * speed;

            // Add slight upward angle for trajectory arc
            float launchAngle = CalculateLaunchAngle(startPosition, deliveryData.PitchPosition, speed);
            _initialVelocity.y += speed * Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Calculate launch angle needed to hit the pitch position.
        /// </summary>
        private float CalculateLaunchAngle(Vector3 start, Vector3 target, float speed)
        {
            float distance = Vector3.Distance(new Vector3(start.x, 0f, start.z),
                                               new Vector3(target.x, 0f, target.z));
            float heightDiff = target.y - start.y;

            // Simplified projectile angle calculation
            float angle = Mathf.Atan2(heightDiff + 0.5f * Gravity * (distance / speed) * (distance / speed),
                                       distance) * Mathf.Rad2Deg;

            return Mathf.Clamp(angle, -10f, 15f);
        }

        /// <summary>
        /// Calculate position at a given time along the trajectory.
        /// Includes air resistance, Magnus effect, and swing.
        /// </summary>
        /// <param name="time">Time elapsed since release in seconds.</param>
        /// <returns>World space position of the ball.</returns>
        public Vector3 GetPositionAtTime(float time)
        {
            Vector3 position = _startPosition;
            Vector3 velocity = _initialVelocity;

            // Use numerical integration (Euler method) for accuracy with drag
            float dt = 0.005f;
            float elapsed = 0f;

            while (elapsed < time)
            {
                float step = Mathf.Min(dt, time - elapsed);

                Vector3 acceleration = CalculateAcceleration(velocity);
                velocity += acceleration * step;
                position += velocity * step;

                elapsed += step;
            }

            return position;
        }

        /// <summary>
        /// Calculate the total acceleration vector including gravity, drag, Magnus, and swing.
        /// </summary>
        private Vector3 CalculateAcceleration(Vector3 velocity)
        {
            Vector3 totalAcceleration = Vector3.zero;

            // Gravity
            totalAcceleration += Vector3.down * Gravity;

            // Air resistance (drag)
            float speedSqr = velocity.sqrMagnitude;
            if (speedSqr > 0.01f)
            {
                float dragForce = 0.5f * AirDensity * DragCoefficient * BallCrossSectionArea * speedSqr;
                Vector3 dragAcceleration = -velocity.normalized * (dragForce / BallMass);
                totalAcceleration += dragAcceleration;
            }

            // Magnus effect (spin causing drift)
            if (_deliveryData.HasSpin)
            {
                Vector3 magnusForce = CalculateMagnusForce(velocity);
                totalAcceleration += magnusForce / BallMass;
            }

            // Lateral swing
            if (_deliveryData.HasSwing)
            {
                Vector3 swingForce = CalculateSwingForce(velocity);
                totalAcceleration += swingForce / BallMass;
            }

            return totalAcceleration;
        }

        /// <summary>
        /// Calculate Magnus force from ball spin.
        /// F_magnus = Cl * 0.5 * rho * A * v^2 * (omega x v_hat)
        /// </summary>
        private Vector3 CalculateMagnusForce(Vector3 velocity)
        {
            float speed = velocity.magnitude;
            if (speed < 0.1f) return Vector3.zero;

            Vector3 spinAxisWorld = _deliveryData.SpinAxis.normalized;
            Vector3 velocityDir = velocity.normalized;

            // Magnus force is perpendicular to both spin axis and velocity
            Vector3 magnusDirection = Vector3.Cross(spinAxisWorld, velocityDir).normalized;

            float magnusMagnitude = MagnusCoefficient * 0.5f * AirDensity * BallCrossSectionArea
                                    * speed * speed * (_deliveryData.SpinRate / 30f);

            return magnusDirection * magnusMagnitude;
        }

        /// <summary>
        /// Calculate lateral swing force from seam position and ball condition.
        /// </summary>
        private Vector3 CalculateSwingForce(Vector3 velocity)
        {
            float speed = velocity.magnitude;
            if (speed < 5f) return Vector3.zero;

            // Swing is lateral (perpendicular to travel direction and vertical)
            Vector3 lateralDir = Vector3.Cross(velocity.normalized, Vector3.up).normalized;

            float swingMagnitude = SwingCoefficient * 0.5f * AirDensity * BallCrossSectionArea
                                   * speed * speed * _deliveryData.SwingAmount;

            return lateralDir * swingMagnitude * _deliveryData.SwingDirection;
        }

        /// <summary>
        /// Get an array of positions along the full trajectory path.
        /// Useful for rendering the ball path or predicting landing.
        /// </summary>
        /// <param name="totalTime">Total time to compute.</param>
        /// <param name="steps">Number of sample points.</param>
        /// <returns>Array of world-space positions along the trajectory.</returns>
        public Vector3[] GetTrajectoryPoints(float totalTime, int steps = 30)
        {
            Vector3[] points = new Vector3[steps];
            float timeStep = totalTime / (steps - 1);

            for (int i = 0; i < steps; i++)
            {
                points[i] = GetPositionAtTime(timeStep * i);
            }

            return points;
        }

        /// <summary>
        /// Calculate the velocity of the ball at a given time.
        /// </summary>
        /// <param name="time">Time elapsed since release.</param>
        /// <returns>Velocity vector at the specified time.</returns>
        public Vector3 GetVelocityAtTime(float time)
        {
            Vector3 velocity = _initialVelocity;
            float dt = 0.005f;
            float elapsed = 0f;

            while (elapsed < time)
            {
                float step = Mathf.Min(dt, time - elapsed);
                Vector3 acceleration = CalculateAcceleration(velocity);
                velocity += acceleration * step;
                elapsed += step;
            }

            return velocity;
        }

        /// <summary>
        /// Estimate the time at which the ball hits the pitch (y approaches 0).
        /// </summary>
        /// <returns>Estimated pitch time in seconds.</returns>
        public float EstimatePitchTime()
        {
            float dt = 0.01f;
            float time = 0f;
            Vector3 position = _startPosition;
            Vector3 velocity = _initialVelocity;
            bool hasRisen = false;

            while (time < 3f)
            {
                Vector3 acceleration = CalculateAcceleration(velocity);
                velocity += acceleration * dt;
                position += velocity * dt;
                time += dt;

                if (position.y > _startPosition.y + 0.1f)
                    hasRisen = true;

                // Ball hits pitch when descending to ground level
                if (hasRisen && position.y <= 0.05f)
                    return time;
            }

            // Fallback: rough estimate based on distance and speed
            float distance = Vector3.Distance(_startPosition, _deliveryData.PitchPosition);
            return distance / _deliveryData.SpeedInMetersPerSecond;
        }

        /// <summary>
        /// Calculate the velocity after bounce, incorporating spin and seam effects.
        /// </summary>
        /// <param name="incomingVelocity">Velocity before hitting the pitch.</param>
        /// <returns>Velocity after bouncing.</returns>
        public Vector3 CalculateBounceVelocity(Vector3 incomingVelocity)
        {
            // Reflect vertical component with energy loss
            float bounceRestitution = 0.6f * _deliveryData.BounceHeight;
            Vector3 bounced = incomingVelocity;
            bounced.y = Mathf.Abs(incomingVelocity.y) * bounceRestitution;

            // Apply seam movement deviation after pitching
            if (_deliveryData.SeamMovement > 0f)
            {
                Vector3 lateralDeviation = Vector3.Cross(bounced.normalized, Vector3.up).normalized;
                bounced += lateralDeviation * _deliveryData.SeamMovement * 3f * _deliveryData.SwingDirection;
            }

            // Apply spin deviation after pitching (spin bites more on bounce)
            if (_deliveryData.HasSpin)
            {
                Vector3 spinDeviation = Vector3.Cross(_deliveryData.SpinAxis.normalized, Vector3.up).normalized;
                bounced += spinDeviation * _deliveryData.SpinRate * 0.05f;
            }

            return bounced;
        }
    }
}
