using Game.Core.StateMachine;

namespace Game.Gameplay.Enemies
{
    public class EnemyIdleState : EnemyBaseState
    {
        public EnemyIdleState(State parent, Enemy enemy)
            : base(parent, enemy)
        {
        }

        protected override void OnEnter()
        {
            enemy.SetAnimation("idle");
        }

        protected override void OnUpdate(float deltaTime)
        {
            // Idle has no per-frame logic unless you add detection range checks here
        }

        protected override State GetTransition()
        {
            if (enemy.IsAggroed && !enemy.IsCoolingDown && !enemy.OnWall)
                return ((EnemyGroundedState)Parent).Chase;
            return null;
        }
    }
}   