using Game.Core.Interfaces;
using UnityEngine;
using Game.Core.Messages;
using MessagePipe;
using VContainer;

namespace Game.Gameplay.Enemies.Runtime
{
    [DisallowMultipleComponent]
    public sealed class EnemyEntity : MonoBehaviour, IDamagable
    {
        [SerializeField] private EnemyDefinition _definition;

        private IPublisher<EnemySpawned> _spawnedPublisher;
        private IPublisher<EnemyDespawned> _despawnedPublisher;
        private EnemyRuntimeSystem _runtimeSystem;
        private bool _initialized;

        public EnemyDefinition Definition => _definition;
        public EnemyInstanceId InstanceId => new(GetInstanceID());

        public bool TryGetCurrentHealth(out float currentHealth)
        {
            if (_runtimeSystem != null)
                return _runtimeSystem.TryGetCurrentHealth(InstanceId, out currentHealth);

            currentHealth = 0f;
            return false;
        }

        [Inject]
        private void Inject(
            IPublisher<EnemySpawned> spawnedPublisher,
            IPublisher<EnemyDespawned> despawnedPublisher,
            EnemyRuntimeSystem runtimeSystem)
        {
            _spawnedPublisher = spawnedPublisher;
            _despawnedPublisher = despawnedPublisher;
            _runtimeSystem = runtimeSystem;
        }

        public void InitializeForRuntime()
        {
            if (_initialized) return;
            if (_definition == null)
            {
                Debug.LogError($"[EnemyEntity] Missing enemy definition on '{name}'.", this);
                return;
            }
            if (!_definition.TryValidate(out string error))
            {
                Debug.LogError($"[EnemyEntity] Invalid enemy definition on '{name}': {error}.", this);
                return;
            }

            _initialized = true;
            _spawnedPublisher.Publish(new EnemySpawned(InstanceId, _definition, transform.position));
        }

        public void Damage(float amount, Vector3 knockbackDirection)
        {
            if (_initialized && _runtimeSystem.ApplyDamage(InstanceId, amount))
                Destroy(gameObject);
        }

        public void Die()
        {
            if (_initialized && _runtimeSystem.Kill(InstanceId))
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (!_initialized || _despawnedPublisher == null) return;
            _despawnedPublisher.Publish(new EnemyDespawned(InstanceId));
        }
    }
}