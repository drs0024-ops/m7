using System;
using System.Collections.Generic;
using Game.Core.Interfaces;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Enemies.Runtime
{
    public sealed class EnemyExecutionAdapterSystem : IStartable, IFixedTickable, IDisposable
    {
        private readonly struct MovementOrder
        {
            public Vector2 Target { get; }
            public float Speed { get; }

            public MovementOrder(Vector2 target, float speed)
            {
                Target = target;
                Speed = speed;
            }
        }

        private readonly IReadOnlyList<EnemyEntity> _entities;
        private readonly IDamagableRegistry _damagableRegistry;
        private readonly ISubscriber<EnemyMoveRequested> _moveSubscriber;
        private readonly ISubscriber<EnemyStopRequested> _stopSubscriber;
        private readonly ISubscriber<EnemyAttackRequested> _attackSubscriber;
        private readonly Dictionary<EnemyInstanceId, Rigidbody2D> _bodies = new();
        private readonly Dictionary<EnemyInstanceId, MovementOrder> _movementOrders = new();
        private readonly List<IDisposable> _subscriptions = new(3);
        private bool _disposed;

        public EnemyExecutionAdapterSystem(
            IReadOnlyList<EnemyEntity> entities,
            IDamagableRegistry damagableRegistry,
            ISubscriber<EnemyMoveRequested> moveSubscriber,
            ISubscriber<EnemyStopRequested> stopSubscriber,
            ISubscriber<EnemyAttackRequested> attackSubscriber)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _damagableRegistry = damagableRegistry ?? throw new ArgumentNullException(nameof(damagableRegistry));
            _moveSubscriber = moveSubscriber ?? throw new ArgumentNullException(nameof(moveSubscriber));
            _stopSubscriber = stopSubscriber ?? throw new ArgumentNullException(nameof(stopSubscriber));
            _attackSubscriber = attackSubscriber ?? throw new ArgumentNullException(nameof(attackSubscriber));
        }

        void IStartable.Start()
        {
            if (_disposed) return;

            _subscriptions.Add(_moveSubscriber.Subscribe(OnMoveRequested));
            _subscriptions.Add(_stopSubscriber.Subscribe(OnStopRequested));
            _subscriptions.Add(_attackSubscriber.Subscribe(OnAttackRequested));

            for (int i = 0; i < _entities.Count; i++)
            {
                EnemyEntity entity = _entities[i];
                if (entity == null) continue;

                Rigidbody2D body = entity.GetComponent<Rigidbody2D>();
                if (body == null)
                    body = entity.GetComponentInChildren<Rigidbody2D>(true);
                if (body == null)
                {
                    Debug.LogError($"[EnemyExecutionAdapterSystem] Enemy '{entity.name}' has no Rigidbody2D.", entity);
                    continue;
                }

                _bodies[entity.InstanceId] = body;
            }
        }

        void IFixedTickable.FixedTick()
        {
            if (_disposed) return;

            foreach (KeyValuePair<EnemyInstanceId, MovementOrder> pair in _movementOrders)
            {
                if (!_bodies.TryGetValue(pair.Key, out Rigidbody2D body) || body == null)
                    continue;

                Vector2 nextPosition = Vector2.MoveTowards(
                    body.position,
                    pair.Value.Target,
                    pair.Value.Speed * Time.fixedDeltaTime);
                body.MovePosition(nextPosition);
            }
        }

        private void OnMoveRequested(EnemyMoveRequested message)
        {
            if (_disposed || !_bodies.ContainsKey(message.InstanceId)) return;
            _movementOrders[message.InstanceId] = new MovementOrder(message.TargetPosition, message.Speed);
        }

        private void OnStopRequested(EnemyStopRequested message)
        {
            if (_disposed) return;
            _movementOrders.Remove(message.InstanceId);

            if (_bodies.TryGetValue(message.InstanceId, out Rigidbody2D body) && body != null)
            {
                Vector2 velocity = body.linearVelocity;
                velocity.x = 0f;
                body.linearVelocity = velocity;
            }
        }

        private void OnAttackRequested(EnemyAttackRequested message)
        {
            if (_disposed) return;
            if (!_damagableRegistry.TryGet(message.TargetEntityId, out IDamagable target))
            {
                Debug.LogWarning(
                    $"[EnemyExecutionAdapterSystem] Target {message.TargetEntityId} is not registered as IDamagable.");
                return;
            }

            target.Damage(message.Damage, message.KnockbackDirection);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
            _movementOrders.Clear();
            _bodies.Clear();
        }
    }
}