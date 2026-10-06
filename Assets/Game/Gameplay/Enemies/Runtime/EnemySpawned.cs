using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    public readonly struct EnemySpawned
    {
        public EnemyInstanceId InstanceId { get; }
        public EnemyDefinition Definition { get; }
        public Vector2 Position { get; }

        public EnemySpawned(EnemyInstanceId instanceId, EnemyDefinition definition, Vector2 position)
        {
            InstanceId = instanceId;
            Definition = definition;
            Position = position;
        }
    }

    public readonly struct EnemyDespawned
    {
        public EnemyInstanceId InstanceId { get; }
        public EnemyDespawned(EnemyInstanceId instanceId) => InstanceId = instanceId;
    }

    public readonly struct EnemyStateChanged
    {
        public EnemyInstanceId InstanceId { get; }
        public string PreviousStateId { get; }
        public string CurrentStateId { get; }

        public EnemyStateChanged(EnemyInstanceId instanceId, string previousStateId, string currentStateId)
        {
            InstanceId = instanceId;
            PreviousStateId = previousStateId;
            CurrentStateId = currentStateId;
        }
    }
}