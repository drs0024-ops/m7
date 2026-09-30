using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using MessagePipe;
using Game.Core.Enums;
using Game.Core.Messages;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Applies follow targets and boundary confiners to virtual cameras.
    /// Follow cameras receive follow + boundary. Non-follow cameras receive boundary only.
    /// The LockedRoom camera also receives its scene-specific room target.
    /// </summary>
    public class CameraTargetManager : IDisposable
    {
        #region Dependencies

        private readonly CameraConfig _config;
        private readonly ISubscriber<CameraTargetUpdate> _targetSub;
        private readonly ISubscriber<CameraBoundsResetRequest> _boundsSub;

        #endregion

        #region State

        private readonly List<IDisposable> _disposables = new List<IDisposable>(2);
        private bool _isDisposed;

        #endregion

        #region Constructor

        public CameraTargetManager(
            CameraConfig config,
            ISubscriber<CameraTargetUpdate> targetSub,
            ISubscriber<CameraBoundsResetRequest> boundsSub)
        {
            _config = config;
            _targetSub = targetSub;
            _boundsSub = boundsSub;
        }

        #endregion

        #region Public API

        public void Initialize()
        {
            _disposables.Add(_targetSub.Subscribe(OnTargetUpdate));
            _disposables.Add(_boundsSub.Subscribe(OnBoundsReset));
        }

        public void UpdateTargets(Transform follow, Collider2D boundary, Transform lockedRoomTarget = null)
        {
            var followRigs = _config.GetFollowRigs();
            for (int i = 0; i < followRigs.Count; i++)
            {
                var rig = followRigs[i];
                if (rig.camera != null && follow != null)
                    rig.camera.Follow = follow;
                SetBoundary(rig.confiner, boundary);
            }

            var nonFollowRigs = _config.GetNonFollowRigs();
            for (int i = 0; i < nonFollowRigs.Count; i++)
            {
                var rig = nonFollowRigs[i];
                if (rig.camera == null) continue;

                if (lockedRoomTarget != null && rig.mode == CameraMode.LockedRoom)
                    rig.camera.Follow = lockedRoomTarget;

                SetBoundary(rig.confiner, boundary);
            }
        }

        public void ResetBounds()
        {
            for (int i = 0; i < _config.rigs.Count; i++)
            {
                SetBoundary(_config.rigs[i].confiner, null);
            }
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

        private void OnTargetUpdate(CameraTargetUpdate req)
        {
            UpdateTargets(req.FollowTarget, req.Boundary, req.LockedRoomTarget);
        }

        private void OnBoundsReset(CameraBoundsResetRequest _)
        {
            ResetBounds();
        }

        #endregion

        #region Internal

        private void SetBoundary(CinemachineConfiner2D confiner, Collider2D boundary)
        {
            if (confiner == null) return;

            if (confiner.BoundingShape2D != boundary)
            {
                confiner.BoundingShape2D = boundary;
                confiner.InvalidateBoundingShapeCache();
            }
        }

        #endregion
    }
}   