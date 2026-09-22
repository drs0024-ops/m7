using UnityEngine;

namespace Game.Gameplay.Enemies
{
    public class EnemyAttackStandard
    {
        private readonly Enemy _enemy;
        private Transform _playerTransform;
        private float _cooldownTimer;
        private readonly float _cooldown;
        private readonly float _range;
        private readonly float _damage;

        public EnemyAttackStandard(Enemy enemy, float cooldown, float range, float damage)
        {
            _enemy = enemy;
            _cooldown = cooldown;
            _range = range;
            _damage = damage;
        }

        public void Initialize(Transform playerTransform)
        {
            _playerTransform = playerTransform;
            _cooldownTimer = _cooldown;
        }

        public void Enter()
        {
            _cooldownTimer = 0f;
        }

        public void Exit()
        {
            _cooldownTimer = _cooldown;
        }

        public void Update()
        {
            if (_playerTransform == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                _playerTransform = go != null ? go.transform : null;
            }
        }

        public void FixedUpdate(float deltaTime)
        {
            _cooldownTimer -= deltaTime;

            if (_playerTransform == null) return;

            _enemy.ChasePlayer(_playerTransform.position);

            //if (_enemy.IsPlayerInAttackRange(_range))
                //_enemy.Attack(_damage, transform.right);
        }
    }
}   