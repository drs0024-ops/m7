using UnityEngine;
using Game.Core;
using Game.Core.Messages;
using Game.Core.Interfaces;
using MessagePipe;
using Game.Gameplay.Upgrades;

namespace Game.Gameplay.Player
{
    public class HealthModel : IPlayerHealth
    {
        public float MaxHealth { get; set; } = 100f;
        public float CurrentHealth { get; private set; }
        public bool IsInvincible { get; set; }
        public bool IsDead => CurrentHealth <= 0f;

        private readonly IPublisher<PlayerDamaged> _damagedPub;
        private readonly IPublisher<PlayerDied> _diedPub;
        private readonly IPublisher<EntityHealthChanged> _healthChangedPub;
        private readonly IPublisher<InvisibilityInterrupted> _invisibilityInterruptedPublisher;
        private readonly float _invisibilityInterruptionDrainPercent;
        private readonly IInputState _input;
        private Transform _transform;

        public HealthModel(
            IPublisher<PlayerDamaged> damagedPub,
            IPublisher<PlayerDied> diedPub,
            IPublisher<EntityHealthChanged> healthChangedPub,
            IInputState input,
            LevelStateSnapshot snapshot,
            IPublisher<InvisibilityInterrupted> invisibilityInterruptedPublisher,
            InvisibilityTuning invisibilityTuning)
        {
            _damagedPub = damagedPub;
            _diedPub = diedPub;
            _healthChangedPub = healthChangedPub;
            _invisibilityInterruptedPublisher = invisibilityInterruptedPublisher
                ?? throw new System.ArgumentNullException(nameof(invisibilityInterruptedPublisher));
            _invisibilityInterruptionDrainPercent = (invisibilityTuning
                ?? throw new System.ArgumentNullException(nameof(invisibilityTuning))).InterruptionDrainPercent;
            _input = input;

            if (snapshot.PlayerHealth > 0f)
                MaxHealth = snapshot.PlayerHealth;

            CurrentHealth = MaxHealth;
        }

        public void SetTransform(Transform t) => _transform = t;

        public void Damage(float amount, Vector3 hitDirection)
        {
            if (IsInvincible || IsDead || amount <= 0f) return;
            if (_transform == null)
            {
                Debug.LogWarning("[Phase2 Verify] HealthModel.Damage ignored: transform is null.");
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            _healthChangedPub.Publish(new EntityHealthChanged(_transform, MaxHealth, CurrentHealth));
            _damagedPub.Publish(new PlayerDamaged(amount, hitDirection, _input.Movement.x));
            _invisibilityInterruptedPublisher.Publish(
                new InvisibilityInterrupted(_invisibilityInterruptionDrainPercent));

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
            if (_transform == null)
            {
                Debug.LogWarning("[Phase2 Verify] HealthModel.Die ignored: transform is null.");
                return;
            }
            _diedPub.Publish(new PlayerDied(_transform.position));
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            if (_transform == null)
            {
                Debug.LogWarning("[Phase2 Verify] HealthModel.Heal ignored: transform is null.");
                return;
            }
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            _healthChangedPub.Publish(new EntityHealthChanged(_transform, MaxHealth, CurrentHealth));
        }

        public void ResetHealth()
        {
            if (_transform == null)
            {
                Debug.LogWarning("[Phase2 Verify] HealthModel.ResetHealth ignored: transform is null.");
                return;
            }
            IsInvincible = false;
            CurrentHealth = MaxHealth;
            _healthChangedPub.Publish(new EntityHealthChanged(_transform, MaxHealth, CurrentHealth));
        }
    }
}   