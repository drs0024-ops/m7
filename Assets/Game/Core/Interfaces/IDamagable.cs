using UnityEngine;

namespace Game.Core.Interfaces
{
    public interface IDamagable
    {
        void Damage(float amount, Vector3 knockbackDirection);
        void Die();
    }
}