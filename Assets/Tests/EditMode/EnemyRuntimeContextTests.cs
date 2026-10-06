using System.Reflection;
using Game.Core.Interfaces;
using Game.Gameplay.Enemies.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class EnemyRuntimeContextTests
    {
        private EnemyDefinition _definition;
        private EnemyStateGraphDefinition _graph;
        private FakePublisher<EnemyStateChanged> _stateChangedPublisher;
        private EnemyDecisionRuntime _decisionRuntime;
        private FakeSubscriber<EnemySpawned> _spawnedSubscriber;
        private FakeSubscriber<EnemyDespawned> _despawnedSubscriber;
        private FakeSubscriber<EnemyPerceptionChanged> _perceptionSubscriber;
        private EnemyRuntimeSystem _runtimeSystem;

        [SetUp]
        public void SetUp()
        {
            _graph = ScriptableObject.CreateInstance<EnemyStateGraphDefinition>();
            SetField(_graph, "_initialStateId", "root");
            SetField(_graph, "_states", new[]
            {
                MakeState("root", null, "idle"),
                MakeState("idle", "root", null),
                MakeState("chase", "root", null),
                MakeState("attack", "root", null)
            });
            SetField(_graph, "_transitions", new[]
            {
                MakeTransition("idle", "chase", EnemyConditionType.TargetAcquired),
                MakeTransition("idle", "attack", EnemyConditionType.Grounded),
                MakeTransition("chase", "idle", EnemyConditionType.TargetLost)
            });

            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            SetField(_definition, "_typeId", "test-enemy");
            SetField(_definition, "_stats", MakeStats(100f));
            SetField(_definition, "_stateGraph", _graph);
            SetField(_definition, "_modules", System.Array.Empty<EnemyModuleDefinition>());

            _stateChangedPublisher = new FakePublisher<EnemyStateChanged>();
            _decisionRuntime = new EnemyDecisionRuntime(_stateChangedPublisher);
            _spawnedSubscriber = new FakeSubscriber<EnemySpawned>();
            _despawnedSubscriber = new FakeSubscriber<EnemyDespawned>();
            _perceptionSubscriber = new FakeSubscriber<EnemyPerceptionChanged>();
            _runtimeSystem = new EnemyRuntimeSystem(
                System.Array.Empty<EnemyEntity>(),
                System.Array.Empty<EnemyPerceptionSensor>(),
                _spawnedSubscriber,
                _despawnedSubscriber,
                _perceptionSubscriber,
                _decisionRuntime);
            _runtimeSystem.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _runtimeSystem.Dispose();
            Object.DestroyImmediate(_definition);
            Object.DestroyImmediate(_graph);
        }

        [Test]
        public void Graph_ValidatesSeparateTransitionRecordsAndHierarchy()
        {
            Assert.That(_graph.TryValidate(out string error), Is.True, error);
            Assert.That(_graph.GetInitialLeafStateId(), Is.EqualTo("idle"));
        }

        [Test]
        public void RuntimeContexts_KeepMutableStatePerInstance()
        {
            var first = new EnemyRuntimeContext(new EnemyInstanceId(1), _definition);
            var second = new EnemyRuntimeContext(new EnemyInstanceId(2), _definition);

            first.ApplyDamage(25f);
            first.EnterState("chase");
            first.SetTarget(99, new Vector2(4f, 7f));
            first.SetTimer("attack", 2f);

            Assert.That(first.Definition, Is.SameAs(second.Definition));
            Assert.That(first.CurrentHealth, Is.EqualTo(75f));
            Assert.That(second.CurrentHealth, Is.EqualTo(100f));
            Assert.That(first.ActiveStateId, Is.EqualTo("chase"));
            Assert.That(second.ActiveStateId, Is.EqualTo("idle"));
            Assert.That(first.TargetEntityId, Is.EqualTo(99));
            Assert.That(second.TargetEntityId, Is.Null);
            Assert.That(first.GetTimer("attack"), Is.EqualTo(2f));
            Assert.That(second.GetTimer("attack"), Is.Zero);
        }

        [Test]
        public void Graph_RejectsTransitionsToUnknownStates()
        {
            SetField(_graph, "_transitions", new[]
            {
                MakeTransition("idle", "missing", EnemyConditionType.TargetAcquired)
            });

            Assert.That(_graph.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain("missing state"));
        }

        [Test]
        public void DecisionRuntime_UsesFirstMatchingTransitionInGraphOrder()
        {
            var context = new EnemyRuntimeContext(new EnemyInstanceId(1), _definition);
            context.SetTarget(99, new Vector2(4f, 7f));

            bool transitioned = _decisionRuntime.TryTransition(
                context,
                new EnemyConditionSnapshot(targetDistance: 1f, isGrounded: true, investigationComplete: false));

            Assert.That(transitioned, Is.True);
            Assert.That(context.ActiveStateId, Is.EqualTo("chase"));
            Assert.That(_stateChangedPublisher.Messages, Has.Count.EqualTo(1));
            Assert.That(_stateChangedPublisher.Messages[0].PreviousStateId, Is.EqualTo("idle"));
            Assert.That(_stateChangedPublisher.Messages[0].CurrentStateId, Is.EqualTo("chase"));
        }

        [Test]
        public void DecisionRuntime_DoesNotTransitionWhenNoConditionMatches()
        {
            var context = new EnemyRuntimeContext(new EnemyInstanceId(1), _definition);

            bool transitioned = _decisionRuntime.TryTransition(
                context,
                new EnemyConditionSnapshot(targetDistance: 20f, isGrounded: false, investigationComplete: false));

            Assert.That(transitioned, Is.False);
            Assert.That(context.ActiveStateId, Is.EqualTo("idle"));
            Assert.That(_stateChangedPublisher.Messages, Is.Empty);
        }

        [Test]
        public void DecisionRuntime_TransitionsOnlyTheProvidedInstance()
        {
            var first = new EnemyRuntimeContext(new EnemyInstanceId(1), _definition);
            var second = new EnemyRuntimeContext(new EnemyInstanceId(2), _definition);
            first.SetTarget(99, Vector2.zero);

            _decisionRuntime.TryTransition(first,
                new EnemyConditionSnapshot(1f, isGrounded: false, investigationComplete: false));
            _decisionRuntime.TryTransition(second,
                new EnemyConditionSnapshot(1f, isGrounded: false, investigationComplete: false));

            Assert.That(first.ActiveStateId, Is.EqualTo("chase"));
            Assert.That(second.ActiveStateId, Is.EqualTo("idle"));
            Assert.That(_stateChangedPublisher.Messages, Has.Count.EqualTo(1));
        }

        [Test]
        public void RuntimeSystem_PerceptionTransitionsSpawnedContextAndDespawnRemovesIt()
        {
            var instanceId = new EnemyInstanceId(7);
            _spawnedSubscriber.Publish(new EnemySpawned(instanceId, _definition, Vector2.zero));

            Assert.That(_runtimeSystem.ActiveContextCount, Is.EqualTo(1));

            _perceptionSubscriber.Publish(new EnemyPerceptionChanged(
                instanceId,
                hasTarget: true,
                targetEntityId: 42,
                targetPosition: new Vector2(3f, 2f),
                targetDistance: 1f));

            Assert.That(_stateChangedPublisher.Messages, Has.Count.EqualTo(1));
            Assert.That(_stateChangedPublisher.Messages[0].InstanceId, Is.EqualTo(instanceId));
            Assert.That(_stateChangedPublisher.Messages[0].CurrentStateId, Is.EqualTo("chase"));

            _despawnedSubscriber.Publish(new EnemyDespawned(instanceId));
            Assert.That(_runtimeSystem.ActiveContextCount, Is.Zero);

            _perceptionSubscriber.Publish(new EnemyPerceptionChanged(
                instanceId,
                hasTarget: false,
                targetEntityId: 0,
                targetPosition: Vector2.zero,
                targetDistance: 0f));
            Assert.That(_stateChangedPublisher.Messages, Has.Count.EqualTo(1));
        }

        [Test]
        public void LocomotionModule_RequestsMovementWhilePursuingAndStopsAfterLeavingPursuit()
        {
            var definition = ScriptableObject.CreateInstance<EnemyLocomotionModuleDefinition>();
            SetField(definition, "_pursuitStateId", "chase");
            var runtime = (IEnemyExecutionModuleRuntime)definition.CreateRuntime();
            var pursuing = new EnemyExecutionContext(
                new EnemyInstanceId(12), _definition, "chase", 42, new Vector2(5f, 2f), 2f);
            var idle = new EnemyExecutionContext(
                new EnemyInstanceId(12), _definition, "idle", null, Vector2.zero, 0f);

            EnemyExecutionIntent? move = runtime.OnContextChanged(pursuing);
            EnemyExecutionIntent? stop = runtime.OnContextChanged(idle);

            Assert.That(move.HasValue, Is.True);
            Assert.That(move.Value.Kind, Is.EqualTo(EnemyExecutionIntentKind.MoveToTarget));
            Assert.That(move.Value.TargetPosition, Is.EqualTo(new Vector2(5f, 2f)));
            Assert.That(move.Value.Value, Is.EqualTo(_definition.Stats.MoveSpeed));
            Assert.That(stop.HasValue, Is.True);
            Assert.That(stop.Value.Kind, Is.EqualTo(EnemyExecutionIntentKind.Stop));

            runtime.Dispose();
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void AttackModule_UsesDefinitionDamageRangeAndPerInstanceCooldown()
        {
            var definition = ScriptableObject.CreateInstance<EnemyAttackModuleDefinition>();
            SetField(definition, "_attackStateId", "attack");
            var runtime = (IEnemyExecutionModuleRuntime)definition.CreateRuntime();
            var inRange = new EnemyExecutionContext(
                new EnemyInstanceId(21), _definition, "attack", 42, new Vector2(1f, 0f), 1f);

            EnemyExecutionIntent? firstAttack = runtime.OnContextChanged(inRange);
            EnemyExecutionIntent? earlyRetry = runtime.Tick(inRange, 0.5f);
            EnemyExecutionIntent? cooldownRetry = runtime.Tick(inRange, 0.5f);

            Assert.That(firstAttack.HasValue, Is.True);
            Assert.That(firstAttack.Value.Kind, Is.EqualTo(EnemyExecutionIntentKind.AttackTarget));
            Assert.That(firstAttack.Value.TargetEntityId, Is.EqualTo(42));
            Assert.That(firstAttack.Value.Value, Is.EqualTo(_definition.Stats.AttackDamage));
            Assert.That(earlyRetry.HasValue, Is.False);
            Assert.That(cooldownRetry.HasValue, Is.True);

            runtime.Dispose();
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ExecutionAdapter_ResolvesAttackTargetByIdAndInvokesDamagable()
        {
            var entityObject = new GameObject("Enemy adapter test");
            entityObject.AddComponent<Rigidbody2D>();
            EnemyEntity entity = entityObject.AddComponent<EnemyEntity>();
            var registry = new DamagableRegistry();
            var target = new FakeDamagable();
            registry.Register(42, target);
 
            var moveSubscriber = new FakeSubscriber<EnemyMoveRequested>();
            var stopSubscriber = new FakeSubscriber<EnemyStopRequested>();
            var attackSubscriber = new FakeSubscriber<EnemyAttackRequested>();
            var adapter = new EnemyExecutionAdapterSystem(
                new[] { entity }, registry, moveSubscriber, stopSubscriber, attackSubscriber);

            try
            {
                ((VContainer.Unity.IStartable)adapter).Start();
                attackSubscriber.Publish(new EnemyAttackRequested(
                    entity.InstanceId, 42, Vector2.left, 7f));

                Assert.That(target.LastDamage, Is.EqualTo(7f));
                Assert.That(target.LastKnockbackDirection, Is.EqualTo(Vector3.left));
            }
            finally
            {
                adapter.Dispose();
                Object.DestroyImmediate(entityObject);
            }
        }

        private static EnemyStats MakeStats(
            float maxHealth,
            float moveSpeed = 3f,
            float attackRange = 2f,
            float attackDamage = 8f,
            float attackCooldown = 1f)
        {
            var stats = new EnemyStats();
            SetField(stats, "_maxHealth", maxHealth);
            SetField(stats, "_moveSpeed", moveSpeed);
            SetField(stats, "_attackRange", attackRange);
            SetField(stats, "_attackDamage", attackDamage);
            SetField(stats, "_attackCooldown", attackCooldown);
            return stats;
        }

        private static EnemyStateNodeDefinition MakeState(string id, string parentId, string initialChildId)
        {
            var state = new EnemyStateNodeDefinition();
            SetField(state, "_id", id);
            SetField(state, "_parentId", parentId);
            SetField(state, "_initialChildId", initialChildId);
            return state;
        }

        private static EnemyTransitionDefinition MakeTransition(
            string sourceId,
            string targetId,
            EnemyConditionType condition)
        {
            var transition = new EnemyTransitionDefinition();
            SetField(transition, "_sourceStateId", sourceId);
            SetField(transition, "_targetStateId", targetId);
            SetField(transition, "_condition", condition);
            return transition;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected serialized field '{name}'.");
            field.SetValue(target, value);
        }

        private sealed class FakeDamagable : IDamagable
        {
            public float LastDamage { get; private set; }
            public Vector3 LastKnockbackDirection { get; private set; }

            public void Damage(float amount, Vector3 knockbackDirection)
            {
                LastDamage = amount;
                LastKnockbackDirection = knockbackDirection;
            }

            public void Die() { }
        }
    }
}