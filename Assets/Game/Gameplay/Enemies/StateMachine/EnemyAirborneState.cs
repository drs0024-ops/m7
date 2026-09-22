using Game.Core.StateMachine;

namespace Game.Gameplay.Enemies
{
    public class EnemyAirborneState : EnemyBaseState
    {
        public EnemyAirborneState(State parent, Enemy enemy)
            : base(parent, enemy)
        {
        }

        protected override void OnFixedUpdate(float deltaTime)
        {
            //enemy.GravityOnDescending();
        }

        protected override State GetTransition()
        {
            //if (enemy.IsGrounded) return ((EnemyRoot)Parent).Grounded;
            //if (enemy.IsWithinStrikingDistance) return ((EnemyRoot)Parent).Attack;
            return null;
        }
    }
}   