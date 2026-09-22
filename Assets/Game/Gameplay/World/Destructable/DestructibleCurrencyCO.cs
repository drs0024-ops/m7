using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay.World
{
    public class DestructibleCurrencyCO : MonoBehaviour, IDamagable
    {
        [SerializeField] private CollectableSOBase _collectable;
        [SerializeField] private float _maxHealth = 1f;

        private float _currentHealth;
        private IPublisher<EntityDamaged> _damagePublisher;

        public bool IsDead => _currentHealth <= 0f;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _damagePublisher = GlobalMessagePipe.GetPublisher<EntityDamaged>();
        }

        public void Damage(float amount, Vector3 knockbackDirection)
        {
            if (IsDead) return;

            _currentHealth -= amount;
            _damagePublisher.Publish(new EntityDamaged(transform, amount, knockbackDirection));

            if (IsDead)
                Die();
        }

        void IDamagable.Die() => Die();

        private void Die()
        {
            _collectable.Collect(gameObject);
            Destroy(gameObject);
        }
    }
}   