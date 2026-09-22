using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Game.Core.Messages;
using MessagePipe;
using VContainer.Unity;
using System;

namespace Game.Gameplay.Camera
{
    public class CameraPanMover : MonoBehaviour, IInitializable, IDisposable
    {
        private readonly ISubscriber<CameraPanRequest> _panSub;
        private readonly List<IDisposable> _disposables = new List<IDisposable>(1);
        private readonly CameraSwitcher _switcher;

        private int _panTweenId = -1;

        [Inject]
        public CameraPanMover(
            CameraSwitcher switcher,
            ISubscriber<CameraPanRequest> panSub)
        {
            _switcher = switcher;
            _panSub = panSub;
        }

        public void Initialize()
        {
            _disposables.Add(_panSub.Subscribe(HandlePan));
        }

        private void HandlePan(CameraPanRequest req)
        {
            var composer = _switcher.CurrentComposer;
            if (composer == null) return;

            if (_panTweenId != -1)
                LeanTween.cancel(_panTweenId);

            Vector3 current = composer.TargetOffset;
            Vector3 target = new Vector3(req.TargetOffset.x, req.TargetOffset.y, current.z);

            var tween = LeanTween.value(gameObject, (Vector3 val) =>
            {
                if (composer != null) composer.TargetOffset = val;
            }, current, target, req.Duration)
            .setEase(LeanTweenType.linear);

            _panTweenId = tween.id;
        }

        public void Dispose()
        {
            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();

            if (_panTweenId != -1) LeanTween.cancel(_panTweenId);
        }

        private void OnDisable()
        {
            if (_panTweenId != -1) LeanTween.cancel(_panTweenId);
        }
    }
}   