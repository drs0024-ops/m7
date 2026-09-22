using UnityEngine;

namespace Game.Gameplay.Enemies
{
    [System.Serializable]
    public class EnemyAttackConfig
    {
        public float attackRange = 1.5f;
        public LayerMask attackableLayer;
        public float damageAmount = 1f;
        public float timeBetweenAttacks = 0.15f;
    }
}