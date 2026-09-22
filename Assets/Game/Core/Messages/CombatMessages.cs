
using UnityEngine;

public class CombatMessages  {}
namespace Game.Core.Messages
{
    public readonly struct EntityDamaged
    {
        public readonly Transform Target;
        public readonly float DamageAmount;
        public readonly Vector2 KnockbackDirection;

        public EntityDamaged(Transform target, float damageAmount, Vector2 knockbackDirection)
        {
            Target = target;
            DamageAmount = damageAmount;
            KnockbackDirection = knockbackDirection;
        }
    }

    public readonly struct EntityHealthChanged
    {
        public readonly Transform Target;
        public readonly float MaxHealth;
        public readonly float CurrentHealth;

        public EntityHealthChanged(Transform target, float maxHealth, float currentHealth)
        {
            Target = target;
            MaxHealth = maxHealth;
            CurrentHealth = currentHealth;
        }
    }

    public readonly struct KnockBackStarted
    {
        public readonly UnityEngine.Transform Target;
        public KnockBackStarted(UnityEngine.Transform target) => Target = target;
    }

    public readonly struct KnockBackEnded
    {
        public readonly UnityEngine.Transform Target;
        public KnockBackEnded(UnityEngine.Transform target) => Target = target;
    }
}   