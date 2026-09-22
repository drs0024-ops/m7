using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    public class DestructibleTriggerHandler : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private LayerMask _whoCanHit;

        private Destructible _destructible;
        private Transform _playerTransform;
        private bool _isTweening;

        private readonly ISubscriber<PlayerSpawned> _playerSpawnedSubscriber;
        private readonly List<IDisposable> _subscriptions = new();

        public DestructibleTriggerHandler()
        {
            _playerSpawnedSubscriber = GlobalMessagePipe.GetSubscriber<PlayerSpawned>();
        }

        private void Awake()
        {
            _destructible = GetComponent<Destructible>();
        }

        void IStartable.Start()
        {
            _subscriptions.Add(_playerSpawnedSubscriber.Subscribe(msg => _playerTransform = msg.Player));
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if ((collision.gameObject.layer & _whoCanHit) == 0) return;
            if (_isTweening || _destructible.IsDead) return;

            _isTweening = true;
            _destructible.TweenHit();

            Invoke(nameof(ResetTweening), 0.2f);
        }

        private void ResetTweening()
        {
            _isTweening = false;
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