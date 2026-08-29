using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.BallPhysics
{
    /// <summary>
    /// Generates cricket deliveries based on difficulty level and match situation.
    /// Selects ball type, speed, line, length using BallDeliveryData configuration.
    /// Triggers BallBowledEvent via EventBus after each delivery.
    /// </summary>
    public class BowlingMachine : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallController _ballController;
        [SerializeField] private Transform _releasePoint;
        [SerializeField] private Transform _batsmanPosition;

        [Header("Difficulty Settings")]
        [SerializeField] private DifficultyLevel _currentDifficulty = DifficultyLevel.Medium;

        [Header("Speed Ranges (kph)")]
        [SerializeField] private float _minSpeed = 70f;
        [SerializeField] private float _maxSpeed = 150f;

        [Header("Career Stage Speed Limits")]
        [SerializeField] private float _gullyMaxSpeed = 90f;
        [SerializeField] private float _districtMaxSpeed = 110f;
        [SerializeField] private float _stateMaxSpeed = 130f;
        [SerializeField] private float _internationalMaxSpeed = 150f;

        // Runtime state
        private int _currentBallNumber;
        private int _currentOverNumber;
        private CareerStage _currentCareerStage;
        private List<BallType> _recentDeliveries = new List<BallType>();
        private int _dotsInRow;
        private int _boundariesInRow;

        /// <summary>
        /// Set the difficulty level for subsequent deliveries.
        /// </summary>
        public void SetDifficulty(DifficultyLevel difficulty)
        {
            _currentDifficulty = difficulty;
        }

        /// <summary>
        /// Set the career stage to adjust speed limits.
        /// </summary>
        public void SetCareerStage(CareerStage stage)
        {
            _currentCareerStage = stage;
        }

        /// <summary>
        /// Set the current over and ball numbers.
        /// </summary>
        public void SetOverState(int overNumber, int ballNumber)
        {
            _currentOverNumber = overNumber;
            _currentBallNumber = ballNumber;
        }

        /// <summary>
        /// Update match situation feedback for AI bowling decisions.
        /// </summary>
        public void UpdateMatchSituation(int dotsInRow, int boundariesInRow)
        {
            _dotsInRow = dotsInRow;
            _boundariesInRow = boundariesInRow;
        }

        /// <summary>
        /// Generate and bowl a delivery based on current difficulty and match situation.
        /// </summary>
        public void BowlDelivery()
        {
            BallDeliveryData deliveryData = GenerateDelivery();
            Vector3 releasePos = _releasePoint != null ? _releasePoint.position : new Vector3(0f, 2.2f, -10f);
            Vector3 targetDir = (_batsmanPosition != null ? _batsmanPosition.position : Vector3.zero) - releasePos;
            targetDir.y = 0f;
            targetDir.Normalize();

            _ballController.Bowl(deliveryData, releasePos, targetDir);

            // Track recent deliveries for variety
            _recentDeliveries.Add(deliveryData.DeliveryType);
            if (_recentDeliveries.Count > 6)
                _recentDeliveries.RemoveAt(0);

            _currentBallNumber++;

            // Publish ball bowled event
            EventBus.Publish(new BallBowledEvent
            {
                DeliveryType = deliveryData.DeliveryType,
                Speed = deliveryData.Speed,
                TargetPosition = deliveryData.TargetPosition,
                SwingAmount = deliveryData.SwingAmount,
                BallNumber = _currentBallNumber,
                OverNumber = _currentOverNumber
            });
        }

        /// <summary>
        /// Generate delivery data based on difficulty, career stage, and match situation.
        /// </summary>
        private BallDeliveryData GenerateDelivery()
        {
            BallDeliveryData data = new BallDeliveryData();

            // Select ball type based on difficulty pattern
            data.DeliveryType = SelectBallType();

            // Set speed based on career stage and ball type
            data.Speed = CalculateSpeed(data.DeliveryType);

            // Set line and length based on difficulty
            SetLineAndLength(data);

            // Configure spin/swing based on ball type
            ConfigureMovement(data);

            // Set bounce height
            data.BounceHeight = CalculateBounceHeight(data);

            // Calculate pitch and target positions
            CalculatePositions(data);

            return data;
        }

        /// <summary>
        /// Select the type of delivery to bowl based on difficulty and match context.
        /// </summary>
        private BallType SelectBallType()
        {
            float random = Random.value;

            switch (_currentDifficulty)
            {
                case DifficultyLevel.Easy:
                    // Mostly straightforward pace and full balls
                    if (random < 0.5f) return BallType.Pace;
                    if (random < 0.7f) return BallType.Seam;
                    if (random < 0.85f) return BallType.OffSpin;
                    return BallType.LegSpin;

                case DifficultyLevel.Medium:
                    // Mix of pace and spin with some swing
                    if (random < 0.3f) return BallType.Pace;
                    if (random < 0.5f) return BallType.Swing;
                    if (random < 0.65f) return BallType.Seam;
                    if (random < 0.8f) return BallType.OffSpin;
                    if (random < 0.9f) return BallType.LegSpin;
                    return BallType.Yorker;

                case DifficultyLevel.Hard:
                    // Varied attack with variations
                    if (random < 0.2f) return BallType.Swing;
                    if (random < 0.35f) return BallType.Seam;
                    if (random < 0.5f) return BallType.LegSpin;
                    if (random < 0.6f) return BallType.Googly;
                    if (random < 0.7f) return BallType.Yorker;
                    if (random < 0.8f) return BallType.Bouncer;
                    if (random < 0.9f) return BallType.SlowerBall;
                    return BallType.Doosra;

                case DifficultyLevel.Legend:
                    // Unpredictable with more variations
                    if (_boundariesInRow >= 2) return SelectAggressiveDelivery();
                    if (_dotsInRow >= 3) return SelectAttackingDelivery();
                    return SelectVariedDelivery();

                default:
                    return BallType.Pace;
            }
        }

        private BallType SelectAggressiveDelivery()
        {
            float random = Random.value;
            if (random < 0.3f) return BallType.Yorker;
            if (random < 0.5f) return BallType.Bouncer;
            if (random < 0.7f) return BallType.SlowerBall;
            if (random < 0.85f) return BallType.Googly;
            return BallType.Doosra;
        }

        private BallType SelectAttackingDelivery()
        {
            float random = Random.value;
            if (random < 0.25f) return BallType.Pace;
            if (random < 0.5f) return BallType.Swing;
            if (random < 0.7f) return BallType.LegSpin;
            if (random < 0.85f) return BallType.OffSpin;
            return BallType.Seam;
        }

        private BallType SelectVariedDelivery()
        {
            // Ensure variety - avoid repeating last delivery
            BallType[] options = {
                BallType.Pace, BallType.Swing, BallType.Seam, BallType.OffSpin,
                BallType.LegSpin, BallType.Googly, BallType.Doosra, BallType.Yorker,
                BallType.Bouncer, BallType.SlowerBall
            };

            BallType selected = options[Random.Range(0, options.Length)];

            // Try to avoid repeating the last delivery
            if (_recentDeliveries.Count > 0 && selected == _recentDeliveries[_recentDeliveries.Count - 1])
            {
                selected = options[Random.Range(0, options.Length)];
            }

            return selected;
        }

        /// <summary>
        /// Calculate ball speed based on career stage and ball type.
        /// </summary>
        private float CalculateSpeed(BallType ballType)
        {
            float maxStageSpeed = GetMaxSpeedForStage();
            float baseSpeed;

            switch (ballType)
            {
                case BallType.Pace:
                case BallType.Bouncer:
                    baseSpeed = Random.Range(maxStageSpeed * 0.85f, maxStageSpeed);
                    break;
                case BallType.Swing:
                case BallType.Seam:
                    baseSpeed = Random.Range(maxStageSpeed * 0.75f, maxStageSpeed * 0.9f);
                    break;
                case BallType.Yorker:
                    baseSpeed = Random.Range(maxStageSpeed * 0.8f, maxStageSpeed * 0.95f);
                    break;
                case BallType.SlowerBall:
                    baseSpeed = Random.Range(maxStageSpeed * 0.55f, maxStageSpeed * 0.7f);
                    break;
                case BallType.OffSpin:
                case BallType.LegSpin:
                case BallType.Googly:
                case BallType.Doosra:
                    baseSpeed = Random.Range(_minSpeed, maxStageSpeed * 0.65f);
                    break;
                default:
                    baseSpeed = Random.Range(maxStageSpeed * 0.7f, maxStageSpeed * 0.85f);
                    break;
            }

            return Mathf.Clamp(baseSpeed, _minSpeed, _maxSpeed);
        }

        /// <summary>
        /// Get the maximum ball speed allowed for the current career stage.
        /// </summary>
        private float GetMaxSpeedForStage()
        {
            switch (_currentCareerStage)
            {
                case CareerStage.GullyCricket:
                case CareerStage.TennisBallTournament:
                    return _gullyMaxSpeed;
                case CareerStage.District:
                    return _districtMaxSpeed;
                case CareerStage.State:
                case CareerStage.RanjiTrophy:
                    return _stateMaxSpeed;
                case CareerStage.IPL:
                case CareerStage.International:
                case CareerStage.WorldCupFinals:
                    return _internationalMaxSpeed;
                default:
                    return _districtMaxSpeed;
            }
        }

        /// <summary>
        /// Set ball line and length based on difficulty.
        /// Higher difficulty means more variation and tighter lines.
        /// </summary>
        private void SetLineAndLength(BallDeliveryData data)
        {
            switch (_currentDifficulty)
            {
                case DifficultyLevel.Easy:
                    // Predictable - mostly middle stump, good to full length
                    data.Line = Random.Range(-0.3f, 0.3f);
                    data.Length = Random.Range(0.25f, 0.55f);
                    break;

                case DifficultyLevel.Medium:
                    data.Line = Random.Range(-0.6f, 0.6f);
                    data.Length = Random.Range(0.15f, 0.7f);
                    break;

                case DifficultyLevel.Hard:
                    // Targets off stump channel or body line
                    data.Line = Random.Range(-0.8f, 0.8f);
                    data.Length = Random.Range(0.1f, 0.85f);
                    break;

                case DifficultyLevel.Legend:
                    data.Line = Random.Range(-1f, 1f);
                    data.Length = Random.Range(0f, 1f);
                    break;
            }

            // Override length for specific delivery types
            if (data.DeliveryType == BallType.Yorker) data.Length = Random.Range(0f, 0.1f);
            if (data.DeliveryType == BallType.Bouncer) data.Length = Random.Range(0.8f, 1f);
        }

        /// <summary>
        /// Configure spin and swing properties based on ball type.
        /// </summary>
        private void ConfigureMovement(BallDeliveryData data)
        {
            switch (data.DeliveryType)
            {
                case BallType.Pace:
                    data.SpinRate = 0f;
                    data.SwingAmount = Random.Range(0f, 0.1f);
                    data.SeamMovement = Random.Range(0f, 0.2f);
                    break;

                case BallType.Swing:
                    data.SpinRate = 0f;
                    data.SwingAmount = Random.Range(0.4f, 0.9f);
                    data.SwingDirection = Random.value > 0.5f ? 1f : -1f;
                    data.SeamMovement = Random.Range(0.1f, 0.3f);
                    break;

                case BallType.Seam:
                    data.SpinRate = 0f;
                    data.SwingAmount = Random.Range(0f, 0.2f);
                    data.SeamMovement = Random.Range(0.4f, 0.8f);
                    data.SwingDirection = Random.value > 0.5f ? 1f : -1f;
                    break;

                case BallType.OffSpin:
                    data.SpinRate = Random.Range(15f, 30f);
                    data.SpinAxis = new Vector3(0f, 1f, 0.3f).normalized;
                    data.SwingAmount = 0f;
                    data.SeamMovement = 0f;
                    break;

                case BallType.LegSpin:
                    data.SpinRate = Random.Range(18f, 35f);
                    data.SpinAxis = new Vector3(0f, -1f, 0.3f).normalized;
                    data.SwingAmount = 0f;
                    data.SeamMovement = 0f;
                    break;

                case BallType.Googly:
                    // Looks like leg spin but turns the other way
                    data.SpinRate = Random.Range(15f, 28f);
                    data.SpinAxis = new Vector3(0f, 1f, -0.3f).normalized;
                    data.SwingAmount = 0f;
                    data.SeamMovement = 0f;
                    break;

                case BallType.Doosra:
                    // Looks like off spin but turns the other way
                    data.SpinRate = Random.Range(12f, 25f);
                    data.SpinAxis = new Vector3(0f, -1f, -0.3f).normalized;
                    data.SwingAmount = 0f;
                    data.SeamMovement = 0f;
                    break;

                case BallType.Yorker:
                    data.SpinRate = 0f;
                    data.SwingAmount = Random.Range(0f, 0.3f);
                    data.SwingDirection = Random.value > 0.5f ? 1f : -1f;
                    data.SeamMovement = 0f;
                    break;

                case BallType.Bouncer:
                    data.SpinRate = 0f;
                    data.SwingAmount = 0f;
                    data.SeamMovement = Random.Range(0f, 0.3f);
                    data.BounceHeight = Random.Range(1.5f, 2.0f);
                    break;

                case BallType.SlowerBall:
                    data.SpinRate = Random.Range(5f, 15f);
                    data.SpinAxis = Vector3.up;
                    data.SwingAmount = Random.Range(0f, 0.2f);
                    data.SeamMovement = 0f;
                    break;
            }
        }

        /// <summary>
        /// Calculate bounce height based on delivery type and length.
        /// </summary>
        private float CalculateBounceHeight(BallDeliveryData data)
        {
            if (data.DeliveryType == BallType.Bouncer) return Random.Range(1.5f, 2.0f);
            if (data.DeliveryType == BallType.Yorker) return Random.Range(0.5f, 0.7f);

            float baseBounce = Mathf.Lerp(0.7f, 1.5f, data.Length);
            return baseBounce + Random.Range(-0.1f, 0.1f);
        }

        /// <summary>
        /// Calculate the pitch landing and target positions from line/length parameters.
        /// </summary>
        private void CalculatePositions(BallDeliveryData data)
        {
            // Pitch length is ~20.12 meters
            float pitchLength = 20.12f;

            // Length determines how far down the pitch the ball lands
            // 0 = very full (near batsman), 1 = very short (near bowler)
            float landingDistance = Mathf.Lerp(pitchLength * 0.85f, pitchLength * 0.35f, data.Length);

            // Line determines lateral offset at the batting crease
            float lateralOffset = data.Line * 0.3f; // Max ~30cm off middle stump

            data.PitchPosition = new Vector3(lateralOffset, 0f, landingDistance);

            // Target position at batsman (stump height varies with length)
            float targetHeight;
            switch (data.GetLengthCategory())
            {
                case BallLengthCategory.Yorker:
                    targetHeight = 0.1f;
                    break;
                case BallLengthCategory.Full:
                    targetHeight = 0.4f;
                    break;
                case BallLengthCategory.GoodLength:
                    targetHeight = 0.6f;
                    break;
                case BallLengthCategory.Short:
                    targetHeight = 1.0f;
                    break;
                case BallLengthCategory.Bouncer:
                    targetHeight = 1.5f;
                    break;
                default:
                    targetHeight = 0.6f;
                    break;
            }

            data.TargetPosition = new Vector3(lateralOffset, targetHeight, pitchLength);
        }

        /// <summary>
        /// Reset for a new over.
        /// </summary>
        public void NewOver()
        {
            _currentBallNumber = 0;
            _currentOverNumber++;
            _recentDeliveries.Clear();
        }
    }
}
