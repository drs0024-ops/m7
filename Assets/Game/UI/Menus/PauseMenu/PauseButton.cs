using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.UI
{
    public class PauseButton : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private UnityEngine.UI.Button _button; 

        private IPublisher<PauseGameRequested> _publisher;
        private ISubscriber<GamePaused> _pausedSub;
        private ISubscriber<GameResumed> _resumedSub;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        public PauseButton() { }

        void IStartable.Start()
        {
            _publisher = GlobalMessagePipe.GetPublisher<PauseGameRequested>();
            _pausedSub = GlobalMessagePipe.GetSubscriber<GamePaused>();
            _resumedSub = GlobalMessagePipe.GetSubscriber<GameResumed>();

            _subscriptions.Add(_pausedSub.Subscribe(_ => SetEnabled(false)));
            _subscriptions.Add(_resumedSub.Subscribe(_ => SetEnabled(true)));
        }

        public void OnClick()
        {
            if (_disposed) return;
            _publisher.Publish(default);
        }

        private void SetEnabled(bool enabled)
        {
            if (_button != null) _button.interactable = enabled;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy() => Dispose();
    }
}   