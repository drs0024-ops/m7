using UnityEngine;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;

namespace Game.Bootstrap
{
    /// <summary>
    /// Owns the current GameState and validates transitions via a state table.
    /// Publishes all state-related messages (GameStateChanged, GameStarted, GamePaused,
    /// GameResumed, TimeScalePause/Resume). Does NOT decide WHEN to transition.
    /// </summary>
    public class GameStateMachine
    {
        #region Dependencies

        private readonly IPublisher<TimeScalePause> _pausePublisher;
        private readonly IPublisher<TimeScaleResume> _resumePublisher;
        private readonly IPublisher<GameStateChanged> _stateChangedPublisher;
        private readonly IPublisher<GameStarted> _gameStartedPublisher;
        private readonly IPublisher<GamePaused> _gamePausedPublisher;
        private readonly IPublisher<GameResumed> _gameResumedPublisher;

        #endregion

        #region State

        private GameState _currentState = GameState.MainMenu;

        public GameState CurrentState => _currentState;

        #endregion

        #region Transition Table

        private static readonly Dictionary<GameState, HashSet<GameState>> _validTransitions = new()
        {
            [GameState.MainMenu]   = new() { GameState.Loading, GameState.IntroVideo, GameState.QuitGame },
            [GameState.Loading]    = new() { GameState.Gameplay, GameState.MainMenu },
            [GameState.Gameplay]   = new() { GameState.Paused, GameState.GameOver, GameState.Loading },
            [GameState.Paused]     = new() { GameState.Gameplay, GameState.MainMenu },
            [GameState.GameOver]   = new() { GameState.MainMenu },
            [GameState.IntroVideo] = new() { GameState.MainMenu, GameState.Loading },
            [GameState.QuitGame]   = new() { },
        };

        #endregion

        #region Construction

        public GameStateMachine(
            IPublisher<TimeScalePause> pausePublisher,
            IPublisher<TimeScaleResume> resumePublisher,
            IPublisher<GameStateChanged> stateChangedPublisher,
            IPublisher<GameStarted> gameStartedPublisher,
            IPublisher<GamePaused> gamePausedPublisher,
            IPublisher<GameResumed> gameResumedPublisher)
        {
            _pausePublisher = pausePublisher;
            _resumePublisher = resumePublisher;
            _stateChangedPublisher = stateChangedPublisher;
            _gameStartedPublisher = gameStartedPublisher;
            _gamePausedPublisher = gamePausedPublisher;
            _gameResumedPublisher = gameResumedPublisher;
        }

        #endregion

        #region Public API

        public bool IsInState(GameState state) => _currentState == state;

        public bool CanTransitionTo(GameState target)
        {
            return _validTransitions.TryGetValue(_currentState, out var allowed)
                   && allowed.Contains(target);
        }

        /// <summary>
        /// Validates and executes a state transition. Returns false (and logs) if invalid.
        /// </summary>
        public bool TransitionTo(GameState target)
        {
            if (_currentState == target) return true;

            if (!CanTransitionTo(target))
            {
                Debug.LogWarning($"[StateMachine] Invalid transition: {_currentState} → {target}");
                return false;
            }

            ExecuteTransition(target);
            return true;
        }

        /// <summary>
        /// Forces a state without validation. Use ONLY for startup and error recovery.
        /// </summary>
        public void ForceState(GameState target)
        {
            if (_currentState == target) return;
            ExecuteTransition(target);
        }

        #endregion

        #region Internal

        private void ExecuteTransition(GameState target)
        {
            GameState previous = _currentState;
            _currentState = target;

            if (ShouldPause(target))
                _pausePublisher.Publish(default);
            else if (ShouldPause(previous))
                _resumePublisher.Publish(default);

            // FIX #79: Publish GameStarted on ANY transition INTO Gameplay,
            // not just MainMenu → Gameplay. This covers both new-game
            // (MainMenu → Loading → Gameplay) and load-game paths.
            if (target == GameState.Gameplay && previous != GameState.Gameplay && previous != GameState.Paused)
                _gameStartedPublisher.Publish(default);

            if (target == GameState.Paused)
                _gamePausedPublisher.Publish(default);

            if (previous == GameState.Paused && target == GameState.Gameplay)
                _gameResumedPublisher.Publish(default);

            _stateChangedPublisher.Publish(new GameStateChanged(target));
        }

        private static bool ShouldPause(GameState state)
        {
            return state == GameState.Paused;
        }

        #endregion
    }
}   