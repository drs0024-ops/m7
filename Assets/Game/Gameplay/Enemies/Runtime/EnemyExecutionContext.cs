namespace Game.Gameplay.Enemies.Runtime
{
    public readonly struct EnemyExecutionContext
    {
        public EnemyInstanceId InstanceId { get; }
        public EnemyDefinition Definition { get; }
        public string ActiveStateId { get; }
        public int? TargetEntityId { get; }
        public UnityEngine.Vector2 TargetPosition { get; }
        public float TargetDistance { get; }
        public UnityEngine.Vector2 EnemyPosition { get; }
        public bool HasTarget => TargetEntityId.HasValue;

        public EnemyExecutionContext(
            EnemyInstanceId instanceId,
            EnemyDefinition definition,
            string activeStateId,
            int? targetEntityId,
            UnityEngine.Vector2 targetPosition,
            float targetDistance,
            UnityEngine.Vector2 enemyPosition = default)
        {
            Definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
            if (targetDistance < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(targetDistance));

            InstanceId = instanceId;
            ActiveStateId = activeStateId;
            TargetEntityId = targetEntityId;
            TargetPosition = targetPosition;
            TargetDistance = targetDistance;
            EnemyPosition = enemyPosition;
        }

    }
}