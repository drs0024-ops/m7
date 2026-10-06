namespace Game.Gameplay.Enemies.Runtime
{
    public readonly struct EnemyMoveRequested
    {
        public EnemyInstanceId InstanceId { get; }
        public UnityEngine.Vector2 TargetPosition { get; }
        public float Speed { get; }

        public EnemyMoveRequested(EnemyInstanceId instanceId, UnityEngine.Vector2 targetPosition, float speed)
        {
            InstanceId = instanceId;
            TargetPosition = targetPosition;
            Speed = speed;
        }

    }
}