using System;

namespace Game.Gameplay.Enemies.Runtime
{
    public readonly struct EnemyConditionSnapshot
    {
        public float TargetDistance { get; }
        public bool? IsGrounded { get; }
        public bool InvestigationComplete { get; }

        public EnemyConditionSnapshot(
            float targetDistance,
            bool? isGrounded = null,
            bool investigationComplete = false)
        {
            if (targetDistance < 0f)
                throw new ArgumentOutOfRangeException(nameof(targetDistance));

            TargetDistance = targetDistance;
            IsGrounded = isGrounded;
            InvestigationComplete = investigationComplete;
        }
    }
}