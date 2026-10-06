using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Enemies.Runtime
{
    public sealed class EnemyRuntimeSystem : IStartable, IDisposable
    {
        private readonly IReadOnlyList<EnemyEntity> _entities;
        private readonly IReadOnlyList<EnemyPerceptionSensor> _sensors;
        private readonly ISubscriber<EnemySpawned> _spawnedSubscriber;
        private readonly ISubscriber<EnemyDespawned> _despawnedSubscriber;
        private readonly ISubscriber<EnemyPerceptionChanged> _perceptionSubscriber;
        private readonly EnemyDecisionRuntime _decisionRuntime;
        private readonly Dictionary<EnemyInstanceId, EnemyRuntimeContext> _contexts = new();
        private readonly List<IDisposable> _subscriptions = new(3);
        private bool _disposed;

        public int ActiveContextCount => _contexts.Count;

        public EnemyRuntimeSystem(
            IReadOnlyList<EnemyEntity> entities,
            IReadOnlyList<EnemyPerceptionSensor> sensors,
            ISubscriber<EnemySpawned> spawnedSubscriber,
            ISubscriber<EnemyDespawned> despawnedSubscriber,
            ISubscriber<EnemyPerceptionChanged> perceptionSubscriber,
            EnemyDecisionRuntime decisionRuntime)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _sensors = sensors ?? throw new ArgumentNullException(nameof(sensors));
            _spawnedSubscriber = spawnedSubscriber ?? throw new ArgumentNullException(nameof(spawnedSubscriber));
            _despawnedSubscriber = despawnedSubscriber ?? throw new ArgumentNullException(nameof(despawnedSubscriber));
            _perceptionSubscriber = perceptionSubscriber ?? throw new ArgumentNullException(nameof(perceptionSubscriber));
            _decisionRuntime = decisionRuntime ?? throw new ArgumentNullException(nameof(decisionRuntime));
        }

        public void Start()
        {
            if (_disposed) return;

            _subscriptions.Add(_spawnedSubscriber.Subscribe(OnEnemySpawned));
            _subscriptions.Add(_despawnedSubscriber.Subscribe(OnEnemyDespawned));
            _subscriptions.Add(_perceptionSubscriber.Subscribe(OnPerceptionChanged));

            for (int i = 0; i < _entities.Count; i++)
                if (_entities[i] != null)
                    _entities[i].InitializeForRuntime();

            for (int i = 0; i < _sensors.Count; i++)
            {
                EnemyPerceptionSensor sensor = _sensors[i];
                if (sensor == null) continue;

                EnemyEntity owner = sensor.GetComponentInParent<EnemyEntity>();
                if (owner == null)
                {
                    Debug.LogError($"[EnemyRuntimeSystem] Sensor '{sensor.name}' has no EnemyEntity parent.", sensor);
                    continue;
                }

                sensor.Initialize(owner.InstanceId, owner.transform);
            }
        }

        private void OnEnemySpawned(EnemySpawned message)
        {
            if (_disposed || _contexts.ContainsKey(message.InstanceId)) return;

            try
            {
                _contexts.Add(message.InstanceId, new EnemyRuntimeContext(message.InstanceId, message.Definition));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EnemyRuntimeSystem] Rejected '{message.Definition?.TypeId ?? "<null>"}': {exception.Message}");
            }
        }

        private void OnEnemyDespawned(EnemyDespawned message)
        {
            if (_disposed) return;
            _contexts.Remove(message.InstanceId);
        }

        private void OnPerceptionChanged(EnemyPerceptionChanged message)
        {
            if (_disposed || !_contexts.TryGetValue(message.EnemyId, out EnemyRuntimeContext context))
                return;

            if (message.HasTarget)
                context.SetTarget(message.TargetEntityId, message.TargetPosition);
            else
                context.ClearTarget();

            var snapshot = new EnemyConditionSnapshot(
                message.TargetDistance,
                isGrounded: null,
                investigationComplete: false);
            _decisionRuntime.TryTransition(context, snapshot);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
            _contexts.Clear();
        }
    }
}