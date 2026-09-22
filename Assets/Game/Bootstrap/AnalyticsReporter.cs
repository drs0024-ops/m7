using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Subscribes to key game flow messages and reports them via IAnalyticsReporter.
    /// Zero network calls. In release builds, IAnalyticsReporter resolves to a no-op.
    /// </summary>
    public class AnalyticsReporter : IStartable, IDisposable
    {
        #region Dependencies

        private readonly IAnalyticsReporter _reporter;
        private readonly GameStateMachine _stateMachine;

        private ISubscriber<GameStateChanged> _stateSub;
        private ISubscriber<SceneTransitionStarted> _transStartSub;
        private ISubscriber<SceneTransitionCompleted> _transCompleteSub;
        private ISubscriber<SaveCompleted> _saveCompleteSub;
        private ISubscriber<SaveFailed> _saveFailedSub;
        private ISubscriber<LoadFailed> _loadFailedSub;
        private ISubscriber<PlayerDied> _diedSub;
        private ISubscriber<VideoFinished> _videoFinishedSub;
        private ISubscriber<VideoSkipRequested> _videoSkipSub;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new();
        private GameState _lastState = GameState.MainMenu;
        private int _transitionStartTimeMs;
        private bool _videoWasSkipped;
        private bool _disposed;

        #endregion

        #region Construction

        [Inject]
        public AnalyticsReporter(IAnalyticsReporter reporter, GameStateMachine stateMachine)
        {
            _reporter = reporter;
            _stateMachine = stateMachine;
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            _lastState = _stateMachine.CurrentState;

            _stateSub = GlobalMessagePipe.GetSubscriber<GameStateChanged>();
            _transStartSub = GlobalMessagePipe.GetSubscriber<SceneTransitionStarted>();
            _transCompleteSub = GlobalMessagePipe.GetSubscriber<SceneTransitionCompleted>();
            _saveCompleteSub = GlobalMessagePipe.GetSubscriber<SaveCompleted>();
            _saveFailedSub = GlobalMessagePipe.GetSubscriber<SaveFailed>();
            _loadFailedSub = GlobalMessagePipe.GetSubscriber<LoadFailed>();
            _diedSub = GlobalMessagePipe.GetSubscriber<PlayerDied>();
            _videoFinishedSub = GlobalMessagePipe.GetSubscriber<VideoFinished>();
            _videoSkipSub = GlobalMessagePipe.GetSubscriber<VideoSkipRequested>();

            _subscriptions.Add(_stateSub.Subscribe(OnStateChanged));
            _subscriptions.Add(_transStartSub.Subscribe(OnTransitionStarted));
            _subscriptions.Add(_transCompleteSub.Subscribe(OnTransitionCompleted));
            _subscriptions.Add(_saveCompleteSub.Subscribe(_ => Track("save_complete", null)));
            _subscriptions.Add(_saveFailedSub.Subscribe(OnSaveFailed));
            _subscriptions.Add(_loadFailedSub.Subscribe(OnLoadFailed));
            _subscriptions.Add(_diedSub.Subscribe(_ => Track("player_died", null)));
            _subscriptions.Add(_videoFinishedSub.Subscribe(OnVideoFinished));
            _subscriptions.Add(_videoSkipSub.Subscribe(_ => _videoWasSkipped = true));
        }

        #endregion

        #region Message Handlers

        private void OnStateChanged(GameStateChanged msg)
        {
            var props = new Dictionary<string, string>
            {
                ["from"] = _lastState.ToString(),
                ["to"] = msg.State.ToString()
            };
            Track("state_change", props);
            _lastState = msg.State;
        }

        private void OnTransitionStarted(SceneTransitionStarted msg)
        {
            _transitionStartTimeMs = Environment.TickCount;
            var props = new Dictionary<string, string>
            {
                ["target"] = msg.TargetScene
            };
            Track("transition_start", props);
        }

        private void OnTransitionCompleted(SceneTransitionCompleted msg)
        {
            int durationMs = Environment.TickCount - _transitionStartTimeMs;
            var props = new Dictionary<string, string>
            {
                ["target"] = msg.TargetScene,
                ["duration_ms"] = durationMs.ToString()
            };
            Track("transition_complete", props);
        }

        private void OnSaveFailed(SaveFailed msg)
        {
            TrackError("save_failed", msg.Error);
        }

        private void OnLoadFailed(LoadFailed msg)
        {
            TrackError("load_failed", msg.Error);
        }

        private void OnVideoFinished(VideoFinished _)
        {
            var props = new Dictionary<string, string>
            {
                ["skipped"] = _videoWasSkipped.ToString().ToLower()
            };
            Track("video_finished", props);
            _videoWasSkipped = false;
        }

        #endregion

        #region Internal

        private void Track(string eventName, Dictionary<string, string> properties)
        {
            if (_disposed) return;
            _reporter.TrackEvent(eventName, properties);
        }

        private void TrackError(string context, string message)
        {
            if (_disposed) return;
            _reporter.TrackError(context, message);
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}   