using UnityEngine;
using Game.Core;
using Game.Core.Messages;
using Game.Core.Interfaces;
using MessagePipe;

namespace Game.Gameplay.Player
{
    public class HealthModel
    {
        public float MaxHealth { get; set; } = 100f;
        public float CurrentHealth { get; private set; }
        public bool IsInvincible { get; set; }
        public bool IsDead => CurrentHealth <= 0f;

        private readonly IPublisher<PlayerDamaged> _damagedPub;
        private readonly IPublisher<PlayerDied> _diedPub;
        private readonly IPublisher<EntityHealthChanged> _healthChangedPub;
        private readonly IInputState _input;
        private Transform _transform;

        public HealthModel(
            IPublisher<PlayerDamaged> damagedPub,
            IPublisher<PlayerDied> diedPub,
            IPublisher<EntityHealthChanged> healthChangedPub,
            IInputState input,
            LevelStateSnapshot snapshot)
        {
            _damagedPub = damagedPub;
            _diedPub = diedPub;
            _healthChangedPub = healthChangedPub;
            _input = input;

            if (snapshot.PlayerHealth > 0f)
                MaxHealth = snapshot.PlayerHealth;

            CurrentHealth = MaxHealth;
        }

        public void SetTransform(Transform t) => _transform = t;

        public void Damage(float amount, Vector3 hitDirection)
        {
            if (IsInvincible || IsDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            _healthChangedPub.Publish(new EntityHealthChanged(_transform, MaxHealth, CurrentHealth));
            _damagedPub.Publish(new PlayerDamaged(amount, hitDirection, _input.Movement.x));

            if (IsDead) Die();
        }

        public void RestoreState(float health, bool invincible)
        {
            CurrentHealth = Mathf.Clamp(health, 0f, MaxHealth);
            IsInvincible = invincible;
        }

        public void Die()
        {
            if (!IsDead) return;
            _diedPub.Publish(new PlayerDied(_transform.position));
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            _healthChangedPub.Publish(new EntityHealthChanged(_transform, MaxHealth, CurrentHealth));
        }

        public void ResetHealth()
        {
            IsInvincible = false;
            CurrentHealth = MaxHealth;
            _healthChangedPub.Publish(new EntityHealthChanged(_transform, MaxHealth, CurrentHealth));
        }
    }
}   