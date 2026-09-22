using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Upgrades
{
    public class InvisibilityInputHandler : MonoBehaviour, IStartable, IDisposable
    {
        private IPublisher<InvisibilityToggleRequested> _togglePublisher;
        private ISubscriber<UpgradeStateChanged> _upgradeStateChangedSubscriber;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _isUpgradeUnlocked;
        private bool _isDisposed;

        [Inject]
        public InvisibilityInputHandler()
        {
        }

        void IStartable.Start()
        {
            _togglePublisher = GlobalMessagePipe.GetPublisher<InvisibilityToggleRequested>();
            _upgradeStateChangedSubscriber = GlobalMessagePipe.GetSubscriber<UpgradeStateChanged>();
            _subscriptions.Add(_upgradeStateChangedSubscriber.Subscribe(OnUpgradeStateChange));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
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