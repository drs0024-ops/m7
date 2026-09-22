using UnityEngine;
using VContainer;
using MessagePipe;
using Game.Core.Messages;
using Game.Core.Interfaces;
using VContainer.Unity;

namespace Game.Gameplay.Player
{
    public class PlayerHealth : MonoBehaviour, IDamagable, ISaveable, IStartable
    {
        [field: SerializeField] public float MaxHealth { get; set; } = 100f;
        public float CurrentHealth { get; private set; }
        public bool IsInvincible { get; set; }
        public bool IsDead => CurrentHealth <= 0f;

        [Inject] private ISaveableRegistry _registry;

        private IPublisher<PlayerDamaged> _damagedPublisher;
        private IPublisher<PlayerDied> _diedPublisher;
        private IPublisher<EntityHealthChanged> _healthChangedPublisher;
        private InputManager _inputManager;

        public string SaveId => "PlayerHealth";

        [Inject]
        private void Inject(InputManager inputManager)
        {
            _inputManager = inputManager;
        }

        void IStartable.Start()
        {
            _damagedPublisher = GlobalMessagePipe.GetPublisher<PlayerDamaged>();
            _diedPublisher = GlobalMessagePipe.GetPublisher<PlayerDied>();
            _healthChangedPublisher = GlobalMessagePipe.GetPublisher<EntityHealthChanged>();

            CurrentHealth = MaxHealth;
            _registry.Register(this);
        }

        public void Damage(float damageAmount, Vector3 hitDirection)
        {
            if (IsInvincible || IsDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageAmount);

            _healthChangedPublisher.Publish(new EntityHealthChanged(transform, MaxHealth, CurrentHealth));
            _damagedPublisher.Publish(new PlayerDamaged(damageAmount, hitDirection, _inputManager.Movement.x));

            if (IsDead)
                Die();
        }

        public void Die()
        {
            if (!IsDead) return;
            _diedPublisher.Publish(new PlayerDied(transform.position));
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            _healthChangedPublisher.Publish(new EntityHealthChanged(transform, MaxHealth, CurrentHealth));
        }

        public void ResetHealth()
        {
            IsInvincible = false;
            CurrentHealth = MaxHealth;
            _healthChangedPublisher.Publish(new EntityHealthChanged(transform, MaxHealth, CurrentHealth));
        }

        private void OnDestroy()
        {
            _registry?.Unregister(this);
        }

        #region ISaveable

        public ISaveData GetSaveData() => new PlayerHealthSaveData
        {
            SaveId = SaveId,
            CurrentHealth = CurrentHealth,
            IsInvincible = IsInvincible
        };

        public void LoadFromData(ISaveData data)
        {
            if (data is PlayerHealthSaveData d)
            {
                CurrentHealth = d.CurrentHealth;
                IsInvincible = d.IsInvincible;
            }
        }

        #endregion
    }
}   