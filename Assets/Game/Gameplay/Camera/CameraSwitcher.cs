using System.Collections.Generic;
using Unity.Cinemachine;
using VContainer;
using MessagePipe;
using Game.Core.Enums;
using Game.Core.Messages;
using VContainer.Unity;
using System;

namespace Game.Gameplay.Camera
{
    public class CameraSwitcher : IInitializable, IDisposable
    {
        private readonly ISubscriber<CameraSwitchRequest> _switchSub;
        private readonly List<IDisposable> _disposables = new List<IDisposable>(1);
        private readonly CameraConfigSO _config;

        private CinemachineCamera _currentCamera;
        private CinemachinePositionComposer _currentComposer;
        private CinemachineConfiner2D _currentConfiner;
        private bool _isDisposed;

        public CinemachinePositionComposer CurrentComposer => _currentComposer;
        public CinemachineConfiner2D CurrentConfiner => _currentConfiner;
        public CinemachineCamera CurrentCamera => _currentCamera;

        [Inject]
        public CameraSwitcher(
            CameraConfigSO config,
            ISubscriber<CameraSwitchRequest> switchSub)
        {
            _config = config;
            _switchSub = switchSub;
        }

        public void Initialize()
        {
            _disposables.Add(_switchSub.Subscribe(HandleSwitch));
            SwitchTo(CameraMode.CenterFollow);
        }

        public void SwitchTo(CameraMode mode)
        {
            if (_isDisposed) return;

            var target = mode switch
            {
                CameraMode.CenterFollow => _config.Center,
                CameraMode.NoYFollow => _config.NoY,
                CameraMode.LockedRoom => _config.Locked,
                _ => _config.Center
            };

            if (target == null || target == _currentCamera) return;

            _currentCamera = target;
            _currentComposer = target.GetComponent<CinemachinePositionComposer>();
            _currentConfiner = target.GetComponent<CinemachineConfiner2D>();

            _config.Center.enabled = target == _config.Center;
            _config.NoY.enabled = target == _config.NoY;
            _config.Locked.enabled = target == _config.Locked;
        }

        private void HandleSwitch(CameraSwitchRequest req)
        {
            if (req.Mode == default) return;
            SwitchTo(req.Mode);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();
        }
    }
}   