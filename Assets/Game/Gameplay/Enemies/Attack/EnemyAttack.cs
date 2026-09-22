using UnityEngine;
using Game.Core.Interfaces;

namespace Game.Gameplay.Enemies
{
    public class EnemyAttack : MonoBehaviour
    {
        [SerializeField] private Transform attackTransform;
        [SerializeField] private EnemyAttackConfig config = new EnemyAttackConfig();
        [SerializeField] private Animator anim;

        private float _cooldownTimer;
        private IDamagable _target;

        public bool CanAttack => _cooldownTimer <= 0f;

        private void Awake()
        {
            _cooldownTimer = config.timeBetweenAttacks;
        }

        public void PerformAttack()
        {
            if (!CanAttack) return;

            _cooldownTimer = config.timeBetweenAttacks;

            anim?.SetTrigger("attack");

            RaycastHit2D hit = Physics2D.CircleCast(
                attackTransform.position,
                config.attackRange,
                transform.right,
                0f,
                config.attackableLayer
            );

            if (hit.collider != null)
            {
                _target = hit.collider.GetComponentInParent<IDamagable>();
                if (_target != null)
                    _target.Damage(config.damageAmount, transform.right);
            }
        }

        public void UpdateCooldown(float deltaTime)
        {
            _cooldownTimer -= deltaTime;
        }

        // Animation event callbacks
        public void OnAttackAnimStart() { }
        public void OnAttackAnimEnd() { }

        private void OnDrawGizmosSelected()
        {
            if (attackTransform == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackTransform.position, config.attackRange);
        }
    }
}   