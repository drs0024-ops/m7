// Objective pulse: fires code fragment on Climax, re-triggers guidance on signal recovery.
using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Micro-confirmation (optional, for your objective system)
/// When a minor objective completes, in whatever class handles it:
/// Flash bars to 4 for one frame, then back:
///_signalPublisher.Publish(new SignalStrengthChangedMessage(4));
// Next frame (or next tick), publish the correct value:
///_signalPublisher.Publish(new SignalStrengthChangedMessage(correctBars));
/// </summary>

namespace Game.UI
{
    public class ObjectivePulseController : MonoBehaviour, IStartable, IDisposable
    {
        private static readonly string[] ClimaxFragments =
        {
            "SIGNAL LOCKED",
            "TERMINAL APPROACH",
            "PATTERN RECOGNIZED",
            "ORIGIN CONFIRMED"
        };

        [SerializeField] private int _fragmentIndex;

        private ISubscriber<GamePhaseChangedMessage> _phaseSub;
        private ISubscriber<SignalStrengthChangedMessage> _signalSub;
        private ISubscriber<GuidanceReceivedMessage> _guidanceSub;
        private IPublisher<CodeFragmentDisplayedMessage> _codePublisher;
        private IPublisher<GuidanceReceivedMessage> _guidancePublisher;

        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;
        private int _lastBars;
        private string _lastGuidanceText;

        public ObjectivePulseController() { }

        void IStartable.Start()
        {
            _phaseSub = GlobalMessagePipe.GetSubscriber<GamePhaseChangedMessage>();
            _signalSub = GlobalMessagePipe.GetSubscriber<SignalStrengthChangedMessage>();
            _guidanceSub = GlobalMessagePipe.GetSubscriber<GuidanceReceivedMessage>();
            _codePublisher = GlobalMessagePipe.GetPublisher<CodeFragmentDisplayedMessage>();
            _guidancePublisher = GlobalMessagePipe.GetPublisher<GuidanceReceivedMessage>();

            _subscriptions.Add(_phaseSub.Subscribe(OnPhaseChanged));
            _subscriptions.Add(_signalSub.Subscribe(OnSignalChanged));
            _subscriptions.Add(_guidanceSub.Subscribe(OnGuidanceReceived));
        }

        private void OnPhaseChanged(GamePhaseChangedMessage msg)
        {
            if (_disposed) return;

            if ((msg.NewPhase & GamePhase.Climax) != 0)
            {
                string fragment = ClimaxFragments[_fragmentIndex % ClimaxFragments.Length];
                _fragmentIndex++;
                _codePublisher.Publish(new CodeFragmentDisplayedMessage(fragment));
            }
        }

        private void OnSignalChanged(SignalStrengthChangedMessage msg)
        {
            if (_disposed) return;

            if (_lastBars == 0 && msg.Bars == 1 && !string.IsNullOrEmpty(_lastGuidanceText))
                _guidancePublisher.Publish(new GuidanceReceivedMessage(_lastGuidanceText, 5f));

            _lastBars = msg.Bars;
        }

        private void OnGuidanceReceived(GuidanceReceivedMessage msg)
        {
            if (_disposed) return;
            _lastGuidanceText = msg.Text;
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