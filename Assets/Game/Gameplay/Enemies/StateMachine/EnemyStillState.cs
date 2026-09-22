using Game.Core.StateMachine;

namespace Game.Gameplay.Enemies
{
    public class EnemyStillState : EnemyBaseState
    {
        public EnemyStillState(State parent, Enemy enemy)
            : base(parent, enemy)
        {
        }

        protected override void OnEnter()
        {
            enemy.SetAnimation("still");
        }

        protected override State GetTransition()
        {
            if (enemy.IsAggroed && !enemy.IsCoolingDown)
                return ((EnemyGroundedState)Parent).Chase;
            return ((EnemyGroundedState)Parent).Idle;
        }
    }
}   