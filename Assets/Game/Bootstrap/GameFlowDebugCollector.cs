#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Editor-only collector that subscribes to game flow messages
    /// and populates GameFlowDebugInfo for the debug window.
    /// Registered as an entry point in the Bootstrap scope.
    /// </summary>
    public class GameFlowDebugCollector : IStartable, IDisposable
    {
        #region Dependencies

        private readonly ISubscriber<GameStateChanged> _stateSub;
        private readonly ISubscriber<GamePhaseChangedMessage> _phaseSub;
        private readonly ISubscriber<SceneTransitionStarted> _transStartSub;
        private readonly ISubscriber<SceneTransitionCompleted> _transCompleteSub;
        private readonly GameStateMachine _stateMachine;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new(4);
        private bool _disposed;
        private GameState _lastState = GameState.MainMenu;

        #endregion

        #region Construction

        public GameFlowDebugCollector(
            GameStateMachine stateMachine,
            ISubscriber<GameStateChanged> stateSub,
            ISubscriber<GamePhaseChangedMessage> phaseSub,
            ISubscriber<SceneTransitionStarted> transStartSub,
            ISubscriber<SceneTransitionCompleted> transCompleteSub)
        {
            _stateMachine = stateMachine;
            _stateSub = stateSub;
            _phaseSub = phaseSub;
            _transStartSub = transStartSub;
            _transCompleteSub = transCompleteSub;
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            GameFlowDebugInfo.CurrentState = _stateMachine.CurrentState;

            _subscriptions.Add(_stateSub.Subscribe(OnStateChanged));
            _subscriptions.Add(_phaseSub.Subscribe(OnPhaseChanged));
            _subscriptions.Add(_transStartSub.Subscribe(_ => GameFlowDebugInfo.IsTransitioning = true));
            _subscriptions.Add(_transCompleteSub.Subscribe(_ => GameFlowDebugInfo.IsTransitioning = false));
        }

        #endregion

        #region Message Handlers

        private void OnStateChanged(GameStateChanged msg)
        {
            GameFlowDebugInfo.RecordTransition(_lastState, msg.State);
            GameFlowDebugInfo.CurrentState = msg.State;
            GameFlowDebugInfo.CurrentInputMap = MapInputForState(msg.State);
            _lastState = msg.State;
        }

        private void OnPhaseChanged(GamePhaseChangedMessage msg)
        {
            GameFlowDebugInfo.CurrentPhase = msg.NewPhase;
        }

        private static string MapInputForState(GameState state)
        {
            return state switch
            {
                GameState.Gameplay => "Player",
                _ => "Menu"
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}
#endif   