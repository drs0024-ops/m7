#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Editor-only collector that subscribes to game flow messages
    /// and populates GameFlowDebugInfo for the debug window.
    /// Registered in the Bootstrap VContainer scope.
    /// </summary>
    public class GameFlowDebugCollector : IStartable, IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;
        private GameState _lastState = GameState.MainMenu;
        private readonly GameStateMachine _stateMachine;

		[Inject]
		public GameFlowDebugCollector(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        void IStartable.Start()
        {
            // Sync initial state (handles IStartable ordering race)
            GameFlowDebugInfo.CurrentState = _stateMachine.CurrentState;

            var stateSub = GlobalMessagePipe.GetSubscriber<GameStateChanged>();
            _subscriptions.Add(stateSub.Subscribe(OnStateChanged));

            var phaseSub = GlobalMessagePipe.GetSubscriber<GamePhaseChangedMessage>();
            _subscriptions.Add(phaseSub.Subscribe(OnPhaseChanged));

            var transStartSub = GlobalMessagePipe.GetSubscriber<SceneTransitionStarted>();
            _subscriptions.Add(transStartSub.Subscribe(_ => GameFlowDebugInfo.IsTransitioning = true));

            var transCompleteSub = GlobalMessagePipe.GetSubscriber<SceneTransitionCompleted>();
            _subscriptions.Add(transCompleteSub.Subscribe(_ => GameFlowDebugInfo.IsTransitioning = false));
        }   

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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }
    }
}
#endif   