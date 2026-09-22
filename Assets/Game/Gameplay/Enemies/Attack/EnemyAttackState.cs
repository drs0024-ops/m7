using Game.Core.StateMachine;

namespace Game.Gameplay.Enemies
{
    public class EnemyAttackState : EnemyBaseState
    {
        private readonly EnemyAttack _attack;
        private float _attackTimer;

        public EnemyAttackState(State parent, Enemy enemy, EnemyAttack attack)
            : base(parent, enemy)
        {
            _attack = attack;
        }

        protected override void OnEnter()
        {
            _attackTimer = 0f;
        }

        protected override void OnFixedUpdate(float deltaTime)
        {
            _attack.UpdateCooldown(deltaTime);

            if (_attack.CanAttack)
            {
                _attack.PerformAttack();
            }
        }

        protected override State GetTransition()
        {
            // Return to Grounded when attack window is over
            // (e.g., after N hits, or when player is out of range)
            //if (!Enemy.IsPlayerInAttackRange)
                //return ((EnemyRoot)Parent).Grounded;

            return null;
        }
    }
}   