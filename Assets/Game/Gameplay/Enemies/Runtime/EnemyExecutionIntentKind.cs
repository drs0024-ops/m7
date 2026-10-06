namespace Game.Gameplay.Enemies.Runtime
{
    public enum EnemyExecutionIntentKind
    {
        MoveToTarget,
        Stop,
        AttackTarget
    }

    public readonly struct EnemyExecutionIntent
    {
        public EnemyExecutionIntentKind Kind { get; }
        public EnemyInstanceId InstanceId { get; }
        public int TargetEntityId { get; }
        public UnityEngine.Vector2 TargetPosition { get; }
        public UnityEngine.Vector2 KnockbackDirection { get; }
        public float Value { get; }

        private EnemyExecutionIntent(
            EnemyExecutionIntentKind kind,
            EnemyInstanceId instanceId,
            int targetEntityId,
            UnityEngine.Vector2 targetPosition,
            UnityEngine.Vector2 knockbackDirection,
            float value)
        {
            Kind = kind;
            InstanceId = instanceId;
            TargetEntityId = targetEntityId;
            TargetPosition = targetPosition;
            KnockbackDirection = knockbackDirection;
            Value = value;
        }

        public static EnemyExecutionIntent MoveToTarget(EnemyInstanceId id, UnityEngine.Vector2 position, float speed) =>
            new(EnemyExecutionIntentKind.MoveToTarget, id, 0, position, UnityEngine.Vector2.zero, speed);

        public static EnemyExecutionIntent Stop(EnemyInstanceId id) =>
            new(EnemyExecutionIntentKind.Stop, id, 0, UnityEngine.Vector2.zero, UnityEngine.Vector2.zero, 0f);

        public static EnemyExecutionIntent AttackTarget(
            EnemyInstanceId id,
            int targetId,
            UnityEngine.Vector2 knockbackDirection,
            float damage) =>
            new(EnemyExecutionIntentKind.AttackTarget, id, targetId, UnityEngine.Vector2.zero, knockbackDirection, damage);

    }
}