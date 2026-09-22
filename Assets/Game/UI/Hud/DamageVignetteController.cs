using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    public class DamageVignetteController : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private Image _vignetteImage;
        [SerializeField] private Material _crtFailureMaterial;

        [Inject] private HudState _hudState;

        private ISubscriber<PlayerDamaged> _damagedSub;
        private readonly List<IDisposable> _subscriptions = new();
        private CancellationTokenSource _seqCts;
        private bool _disposed;

        public DamageVignetteController() { }

        void IStartable.Start()
        {
            _seqCts = new CancellationTokenSource();

            _damagedSub = GlobalMessagePipe.GetSubscriber<PlayerDamaged>();
            _subscriptions.Add(_damagedSub.Subscribe(OnPlayerDamaged));

            _vignetteImage.color = new Color(1f, 0f, 0f, 0f);
 
        }

        private void OnPlayerDamaged(PlayerDamaged msg)
        {
            if (_disposed) return;
            if (_hudState.IsFrozen) return;

            _seqCts.Cancel();
            _seqCts = new CancellationTokenSource();

            if (msg.IsNearDeath)
                NearDeathSequence(_seqCts.Token).Forget();
            else
                NormalHitSequence(_seqCts.Token).Forget();
        }

        private async UniTaskVoid NormalHitSequence(CancellationToken token)
        {
            // Vignette
            await FadeTo(0.4f, 0.1f, token);
            if (token.IsCancellationRequested) return;
            await FadeTo(0f, 0.5f, token);

            // CRT failure: brief flash
            _crtFailureMaterial.SetFloat("_FailureAmount", 0.3f);
            _crtFailureMaterial.SetFloat("_RetraceY", -1f);
            _crtFailureMaterial.SetFloat("_Collapse", 0f);
            await UniTask.Delay(100, cancellationToken: token);
            if (token.IsCancellationRequested) return;
            _crtFailureMaterial.SetFloat("_FailureAmount", 0f);
        }

        private async UniTaskVoid NearDeathSequence(CancellationToken token)
        {
            // CRT failure: full sequence
            _crtFailureMaterial.SetFloat("_Collapse", 0f);

            // Attack: failure rises, retrace sweeps
            _crtFailureMaterial.SetFloat("_FailureAmount", 0.6f);
            var retraceTween = LeanTween.value(gameObject, (float v) =>
            {
                _crtFailureMaterial.SetFloat("_RetraceY", v);
            }, 0f, 1f, 0.1f).setEase(LeanTweenType.linear);

            await UniTask.Delay(100, cancellationToken: token);
            if (token.IsCancellationRequested) { LeanTween.cancel(retraceTween.id); return; }

            // Hold: retrace off, failure holds, flicker active
            _crtFailureMaterial.SetFloat("_RetraceY", -1f);
            await UniTask.Delay(500, cancellationToken: token);
            if (token.IsCancellationRequested) return;

            // Collapse: screen contracts to line, then dot
            var collapseTween = LeanTween.value(gameObject, (float v) =>
            {
                _crtFailureMaterial.SetFloat("_Collapse", v);
            }, 0f, 1f, 1f).setEase(LeanTweenType.easeInQuad);

            await UniTask.Delay(1000, cancellationToken: token);
            if (token.IsCancellationRequested) { LeanTween.cancel(collapseTween.id); return; }

            // Reset
            _crtFailureMaterial.SetFloat("_FailureAmount", 0f);
            _crtFailureMaterial.SetFloat("_Collapse", 0f);
            _crtFailureMaterial.SetFloat("_RetraceY", -1f);
        }

        private async UniTask FadeTo(float targetAlpha, float duration, CancellationToken token)
        {
            var tween = LeanTween.alpha(_vignetteImage.gameObject, targetAlpha, duration)
                .setEase(LeanTweenType.easeOutQuad);

            await UniTask.Delay((int)(duration * 1000f), cancellationToken: token);
            if (token.IsCancellationRequested)
                LeanTween.cancel(tween.id);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _seqCts?.Cancel();
            _seqCts?.Dispose();

            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
    }
}   