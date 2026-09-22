using UnityEngine;
using Game.Core.Interfaces;

namespace Game.Gameplay.Enemies
{
    public class EnemyStrikingDistanceCheck : MonoBehaviour
{
    public GameObject PlayerTarget { get; set; }
    private Enemy _enemy;
    private float attackTimeCounter;
        private IDamagable iDamageable;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();

    }

    private void OnTriggerEnter2D(Collider2D collision) {
       
        if (collision.CompareTag("Player"))
        {
            _enemy.SetStrikingDistanceBool(true);
        }
    }


    void OnTriggerExit2D(Collider2D collision)
    {

        if (collision.gameObject.CompareTag("Player"))
        {
            _enemy.SetStrikingDistanceBool(false); 
        }
    }
}

    
}
