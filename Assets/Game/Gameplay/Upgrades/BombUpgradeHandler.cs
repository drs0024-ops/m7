using System;
using System.Collections.Generic;
using Game.Core.Messages;
using Game.Gameplay.Upgrades;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Handles bomb upgrade state: tracks unlock, remaining bombs, and cooldown.
    /// Publishes BombPlaced when the player places a bomb.
    /// </summary>
    public class BombUpgradeHandler : MonoBehaviour, IDisposable
    {
        #region Bomb Config

        [Header("Bomb Config")]
        [SerializeField] private int _maxBombs = 3;
        [SerializeField] private float _cooldown = 2f;

        #endregion

        #region Dependencies

        [Inject]
        private ISubscriber<UpgradeStateChanged> _upgradeStateChangedSubscriber;
        [Inject]
        private IPublisher<BombPlaced> _bombPlacedPublisher;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new(1);
        private bool _isUnlocked;
        private int _bombsRemaining;
        private float _cooldownTimer;
        private bool _disposed;

        #endregion

        #region Public API

        public bool CanPlaceBomb => _isUnlocked && _bombsRemaining > 0 && _cooldownTimer <= 0f;
        public int BombsRemaining => _bombsRemaining;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

        public void TryPlaceBomb(Vector2 position)
        {
            if (!CanPlaceBomb) return;

            _bombsRemaining--;
            _cooldownTimer = _cooldown;
            _bombPlacedPublisher.Publish(new BombPlaced(position));
        }

        public void RefillBombs()
        {
            _bombsRemaining = _maxBombs;
            _cooldownTimer = 0f;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }

        #endregion

        #region MonoBehaviour

        private void Start()
        {
            _subscriptions.Add(_upgradeStateChangedSubscriber.Subscribe(OnUpgradeStateChanged));
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Message Handlers

        private void OnUpgradeStateChanged(UpgradeStateChanged message)
        {
            if (message.UpgradeID != UpgradeIDs.Bomb) return;

            if (message.IsActive && !_isUnlocked)
            {
                _isUnlocked = true;
                _bombsRemaining = _maxBombs;
            }
            else if (!message.IsActive)
            {
                _isUnlocked = false;
                _bombsRemaining = 0;
                _cooldownTimer = 0f;
            }
        }

        #endregion
    }
}   