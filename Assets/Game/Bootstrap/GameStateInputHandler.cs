using System;
using Game.Core.Enums;
using Game.Core.Messages;
using Game.Gameplay.Player;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Handles the Escape key as a context-sensitive toggle:
    /// Gameplay → Pause, Paused → Resume.
    /// Yields Escape handling to OptionsMenuController while the Options panel is open.
    /// Ticks every frame via ITickable. Respects GamePhaseController.IsInputEnabled.
    /// </summary>
    public class GameStateInputHandler : ITickable, IStartable
    {
        #region Dependencies

        private IPublisher<PauseGameRequested> _pausePublisher;
        private IPublisher<ResumeGameRequested> _resumePublisher;
        private readonly GameStateMachine _stateMachine;
        private readonly InputManager _inputManager;
        private readonly GamePhaseController _phaseController;

        private bool _optionsOpen;

        #endregion

        #region Construction

        [Inject]
        public GameStateInputHandler(
            GameStateMachine stateMachine,
            InputManager inputManager,
            GamePhaseController gamePhaseController)
        {
            _stateMachine = stateMachine;
            _inputManager = inputManager;
            _phaseController = gamePhaseController;
        }

        #endregion

        #region Lifecycle

        void IStartable.Start()
        {
            _pausePublisher = GlobalMessagePipe.GetPublisher<PauseGameRequested>();
            _resumePublisher = GlobalMessagePipe.GetPublisher<ResumeGameRequested>();

            GlobalMessagePipe.GetSubscriber<OptionsPanelStateChanged>()
                .Subscribe(msg => _optionsOpen = msg.IsOpen);
        }

        #endregion

        #region Tick

        void ITickable.Tick()
        {
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