using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using Game.Gameplay.Player;
using MessagePipe;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Handles the Escape key as a context-sensitive toggle:
    /// Gameplay → Pause, Paused → Resume.
    /// Yields Escape handling to OptionsMenuController while the Options panel is open.
    /// Ticks every frame via ITickable. Respects GamePhaseController.IsInputEnabled.
    /// </summary>
    public class GameStateInputHandler : ITickable, IStartable, IDisposable
    {
        #region Dependencies

        private readonly IPublisher<PauseGameRequested> _pausePublisher;
        private readonly IPublisher<ResumeGameRequested> _resumePublisher;
        private readonly ISubscriber<OptionsPanelStateChanged> _optionsStateSub;
        private readonly GameStateMachine _stateMachine;
        private readonly InputManager _inputManager;
        private readonly GamePhaseController _phaseController;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new(1);
        private bool _optionsOpen;
        private bool _disposed;

        #endregion

        #region Construction

        public GameStateInputHandler(
            GameStateMachine stateMachine,
            InputManager inputManager,
            GamePhaseController gamePhaseController,
            IPublisher<PauseGameRequested> pausePublisher,
            IPublisher<ResumeGameRequested> resumePublisher,
            ISubscriber<OptionsPanelStateChanged> optionsStateSub)
        {
            _stateMachine = stateMachine;
            _inputManager = inputManager;
            _phaseController = gamePhaseController;
            _pausePublisher = pausePublisher;
            _resumePublisher = resumePublisher;
            _optionsStateSub = optionsStateSub;
        }

        #endregion

        #region Lifecycle

        void IStartable.Start()
        {
            _subscriptions.Add(_optionsStateSub.Subscribe(msg => _optionsOpen = msg.IsOpen));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }

        #endregion

        #region Tick

        void ITickable.Tick()
        {
            if (_disposed) return;
            if (!_phaseController.IsInputEnabled) return;
            if (_optionsOpen) return;
            if (!_inputManager.EscapeWasPressed) return;

            switch (_stateMachine.CurrentState)
            {
                case GameState.Gameplay:
                    _pausePublisher.Publish(default);
                    break;
                case GameState.Paused:
                    _resumePublisher.Publish(default);
                    break;
            }
        }

        #endregion
    }
}   