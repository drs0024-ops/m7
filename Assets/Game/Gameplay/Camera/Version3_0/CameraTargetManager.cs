using System;
using UnityEngine;
using Unity.Cinemachine;
using Game.Core.Messages;
using MessagePipe;
using VContainer;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Manages virtual camera target switching:
    /// Spawn point (default) → Player (on spawn) → Checkpoint (on death/respawn).
    /// Prevents NaN transforms when no player exists yet.
    /// Compatible with Cinemachine 3.1.5 (uses CinemachineVirtualCameraBase).
    /// </summary>
    public class CameraTargetManager : MonoBehaviour, IDisposable
    {
        #region Serialized

        [SerializeField] private Transform _defaultTarget;

        #endregion

        #region Dependencies

        [Inject] private CameraConfig _config;
        [Inject] private ISubscriber<CameraTargetUpdate> _targetSub;
        [Inject] private ISubscriber<CameraBoundsResetRequest> _boundsSub;
        [Inject] private ISubscriber<PlayerSpawned> _spawnedSub;
        [Inject] private ISubscriber<PlayerDied> _diedSub;

        #endregion

        #region State

        private CinemachineVirtualCameraBase _activeVcam;
        private Transform _currentTarget;
        private Transform _followObject;
        private bool _initialized;

        private IDisposable _targetSubscription;
        private IDisposable _boundsSubscription;
        private IDisposable _spawnedSubscription;
        private IDisposable _diedSubscription;

        #endregion

        #region Public API

        public Transform ActiveTarget => _currentTarget;

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _currentTarget = _defaultTarget;

            _targetSubscription = _targetSub.Subscribe(OnTargetUpdate);
            _boundsSubscription = _boundsSub.Subscribe(_ => OnBoundsReset());
            _spawnedSubscription = _spawnedSub.Subscribe(OnPlayerSpawned);
            _diedSubscription = _diedSub.Subscribe(_ => OnPlayerDied());

            if (_currentTarget != null)
                ApplyTarget(_currentTarget);
        }

        public void SetTarget(Transform target)
        {
            if (!_initialized || target == null) return;
            _currentTarget = target;
            ApplyTarget(target);
        }

        public void SetActiveVcam(CinemachineVirtualCameraBase vcam)
        {
            if (!_initialized) return;
            _activeVcam = vcam;
            if (_currentTarget != null)
                ApplyCameraTarget(vcam, _currentTarget);
        }

        public void Dispose()
        {
            _targetSubscription?.Dispose();
            _boundsSubscription?.Dispose();
            _spawnedSubscription?.Dispose();
            _diedSubscription?.Dispose();
        }

        #endregion

        #region Message Handlers

        private void OnPlayerSpawned(PlayerSpawned msg)
        {
            // The follow object already mirrors the player; targeting the player directly bypasses it.
            SetTarget(_followObject != null ? _followObject : msg.Player);
        }

        private void OnPlayerDied()
        {
            SetTarget(_defaultTarget);
        }

        // STUB: Wire up actual target update logic when implemented.
        private void OnTargetUpdate(CameraTargetUpdate msg)
        {
            _followObject = msg.FollowTarget;
            SetTarget(msg.FollowTarget);
            ApplyBoundary(msg.Boundary);
        }

        private void ApplyBoundary(Collider2D boundary)
        {
            if (boundary == null || _config == null || _config.rigs == null) return;

            for (int i = 0; i < _config.rigs.Count; i++)
            {
                var confiner = _config.rigs[i].confiner;
                if (confiner == null) continue;

                confiner.BoundingShape2D = boundary;
                confiner.InvalidateBoundingShapeCache();
            }
        }

        // STUB: Wire up actual bounds reset logic when implemented.
        private void OnBoundsReset()
        {
            // Intentionally empty — placeholder for future collision-bounds reset logic.
        }

        private void ApplyTarget(Transform target)
        {
            bool appliedToFollowCamera = false;
            if (_config != null && _config.rigs != null)
            {
                for (int i = 0; i < _config.rigs.Count; i++)
                {
                    var rig = _config.rigs[i];
                    if (!rig.isFollowCam || rig.camera == null) continue;

                    ApplyCameraTarget(rig.camera, target);
                    appliedToFollowCamera = true;
                }
            }

            if (!appliedToFollowCamera && _activeVcam != null)
                ApplyCameraTarget(_activeVcam, target);
        }

        private static void ApplyCameraTarget(CinemachineVirtualCameraBase vcam, Transform target)
        {
            if (vcam == null || target == null) return;
            vcam.Follow = target;
            vcam.LookAt = target;
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion
    }
}   