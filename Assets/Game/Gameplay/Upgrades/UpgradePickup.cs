using System;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Upgrades
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class UpgradePickup : MonoBehaviour, IDisposable
    {
        [SerializeField] private PlayerUpgrade _upgrade;

        private IPublisher<UpgradePickedUp> _upgradePublisher;
        private bool _disposed;

        [Inject]
        private void Construct(IPublisher<UpgradePickedUp> upgradePublisher)
        {
            _upgradePublisher = upgradePublisher;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || _upgrade == null) return;

            _upgradePublisher.Publish(new UpgradePickedUp(_upgrade.UpgradeID));
            Destroy(gameObject);
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private void OnDestroy() => Dispose();
    }
}   