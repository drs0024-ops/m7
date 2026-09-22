using System;
using System.Collections.Generic;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay.World
{
    public abstract class TriggerInteractionBase : MonoBehaviour, IInteractable, IDisposable
    {
        private ISubscriber<PlayerSpawned> _playerSpawnedSubscriber;
        private readonly List<IDisposable> _disposables = new();

        private Transform _player;

        public Transform Player => _player;
        public bool CanInteract { get; protected set; }
        public bool PlayerEnteredFromRight { get; protected set; }

        public TriggerInteractionBase() {}

        private void Start()
        {
            _playerSpawnedSubscriber = GlobalMessagePipe.GetSubscriber<PlayerSpawned>();
            _disposables.Add(_playerSpawnedSubscriber.Subscribe(OnPlayerSpawned));
        }

        private void OnPlayerSpawned(PlayerSpawned message)
        {
            _player = message.Player;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_player == null || collision.transform != _player) return;

            Vector3 direction = transform.position - _player.position;
            PlayerEnteredFromRight = direction.x <= 0f;
            CanInteract = true;

            Interact();
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (_player == null || collision.transform != _player) return;
            CanInteract = false;
        }

        public abstract void Interact();

        public virtual void Dispose()
        {
            foreach (var d in _disposables)
                d?.Dispose();
            _disposables.Clear();
        }

        private void OnDestroy()
        {
            if (_disposables.Count != 0)
            Dispose();
        }
    }
}   