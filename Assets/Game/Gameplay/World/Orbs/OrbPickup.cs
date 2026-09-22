// Trigger on orb: publishes OrbPickedUp on player overlap, destroys itself.
using UnityEngine;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using VContainer.Unity;
using System;

namespace Game.Gameplay.World
{
    [RequireComponent(typeof(Collider2D))]
    public class OrbPickup : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private OrbType _type = OrbType.Humanoid;
        [SerializeField] private LayerMask _playerLayer;

        private IPublisher<OrbPickedUp> _publisher;
        private bool _collected;
        private bool _disposed;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void IStartable.Start()
        {
            _publisher = GlobalMessagePipe.GetPublisher<OrbPickedUp>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || _disposed) return;
            if ((_playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

            _collected = true;
            _publisher.Publish(new OrbPickedUp(_type));
            Destroy(gameObject);
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
    }
}   