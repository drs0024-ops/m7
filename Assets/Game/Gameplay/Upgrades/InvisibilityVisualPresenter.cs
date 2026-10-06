using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Player
{
    /// <summary>Applies continuous invisibility charge to the player's cloak shader.</summary>
    public sealed class InvisibilityVisualPresenter : MonoBehaviour, IDisposable
    {
        private static readonly int CloakFactorId = Shader.PropertyToID("_CloakFactor");

        private IBufferedSubscriber<InvisibilityChargeChanged> _chargeSubscriber;
        private ISubscriber<InvisibilityTierChanged> _tierSubscriber;
        private ISubscriber<PlayerSpawned> _playerSpawnedSubscriber;
        private readonly List<IDisposable> _subscriptions = new(3);

        private SpriteRenderer _targetRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private float _latestFactor;
        private float _lastAppliedFactor = -1f;
        private bool _isInitialized;
        private bool _isDisposed;

        /// <summary>Injects buffered charge state and player/tier messages.</summary>
        [Inject]
        private void Inject(
            IBufferedSubscriber<InvisibilityChargeChanged> chargeSubscriber,
            ISubscriber<InvisibilityTierChanged> tierSubscriber,
            ISubscriber<PlayerSpawned> playerSpawnedSubscriber)
        {
            _chargeSubscriber = chargeSubscriber;
            _tierSubscriber = tierSubscriber;
            _playerSpawnedSubscriber = playerSpawnedSubscriber;
        }

        /// <summary>Starts subscriptions before the player-spawn entry point runs.</summary>
        public void Initialize()
        {
            if (_isInitialized || _isDisposed) return;
#if UNITY_EDITOR
            Debug.Assert(_chargeSubscriber != null && _tierSubscriber != null
                && _playerSpawnedSubscriber != null,
                "InvisibilityVisualPresenter dependencies must be injected before Initialize().", this);
#endif
            if (_chargeSubscriber == null || _tierSubscriber == null || _playerSpawnedSubscriber == null)
                return;

            _propertyBlock = new MaterialPropertyBlock();
            _subscriptions.Add(_chargeSubscriber.Subscribe(OnChargeChanged));
            _subscriptions.Add(_tierSubscriber.Subscribe(OnTierChanged));
            _subscriptions.Add(_playerSpawnedSubscriber.Subscribe(OnPlayerSpawned));
            _isInitialized = true;
        }

        private void Start() => Initialize();

        private void OnPlayerSpawned(PlayerSpawned message)
        {
            if (message.Player == null) return;

            var renderers = message.Player.GetComponentsInChildren<SpriteRenderer>(true);
            _targetRenderer = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer candidate = renderers[i];
                if (candidate.sprite == null || candidate.sharedMaterial == null) continue;
#if UNITY_EDITOR
                Debug.Assert(candidate.sharedMaterial.HasProperty(CloakFactorId),
                    $"SpriteRenderer '{candidate.name}' is missing _CloakFactor.", candidate);
#endif
                if (!candidate.sharedMaterial.HasProperty(CloakFactorId)) continue;
                _targetRenderer = candidate;
                break;
            }

            if (_targetRenderer == null)
            {
                Debug.LogWarning("[InvisibilityVisualPresenter] No player SpriteRenderer has a _CloakFactor material.", this);
                return;
            }

            _lastAppliedFactor = -1f;
            ApplyFactor(_latestFactor);
        }

        private void OnChargeChanged(InvisibilityChargeChanged message)
        {
            _latestFactor = Mathf.Clamp01(message.ChargePercent / 100f);
            ApplyFactor(_latestFactor);
        }

        private void ApplyFactor(float factor)
        {
            if (_targetRenderer == null || _propertyBlock == null
                || Mathf.Approximately(_lastAppliedFactor, factor))
                return;

            _targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(CloakFactorId, factor);
            _targetRenderer.SetPropertyBlock(_propertyBlock);
            _lastAppliedFactor = factor;
        }

        private void OnTierChanged(InvisibilityTierChanged message)
        {
            Debug.Log($"[InvisibilityVisualPresenter] Visibility tier: {message.NewTier}.", this);
        }

        /// <summary>Disposes all MessagePipe subscriptions.</summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy() => Dispose();
    }
}
