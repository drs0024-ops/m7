using System;
using System.Collections.Generic;
using UnityEngine;
using MessagePipe;
using Game.Core.Enums;
using Game.Core.Messages;
using VContainer;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Orchestrates initial camera setup for the scene: positions the follow object,
    /// fires the initial target update (including locked room anchor), and switches
    /// to the configured initial camera mode.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SceneCameraSetup : MonoBehaviour, IDisposable
    {
        #region Dependencies

        [SerializeField] private Transform _startSpawnPoint;
        [SerializeField] private CameraFollowObject _cameraFollowObject;
        [SerializeField] private Collider2D _boundaryCollider;
        [SerializeField] private CameraMode _initialMode = CameraMode.CenterFollow;
        [SerializeField] private Transform _lockedRoomAnchor;

        private ISubscriber<CameraSystemReady> _cameraReadySub;
        private ISubscriber<PlayerSpawned> _playerSpawnedSub;
        private IPublisher<CameraSwitchRequest> _switchPublisher;
        private IPublisher<CameraTargetUpdate> _targetPublisher;

        #endregion

        #region State

        private readonly List<IDisposable> _disposables = new List<IDisposable>(2);
        private bool _isInitialized;
        private bool _isDisposed;

        #endregion

        #region Public API

        [Inject]
        private void Construct(
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
                throw new InvalidOperationException(
                    $"[SceneCameraSetup] Missing required reference in scene '{gameObject.scene.name}'. Check Inspector.");

            _disposables.Add(_cameraReadySub.Subscribe(OnCameraReady));
            _disposables.Add(_playerSpawnedSub.Subscribe(OnPlayerSpawned));

            _cameraFollowObject.transform.SetPositionAndRotation(
                _startSpawnPoint.position, _startSpawnPoint.rotation);

            FireTargetUpdate(_cameraFollowObject.transform, _boundaryCollider);
            _switchPublisher.Publish(new CameraSwitchRequest(_initialMode));

            _isInitialized = true;
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

        #region Message Handlers

        private void OnCameraReady(CameraSystemReady _)
        {
            if (!_isInitialized) return;

            FireTargetUpdate(_cameraFollowObject.transform, _boundaryCollider);
            _switchPublisher.Publish(new CameraSwitchRequest(_initialMode));
        }

        private void OnPlayerSpawned(PlayerSpawned msg)
        {
            if (msg.Player == null) return;

            FireTargetUpdate(_cameraFollowObject.transform, _boundaryCollider);
        }

        #endregion

        #region Internal

        private void FireTargetUpdate(Transform follow, Collider2D boundary)
        {
            _targetPublisher.Publish(new CameraTargetUpdate(follow, boundary, _lockedRoomAnchor));
        }

        #endregion
    }
}   