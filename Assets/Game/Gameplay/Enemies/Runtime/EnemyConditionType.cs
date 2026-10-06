namespace Game.Gameplay.Enemies.Runtime
{
    public enum EnemyConditionType
    {
        TargetAcquired,
        TargetLost,
        TargetInAttackRange,
        TargetOutOfAttackRange,
        AttackCooldownElapsed,
        HealthAtOrBelowThreshold,
        Grounded,
        Airborne,
        InvestigationComplete
    }
}