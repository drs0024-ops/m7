using Game.Core.StateMachine;

namespace Game.Gameplay.Enemies
{
    public class EnemyRoot : EnemyBaseState
    {
        public readonly EnemyGroundedState Grounded;
        public readonly EnemyAirborneState Airborne;
        public readonly EnemyAttackState Attack;

        public EnemyRoot(Enemy enemy)
            : base(null, enemy)
        {
            Grounded = new EnemyGroundedState(this, enemy);
            Airborne = new EnemyAirborneState(this, enemy);
            Attack = new EnemyAttackState(this, enemy, enemy.AttackComponent);
        }

        protected override State GetInitialState() => Grounded;

        protected override State GetTransition()
        {
            // Root only handles the Grounded ↔ Airborne split.
            // Attack exits are handled by EnemyAttackState itself.
            if (Leaf() is EnemyAttackState) return null;

            return enemy.IsGrounded ? Grounded : Airborne;
        }
    }
}   