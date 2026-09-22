using System.Collections.Generic;
using UnityEngine;
using VContainer;
using MessagePipe;
using Game.Core.Enums;
using Game.Core.Messages;
using VContainer.Unity;
using System;

namespace Game.Gameplay.Camera
{
    public class SceneCameraSetup : MonoBehaviour, IInitializable, IDisposable
    {
        [SerializeField] private Transform _startSpawnPoint;
        [SerializeField] private CameraFollowObject _cameraFollowObject;
        [SerializeField] private Collider2D _boundaryCollider;
        [SerializeField] private CameraMode _initialMode = CameraMode.CenterFollow;
        [SerializeField] private Transform _lockedRoomAnchor;

        private readonly ISubscriber<CameraSystemReady> _cameraReadySub;
        private readonly ISubscriber<PlayerSpawned> _playerSpawnedSub;
        private readonly IPublisher<CameraSwitchRequest> _switchPublisher;
        private readonly IPublisher<CameraTargetUpdate> _targetPublisher;

        private readonly List<IDisposable> _disposables = new List<IDisposable>(2);
        private bool _isInitialized;

        [Inject]
        public SceneCameraSetup(
            ISubscriber<CameraSystemReady> cameraReadySub,
            ISubscriber<PlayerSpawned> playerSpawnedSub,
            IPublisher<CameraSwitchRequest> switchPublisher,
            IPublisher<CameraTargetUpdate> targetPublisher)
        {
            _cameraReadySub = cameraReadySub;
            _playerSpawnedSub = playerSpawnedSub;
            _switchPublisher = switchPublisher;
            _targetPublisher = targetPublisher;
        }

        public void Initialize()
        {
            if (_startSpawnPoint == null || _boundaryCollider == null || _cameraFollowObject == null)
            {
                Debug.LogError($"[Camera] Missing config in scene '{gameObject.scene.name}'. Check Inspector.");
                return;
            }

            _disposables.Add(_cameraReadySub.Subscribe(OnCameraReady));
            _disposables.Add(_playerSpawnedSub.Subscribe(OnPlayerSpawned));

            _cameraFollowObject.transform.SetPositionAndRotation(
                _startSpawnPoint.position, _startSpawnPoint.rotation);

            FireTargetUpdate(_cameraFollowObject.transform, _boundaryCollider);
            _switchPublisher.Publish(new CameraSwitchRequest(_initialMode, Vector2.zero));

            _isInitialized = true;
        }

        public void Dispose()
        {
            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();
        }

        private void OnCameraReady(CameraSystemReady _)
        {
            if (!_isInitialized) return;

            FireTargetUpdate(_cameraFollowObject.transform, _boundaryCollider);
            _switchPublisher.Publish(new CameraSwitchRequest(_initialMode, Vector2.zero));
        }

        private void OnPlayerSpawned(PlayerSpawned msg)
        {
            if (msg.Player == null) return;

            //_cameraFollowObject.SetFollowTarget(msg.Player.transform); CameraFollowObject owns it (delete the call from SceneCameraSetup):
            FireTargetUpdate(_cameraFollowObject.transform, _boundaryCollider);
        }

        private void FireTargetUpdate(Transform follow, Collider2D boundary)
        {
            _targetPublisher.Publish(new CameraTargetUpdate(follow, boundary));
        }
    }
}   