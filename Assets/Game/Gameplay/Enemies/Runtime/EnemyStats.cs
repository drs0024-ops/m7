using System;
using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    [Serializable]
    public sealed class EnemyStats
    {
        [SerializeField, Min(0f)] private float _maxHealth;
        [SerializeField, Min(0f)] private float _moveSpeed;
        [SerializeField, Min(0f)] private float _perceptionRange;
        [SerializeField, Min(0f)] private float _attackRange;
        [SerializeField, Min(0f)] private float _attackDamage;
        [SerializeField, Min(0f)] private float _attackCooldown;

        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float PerceptionRange => _perceptionRange;
        public float AttackRange => _attackRange;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;

        public bool TryValidate(out string error)
        {
            if (_maxHealth <= 0f) return Fail("Max health must be greater than zero.", out error);
            if (_moveSpeed < 0f) return Fail("Move speed cannot be negative.", out error);
            if (_perceptionRange < 0f) return Fail("Perception range cannot be negative.", out error);
            if (_attackRange < 0f) return Fail("Attack range cannot be negative.", out error);
            if (_attackDamage < 0f) return Fail("Attack damage cannot be negative.", out error);
            if (_attackCooldown < 0f) return Fail("Attack cooldown cannot be negative.", out error);

            error = null;
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}