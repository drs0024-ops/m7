using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class CollectableTriggerHandler : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private LayerMask _whoCanCollect;

        [Header("Magnet")]
        [SerializeField] private bool _magnetEnabled;
        [SerializeField] private float _magnetDistance = 3f;
        [SerializeField] private float _magnetSpeed = 5f;

        private Collectable _collectable;
        private Transform _playerTransform;
        private Coroutine _magnetCoroutine;

        private ISubscriber<PlayerSpawned> _playerSpawnedSubscriber;
        private readonly List<IDisposable> _subscriptions = new();

        public CollectableTriggerHandler()
        {
        }

        private void Awake()
        {
            _collectable = GetComponent<Collectable>();
        }

        void IStartable.Start()
        {
            _playerSpawnedSubscriber = GlobalMessagePipe.GetSubscriber<PlayerSpawned>();
            _subscriptions.Add(_playerSpawnedSubscriber.Subscribe(msg => _playerTransform = msg.Player));
        }

        private void Update()
        {
            if (!_magnetEnabled || _playerTransform == null) return;

            if (Vector2.Distance(_playerTransform.position, transform.position) < _magnetDistance)
            {
                if (_magnetCoroutine == null)
                    _magnetCoroutine = StartCoroutine(MoveToPlayer());
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if ((collision.gameObject.layer & _whoCanCollect) == 0) return;

            _collectable.Collect(collision.gameObject);
            DestroySelf();
        }

        private IEnumerator MoveToPlayer()
        {
            while (_playerTransform != null &&
                   Vector2.Distance(transform.position, _playerTransform.position) > 0.1f)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position, _playerTransform.position, _magnetSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private void DestroySelf()
        {
            if (transform.parent != null)
            {
                if (GetComponentInParent<CollumCollectible>() != null)
                    Destroy(gameObject);
                else
                    Destroy(transform.parent.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
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