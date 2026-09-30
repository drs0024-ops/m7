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
    /// <summary>
    /// Base class for all interactive NPCs. Handles proximity detection,
    /// interact input, and player reference tracking.
    /// </summary>
    public abstract class NPC : MonoBehaviour, IInteractable, IStartable, IDisposable
    {
        #region Dependencies

        [SerializeField] private SpriteRenderer _interactSprite;
        [SerializeField] private float _interactDistance = 5f;

        [Inject] private InputManager _inputManager;

        #endregion

        #region State

        private Transform _playerTransform;
        private bool _canInteract;
        private bool _isDisposed;

        private ISubscriber<PlayerSpawned> _playerSpawnedSubscriber;
        private readonly List<IDisposable> _subscriptions = new();

        #endregion

        #region Public API

        public Transform Player => _playerTransform;
        public bool CanInteract => _canInteract;
        public bool PlayerEnteredFromRight { get; protected set; }

        public NPC() { }

        void IStartable.Start()
        {
            _playerSpawnedSubscriber = GlobalMessagePipe.GetSubscriber<PlayerSpawned>();
            _subscriptions.Add(_playerSpawnedSubscriber.Subscribe(msg => _playerTransform = msg.Player));
        }

        public abstract void Interact();

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            foreach (var d in _subscriptions)
                d?.Dispose();
            _subscriptions.Clear();
        }

        #endregion

        #region Internal

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

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion
    }
}   