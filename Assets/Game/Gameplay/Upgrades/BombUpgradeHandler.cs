using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
/// <summary>
/// Handles bomb placement for the player. Enforces a maximum bomb count,
/// a cooldown between placements, and a one-bomb-at-a-time rule.
/// Exposes <see cref="BombsRemaining"/> and <see cref="CooldownRemaining"/>
/// for optional HUD display. Fires <see cref="BombPlaced"/> when a bomb is
/// successfully placed so a Bomb component can spawn the prefab, run the
/// explosion timer, and apply damage via EntityDamaged.
/// </summary>
namespace Game.Gameplay.Upgrades
{
    public class BombUpgradeHandler : MonoBehaviour, IStartable, ITickable, IDisposable
    {
        [Header("Bomb Config")]
        [SerializeField] private int _maxBombs = 3;
        [SerializeField] private float _cooldown = 2f;

        private readonly ISubscriber<UpgradeStateChanged> _upgradeStateChangedSubscriber;
        private readonly IPublisher<BombPlaced> _bombPlacedPublisher;
        private readonly List<IDisposable> _subscriptions = new();

        private bool _isUnlocked;
        private int _bombsRemaining;
        private float _cooldownTimer;

        public bool CanPlaceBomb => _isUnlocked && _bombsRemaining > 0 && _cooldownTimer <= 0f;
        public int BombsRemaining => _bombsRemaining;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

        [Inject]
        public BombUpgradeHandler()
        {
            _upgradeStateChangedSubscriber = GlobalMessagePipe.GetSubscriber<UpgradeStateChanged>();
            _bombPlacedPublisher = GlobalMessagePipe.GetPublisher<BombPlaced>();
        }

        void IStartable.Start()
        {
            _subscriptions.Add(_upgradeStateChangedSubscriber.Subscribe(OnUpgradeStateChanged));
        }

        public void Tick()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

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
            foreach (var d in _subscriptions)
                d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (_subscriptions.Count != 0)
                Dispose();
        }
    }
}