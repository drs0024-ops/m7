using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    public readonly struct EnemyPerceptionChanged
    {
        public EnemyInstanceId EnemyId { get; }
        public bool HasTarget { get; }
        public int TargetEntityId { get; }
        public Vector2 TargetPosition { get; }
        public float TargetDistance { get; }
        public Vector2 EnemyPosition { get; }

        public EnemyPerceptionChanged(
            EnemyInstanceId enemyId,
            bool hasTarget,
            int targetEntityId,
            Vector2 targetPosition,
            float targetDistance,
            Vector2 enemyPosition = default)
        {
            EnemyId = enemyId;
            HasTarget = hasTarget;
            TargetEntityId = targetEntityId;
            TargetPosition = targetPosition;
            TargetDistance = targetDistance;
            EnemyPosition = enemyPosition;
        }
    }
}