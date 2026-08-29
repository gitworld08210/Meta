using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.BallPhysics
{
    /// <summary>
    /// Simulates fielding outcomes: determines whether a shot results in a catch,
    /// boundary (4 or 6), or runs scored based on ball trajectory and field placement.
    /// Uses probability-based calculations with difficulty scaling.
    /// </summary>
    public class FieldingSimulator : MonoBehaviour
    {
        [Header("Field Dimensions")]
        [SerializeField] private float _boundaryDistance = 65f; // meters from center of pitch
        [SerializeField] private float _innerCircleRadius = 30f;

        [Header("Fielding Quality")]
        [SerializeField] private DifficultyLevel _difficulty = DifficultyLevel.Medium;

        [Header("Catch Probabilities")]
        [SerializeField] private float _baseCatchProbability = 0.3f;
        [SerializeField] private float _closeFielderCatchBonus = 0.25f;
        [SerializeField] private float _edgeCatchProbability = 0.6f;

        /// <summary>
        /// Field placement data - tracks where fielders are positioned.
        /// </summary>
        private FieldingPosition[] _currentFieldPlacements;
        private Vector3[] _fielderPositions;

        /// <summary>
        /// Initialize with default field placement based on difficulty.
        /// </summary>
        private void Awake()
        {
            SetDefaultFieldPlacement();
        }

        /// <summary>
        /// Set the difficulty which affects fielding quality.
        /// </summary>
        public void SetDifficulty(DifficultyLevel difficulty)
        {
            _difficulty = difficulty;
        }

        /// <summary>
        /// Simulate the fielding result for a shot.
        /// </summary>
        /// <param name="exitVelocity">Ball velocity after leaving the bat.</param>
        /// <param name="exitPosition">Position where ball leaves the bat.</param>
        /// <param name="shotType">Type of shot played.</param>
        /// <returns>Fielding result with runs scored and dismissal info.</returns>
        public FieldingResult SimulateFielding(Vector3 exitVelocity, Vector3 exitPosition, ShotType shotType)
        {
            FieldingResult result = new FieldingResult();

            float exitSpeed = exitVelocity.magnitude;
            float elevation = Vector3.Angle(new Vector3(exitVelocity.x, 0f, exitVelocity.z), exitVelocity);
            bool isAerial = exitVelocity.y > 2f;

            // Calculate where the ball lands/reaches
            Vector3 landingPosition = CalculateLandingPosition(exitVelocity, exitPosition);
            float distanceFromPitch = Vector3.Distance(Vector3.zero, new Vector3(landingPosition.x, 0f, landingPosition.z));

            result.LandingPosition = landingPosition;
            result.DistanceFromCenter = distanceFromPitch;

            // Check if it is a six (clears boundary without bouncing)
            if (isAerial && distanceFromPitch >= _boundaryDistance && exitVelocity.y > 5f)
            {
                result.IsBoundary = true;
                result.Runs = 6;
                result.IsSix = true;
                result.Description = "SIX! Over the boundary!";
                return result;
            }

            // Check for catch opportunity if ball is in the air
            if (isAerial)
            {
                bool isCaught = EvaluateCatchChance(landingPosition, exitSpeed, elevation);
                if (isCaught)
                {
                    result.IsWicket = true;
                    result.DismissalType = DismissalType.Caught;
                    result.Runs = 0;
                    result.Description = "CAUGHT! Fielder takes the catch!";
                    return result;
                }
            }

            // Check if it reaches the boundary on the ground
            if (distanceFromPitch >= _boundaryDistance)
            {
                result.IsBoundary = true;
                result.Runs = 4;
                result.IsSix = false;
                result.Description = "FOUR! Races to the boundary!";
                return result;
            }

            // Calculate runs based on field placement and ball speed
            result.Runs = CalculateRunsScored(landingPosition, exitSpeed, shotType);
            result.IsBoundary = false;
            result.Description = GetRunDescription(result.Runs);

            return result;
        }

        /// <summary>
        /// Calculate where the ball will land based on exit velocity.
        /// Uses projectile motion with air resistance approximation.
        /// </summary>
        private Vector3 CalculateLandingPosition(Vector3 exitVelocity, Vector3 exitPosition)
        {
            float gravity = 9.81f;
            float vy = exitVelocity.y;
            float vHorizontal = new Vector2(exitVelocity.x, exitVelocity.z).magnitude;

            // Time to hit ground (quadratic formula for y = vy*t - 0.5*g*t^2 + exitHeight = 0)
            float exitHeight = Mathf.Max(exitPosition.y, 1f);
            float discriminant = vy * vy + 2f * gravity * exitHeight;

            float timeToLand;
            if (discriminant > 0f)
            {
                timeToLand = (vy + Mathf.Sqrt(discriminant)) / gravity;
            }
            else
            {
                timeToLand = 2f * vy / gravity;
            }

            timeToLand = Mathf.Max(timeToLand, 0.5f);

            // Apply simple air resistance factor
            float dragFactor = 0.85f;
            float horizontalDistance = vHorizontal * timeToLand * dragFactor;

            Vector3 horizontalDir = new Vector3(exitVelocity.x, 0f, exitVelocity.z).normalized;
            return exitPosition + horizontalDir * horizontalDistance;
        }

        /// <summary>
        /// Evaluate whether a catch is taken based on ball position, speed, and fielder placement.
        /// </summary>
        private bool EvaluateCatchChance(Vector3 landingPosition, float exitSpeed, float elevation)
        {
            float catchProbability = _baseCatchProbability;
            float distFromCenter = new Vector2(landingPosition.x, landingPosition.z).magnitude;

            // Closer to fielders = higher catch chance
            float nearestFielderDist = GetNearestFielderDistance(landingPosition);
            if (nearestFielderDist < 5f)
            {
                catchProbability += _closeFielderCatchBonus * (1f - nearestFielderDist / 5f);
            }

            // Higher elevation = easier catch (slower ball)
            if (elevation > 45f)
            {
                catchProbability += 0.15f;
            }

            // Very fast shots are harder to catch
            if (exitSpeed > 30f)
            {
                catchProbability -= 0.1f;
            }

            // Difficulty modifiers
            switch (_difficulty)
            {
                case DifficultyLevel.Easy:
                    catchProbability *= 0.5f;
                    break;
                case DifficultyLevel.Medium:
                    catchProbability *= 0.75f;
                    break;
                case DifficultyLevel.Hard:
                    catchProbability *= 1.0f;
                    break;
                case DifficultyLevel.Legend:
                    catchProbability *= 1.3f;
                    break;
            }

            catchProbability = Mathf.Clamp01(catchProbability);
            return Random.value < catchProbability;
        }

        /// <summary>
        /// Calculate how many runs are scored from running between the wickets.
        /// </summary>
        private int CalculateRunsScored(Vector3 landingPosition, float exitSpeed, ShotType shotType)
        {
            float distance = new Vector2(landingPosition.x, landingPosition.z).magnitude;
            float nearestFielderDist = GetNearestFielderDistance(landingPosition);

            // Base runs from distance
            int runs = 0;
            if (distance < 10f && nearestFielderDist < 8f)
            {
                runs = 0; // Dot ball - fielder close to landing
            }
            else if (distance < 20f)
            {
                runs = Random.value > 0.4f ? 1 : 0;
            }
            else if (distance < 35f)
            {
                runs = Random.value > 0.3f ? 2 : 1;
            }
            else if (distance < 50f)
            {
                runs = Random.value > 0.5f ? 3 : 2;
            }
            else
            {
                runs = 3; // Close to boundary but not quite
            }

            // Defensive block rarely scores
            if (shotType == ShotType.DefensiveBlock)
            {
                runs = Random.value > 0.7f ? 1 : 0;
            }

            // Speed affects runs (faster ball = more time to run)
            if (exitSpeed > 25f && runs > 0)
            {
                runs = Mathf.Min(runs + (Random.value > 0.7f ? 1 : 0), 3);
            }

            // Gap finding bonus (no fielder nearby)
            if (nearestFielderDist > 15f && runs < 3)
            {
                runs = Mathf.Min(runs + 1, 3);
            }

            // Run-out risk on difficulty (reduces runs on harder modes)
            if (_difficulty == DifficultyLevel.Legend && runs >= 2 && Random.value < 0.1f)
            {
                // Potential run-out scenario
                runs = Mathf.Max(runs - 1, 0);
            }

            return runs;
        }

        /// <summary>
        /// Get the distance to the nearest fielder from a given position.
        /// </summary>
        private float GetNearestFielderDistance(Vector3 position)
        {
            if (_fielderPositions == null || _fielderPositions.Length == 0)
                return 20f;

            float nearest = float.MaxValue;
            Vector2 pos2D = new Vector2(position.x, position.z);

            foreach (Vector3 fielderPos in _fielderPositions)
            {
                float dist = Vector2.Distance(pos2D, new Vector2(fielderPos.x, fielderPos.z));
                if (dist < nearest)
                    nearest = dist;
            }

            return nearest;
        }

        /// <summary>
        /// Set default field placement based on match situation.
        /// </summary>
        private void SetDefaultFieldPlacement()
        {
            _currentFieldPlacements = new FieldingPosition[]
            {
                FieldingPosition.Wicketkeeper,
                FieldingPosition.FirstSlip,
                FieldingPosition.Point,
                FieldingPosition.Cover,
                FieldingPosition.MidOff,
                FieldingPosition.MidOn,
                FieldingPosition.MidWicket,
                FieldingPosition.SquareLeg,
                FieldingPosition.FineLeg,
                FieldingPosition.ThirdMan,
                FieldingPosition.LongOff
            };

            UpdateFielderPositions();
        }

        /// <summary>
        /// Set custom field placement.
        /// </summary>
        public void SetFieldPlacement(FieldingPosition[] positions)
        {
            _currentFieldPlacements = positions;
            UpdateFielderPositions();
        }

        /// <summary>
        /// Convert field positions to world positions.
        /// </summary>
        private void UpdateFielderPositions()
        {
            _fielderPositions = new Vector3[_currentFieldPlacements.Length];

            for (int i = 0; i < _currentFieldPlacements.Length; i++)
            {
                _fielderPositions[i] = GetWorldPositionForFielder(_currentFieldPlacements[i]);
            }
        }

        /// <summary>
        /// Get the approximate world position for a fielding position.
        /// </summary>
        private Vector3 GetWorldPositionForFielder(FieldingPosition position)
        {
            // Positions relative to batsman (at origin), facing bowler (negative z)
            switch (position)
            {
                case FieldingPosition.Wicketkeeper:
                    return new Vector3(0f, 0f, -2f);
                case FieldingPosition.FirstSlip:
                    return new Vector3(2f, 0f, -3f);
                case FieldingPosition.SecondSlip:
                    return new Vector3(3.5f, 0f, -2.5f);
                case FieldingPosition.ThirdSlip:
                    return new Vector3(5f, 0f, -2f);
                case FieldingPosition.Gully:
                    return new Vector3(6f, 0f, 0f);
                case FieldingPosition.Point:
                    return new Vector3(20f, 0f, 5f);
                case FieldingPosition.CoverPoint:
                    return new Vector3(18f, 0f, 12f);
                case FieldingPosition.Cover:
                    return new Vector3(15f, 0f, 20f);
                case FieldingPosition.MidOff:
                    return new Vector3(5f, 0f, 25f);
                case FieldingPosition.MidOn:
                    return new Vector3(-5f, 0f, 25f);
                case FieldingPosition.MidWicket:
                    return new Vector3(-15f, 0f, 20f);
                case FieldingPosition.SquareLeg:
                    return new Vector3(-20f, 0f, 5f);
                case FieldingPosition.FineLeg:
                    return new Vector3(-10f, 0f, -20f);
                case FieldingPosition.ThirdMan:
                    return new Vector3(15f, 0f, -25f);
                case FieldingPosition.LongOff:
                    return new Vector3(10f, 0f, 55f);
                case FieldingPosition.LongOn:
                    return new Vector3(-10f, 0f, 55f);
                case FieldingPosition.DeepMidWicket:
                    return new Vector3(-40f, 0f, 30f);
                case FieldingPosition.DeepSquareLeg:
                    return new Vector3(-50f, 0f, 5f);
                default:
                    return new Vector3(15f, 0f, 15f);
            }
        }

        /// <summary>
        /// Get description text for runs scored.
        /// </summary>
        private string GetRunDescription(int runs)
        {
            switch (runs)
            {
                case 0: return "Dot ball!";
                case 1: return "Single taken.";
                case 2: return "Good running, two runs!";
                case 3: return "Excellent running, three runs!";
                default: return $"{runs} runs scored.";
            }
        }
    }

    /// <summary>
    /// Result of the fielding simulation for a delivery.
    /// </summary>
    [System.Serializable]
    public class FieldingResult
    {
        public int Runs;
        public bool IsBoundary;
        public bool IsSix;
        public bool IsWicket;
        public DismissalType DismissalType;
        public Vector3 LandingPosition;
        public float DistanceFromCenter;
        public string Description;

        public FieldingResult()
        {
            Runs = 0;
            IsBoundary = false;
            IsSix = false;
            IsWicket = false;
            DismissalType = DismissalType.NotOut;
            LandingPosition = Vector3.zero;
            DistanceFromCenter = 0f;
            Description = "";
        }
    }
}
