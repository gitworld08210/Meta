using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;
using MetaCricket.BallPhysics;

namespace MetaCricket.MatchEngine
{
    /// <summary>
    /// MonoBehaviour orchestrating a complete cricket match: manages innings, overs,
    /// deliveries, batting order, score, and wickets. Handles match state transitions
    /// (InningsStart, BetweenBalls, BallInPlay, BetweenOvers, InningsEnd, MatchEnd).
    /// Supports T20, ODI, and Test formats.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BowlingMachine _bowlingMachine;
        [SerializeField] private FieldingSimulator _fieldingSimulator;
        [SerializeField] private OpponentAI _opponentAI;

        [Header("Match Configuration")]
        [SerializeField] private MatchConfig _matchConfig;

        // Match state
        private MatchState _currentState;
        private int _currentInnings;
        private int _currentOver;
        private int _currentBall;
        private int _runsInCurrentOver;
        private int _wicketsInCurrentOver;
        private List<int> _ballByBallInOver;

        // Innings data
        private InningsData _playerInnings;
        private InningsData _opponentInnings;
        private Scorecard _scorecard;

        // Batsman tracking
        private BatsmanInningsData _currentBatsman;
        private int _playerBattingPosition;

        // Over tracking
        private int _consecutiveDots;
        private int _consecutiveBoundaries;

        /// <summary>
        /// Current match state.
        /// </summary>
        public MatchState CurrentState => _currentState;

        /// <summary>
        /// Current innings number (1 or 2).
        /// </summary>
        public int CurrentInnings => _currentInnings;

        /// <summary>
        /// Get the current scorecard.
        /// </summary>
        public Scorecard CurrentScorecard => _scorecard;

        /// <summary>
        /// Get player's innings data.
        /// </summary>
        public InningsData PlayerInnings => _playerInnings;

        /// <summary>
        /// Get opponent's innings data.
        /// </summary>
        public InningsData OpponentInnings => _opponentInnings;

        /// <summary>
        /// Get match configuration.
        /// </summary>
        public MatchConfig Config => _matchConfig;

        /// <summary>
        /// Whether the player's innings is active.
        /// </summary>
        public bool IsPlayerBatting => _currentInnings == 1 || (_currentInnings == 2 && _matchConfig.IsChasing);

        private void Awake()
        {
            _ballByBallInOver = new List<int>();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<MatchManager>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<WicketEvent>(OnWicketFallen);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WicketEvent>(OnWicketFallen);
        }

        /// <summary>
        /// Start a new match with the given configuration.
        /// </summary>
        public void StartMatch(MatchConfig config)
        {
            _matchConfig = config;
            _currentState = MatchState.NotStarted;
            _currentInnings = 0;

            // Initialize scorecard
            _scorecard = new Scorecard
            {
                MatchFormat = config.MatchFormat,
                PlayerTeam = config.PlayerTeamName,
                OpponentTeam = config.OpponentTeamName,
                Venue = config.VenueName
            };

            // Initialize innings
            _playerInnings = new InningsData();
            _opponentInnings = new InningsData();

            // Configure bowling machine
            if (_bowlingMachine != null)
            {
                _bowlingMachine.SetDifficulty(config.Difficulty);
                if (config.IsCareerMatch)
                {
                    _bowlingMachine.SetCareerStage(config.CareerMatchStage);
                }
            }

            // Configure fielding
            if (_fieldingSimulator != null)
            {
                _fieldingSimulator.SetDifficulty(config.Difficulty);
            }

            // Configure opponent AI
            if (_opponentAI != null)
            {
                _opponentAI.SetDifficulty(config.Difficulty);
            }

            // Start the first innings
            StartInnings();
        }

        /// <summary>
        /// Start a new innings.
        /// </summary>
        private void StartInnings()
        {
            _currentInnings++;
            _currentOver = 0;
            _currentBall = 0;
            _runsInCurrentOver = 0;
            _wicketsInCurrentOver = 0;
            _consecutiveDots = 0;
            _consecutiveBoundaries = 0;
            _ballByBallInOver = new List<int>();
            _playerBattingPosition = 0;

            // Initialize current batsman
            _currentBatsman = new BatsmanInningsData { BatsmanName = "Player" };
            _playerInnings.BatsmanScores.Add(_currentBatsman);

            TransitionState(MatchState.InningsStart);

            // If player is chasing, simulate opponent first
            if (_currentInnings == 1 && !_matchConfig.IsChasing)
            {
                // Player bats first - proceed to ball delivery
                TransitionState(MatchState.BetweenBalls);
            }
            else if (_currentInnings == 1 && _matchConfig.IsChasing)
            {
                // Opponent bats first - simulate their innings
                SimulateOpponentInnings();
                TransitionState(MatchState.BetweenBalls);
            }
            else if (_currentInnings == 2)
            {
                // Second innings
                if (!_matchConfig.IsChasing)
                {
                    // Opponent chasing
                    SimulateOpponentChase();
                    EndMatch();
                }
                else
                {
                    TransitionState(MatchState.BetweenBalls);
                }
            }
        }

        /// <summary>
        /// Called to deliver the next ball.
        /// </summary>
        public void DeliverBall()
        {
            if (_currentState != MatchState.BetweenBalls)
                return;

            TransitionState(MatchState.BallInPlay);

            if (_bowlingMachine != null)
            {
                _bowlingMachine.SetOverState(_currentOver, _currentBall);
                _bowlingMachine.UpdateMatchSituation(_consecutiveDots, _consecutiveBoundaries);
                _bowlingMachine.BowlDelivery();
            }
        }

        /// <summary>
        /// Process the result of a delivery after the ball has been played.
        /// Called by the game after bat collision or ball passes batsman.
        /// </summary>
        /// <param name="runs">Runs scored on this delivery.</param>
        /// <param name="shotType">Shot type used by batsman.</param>
        /// <param name="direction">Direction the ball traveled.</param>
        /// <param name="distance">Distance the ball traveled.</param>
        public void ProcessDeliveryResult(int runs, ShotType shotType, Vector3 direction, float distance)
        {
            _currentBall++;
            _ballByBallInOver.Add(runs);

            // Update innings
            _playerInnings.AddRuns(runs);
            _playerInnings.RecordBall();

            // Update batsman stats
            if (runs > 0)
            {
                _currentBatsman.AddRuns(runs, shotType);
                _consecutiveDots = 0;
                if (runs >= 4) _consecutiveBoundaries++;
                else _consecutiveBoundaries = 0;
            }
            else
            {
                _currentBatsman.AddDotBall();
                _consecutiveDots++;
                _consecutiveBoundaries = 0;
            }

            _runsInCurrentOver += runs;

            // Update scorecard
            _scorecard.RecordShot(runs, shotType, direction, distance);

            // Publish score update
            PublishScoreUpdate();

            // Check for over completion
            if (_currentBall >= 6)
            {
                CompleteOver();
            }
            else
            {
                TransitionState(MatchState.BetweenBalls);
            }

            // Check if target reached (if chasing)
            if (_matchConfig.IsChasing && _matchConfig.TargetScore > 0)
            {
                if (_playerInnings.TotalRuns > _matchConfig.TargetScore)
                {
                    EndMatch();
                }
            }
        }

        /// <summary>
        /// Process a wicket falling.
        /// </summary>
        private void OnWicketFallen(WicketEvent wicketEvent)
        {
            if (!IsPlayerBatting) return;

            _currentBall++;
            _wicketsInCurrentOver++;
            _ballByBallInOver.Add(-1); // -1 indicates wicket

            _playerInnings.RecordBall();
            _playerInnings.RecordWicket(wicketEvent.DismissalMethod,
                _currentBatsman.Runs, _currentBatsman.BallsFaced);

            _currentBatsman.Dismissal = wicketEvent.DismissalMethod;
            _currentBatsman.IsNotOut = false;

            // Record dismissal on scorecard
            _scorecard.RecordDismissal(wicketEvent.DismissalMethod);

            // Check for duck
            if (_currentBatsman.Runs == 0)
            {
                EventBus.Publish(new DuckOutEvent
                {
                    BallsFaced = _currentBatsman.BallsFaced,
                    DismissalMethod = wicketEvent.DismissalMethod,
                    IsGoldenDuck = _currentBatsman.BallsFaced <= 1
                });
            }

            // Check if all out
            if (_playerInnings.Wickets >= _matchConfig.GetMaxWickets())
            {
                CompleteInnings();
                return;
            }

            // New batsman
            _playerBattingPosition++;
            _currentBatsman = new BatsmanInningsData { BatsmanName = $"Batsman {_playerBattingPosition + 1}" };
            _playerInnings.BatsmanScores.Add(_currentBatsman);

            // Publish score update
            PublishScoreUpdate();

            // Check over completion
            if (_currentBall >= 6)
            {
                CompleteOver();
            }
            else
            {
                TransitionState(MatchState.BetweenBalls);
            }
        }

        /// <summary>
        /// Complete the current over and transition states.
        /// </summary>
        private void CompleteOver()
        {
            // Record over summary
            _scorecard.RecordOverSummary(_currentOver + 1, _runsInCurrentOver, _wicketsInCurrentOver, _ballByBallInOver);

            EventBus.Publish(new OverCompleteEvent
            {
                OverNumber = _currentOver + 1,
                RunsInOver = _runsInCurrentOver,
                WicketsInOver = _wicketsInCurrentOver,
                TotalScore = _playerInnings.TotalRuns,
                TotalWickets = _playerInnings.Wickets,
                RunRate = _playerInnings.RunRate
            });

            _currentOver++;
            _currentBall = 0;
            _runsInCurrentOver = 0;
            _wicketsInCurrentOver = 0;
            _ballByBallInOver = new List<int>();

            // Check if overs limit reached
            if (_currentOver >= _matchConfig.GetOversLimit())
            {
                CompleteInnings();
                return;
            }

            // New over in bowling machine
            if (_bowlingMachine != null)
            {
                _bowlingMachine.NewOver();
            }

            TransitionState(MatchState.BetweenOvers);

            // After a brief pause, transition back to between balls
            TransitionState(MatchState.BetweenBalls);
        }

        /// <summary>
        /// Complete the current innings.
        /// </summary>
        private void CompleteInnings()
        {
            TransitionState(MatchState.InningsEnd);

            EventBus.Publish(new InningsCompleteEvent
            {
                InningsNumber = _currentInnings,
                TotalRuns = _playerInnings.TotalRuns,
                Wickets = _playerInnings.Wickets,
                Overs = _playerInnings.GetOversFloat(),
                RunRate = _playerInnings.RunRate,
                IsAllOut = _playerInnings.Wickets >= _matchConfig.GetMaxWickets(),
                IsOversComplete = _currentOver >= _matchConfig.GetOversLimit()
            });

            // Update scorecard totals
            _scorecard.PlayerTeamTotal = _playerInnings.TotalRuns;
            _scorecard.PlayerTeamWickets = _playerInnings.Wickets;
            _scorecard.PlayerTeamOvers = _playerInnings.GetOversFloat();

            // Handle match progression
            if (_currentInnings == 1 && !_matchConfig.IsChasing)
            {
                // Player batted first, now opponent chases
                SimulateOpponentChase();
                EndMatch();
            }
            else
            {
                // Match is over (player was chasing or second innings complete)
                EndMatch();
            }
        }

        /// <summary>
        /// Simulate the opponent's first innings (batting first).
        /// </summary>
        private void SimulateOpponentInnings()
        {
            if (_opponentAI != null)
            {
                _opponentInnings = _opponentAI.SimulateInnings(_matchConfig, 0);
                _matchConfig.TargetScore = _opponentInnings.TotalRuns;
                _matchConfig.IsChasing = true;

                _scorecard.OpponentTeamTotal = _opponentInnings.TotalRuns;
                _scorecard.OpponentTeamWickets = _opponentInnings.Wickets;
                _scorecard.OpponentTeamOvers = _opponentInnings.GetOversFloat();
            }
        }

        /// <summary>
        /// Simulate the opponent chasing the player's total.
        /// </summary>
        private void SimulateOpponentChase()
        {
            if (_opponentAI != null)
            {
                _opponentInnings = _opponentAI.SimulateInnings(_matchConfig, _playerInnings.TotalRuns);

                _scorecard.OpponentTeamTotal = _opponentInnings.TotalRuns;
                _scorecard.OpponentTeamWickets = _opponentInnings.Wickets;
                _scorecard.OpponentTeamOvers = _opponentInnings.GetOversFloat();
            }
        }

        /// <summary>
        /// End the match and determine the result.
        /// </summary>
        private void EndMatch()
        {
            TransitionState(MatchState.MatchEnd);

            bool playerWins = _playerInnings.TotalRuns > _opponentInnings.TotalRuns;
            int experience = CalculateExperience(playerWins);

            EventBus.Publish(new MatchEndEvent
            {
                IsPlayerWin = playerWins,
                PlayerScore = _playerInnings.TotalRuns,
                OpponentScore = _opponentInnings.TotalRuns,
                ExperienceEarned = experience,
                MatchFormat = _matchConfig.MatchFormat
            });
        }

        /// <summary>
        /// Calculate experience earned from the match.
        /// </summary>
        private int CalculateExperience(bool isWin)
        {
            int xp = 0;

            // Base XP per run
            xp += _scorecard.PlayerRuns * Constants.GameBalance.BaseExperiencePerRun;

            // Boundary bonuses
            xp += _scorecard.PlayerFours * Constants.GameBalance.BonusExperiencePerBoundary;
            xp += _scorecard.PlayerSixes * Constants.GameBalance.BonusExperiencePerSix;

            // Win bonus
            if (isWin)
            {
                xp += Constants.GameBalance.WinBonusExperience;
            }

            // Milestone bonuses
            if (_scorecard.PlayerRuns >= 100) xp += 1000;
            else if (_scorecard.PlayerRuns >= 50) xp += 500;

            // Difficulty multiplier
            switch (_matchConfig.Difficulty)
            {
                case DifficultyLevel.Easy:
                    xp = (int)(xp * 0.7f);
                    break;
                case DifficultyLevel.Medium:
                    xp = (int)(xp * 1.0f);
                    break;
                case DifficultyLevel.Hard:
                    xp = (int)(xp * 1.5f);
                    break;
                case DifficultyLevel.Legend:
                    xp = (int)(xp * 2.0f);
                    break;
            }

            return xp;
        }

        /// <summary>
        /// Publish a score update event.
        /// </summary>
        private void PublishScoreUpdate()
        {
            EventBus.Publish(new ScoreUpdateEvent
            {
                TotalRuns = _playerInnings.TotalRuns,
                Wickets = _playerInnings.Wickets,
                Overs = _playerInnings.OversCompleted,
                BallsInOver = _playerInnings.BallsInCurrentOver,
                RunsThisBall = _ballByBallInOver.Count > 0 ? _ballByBallInOver[_ballByBallInOver.Count - 1] : 0,
                StrikeRate = _currentBatsman.StrikeRate
            });
        }

        /// <summary>
        /// Transition to a new match state.
        /// </summary>
        private void TransitionState(MatchState newState)
        {
            MatchState previousState = _currentState;
            _currentState = newState;

            EventBus.Publish(new MatchStateChangedEvent
            {
                PreviousState = previousState,
                NewState = newState
            });
        }

        /// <summary>
        /// Get a summary string of the current match situation.
        /// </summary>
        public string GetMatchSummary()
        {
            if (_matchConfig == null) return "No match in progress";

            string score = $"{_playerInnings.TotalRuns}/{_playerInnings.Wickets} ({_playerInnings.GetOversString()} ov)";

            if (_matchConfig.IsChasing && _matchConfig.TargetScore > 0)
            {
                int needed = _matchConfig.TargetScore - _playerInnings.TotalRuns + 1;
                return $"{score} - Need {needed} more runs to win";
            }

            return score;
        }
    }
}
