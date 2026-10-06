using System;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Upgrades
{
    /// <summary>Adapts buffered charger state into enemy-specific investigation messages.</summary>
    [RequireComponent(typeof(Game.Gameplay.Enemies.Enemy))]
    public sealed class InvisibilityEnemyInvestigator : MonoBehaviour, IDisposable
    {
        private IBufferedSubscriber<InvisibilityBroadcastStateChanged> _broadcastSubscriber;
        private IPublisher<EnemyInvestigationRequested> _requestPublisher;
        private IPublisher<EnemyInvestigationCancelled> _cancelPublisher;
        private IDisposable _subscription;
        private Vector2 _investigationPosition;
        private bool _isInvestigating;
        private bool _isInitialized;
        private bool _isDisposed;

        /// <summary>Injects buffered broadcast state and enemy command publishers.</summary>
        [Inject]
        private void Inject(
            IBufferedSubscriber<InvisibilityBroadcastStateChanged> broadcastSubscriber,
            IPublisher<EnemyInvestigationRequested> requestPublisher,
            IPublisher<EnemyInvestigationCancelled> cancelPublisher)
        {
            _broadcastSubscriber = broadcastSubscriber;
            _requestPublisher = requestPublisher;
            _cancelPublisher = cancelPublisher;
        }

        /// <summary>Subscribes to the latest charger snapshot after injection.</summary>
        public void Initialize()
        {
            if (_isInitialized || _isDisposed) return;
#if UNITY_EDITOR
            Debug.Assert(_broadcastSubscriber != null, "Investigator requires buffered broadcast state.", this);
            Debug.Assert(_requestPublisher != null && _cancelPublisher != null,
                "Investigator requires MessagePipe publishers.", this);
            Debug.Assert(GetComponent<Game.Gameplay.Enemies.Enemy>() != null,
                "Investigator must be on the Enemy GameObject.", this);
#endif
            if (_broadcastSubscriber == null || _requestPublisher == null || _cancelPublisher == null)
                return;

            _subscription = _broadcastSubscriber.Subscribe(OnBroadcastStateChanged);
            _isInitialized = true;
        }

        private void Start() => Initialize();

        private void OnBroadcastStateChanged(InvisibilityBroadcastStateChanged message)
        {
            if (_isDisposed) return;

            Vector2 selfPosition = transform.position;
            Vector2 offset = message.Position - selfPosition;
            bool shouldInvestigate = message.IsActive && offset.sqrMagnitude <= message.Radius * message.Radius;

            if (shouldInvestigate)
            {
                if (_isInvestigating && _investigationPosition == message.Position) return;
                _isInvestigating = true;
                _investigationPosition = message.Position;
                _requestPublisher.Publish(new EnemyInvestigationRequested(
                    gameObject.GetInstanceID(), message.Position));
                return;
            }

            CancelInvestigation();
        }

        private void CancelInvestigation()
        {
            if (!_isInvestigating) return;
            _isInvestigating = false;
            _cancelPublisher.Publish(new EnemyInvestigationCancelled(gameObject.GetInstanceID()));
        }

        /// <summary>Disposes the buffered MessagePipe subscription.</summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _subscription?.Dispose();
            _subscription = null;
        }

        private void OnDestroy() => Dispose();
    }
}
