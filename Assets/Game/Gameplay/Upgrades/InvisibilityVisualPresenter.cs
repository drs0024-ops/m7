using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Game.Core.Messages;
using MessagePipe;
using VContainer;

namespace Game.Gameplay.Player
{
    public class InvisibilityVisualPresenter : MonoBehaviour, IDisposable
    {
        [SerializeField] private float _transitionDuration = 0.5f;

        private ISubscriber<InvisibilityStateChanged> _stateSub;
        private ISubscriber<PlayerSpawned> _playerSpawnedSub;

        private Renderer _targetRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private CancellationTokenSource _transitionCts;
        private bool _isDisposed;

        private readonly List<IDisposable> _subscriptions = new(2);
        private static readonly int CloakFactorId = Shader.PropertyToID("_CloakFactor");

        [Inject]
        private void Inject(
            ISubscriber<InvisibilityStateChanged> stateSub,
            ISubscriber<PlayerSpawned> playerSpawnedSub)
        {
            _stateSub = stateSub;
            _playerSpawnedSub = playerSpawnedSub;
        }

        private void Start()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _subscriptions.Add(_playerSpawnedSub.Subscribe(OnPlayerSpawned));
            _subscriptions.Add(_stateSub.Subscribe(OnStateChanged));
        }

        private void OnPlayerSpawned(PlayerSpawned message)
        {
            if (message.Player == null) return;
            _targetRenderer = message.Player.GetComponentInChildren<Renderer>();
            if (_targetRenderer == null)
                Debug.LogWarning("[InvisibilityVisualPresenter] Player has no Renderer.");
        }

        private void OnStateChanged(InvisibilityStateChanged state)
        {
            if (_targetRenderer == null) return;

            _transitionCts?.Cancel();
            _transitionCts?.Dispose();

            _transitionCts = new CancellationTokenSource();
            _ = AnimateCloak(state.TargetFactor, _transitionCts.Token);
        }

        private async UniTask AnimateCloak(float targetFactor, CancellationToken token)
        {
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

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            if (!token.IsCancellationRequested)
            {
                _propertyBlock.SetFloat(CloakFactorId, targetFactor);
                _targetRenderer.SetPropertyBlock(_propertyBlock);
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