using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using VContainer.Unity;

namespace Game.Gameplay.Upgrades
{
    /// <summary>
    /// Pure state machine for invisibility. No Unity dependencies.
    /// Testable in plain NUnit.
    /// </summary>
    public class InvisibilityModel : ITickable, IDisposable
    {
        private readonly IPublisher<InvisibilityStateChanged> _statePublisher;
        private readonly ISubscriber<InvisibilityToggleRequested> _toggleSub;
        private readonly ISubscriber<UpgradeExpired> _expiredSub;
        private readonly ISubscriber<UpgradeStateChanged> _stateChangedSub;

        private readonly List<IDisposable> _subscriptions = new();

        private bool _isActive;
        private bool _isTimedEffect;
        private float _callerTimer;
        private bool _callerActive;
        private bool _disposed;

        public bool IsActive => _isActive;
        public bool IsCallerActive => _callerActive;

        public InvisibilityModel(
            IPublisher<InvisibilityStateChanged> statePublisher,
            ISubscriber<InvisibilityToggleRequested> toggleSub,
            ISubscriber<UpgradeExpired> expiredSub,
            ISubscriber<UpgradeStateChanged> stateChangedSub)
        {
            _statePublisher = statePublisher;
            _toggleSub = toggleSub;
            _expiredSub = expiredSub;
            _stateChangedSub = stateChangedSub;

            _subscriptions.Add(_toggleSub.Subscribe(OnToggleRequested));
            _subscriptions.Add(_expiredSub.Subscribe(OnUpgradeExpired));
            _subscriptions.Add(_stateChangedSub.Subscribe(OnUpgradeStateChanged));
        }

        #region Public API (caller objects)

        public void ActivateForDuration(float seconds)
        {
            if (_disposed || seconds <= 0f) return;
            _callerActive = true;
            _callerTimer = seconds;
            SetActive(true);
        }

        public void Deactivate()
        {
            if (_disposed) return;
            _callerActive = false;
            _callerTimer = 0f;
            SetActive(false);
        }

        #endregion

        #region ITickable

        void ITickable.Tick()
        {
            if (_disposed) return;

            if (_callerActive)
            {
                _callerTimer -= UnityEngine.Time.deltaTime;
                if (_callerTimer <= 0f)
                {
                    _callerActive = false;
                    _callerTimer = 0f;
                    SetActive(false);
                }
            }
        }

        #endregion

        #region Handlers

        private void OnToggleRequested(InvisibilityToggleRequested _)
        {
            if (_disposed) return;
            if (_callerActive) return;

            _isActive = !_isActive;
            _statePublisher.Publish(new InvisibilityStateChanged(
                _isActive ? 1f : 0f, _isTimedEffect));
        }

        private void OnUpgradeExpired(UpgradeExpired msg)
        {
            if (_disposed) return;
            if (msg.UpgradeId != UpgradeIDs.Invisibility) return;

            // FIX #2: Cancel caller timer when upgrade expires
            _callerActive = false;
            _callerTimer = 0f;

            if (_isActive)
            {
                _isActive = false;
                _isTimedEffect = false;
                _statePublisher.Publish(new InvisibilityStateChanged(0f, false));
            }
        }

        private void OnUpgradeStateChanged(UpgradeStateChanged msg)
        {
            if (msg.UpgradeID != UpgradeIDs.Invisibility) return;
            _isTimedEffect = msg.IsActive && msg.Duration > 0f;
        }

        #endregion

        #region Internal

        private void SetActive(bool active)
        {
            _isActive = active;
            _statePublisher.Publish(new InvisibilityStateChanged(
                active ? 1f : 0f, _isTimedEffect));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var s in _subscriptions) s?.Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}   