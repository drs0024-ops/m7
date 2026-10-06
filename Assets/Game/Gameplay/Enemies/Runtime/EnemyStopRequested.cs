namespace Game.Gameplay.Enemies.Runtime
{
    public readonly struct EnemyStopRequested
    {
        public EnemyInstanceId InstanceId { get; }
        public EnemyStopRequested(EnemyInstanceId instanceId) => InstanceId = instanceId;
    }
}