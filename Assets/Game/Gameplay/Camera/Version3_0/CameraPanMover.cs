using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using MessagePipe;
using Game.Core.Messages;
using VContainer;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Pans the active camera by tweening the composer's TargetOffset.
    /// Reads the active composer from CameraConfig — no dependency on CameraSwitcher.
    /// </summary>
    public class CameraPanMover : MonoBehaviour, IDisposable
    {
        #region Dependencies

        private CameraConfig _config;
        private ISubscriber<CameraPanRequest> _panSub;

        #endregion

        #region State

        private readonly List<IDisposable> _disposables = new List<IDisposable>(1);
        private int _panTweenId = -1;
        private bool _isDisposed;

        #endregion

        #region Public API

        [Inject]
        public void Construct(CameraConfig config, ISubscriber<CameraPanRequest> panSub)
        {
            _config = config;
            _panSub = panSub;
        }

        public void Initialize()
        {
            _disposables.Add(_panSub.Subscribe(HandlePan));
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i]?.Dispose();
            _disposables.Clear();

            if (_panTweenId != -1 && gameObject != null)
                LeanTween.cancel(_panTweenId);
        }

        #endregion

        #region Message Handlers

        private void HandlePan(CameraPanRequest req)
        {
            var composer = _config.ActiveComposer;
            if (composer == null) return;

            if (_panTweenId != -1)
                LeanTween.cancel(_panTweenId);

            Vector3 current = composer.TargetOffset;
            Vector3 target = new Vector3(req.TargetOffset.x, req.TargetOffset.y, current.z);

            var tween = LeanTween.value(gameObject, (Vector3 val) =>
            {
                composer.TargetOffset = val;
            }, current, target, req.Duration)
            .setEase(LeanTweenType.linear);

            _panTweenId = tween.id;
        }

        #endregion

        #region Cleanup

        private void OnDisable()
        {
            if (_panTweenId != -1) LeanTween.cancel(_panTweenId);
        }

        #endregion
    }
}   