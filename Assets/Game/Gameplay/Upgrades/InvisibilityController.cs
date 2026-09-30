using System;
using VContainer;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay.Upgrades
{
    /// <summary>
    /// MonoBehaviour wrapper around InvisibilityModel.
    /// Exists so VContainer can register it in the scene scope.
    /// All logic lives in InvisibilityModel (testable).
    /// </summary>
    public class InvisibilityController : MonoBehaviour, IDisposable
    {
        private InvisibilityModel _model;

        public bool IsActive => _model?.IsActive ?? false;

        [Inject]
        private void Inject(
            IPublisher<InvisibilityStateChanged> statePublisher,
            ISubscriber<InvisibilityToggleRequested> toggleSub,
            ISubscriber<UpgradeExpired> expiredSub,
            ISubscriber<UpgradeStateChanged> stateChangedSub)
        {
            _model = new InvisibilityModel(statePublisher, toggleSub, expiredSub, stateChangedSub);
        }

        public void ActivateForDuration(float seconds) => _model?.ActivateForDuration(seconds);
        public void Deactivate() => _model?.Deactivate();

        public void Dispose() => _model?.Dispose();

        private void OnDestroy()
        {
            if (_model != null) _model.Dispose();
        }
    }
}   