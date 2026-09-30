using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using MessagePipe;
using Game.Core.Enums;
using Game.Core.Messages;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Executes camera mode switches by enabling/disabling virtual cameras.
    /// Iterates the data-driven rig list in CameraConfig.
    /// Publishes CameraModeActive on each successful switch.
    /// </summary>
    public class CameraSwitcher : IDisposable
    {
        #region Dependencies

        private readonly CameraConfig _config;
        private readonly ISubscriber<CameraSwitchRequest> _switchSub;
        private readonly IPublisher<CameraModeActive> _modePublisher;

        #endregion

        #region State

        private readonly List<IDisposable> _disposables = new List<IDisposable>(1);

        private CameraRigEntry _currentRig;
        private bool _hasCurrent;
        private bool _isSwitching;
        private bool _isDisposed;

        #endregion

        #region Constructor

        public CameraSwitcher(
            CameraConfig config,
            ISubscriber<CameraSwitchRequest> switchSub,
            IPublisher<CameraModeActive> modePublisher)
        {
            _config = config;
            _switchSub = switchSub;
            _modePublisher = modePublisher;
        }

        #endregion

        #region Public API

        public CinemachineCamera CurrentCamera => _hasCurrent ? _currentRig.camera : null;
        public CinemachinePositionComposer CurrentComposer => _config.ActiveComposer;
        public CinemachineConfiner2D CurrentConfiner => _hasCurrent ? _currentRig.confiner : null;

        public void Initialize()
        {
            _disposables.Add(_switchSub.Subscribe(HandleSwitch));
            SwitchTo(CameraMode.CenterFollow);
        }

        /// <summary>
        /// Switches the active virtual camera to the specified mode.
        /// Ignores the call if already on that mode or currently switching.
        /// </summary>
        public void SwitchTo(CameraMode mode)
        {
            if (_isDisposed || _isSwitching) return;

            var target = _config.FindRig(mode);
            if (target == null) return;

            if (_hasCurrent && target.Value.camera == _currentRig.camera) return;

            _isSwitching = true;

            for (int i = 0; i < _config.rigs.Count; i++)
            {
                var rig = _config.rigs[i];
                if (rig.camera == null) continue;

                bool isActive = rig.camera == target.Value.camera;
                rig.camera.enabled = isActive;
                rig.camera.StandbyUpdate = isActive
                    ? CinemachineVirtualCameraBase.StandbyUpdateMode.Always
                    : CinemachineVirtualCameraBase.StandbyUpdateMode.Never;
            }

            _currentRig = target.Value;
            _hasCurrent = true;
            _config.SetActiveComposer(_currentRig.composer);

            _modePublisher.Publish(new CameraModeActive(mode));

            _isSwitching = false;
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

        private void HandleSwitch(CameraSwitchRequest req)
        {
            if (req.Mode == default) return;
            SwitchTo(req.Mode);
        }

        #endregion
    }
}   