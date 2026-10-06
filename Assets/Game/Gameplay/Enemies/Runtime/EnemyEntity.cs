using UnityEngine;
using Game.Core.Messages;
using MessagePipe;
using VContainer;

namespace Game.Gameplay.Enemies.Runtime
{
    [DisallowMultipleComponent]
    public sealed class EnemyEntity : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition _definition;

        private IPublisher<EnemySpawned> _spawnedPublisher;
        private IPublisher<EnemyDespawned> _despawnedPublisher;
        private bool _initialized;

        public EnemyDefinition Definition => _definition;
        public EnemyInstanceId InstanceId => new(GetInstanceID());

        [Inject]
        private void Inject(
            IPublisher<EnemySpawned> spawnedPublisher,
            IPublisher<EnemyDespawned> despawnedPublisher)
        {
            _spawnedPublisher = spawnedPublisher;
            _despawnedPublisher = despawnedPublisher;
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

        private void OnDestroy()
        {
            if (!_initialized || _despawnedPublisher == null) return;
            _despawnedPublisher.Publish(new EnemyDespawned(InstanceId));
        }
    }
}