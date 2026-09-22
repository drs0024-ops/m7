
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "EnemyBaseSO", menuName = "Scriptable Objects/EnemyBaseSO")]
    public class EnemyBaseSO : ScriptableObject
    {
        [Header("Enemy Stats")]
        public string enemyName;
        public string description;
        public int health = 100;
        public float speed = 2f;
        public float detectRange = 10f;
        public int damage = 10;
        public GameObject enemyModel;

        [Header("Collision Checks")]
        public Collider2D bodyCollider;
        public Collider2D feetCollider;

        [Header("References")]
        public Animator animator;
        public PlayerStateDriverShell player;

    }
    
}

    
