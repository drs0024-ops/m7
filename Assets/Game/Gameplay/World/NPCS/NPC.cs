using System;
using System.Collections.Generic;
using Game.Core.Interfaces;
using Game.Core.Messages;
using Game.Gameplay.Player;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    public abstract class NPC : MonoBehaviour, IInteractable, IStartable, IDisposable
    {
        [SerializeField] private SpriteRenderer _interactSprite;
        [SerializeField] private float _interactDistance = 5f;

        [Inject] private InputManager _inputManager;

        private Transform _playerTransform;
        private bool _canInteract;

        private readonly ISubscriber<PlayerSpawned> _playerSpawnedSubscriber;
        private readonly List<IDisposable> _subscriptions = new();

        public Transform Player => _playerTransform;
        public bool CanInteract => _canInteract;
        public bool PlayerEnteredFromRight { get; protected set; }

        public NPC()
        {
            _playerSpawnedSubscriber = GlobalMessagePipe.GetSubscriber<PlayerSpawned>();
        }

        void IStartable.Start()
        {
            _subscriptions.Add(_playerSpawnedSubscriber.Subscribe(msg => _playerTransform = msg.Player));
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            bool inRange = Vector2.Distance(_playerTransform.position, transform.position) < _interactDistance;
            _canInteract = inRange;

            if (_interactSprite != null)
                _interactSprite.gameObject.SetActive(inRange);

            if (_inputManager.InteractWasPressed && inRange)
                Interact();
        }

        public abstract void Interact();

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