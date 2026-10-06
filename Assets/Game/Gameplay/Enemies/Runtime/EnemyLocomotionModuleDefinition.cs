namespace Game.Gameplay.Enemies.Runtime
{
    [UnityEngine.CreateAssetMenu(fileName = "EnemyLocomotionModule", menuName = "Game/Enemies/Modules/Locomotion")]
    public sealed class EnemyLocomotionModuleDefinition : EnemyModuleDefinition
    {
        [UnityEngine.SerializeField] private string _pursuitStateId = "chase";

        public override EnemyModuleKind Kind => EnemyModuleKind.Execution;
        public override IEnemyModuleRuntime CreateRuntime() => new EnemyLocomotionModuleRuntime(_pursuitStateId);
    }

    public sealed class EnemyLocomotionModuleRuntime : IEnemyExecutionModuleRuntime
    {
        private readonly string _pursuitStateId;
        private bool _isMoving;

        public EnemyLocomotionModuleRuntime(string pursuitStateId) => _pursuitStateId = pursuitStateId;

        public EnemyExecutionIntent? OnContextChanged(EnemyExecutionContext context)
        {
            if (context.ActiveStateId == _pursuitStateId && context.HasTarget)
            {
                _isMoving = true;
                return EnemyExecutionIntent.MoveToTarget(
                    context.InstanceId, context.TargetPosition, context.Definition.Stats.MoveSpeed);
            }

            if (!_isMoving) return null;
            _isMoving = false;
            return EnemyExecutionIntent.Stop(context.InstanceId);
        }

        public EnemyExecutionIntent? Tick(EnemyExecutionContext context, float deltaTime) => null;

        public void Dispose() { }

    }
}