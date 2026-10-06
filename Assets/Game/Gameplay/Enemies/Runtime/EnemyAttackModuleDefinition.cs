using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    [CreateAssetMenu(fileName = "EnemyAttackModule", menuName = "Game/Enemies/Modules/Attack")]
    public sealed class EnemyAttackModuleDefinition : EnemyModuleDefinition
    {
        [SerializeField] private string _attackStateId = "attack";

        public override EnemyModuleKind Kind => EnemyModuleKind.Execution;
        public override IEnemyModuleRuntime CreateRuntime() => new EnemyAttackModuleRuntime(_attackStateId);
    }

    public sealed class EnemyAttackModuleRuntime : IEnemyExecutionModuleRuntime
    {
        private readonly string _attackStateId;
        private float _cooldownRemaining;
        private bool _disposed;

        public EnemyAttackModuleRuntime(string attackStateId) => _attackStateId = attackStateId;

        public EnemyExecutionIntent? OnContextChanged(EnemyExecutionContext context) => TryAttack(context);

        public EnemyExecutionIntent? Tick(EnemyExecutionContext context, float deltaTime)
        {
            if (_disposed) return null;
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Mathf.Max(0f, deltaTime));
            return TryAttack(context);
        }

        public void Dispose() => _disposed = true;

        private EnemyExecutionIntent? TryAttack(EnemyExecutionContext context)
        {
            if (_disposed
                || context.ActiveStateId != _attackStateId
                || !context.TargetEntityId.HasValue
                || context.TargetDistance > context.Definition.Stats.AttackRange
                || _cooldownRemaining > 0f)
                return null;

            EnemyStats stats = context.Definition.Stats;
            _cooldownRemaining = stats.AttackCooldown;
            UnityEngine.Vector2 direction = (context.TargetPosition - context.EnemyPosition).normalized;
            if (direction == UnityEngine.Vector2.zero)
                direction = UnityEngine.Vector2.right;
            return EnemyExecutionIntent.AttackTarget(
                context.InstanceId, context.TargetEntityId.Value, direction, stats.AttackDamage);
        }
    }
}