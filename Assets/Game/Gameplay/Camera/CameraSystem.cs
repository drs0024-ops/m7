using System.Collections.Generic;
using VContainer;
using MessagePipe;
using Game.Core.Messages;
using VContainer.Unity;
using System;

namespace Game.Gameplay.Camera
{
    public class CameraSystem : IInitializable, IDisposable
    {
        private readonly ISubscriber<CameraTargetUpdate> _targetSub;
        private readonly ISubscriber<CameraBoundsResetRequest> _boundsSub;
        private readonly List<IDisposable> _disposables = new List<IDisposable>(2);

        private readonly CameraSwitcher _switcher;
        private readonly CameraTargetManager _targetManager;

        [Inject]
        public CameraSystem(
            CameraSwitcher switcher,
            CameraTargetManager targetManager,
            ISubscriber<CameraTargetUpdate> targetSub,
            ISubscriber<CameraBoundsResetRequest> boundsSub)
        {
            _switcher = switcher;
            _targetManager = targetManager;
            _targetSub = targetSub;
            _boundsSub = boundsSub;
        }

        public void Initialize()
        {
            _disposables.Add(_targetSub.Subscribe(OnTargetUpdate));
            _disposables.Add(_boundsSub.Subscribe(OnBoundsReset));
            _switcher.Initialize();
        }

        private void OnTargetUpdate(CameraTargetUpdate req)
        {
            _targetManager.UpdateTargets(req.FollowTarget, req.Boundary);
        }

        private void OnBoundsReset(CameraBoundsResetRequest req)
        {
            _targetManager.ResetBounds();
        }

        public void Dispose()
        {
            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();

            _switcher.Dispose();
        }
    }
}   