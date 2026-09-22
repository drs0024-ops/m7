using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    public class FlinchHUDController : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private HudManager _hudManager;
        [SerializeField] private CanvasGroup _hudCanvasGroup;
        [SerializeField] private CRTTransitionController _crtController;
        [SerializeField] private float _flickerDuration = 0.1f;
        [SerializeField] private float _freezeDuration = 1.5f;

        [Header("CRT Glitch on Flinch")]
        [SerializeField] private float _flinchRGBSplitAmount = 0.005f;
        [SerializeField] private float _flinchRGBSplitDuration = 0.2f;

        [Inject] private HudState _hudState;

        public bool IsFrozen => _hudState.IsFrozen;

        private ISubscriber<FlinchTriggeredMessage> _flinchSub;
        private readonly List<IDisposable> _subscriptions = new();
        private CancellationTokenSource _cts;
        private bool _disposed;
        private int _flickerTweenId = -1;

        public FlinchHUDController() { }

        void IStartable.Start()
        {
            _cts = new CancellationTokenSource();

            _flinchSub = GlobalMessagePipe.GetSubscriber<FlinchTriggeredMessage>();
            _subscriptions.Add(_flinchSub.Subscribe(_ => OnFlinch()));
        }

        private void OnFlinch()
        {
            if (_disposed || _hudState.IsFrozen) return;

            _hudState.IsFrozen = true;

            // CRT stutters
            _crtController?.PlayRGBSplit(_flinchRGBSplitAmount, _flinchRGBSplitDuration);

            float originalAlpha = _hudCanvasGroup.alpha;
            _flickerTweenId = LeanTween.alpha(_hudCanvasGroup.gameObject, 0f, _flickerDuration * 0.5f)
                .setEase(LeanTweenType.linear)
                .setOnComplete(() =>
                {
                    if (_disposed) return;
                    LeanTween.alpha(_hudCanvasGroup.gameObject, originalAlpha, _flickerDuration * 0.5f)
                        .setEase(LeanTweenType.linear)
                        .setOnComplete(() => _flickerTweenId = -1);
                }).id;

            FreezeThenResume(_cts.Token).Forget();
        }

        private async UniTaskVoid FreezeThenResume(CancellationToken token)
        {
            await UniTask.Delay((int)(_freezeDuration * 1000f), cancellationToken: token);
            if (_disposed || token.IsCancellationRequested) return;

            _hudState.IsFrozen = false;
            GlobalMessagePipe.GetPublisher<HudUnfrozenMessage>().Publish(new HudUnfrozenMessage());
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();

            if (_flickerTweenId != -1)
                LeanTween.cancel(_flickerTweenId);

            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
    }
}   