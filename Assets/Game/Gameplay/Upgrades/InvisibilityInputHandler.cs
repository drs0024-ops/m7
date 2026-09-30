using System;
using System.Collections.Generic;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Upgrades
{
    public class InvisibilityInputHandler : MonoBehaviour, IDisposable
    {
        private IPublisher<InvisibilityToggleRequested> _togglePublisher;
        private ISubscriber<UpgradeStateChanged> _upgradeStateChangedSubscriber;
        private IInputState _input;

        private readonly List<IDisposable> _subscriptions = new();
        private bool _isUpgradeUnlocked;
        private bool _isDisposed;

        [Inject]
        private void Inject(
            IPublisher<InvisibilityToggleRequested> togglePublisher,
            ISubscriber<UpgradeStateChanged> upgradeStateChangedSubscriber,
            IInputState input)
        {
            _togglePublisher = togglePublisher;
            _upgradeStateChangedSubscriber = upgradeStateChangedSubscriber;
            _input = input;
        }

        private void Start()
        {
            _subscriptions.Add(_upgradeStateChangedSubscriber.Subscribe(OnUpgradeStateChange));
        }

        private void Update()
        {
            if (_input == null) return;
            if (_input.InvisibilityWasPressed)
                HandleInputPress();
        }

        private void HandleInputPress()
        {
            if (!_isUpgradeUnlocked) return;
            _togglePublisher.Publish(InvisibilityToggleRequested.Default);
        }

        private void OnUpgradeStateChange(UpgradeStateChanged message)
        {
            if (message.UpgradeID != UpgradeIDs.Invisibility) return;
            _isUpgradeUnlocked = message.IsActive;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (!_isDisposed) Dispose();
        }
    }
}   