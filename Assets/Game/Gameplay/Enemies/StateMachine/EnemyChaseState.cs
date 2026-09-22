using Game.Core.StateMachine;
namespace Game.Gameplay.Enemies
{
    public class EnemyChaseState : EnemyBaseState
    {
        public EnemyChaseState(State parent, Enemy enemy)
            : base(parent, enemy)
        {
        }

        protected override void OnEnter()
        {
            enemy.IsChasing = true;
            enemy.SetAnimation("chase");
        }

        protected override void OnExit()
        {
            enemy.IsChasing = false;
        }

        protected override void OnFixedUpdate(float deltaTime)
        {
            enemy.ChasePlayer(enemy.PlayerTransform, deltaTime);
        }

        protected override State GetTransition()
        {
            if (!enemy.IsAggroed)
                return ((EnemyGroundedState)Parent).Idle;

            if (enemy.IsCoolingDown || enemy.OnWall)
                return ((EnemyGroundedState)Parent).Idle;

            if (enemy.IsWithinStrikingDistance)
                return ((EnemyRoot)Parent).Attack;

            return null;
        }
    }
}   