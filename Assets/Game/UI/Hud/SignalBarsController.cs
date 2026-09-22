using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;

namespace Game.UI
{
    public class SignalBarsController : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private Image[] _bars;
        [SerializeField] private Color _barColor = new Color(0.878f, 0.941f, 1f, 1f);

        private ISubscriber<SignalStrengthChangedMessage> _signalSub;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;
        private int _currentBars;

        public SignalBarsController() { }

        void IStartable.Start()
        {
            _signalSub = GlobalMessagePipe.GetSubscriber<SignalStrengthChangedMessage>();
            _subscriptions.Add(_signalSub.Subscribe(OnSignalChanged));

            for (int i = 0; i < _bars.Length; i++)
                _bars[i].color = new Color(_barColor.r, _barColor.g, _barColor.b, 0f);
        }

        private void OnSignalChanged(SignalStrengthChangedMessage msg)
        {
            if (_disposed) return;

            int newBars = Mathf.Clamp(msg.Bars, 0, _bars.Length);
            if (newBars == _currentBars) return;

            bool wasZero = _currentBars == 0;
            _currentBars = newBars;

            for (int i = 0; i < _bars.Length; i++)
            {
                float targetAlpha = i < _currentBars ? 1f : 0f;
                _bars[i].color = new Color(_barColor.r, _barColor.g, _barColor.b, targetAlpha);
            }

            // Flicker boundary bar (skip at full signal)
            if (_currentBars < _bars.Length)
            {
                int changedIndex = Mathf.Max(_currentBars - 1, 0);
                FlickerBar(changedIndex);
            }

            // 0→1: re-trigger typewriter
            if (wasZero && _currentBars == 1)
                GlobalMessagePipe.GetPublisher<GuidanceRetriggerMessage>()
                    .Publish(new GuidanceRetriggerMessage());
        }

        private void FlickerBar(int index)
        {
            var bar = _bars[index];
            float targetAlpha = index < _currentBars ? 1f : 0f;

            LeanTween.value(bar.gameObject, (float v) =>
            {
                bar.color = new Color(_barColor.r, _barColor.g, _barColor.b, v);
            }, 0f, targetAlpha, 0.05f)
                .setEase(LeanTweenType.linear);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
    }
}   