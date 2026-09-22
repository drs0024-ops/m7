using UnityEngine;
using MessagePipe;
using Game.Core.Interfaces;
using Game.Core.Messages;
using Game.Gameplay.Camera;

namespace Game.Gameplay.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamagable
    {
        [SerializeField] private float _maxHealth = 3f;
        [SerializeField] private GameObject _collectiblePrefab;
        [SerializeField] private ScreenShakeProfile _shakeProfile;

        private float _currentHealth;
        // ❌ Removed: _damageFlash, _knockBack

        private IPublisher<EntityHealthChanged> _healthPublisher;
        private IPublisher<EntityDamaged> _damagePublisher;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsDead => _currentHealth <= 0f;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _healthPublisher = GlobalMessagePipe.GetPublisher<EntityHealthChanged>();
            _damagePublisher = GlobalMessagePipe.GetPublisher<EntityDamaged>();
        }

        public void Damage(float amount, Vector3 knockbackDirection)
        {
            if (IsDead) return;

            _currentHealth -= amount;
            _healthPublisher.Publish(new EntityHealthChanged(transform, _maxHealth, _currentHealth));
            _damagePublisher.Publish(new EntityDamaged(transform, amount, knockbackDirection));

            if (IsDead)
                Die();
        }

        void IDamagable.Die() => Die();

        private void Die()
        {
            // spawn collectible, shake, etc.
        }
    }
}   