using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine.Rendering.Universal;

namespace Game.Gameplay.Player
{
    public class InvisibilityVisualPresenter : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private ShadowCaster2D _shadowCaster2D;
        [SerializeField] private float _transitionDuration = 0.5f;

        private ISubscriber<InvisibilityStateChanged> _stateSub;
        private MaterialPropertyBlock _propertyBlock;
        private CancellationTokenSource _transitionCts;
        private bool _isDisposed;

        private readonly List<IDisposable> _subscriptions = new(1);
        private static readonly int CloakFactorId = Shader.PropertyToID("_CloakFactor");

        [Inject]
        public InvisibilityVisualPresenter()
        {
        }

        void IStartable.Start()
        {
            _stateSub = GlobalMessagePipe.GetSubscriber<InvisibilityStateChanged>();
            _propertyBlock = new MaterialPropertyBlock();
            _subscriptions.Add(_stateSub.Subscribe(OnStateChanged));
        }

        private void OnStateChanged(InvisibilityStateChanged state)
        {
            _transitionCts?.Cancel();
            _transitionCts?.Dispose();

            _transitionCts = new CancellationTokenSource();
            _ = AnimateCloak(state.TargetFactor, _transitionCts.Token);
        }

        private async UniTask AnimateCloak(float targetFactor, CancellationToken token)
        {
            if (_targetRenderer == null) return;

            _targetRenderer.GetPropertyBlock(_propertyBlock);
            float startFactor = _propertyBlock.GetFloat(CloakFactorId);
            float elapsed = 0f;

            while (elapsed < _transitionDuration && !token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _transitionDuration);
                float currentFactor = Mathf.Lerp(startFactor, targetFactor, t);

                _propertyBlock.SetFloat(CloakFactorId, currentFactor);
                _targetRenderer.SetPropertyBlock(_propertyBlock);

                if (_shadowCaster2D != null)
                    _shadowCaster2D.enabled = currentFactor < 0.9f;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            if (!token.IsCancellationRequested)
            {
                _propertyBlock.SetFloat(CloakFactorId, targetFactor);
                _targetRenderer.SetPropertyBlock(_propertyBlock);

                if (_shadowCaster2D != null)
                    _shadowCaster2D.enabled = targetFactor < 0.9f;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _transitionCts?.Cancel();
            _transitionCts?.Dispose();

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}   