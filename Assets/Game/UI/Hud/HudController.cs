using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    public class HudController : MonoBehaviour, IStartable, IDisposable
    {
        [Header("HudManager")]
        [SerializeField] private HudManager _hudManager;

        [Header("Child Controllers")]
        [SerializeField] private GuidanceTextController _guidance;
        [SerializeField] private SignalBarsController _signalBars; 
        [SerializeField] private OrbCountController _orbCount;

        [Header("Panel Indices")]
        [SerializeField] private int _guidancePanelIndex = 0;
        [SerializeField] private int _signalBarsPanelIndex = 1;
        [SerializeField] private int _orbCountPanelIndex = 2;

        [Inject] private HudState _hudState;

        private ISubscriber<GamePhaseChangedMessage> _phaseSub;
        private ISubscriber<HudUnfrozenMessage> _unfrozenSub;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;
        
        [NonSerialized] private long _currentPhaseRaw;
        private GamePhase _currentPhase => (GamePhase)_currentPhaseRaw;

        public HudController() { }

        void IStartable.Start()
        {
            _phaseSub = GlobalMessagePipe.GetSubscriber<GamePhaseChangedMessage>();
            _subscriptions.Add(_phaseSub.Subscribe(OnPhaseChanged));

            _unfrozenSub = GlobalMessagePipe.GetSubscriber<HudUnfrozenMessage>();
            _subscriptions.Add(_unfrozenSub.Subscribe(_ => RefreshPanels()));

            HideAllPanels();
        }

        private void OnPhaseChanged(GamePhaseChangedMessage msg)
        {
            if (_disposed) return;

            _currentPhaseRaw = (long)msg.NewPhase;

            if ((msg.NewPhase & GamePhase.Climax) != 0)
                GlobalMessagePipe.GetPublisher<CodeFragmentDisplayedMessage>()
                    .Publish(new CodeFragmentDisplayedMessage(""));

            if (IsHudVisible(msg.NewPhase))
                ShowAllPanels();
            else
                HideAllPanels();
        }

        private static bool IsHudVisible(GamePhase phase)
        {
            return (phase & GamePhase.Gameplay) != 0
                || (phase & GamePhase.Climax) != 0;
        }

        private void ShowAllPanels()
        {
            if (_hudState.IsFrozen) return;
            _hudManager.ShowPanels(new[]
            {
                _guidancePanelIndex,
                _signalBarsPanelIndex,
                _orbCountPanelIndex
            });
        }

        private void HideAllPanels()
        {
            _hudManager.HidePanels(new[]
            {
                _guidancePanelIndex,
                _signalBarsPanelIndex,
                _orbCountPanelIndex
            });
        }

        public void RefreshPanels()
        {
            if (_disposed) return;
            if (IsHudVisible(_currentPhase))
                ShowAllPanels();
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