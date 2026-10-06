namespace Game.Gameplay.Enemies.Runtime
{
    public readonly struct EnemyAttackRequested
    {
        public EnemyInstanceId InstanceId { get; }
        public int TargetEntityId { get; }
        public UnityEngine.Vector2 KnockbackDirection { get; }
        public float Damage { get; }

        public EnemyAttackRequested(
            EnemyInstanceId instanceId,
            int targetEntityId,
            UnityEngine.Vector2 knockbackDirection,
            float damage)
        {
            InstanceId = instanceId;
            TargetEntityId = targetEntityId;
            KnockbackDirection = knockbackDirection;
            Damage = damage;
        }

    }
}