using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using VContainer.Unity;

namespace Game.Gameplay.Enemies.Runtime
{
    public sealed class EnemyExecutionSystem : IStartable, ITickable, IDisposable
    {
        private sealed class InstanceModules
        {
            public readonly EnemyDefinition Definition;
            public readonly List<IEnemyExecutionModuleRuntime> Modules = new();
            public string ActiveStateId;
            public int? TargetEntityId;
            public UnityEngine.Vector2 TargetPosition;
            public float TargetDistance;
            public UnityEngine.Vector2 EnemyPosition;

            public InstanceModules(EnemyDefinition definition, UnityEngine.Vector2 enemyPosition)
            {
                Definition = definition;
                ActiveStateId = definition.StateGraph.GetInitialLeafStateId();
                EnemyPosition = enemyPosition;
            }

            public EnemyExecutionContext Snapshot(EnemyInstanceId id) => new(
                id,
                Definition,
                ActiveStateId,
                TargetEntityId,
                TargetPosition,
                TargetDistance,
                EnemyPosition);
        }

        private readonly IReadOnlyList<EnemyEntity> _entities;
        private readonly ISubscriber<EnemySpawned> _spawnedSubscriber;
        private readonly ISubscriber<EnemyDespawned> _despawnedSubscriber;
        private readonly ISubscriber<EnemyStateChanged> _stateChangedSubscriber;
        private readonly ISubscriber<EnemyPerceptionChanged> _perceptionSubscriber;
        private readonly IPublisher<EnemyMoveRequested> _movePublisher;
        private readonly IPublisher<EnemyStopRequested> _stopPublisher;
        private readonly IPublisher<EnemyAttackRequested> _attackPublisher;
        private readonly Dictionary<EnemyInstanceId, InstanceModules> _instances = new();
        private readonly List<IDisposable> _subscriptions = new(4);
        private bool _disposed;

        public int ActiveInstanceCount => _instances.Count;

        public EnemyExecutionSystem(
            IReadOnlyList<EnemyEntity> entities,
            ISubscriber<EnemySpawned> spawnedSubscriber,
            ISubscriber<EnemyDespawned> despawnedSubscriber,
            ISubscriber<EnemyStateChanged> stateChangedSubscriber,
            ISubscriber<EnemyPerceptionChanged> perceptionSubscriber,
            IPublisher<EnemyMoveRequested> movePublisher,
            IPublisher<EnemyStopRequested> stopPublisher,
            IPublisher<EnemyAttackRequested> attackPublisher)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _spawnedSubscriber = spawnedSubscriber ?? throw new ArgumentNullException(nameof(spawnedSubscriber));
            _despawnedSubscriber = despawnedSubscriber ?? throw new ArgumentNullException(nameof(despawnedSubscriber));
            _stateChangedSubscriber = stateChangedSubscriber ?? throw new ArgumentNullException(nameof(stateChangedSubscriber));
            _perceptionSubscriber = perceptionSubscriber ?? throw new ArgumentNullException(nameof(perceptionSubscriber));
            _movePublisher = movePublisher ?? throw new ArgumentNullException(nameof(movePublisher));
            _stopPublisher = stopPublisher ?? throw new ArgumentNullException(nameof(stopPublisher));
            _attackPublisher = attackPublisher ?? throw new ArgumentNullException(nameof(attackPublisher));
        }

        public void Start()
        {
            if (_disposed) return;

            _subscriptions.Add(_spawnedSubscriber.Subscribe(OnEnemySpawned));
            _subscriptions.Add(_despawnedSubscriber.Subscribe(OnEnemyDespawned));
            _subscriptions.Add(_stateChangedSubscriber.Subscribe(OnStateChanged));
            _subscriptions.Add(_perceptionSubscriber.Subscribe(OnPerceptionChanged));

            for (int i = 0; i < _entities.Count; i++)
            {
                EnemyEntity entity = _entities[i];
                if (entity == null || entity.Definition == null) continue;

                OnEnemySpawned(new EnemySpawned(
                    entity.InstanceId,
                    entity.Definition,
                    entity.transform.position));
            }
        }

        public void Tick()
        {
            if (_disposed) return;

            float deltaTime = UnityEngine.Time.deltaTime;
            foreach (KeyValuePair<EnemyInstanceId, InstanceModules> pair in _instances)
            {
                EnemyExecutionContext context = pair.Value.Snapshot(pair.Key);
                for (int i = 0; i < pair.Value.Modules.Count; i++)
                    Dispatch(pair.Value.Modules[i].Tick(context, deltaTime));
            }
        }

        private void OnEnemySpawned(EnemySpawned message)
        {
            if (_disposed || _instances.ContainsKey(message.InstanceId) || message.Definition == null)
                return;

            if (!message.Definition.TryValidate(out string error))
            {
                UnityEngine.Debug.LogError($"[EnemyExecutionSystem] Invalid definition '{message.Definition.TypeId}': {error}");
                return;
            }

            var instance = new InstanceModules(message.Definition, message.Position);
            EnemyModuleDefinition[] definitions = message.Definition.Modules;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i].Kind != EnemyModuleKind.Execution) continue;

                try
                {
                    IEnemyModuleRuntime runtime = definitions[i].CreateRuntime();
                    if (runtime is IEnemyExecutionModuleRuntime executionRuntime)
                        instance.Modules.Add(executionRuntime);
                    else
                    {
                        runtime?.Dispose();
                        UnityEngine.Debug.LogError(
                            $"[EnemyExecutionSystem] Module '{definitions[i].name}' did not create an execution runtime.",
                            definitions[i]);
                    }
                }
                catch (Exception exception)
                {
                    DisposeModules(instance);
                    UnityEngine.Debug.LogError(
                        $"[EnemyExecutionSystem] Failed to create execution module '{definitions[i].name}': {exception.Message}",
                        definitions[i]);
                    return;
                }
            }

            _instances.Add(message.InstanceId, instance);
            EvaluateOnContextChanged(message.InstanceId, instance);
        }

        private void OnEnemyDespawned(EnemyDespawned message)
        {
            if (_disposed || !_instances.Remove(message.InstanceId, out InstanceModules instance))
                return;

            DisposeModules(instance);
        }

        private void OnStateChanged(EnemyStateChanged message)
        {
            if (_disposed || !_instances.TryGetValue(message.InstanceId, out InstanceModules instance))
                return;

            instance.ActiveStateId = message.CurrentStateId;
            EvaluateOnContextChanged(message.InstanceId, instance);
        }

        private void OnPerceptionChanged(EnemyPerceptionChanged message)
        {
            if (_disposed || !_instances.TryGetValue(message.EnemyId, out InstanceModules instance))
                return;

            instance.TargetEntityId = message.HasTarget ? message.TargetEntityId : null;
            instance.TargetPosition = message.HasTarget ? message.TargetPosition : UnityEngine.Vector2.zero;
            instance.TargetDistance = message.TargetDistance;
            instance.EnemyPosition = message.EnemyPosition;
            EvaluateOnContextChanged(message.EnemyId, instance);
        }

        private void EvaluateOnContextChanged(EnemyInstanceId id, InstanceModules instance)
        {
            EnemyExecutionContext context = instance.Snapshot(id);
            for (int i = 0; i < instance.Modules.Count; i++)
                Dispatch(instance.Modules[i].OnContextChanged(context));
        }

        private void Dispatch(EnemyExecutionIntent? intent)
        {
            if (!intent.HasValue) return;

            EnemyExecutionIntent command = intent.Value;
            switch (command.Kind)
            {
                case EnemyExecutionIntentKind.MoveToTarget:
                    _movePublisher.Publish(new EnemyMoveRequested(
                        command.InstanceId, command.TargetPosition, command.Value));
                    break;
                case EnemyExecutionIntentKind.Stop:
                    _stopPublisher.Publish(new EnemyStopRequested(command.InstanceId));
                    break;
                case EnemyExecutionIntentKind.AttackTarget:
                    _attackPublisher.Publish(new EnemyAttackRequested(
                        command.InstanceId,
                        command.TargetEntityId,
                        command.KnockbackDirection,
                        command.Value));
                    break;
            }
        }

        private static void DisposeModules(InstanceModules instance)
        {
            for (int i = 0; i < instance.Modules.Count; i++)
                instance.Modules[i].Dispose();
            instance.Modules.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();

            foreach (InstanceModules instance in _instances.Values)
                DisposeModules(instance);
            _instances.Clear();
        }
    }
}