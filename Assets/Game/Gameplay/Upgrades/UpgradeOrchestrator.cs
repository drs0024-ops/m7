using System;
using System.Collections.Generic;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Upgrades
{
    /// <summary>
    /// Orchestrates upgrade pickup: validates, unlocks, applies timed effects,
    /// and publishes downstream messages for UI, save, and player systems.
    /// </summary>
    public class UpgradeOrchestrator : IUpgradeManager, IStartable, IDisposable
    {
        #region Dependencies

        private readonly ISubscriber<UpgradePickedUp> _pickedUpSub;
        private readonly IPublisher<UpgradeActivated> _activatedPublisher;
        private readonly IPublisher<UpgradeUnlocked> _unlockedPublisher;
        private readonly IPublisher<UpgradeStateChanged> _stateChangedPublisher;
        private readonly IPublisher<UpgradeApplied> _appliedPublisher;
        private readonly IPublisher<CodeFragmentDisplayedMessage> _codePublisher;
        private readonly IPublisher<SaveRequest> _saveRequestPublisher;
        private readonly UpgradeStateManager _stateManager;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new(1);
        private bool _disposed;

        #endregion

        #region Construction

        public UpgradeOrchestrator(
            UpgradeStateManager stateManager,
            ISubscriber<UpgradePickedUp> pickedUpSub,
            IPublisher<UpgradeActivated> activatedPublisher,
            IPublisher<UpgradeUnlocked> unlockedPublisher,
            IPublisher<UpgradeStateChanged> stateChangedPublisher,
            IPublisher<UpgradeApplied> appliedPublisher,
            IPublisher<CodeFragmentDisplayedMessage> codePublisher,
            IPublisher<SaveRequest> saveRequestPublisher)
        {
            _stateManager = stateManager;
            _pickedUpSub = pickedUpSub;
            _activatedPublisher = activatedPublisher;
            _unlockedPublisher = unlockedPublisher;
            _stateChangedPublisher = stateChangedPublisher;
            _appliedPublisher = appliedPublisher;
            _codePublisher = codePublisher;
            _saveRequestPublisher = saveRequestPublisher;
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            _subscriptions.Add(_pickedUpSub.Subscribe(OnUpgradePickedUp));
        }

        #endregion

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

            NotifyUpgradeApplied(upgrade);

            _activatedPublisher.Publish(new UpgradeActivated(upgradeId));
            _codePublisher.Publish(new CodeFragmentDisplayedMessage(upgrade.CodeFragmentText));
            _stateChangedPublisher.Publish(new UpgradeStateChanged(upgradeId, true, upgrade.Duration));
            _saveRequestPublisher.Publish(new SaveRequest("UpgradeOrchestrator", isNew));
        }

        private void NotifyUpgradeApplied(PlayerUpgrade upgrade)
        {
            if (_stateManager.CurrentPlayer == null) return;

            _appliedPublisher.Publish(new UpgradeApplied(
                upgrade.UpgradeID, upgrade.Duration));
        }

        #endregion

        #region Cleanup

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