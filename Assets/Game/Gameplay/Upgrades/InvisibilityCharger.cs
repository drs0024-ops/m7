using System;
using System.Collections.Generic;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Upgrades
{
    /// <summary>2D platform trigger that charges an unlocked player's invisibility.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class InvisibilityCharger : MonoBehaviour, IDisposable
    {
        [SerializeField, Min(0f)] private float _chargeRatePercentPerSecond = 10f;
        [SerializeField, Range(0f, 100f)] private float _maxChargePercent = 100f;
        [SerializeField, Min(0f)] private float _maxChargeTimeSeconds;
        [SerializeField, Min(0f)] private float _broadcastRadius = 15f;
        [SerializeField, Min(0f)] private float _crouchBroadcastMultiplier = 0.5f;

        private readonly HashSet<int> _playerColliderIds = new(4);
        private readonly List<IDisposable> _subscriptions = new(2);
        private IPublisher<InvisibilityChargingChanged> _chargingPublisher;
        private ISubscriber<UpgradeStateChanged> _upgradeStateSubscriber;
        private ISubscriber<UpgradeExpired> _upgradeExpiredSubscriber;
        private IInputState _input;
        private IUpgradeState _upgradeState;
        private bool _isInitialized;
        private bool _isDisposed;
        private bool _isUnlocked;
        private bool _isCharging;
        private bool _lastCrouching;

        /// <summary>Injects unlock, input, and MessagePipe dependencies.</summary>
        [Inject]
        private void Inject(
            IPublisher<InvisibilityChargingChanged> chargingPublisher,
            ISubscriber<UpgradeStateChanged> upgradeStateSubscriber,
            ISubscriber<UpgradeExpired> upgradeExpiredSubscriber,
            IInputState input,
            IUpgradeState upgradeState)
        {
            _chargingPublisher = chargingPublisher;
            _upgradeStateSubscriber = upgradeStateSubscriber;
            _upgradeExpiredSubscriber = upgradeExpiredSubscriber;
            _input = input;
            _upgradeState = upgradeState;
        }

        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
#if UNITY_EDITOR
            Debug.Assert(_chargeRatePercentPerSecond >= 0f, "Charger rate cannot be negative.", this);
            Debug.Assert(_maxChargePercent >= 0f && _maxChargePercent <= 100f,
                "Charger maximum must be between zero and one hundred.", this);
            Debug.Assert(_maxChargeTimeSeconds >= 0f, "Charger duration cannot be negative.", this);
            Debug.Assert(_broadcastRadius >= 0f, "Charger broadcast radius cannot be negative.", this);
            Debug.Assert(_crouchBroadcastMultiplier >= 0f,
                "Crouch broadcast multiplier cannot be negative.", this);
#endif
        }

        /// <summary>Subscribes to unlock and expiry messages after injection.</summary>
        public void Initialize()
        {
            if (_isInitialized || _isDisposed) return;
#if UNITY_EDITOR
            Debug.Assert(_chargingPublisher != null, "InvisibilityCharger requires a charging publisher.", this);
            Debug.Assert(_upgradeStateSubscriber != null && _upgradeExpiredSubscriber != null,
                "InvisibilityCharger requires upgrade-state subscribers.", this);
            Debug.Assert(_input != null && _upgradeState != null,
                "InvisibilityCharger requires input and upgrade state.", this);
            Debug.Assert(GetComponent<BoxCollider2D>().isTrigger,
                "InvisibilityCharger BoxCollider2D must be a trigger.", this);
#endif
            if (_chargingPublisher == null || _upgradeStateSubscriber == null
                || _upgradeExpiredSubscriber == null || _input == null || _upgradeState == null)
                return;

            _isUnlocked = _upgradeState.IsUnlocked(UpgradeIDs.Invisibility);
            _subscriptions.Add(_upgradeStateSubscriber.Subscribe(OnUpgradeStateChanged));
            _subscriptions.Add(_upgradeExpiredSubscriber.Subscribe(OnUpgradeExpired));
            _isInitialized = true;
        }

        private void Start() => Initialize();

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDisposed || !IsPlayer(other)) return;
            if (!_playerColliderIds.Add(other.GetInstanceID())) return;
            TryStartCharging();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other) || !_playerColliderIds.Remove(other.GetInstanceID())) return;
            if (_playerColliderIds.Count == 0)
                SetCharging(false);
        }

        private void Update()
        {
            if (!_isCharging || _input == null || _input.IsCrouching == _lastCrouching) return;
            _lastCrouching = _input.IsCrouching;
            PublishCharging(true);
        }

        private void TryStartCharging()
        {
            if (!_isInitialized || !_isUnlocked || _isCharging || _playerColliderIds.Count == 0) return;
            SetCharging(true);
        }

        private void SetCharging(bool isCharging)
        {
            if (_isCharging == isCharging) return;
            _isCharging = isCharging;
            _lastCrouching = _input.IsCrouching;
            PublishCharging(isCharging);
        }

        private void PublishCharging(bool isCharging)
        {
            if (_chargingPublisher == null) return;
            float radius = _broadcastRadius
                * (_input != null && _input.IsCrouching ? _crouchBroadcastMultiplier : 1f);

            _chargingPublisher.Publish(new InvisibilityChargingChanged(
                isCharging,
                gameObject.GetInstanceID(),
                _chargeRatePercentPerSecond,
                _maxChargePercent,
                _maxChargeTimeSeconds,
                transform.position,
                radius));
        }

        private void OnUpgradeStateChanged(UpgradeStateChanged message)
        {
            if (message.UpgradeID != UpgradeIDs.Invisibility) return;
            _isUnlocked = message.IsActive;
            if (_isUnlocked)
                TryStartCharging();
            else
                SetCharging(false);
        }

        private void OnUpgradeExpired(UpgradeExpired message)
        {
            if (message.UpgradeId != UpgradeIDs.Invisibility) return;
            _isUnlocked = false;
            SetCharging(false);
        }

        private static bool IsPlayer(Collider2D other) =>
            other != null && other.transform.root.CompareTag("Player");

        /// <summary>Stops charging and disposes all subscriptions.</summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            if (_isCharging) PublishCharging(false);
            _isDisposed = true;
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
            _playerColliderIds.Clear();
            _isCharging = false;
        }

        private void OnDisable()
        {
            StopChargingOnDisable();
            _playerColliderIds.Clear();
        }
        private void OnDestroy() => Dispose();

        private void StopChargingOnDisable()
        {
            if (_isCharging)
                SetCharging(false);
        }
    }
}
