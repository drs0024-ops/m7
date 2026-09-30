using System;
using System.Collections.Generic;
using UnityEngine;
using MessagePipe;
using Game.Core.Messages;
using VContainer;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Mirrors the player's position and handles Y-flip for camera facing.
    /// Serves as the Follow target for all follow-camera rigs.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraFollowObject : MonoBehaviour, IDisposable
    {
        #region Dependencies

        [SerializeField] private float _flipYRotationTime = 0.5f;

        private ISubscriber<PlayerSpawned> _spawnSub;
        private ISubscriber<PlayerFacingChanged> _facingSub;
        private IPublisher<CameraSystemReady> _readyPublisher;

        #endregion

        #region State

        private readonly List<IDisposable> _disposables = new List<IDisposable>(2);

        private Transform _playerTransform;
        private bool _isFollowing;
        private bool _isFacingRight = true;
        private bool _isDisposed;

        #endregion

        #region Public API

        [Inject]
        private void Construct(
            ISubscriber<PlayerSpawned> spawnSub,
            ISubscriber<PlayerFacingChanged> facingSub,
            IPublisher<CameraSystemReady> readyPublisher)
        {
            _spawnSub = spawnSub;
            _facingSub = facingSub;
            _readyPublisher = readyPublisher;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();
        }

        #endregion

        #region Internal

        private void Start()
        {
            _disposables.Add(_spawnSub.Subscribe(OnPlayerSpawned));
            _disposables.Add(_facingSub.Subscribe(OnPlayerFacingChanged));
            _readyPublisher.Publish(CameraSystemReady.Default);
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnPlayerSpawned(PlayerSpawned msg)
        {
            if (msg.Player == null) return;

            _playerTransform = msg.Player.transform;
            _isFollowing = true;
            transform.localRotation = Quaternion.identity;
            _isFacingRight = true;
        }

        private void OnPlayerFacingChanged(PlayerFacingChanged msg)
        {
            FlipTo(msg.IsFacingRight);
        }

        private void FlipTo(bool facingRight)
        {
            _isFacingRight = facingRight;
            float targetY = facingRight ? 0f : 180f;

            LeanTween.cancel(gameObject);
            LeanTween.rotateY(gameObject, targetY, _flipYRotationTime)
                .setEaseInOutSine();
        }

        private void Update()
        {
            if (!_isFollowing) return;

            if (_playerTransform == null)
            {
                _isFollowing = false;
                return;
            }

            transform.position = new Vector3(
                _playerTransform.position.x,
                _playerTransform.position.y,
                transform.position.z);
        }

        #endregion
    }
}   