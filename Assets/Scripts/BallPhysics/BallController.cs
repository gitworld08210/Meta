using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.BallPhysics
{
    /// <summary>
    /// MonoBehaviour controlling the cricket ball's physics-based movement.
    /// Manages Rigidbody-driven ball motion including spin, swing, bounce,
    /// and speed variations (70-150 kph based on career stage).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class BallController : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float _dragInAir = 0.3f;
        [SerializeField] private float _angularDragInAir = 0.05f;
        [SerializeField] private float _bounciness = 0.6f;
        [SerializeField] private float _friction = 0.4f;

        [Header("Ball Properties")]
        [SerializeField] private float _ballMass = 0.156f;
        [SerializeField] private float _ballRadius = 0.036f;

        [Header("Visual")]
        [SerializeField] private TrailRenderer _trailRenderer;

        // Runtime state
        private Rigidbody _rigidbody;
        private SphereCollider _collider;
        private BallDeliveryData _currentDelivery;
        private BallTrajectory _trajectory;
        private BallState _state;
        private bool _hasBounced;
        private bool _hasReachedBatsman;
        private float _releaseTime;
        private Vector3 _releasePosition;

        /// <summary>
        /// Current ball state.
        /// </summary>
        public BallState State => _state;

        /// <summary>
        /// Whether the ball has bounced on the pitch.
        /// </summary>
        public bool HasBounced => _hasBounced;

        /// <summary>
        /// Time since the ball was released.
        /// </summary>
        public float TimeSinceRelease => Time.time - _releaseTime;

        /// <summary>
        /// Current speed of the ball in kph.
        /// </summary>
        public float CurrentSpeedKph => _rigidbody != null ? _rigidbody.linearVelocity.magnitude * 3.6f : 0f;

        /// <summary>
        /// The delivery data for the current ball.
        /// </summary>
        public BallDeliveryData CurrentDelivery => _currentDelivery;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();
            SetupPhysics();
            _state = BallState.Idle;
        }

        /// <summary>
        /// Configure the Rigidbody and collider for cricket ball physics.
        /// </summary>
        private void SetupPhysics()
        {
            _rigidbody.mass = _ballMass;
            _rigidbody.linearDamping = _dragInAir;
            _rigidbody.angularDamping = _angularDragInAir;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            _collider.radius = _ballRadius;

            // Create physics material for bounce behavior
            PhysicsMaterial ballMaterial = new PhysicsMaterial("CricketBall")
            {
                bounciness = _bounciness,
                dynamicFriction = _friction,
                staticFriction = _friction,
                bounceCombine = PhysicsMaterialCombine.Average,
                frictionCombine = PhysicsMaterialCombine.Average
            };
            _collider.material = ballMaterial;
        }

        /// <summary>
        /// Bowl the ball with the given delivery data from the specified release point.
        /// </summary>
        /// <param name="deliveryData">Configuration for this delivery.</param>
        /// <param name="releasePoint">World position of the bowler's release.</param>
        /// <param name="targetDirection">Direction towards the batsman.</param>
        public void Bowl(BallDeliveryData deliveryData, Vector3 releasePoint, Vector3 targetDirection)
        {
            _currentDelivery = deliveryData;
            _hasBounced = false;
            _hasReachedBatsman = false;
            _releaseTime = Time.time;
            _releasePosition = releasePoint;

            // Position ball at release point
            transform.position = releasePoint;

            // Create trajectory calculator
            _trajectory = new BallTrajectory(deliveryData, releasePoint, targetDirection);

            // Enable physics
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;

            // Calculate initial velocity
            float speed = deliveryData.SpeedInMetersPerSecond;
            Vector3 velocity = targetDirection.normalized * speed;

            // Add arc to clear the pitch distance
            float pitchDistance = Vector3.Distance(releasePoint, deliveryData.PitchPosition);
            float flightTime = pitchDistance / speed;
            velocity.y += Gravity * flightTime * 0.4f;

            _rigidbody.linearVelocity = velocity;

            // Apply spin as angular velocity
            if (deliveryData.HasSpin)
            {
                Vector3 angularVel = deliveryData.SpinAxis.normalized * deliveryData.SpinRate * Mathf.PI * 2f;
                _rigidbody.angularVelocity = angularVel;
            }

            _state = BallState.InFlight;

            // Enable trail if available
            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = true;
                _trailRenderer.Clear();
            }
        }

        private void FixedUpdate()
        {
            if (_state == BallState.Idle || _state == BallState.Dead)
                return;

            ApplyAerodynamicForces();

            // Check if ball has passed the batsman
            if (_state == BallState.InFlight || _state == BallState.PostBounce)
            {
                CheckBatsmanReached();
            }
        }

        /// <summary>
        /// Apply Magnus effect and swing forces each physics frame.
        /// </summary>
        private void ApplyAerodynamicForces()
        {
            if (_currentDelivery == null) return;

            Vector3 velocity = _rigidbody.linearVelocity;
            float speed = velocity.magnitude;

            if (speed < 1f) return;

            // Magnus effect from spin
            if (_currentDelivery.HasSpin && !_hasBounced)
            {
                Vector3 spinAxis = _currentDelivery.SpinAxis.normalized;
                Vector3 magnusForce = Vector3.Cross(spinAxis, velocity.normalized);
                float magnusMagnitude = 0.5f * 1.225f * Mathf.PI * _ballRadius * _ballRadius
                                        * speed * speed * 0.33f * (_currentDelivery.SpinRate / 30f);
                _rigidbody.AddForce(magnusForce * magnusMagnitude, ForceMode.Force);
            }

            // Swing force (conventional and reverse)
            if (_currentDelivery.HasSwing && !_hasBounced)
            {
                Vector3 lateralDir = Vector3.Cross(velocity.normalized, Vector3.up).normalized;
                float swingForce = 0.5f * 1.225f * Mathf.PI * _ballRadius * _ballRadius
                                   * speed * speed * 0.15f * _currentDelivery.SwingAmount;
                _rigidbody.AddForce(lateralDir * swingForce * _currentDelivery.SwingDirection, ForceMode.Force);
            }
        }

        /// <summary>
        /// Check if the ball has reached the batsman's position.
        /// </summary>
        private void CheckBatsmanReached()
        {
            // Ball has traveled past a certain distance from release point
            float distanceFromRelease = Vector3.Distance(transform.position, _releasePosition);
            float pitchLength = 20.12f; // Standard cricket pitch length in meters

            if (distanceFromRelease >= pitchLength && !_hasReachedBatsman)
            {
                _hasReachedBatsman = true;
                _state = BallState.AtBatsman;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_state == BallState.Dead) return;

            // Pitch bounce
            if (collision.gameObject.CompareTag(Constants.Tags.Pitch) && !_hasBounced)
            {
                HandlePitchBounce(collision);
            }
            // Stumps hit
            else if (collision.gameObject.CompareTag(Constants.Tags.Stumps))
            {
                HandleStumpsHit();
            }
            // Bat contact is handled by BatCollision component
        }

        /// <summary>
        /// Handle the ball bouncing off the pitch. Apply spin and seam deviations.
        /// </summary>
        private void HandlePitchBounce(Collision collision)
        {
            _hasBounced = true;
            _state = BallState.PostBounce;

            if (_currentDelivery == null) return;

            Vector3 velocity = _rigidbody.linearVelocity;

            // Apply seam movement on bounce
            if (_currentDelivery.SeamMovement > 0f)
            {
                Vector3 lateral = Vector3.Cross(velocity.normalized, Vector3.up).normalized;
                Vector3 seamDeviation = lateral * _currentDelivery.SeamMovement * 3f * _currentDelivery.SwingDirection;
                _rigidbody.linearVelocity += seamDeviation;
            }

            // Apply spin effect on bounce (spin grips the pitch)
            if (_currentDelivery.HasSpin)
            {
                Vector3 spinDeviation = Vector3.Cross(_currentDelivery.SpinAxis.normalized, Vector3.up).normalized;
                _rigidbody.linearVelocity += spinDeviation * _currentDelivery.SpinRate * 0.04f;
            }

            // Apply bounce height modifier
            Vector3 postBounceVel = _rigidbody.linearVelocity;
            postBounceVel.y = Mathf.Abs(postBounceVel.y) * _currentDelivery.BounceHeight;
            _rigidbody.linearVelocity = postBounceVel;
        }

        /// <summary>
        /// Handle the ball hitting the stumps (bowled dismissal).
        /// </summary>
        private void HandleStumpsHit()
        {
            _state = BallState.Dead;
            _rigidbody.linearVelocity *= 0.3f;

            EventBus.Publish(new WicketEvent
            {
                DismissalMethod = DismissalType.Bowled,
                BatsmanScore = 0,
                BallsFaced = 0,
                WicketsDown = 0
            });
        }

        /// <summary>
        /// Called after the ball has been hit by the batsman.
        /// Updates ball velocity based on shot result.
        /// </summary>
        /// <param name="exitVelocity">New velocity after bat contact.</param>
        public void OnBatContact(Vector3 exitVelocity)
        {
            _state = BallState.PostHit;
            _rigidbody.linearVelocity = exitVelocity;

            // Reduce angular velocity on bat contact
            _rigidbody.angularVelocity *= 0.3f;
        }

        /// <summary>
        /// Reset the ball to idle state for the next delivery.
        /// </summary>
        public void ResetBall()
        {
            _state = BallState.Idle;
            _hasBounced = false;
            _hasReachedBatsman = false;
            _currentDelivery = null;

            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = false;
                _trailRenderer.Clear();
            }
        }

        /// <summary>
        /// Mark the ball as dead (end of play for this delivery).
        /// </summary>
        public void Kill()
        {
            _state = BallState.Dead;
        }

        // Gravity constant for calculations
        private const float Gravity = 9.81f;
    }

    /// <summary>
    /// States of the ball during a delivery.
    /// </summary>
    public enum BallState
    {
        Idle,
        InFlight,
        PostBounce,
        AtBatsman,
        PostHit,
        Dead
    }
}
