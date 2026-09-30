using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Core;
using Game.Core.Interfaces;
using MessagePipe;
using Game.Core.Messages;

namespace Game.Gameplay.Player
{
    public class PlayerHealth : MonoBehaviour, IDamagable, ISaveable, IStartable
    {
        [Inject] private ISaveableRegistry _registry;
        private HealthModel _model;

        public float MaxHealth => _model.MaxHealth;
        public float CurrentHealth => _model.CurrentHealth;
        public bool IsInvincible
        {
            get => _model.IsInvincible;
            set => _model.IsInvincible = value;
        }
        public bool IsDead => _model.IsDead;
        public string SaveId => "PlayerHealth";

        [Inject]
        private void Inject(
            ISaveableRegistry registry,
            IPublisher<PlayerDamaged> damagedPub,
            IPublisher<PlayerDied> diedPub,
            IPublisher<EntityHealthChanged> healthChangedPub,
            IInputState input,
            LevelStateSnapshot snapshot)
        {
            _registry = registry;
            _model = new HealthModel(damagedPub, diedPub, healthChangedPub, input, snapshot);
            _model.SetTransform(transform);
        }

        void IStartable.Start()
        {
            _registry.Register(this);
        }

        public void Damage(float amount, Vector3 dir) => _model.Damage(amount, dir);
        public void Die() => _model.Die();
        public void Heal(float amount) => _model.Heal(amount);
        public void ResetHealth() => _model.ResetHealth();

        private void OnDestroy() => _registry?.Unregister(this);

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
                _model.RestoreState(d.CurrentHealth, d.IsInvincible);
            }
        }
    }
        #endregion
    
}   