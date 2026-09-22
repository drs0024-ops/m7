using Game.Gameplay.Enemies;
using Game.Gameplay.World;
using UnityEngine;
namespace ESM{
public class KnockBackTriggerCheck : MonoBehaviour
    {
        public GameObject PlayerTarget { get; set; }
        private Enemy _enemy;
        private CollumCollectible collumCollectible;

        private void Awake()
        {
            _enemy = GetComponentInParent<Enemy>();
            collumCollectible = GetComponentInParent<CollumCollectible>();

        }

        private void OnTriggerEnter2D(Collider2D collision) {
            
            if (collision.gameObject.CompareTag("Player"))
            {
                if (_enemy != null)
                {
                   _enemy.SetKnockBackCheck();  
                }
                
            }
        }




    } 

}
