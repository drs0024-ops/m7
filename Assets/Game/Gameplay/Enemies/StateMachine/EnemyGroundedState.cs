using Game.Core.StateMachine;

namespace Game.Gameplay.Enemies
{
    public class EnemyGroundedState : EnemyBaseState
    {
        public readonly EnemyStillState Still;
        public readonly EnemyIdleState Idle;
        public readonly EnemyChaseState Chase;
        public readonly EnemyInvestigateState Investigate;

        public EnemyGroundedState(State parent, Enemy enemy)
            : base(parent, enemy)
        {
            Still = new EnemyStillState(this, enemy);
            Idle = new EnemyIdleState(this, enemy);
            Chase = new EnemyChaseState(this, enemy);
            Investigate = new EnemyInvestigateState(this, enemy);
        }

        protected override State GetInitialState() => Still;

        protected override State GetTransition()
        {
            if (!enemy.IsGrounded) return ((EnemyRoot)Parent).Airborne;
            if (enemy.IsWithinStrikingDistance) return ((EnemyRoot)Parent).Attack;
            if (enemy.IsInvestigating) return Investigate;
            return null;
        }
    }
}   