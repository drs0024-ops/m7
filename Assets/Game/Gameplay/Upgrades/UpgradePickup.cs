using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Upgrades
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class UpgradePickup : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private PlayerUpgrade _upgrade;

        private IPublisher<UpgradePickedUp> _upgradePublisher;
        private bool _disposed;
        private readonly List<IDisposable> _subscriptions = new();

        public UpgradePickup() {}

        public void Start()
        {
            _upgradePublisher = GlobalMessagePipe.GetPublisher<UpgradePickedUp>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || _upgrade == null) return;

            _upgradePublisher.Publish(new UpgradePickedUp(_upgrade.UpgradeID));
            Destroy(gameObject);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var d in _subscriptions)
                d?.Dispose();
            _subscriptions.Clear();
        }

        public void OnDestroy() => Dispose();
    }
}   

