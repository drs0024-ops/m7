using System;
using System.Collections.Generic;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Upgrades
{
    public class UpgradeOrchestrator : IUpgradeManager, IStartable, IDisposable
    {
        private ISubscriber<UpgradePickedUp> _pickedUpSub;
        private IPublisher<UpgradeActivated> _activatedPublisher;
        private IPublisher<UpgradeUnlocked> _unlockedPublisher;
        private IPublisher<UpgradeStateChanged> _stateChangedPublisher;
        private IPublisher<UpgradeApplied> _appliedPublisher;
        private IPublisher<CodeFragmentDisplayedMessage> _codePublisher;
        
        private IPublisher<SaveRequest> _saveRequestPublisher;

        private readonly UpgradeStateManager _stateManager;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        [Inject]
        public UpgradeOrchestrator(UpgradeStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        void IStartable.Start()
        {
            _pickedUpSub = GlobalMessagePipe.GetSubscriber<UpgradePickedUp>();
            _activatedPublisher = GlobalMessagePipe.GetPublisher<UpgradeActivated>();
            _stateChangedPublisher = GlobalMessagePipe.GetPublisher<UpgradeStateChanged>();
            _appliedPublisher = GlobalMessagePipe.GetPublisher<UpgradeApplied>();
            _codePublisher = GlobalMessagePipe.GetPublisher<CodeFragmentDisplayedMessage>();
            _saveRequestPublisher = GlobalMessagePipe.GetPublisher<SaveRequest>();

            _subscriptions.Add(_pickedUpSub.Subscribe(OnUpgradePickedUp));
        }

        #region IUpgradeManager

        public bool IsUpgradeActive(string upgradeId)
        {
            if (!_stateManager.HasUpgrade(upgradeId)) return false;
            var upgrade = _stateManager.GetUpgrade(upgradeId);
            if (upgrade == null) return false;
            return upgrade.Duration == 0f || _stateManager.IsEffectActive(upgradeId);
        }

        public void RequestUpgrade(string upgradeId) => UnlockUpgrade(upgradeId);

        public float GetRemainingTime(string upgradeId) => _stateManager.GetRemainingTime(upgradeId);

        #endregion

        #region Core Logic

        private void OnUpgradePickedUp(UpgradePickedUp message)
        {
            if (_disposed) return;
            UnlockUpgrade(message.UpgradeID);
        }

        private void UnlockUpgrade(string upgradeId)
        {
            if (_disposed) return;

            var upgrade = _stateManager.GetUpgrade(upgradeId);
            if (upgrade == null)
            {
                Debug.LogError($"[UpgradeOrchestrator] Unknown upgrade: {upgradeId}");
                return;
            }

            bool isNew = _stateManager.AddUpgrade(upgradeId);

            if (upgrade.Duration > 0f)
                _stateManager.StartTimedEffect(upgradeId, upgrade.Duration);

            ApplyUpgradeToPlayer(upgrade);

            _activatedPublisher.Publish(new UpgradeActivated(upgradeId));
            _codePublisher.Publish(new CodeFragmentDisplayedMessage(upgrade.CodeFragmentText));
            _stateChangedPublisher.Publish(new UpgradeStateChanged(upgradeId, true, upgrade.Duration));
            _saveRequestPublisher.Publish(new SaveRequest("UpgradeOrchestrator", isNew));
        }

        private void ApplyUpgradeToPlayer(PlayerUpgrade upgrade)
        {
            var player = _stateManager.CurrentPlayer;
            if (player == null) return;

            _appliedPublisher.Publish(new UpgradeApplied(
                upgrade.UpgradeID, upgrade.Duration));
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}   