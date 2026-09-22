using UnityEngine;
using VContainer;
using Game.Core.Messages;
using MessagePipe;
using System;
using System.Collections.Generic;

namespace Game.Gameplay.Camera
{
    [DefaultExecutionOrder(100)]
    public class CameraFollowObject : MonoBehaviour, IDisposable
    {
        [SerializeField] private float _flipYRotationTime = 0.5f;

        private readonly ISubscriber<PlayerSpawned> _spawnSub;
        private readonly ISubscriber<PlayerFacingChanged> _facingSub;
        private readonly IPublisher<CameraSystemReady> _readyPublisher;
        private readonly List<IDisposable> _disposables = new List<IDisposable>(2);

        private Transform _playerTransform;
        private bool _isFollowing;
        private bool _isFacingRight = true;

        [Inject]
        public CameraFollowObject(
            ISubscriber<PlayerSpawned> spawnSub,
            ISubscriber<PlayerFacingChanged> facingSub,
            IPublisher<CameraSystemReady> readyPublisher)
        {
            _spawnSub = spawnSub;
            _facingSub = facingSub;
            _readyPublisher = readyPublisher;
        }

        private void Start()
        {
            _disposables.Add(_spawnSub.Subscribe(OnPlayerSpawned));
            _disposables.Add(_facingSub.Subscribe(OnPlayerFacingChanged));
            _readyPublisher.Publish(CameraSystemReady.Default);
        }

        public void SetFollowTarget(Transform target)
        {
            _playerTransform = target;
            _isFollowing = true;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();
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

        public void CallTurn()
        {
            if (!_isFollowing) return;
            FlipTo(!_isFacingRight);
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
    }
}   