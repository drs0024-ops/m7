using Game.Gameplay.World;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    public class EnemyAggroCheck : MonoBehaviour
    {
        public GameObject PlayerTarget { get; set; }
        private Enemy _enemy;
        private CollumCollectible collumCollectible;
        private FallingPlatform fallingPlatform;

        private void Awake()
        {
            _enemy = GetComponentInParent<Enemy>();
            collumCollectible = GetComponentInParent<CollumCollectible>();
            fallingPlatform = GetComponentInParent<FallingPlatform>();

        }

        private void OnTriggerEnter2D(Collider2D collision) {
            
            if (collision.gameObject.CompareTag("Player"))
            {

                Debug.Log("Player collided with Aggroed Check");
                if (_enemy != null)
                {
                   _enemy.SetAggroStatus(true);  
                }
                
                if (collumCollectible != null)
                {
                    Debug.Log("Player Aggroed");
                    collumCollectible.SetAggroStatus(true); 
                }

                if (fallingPlatform != null)
                {
                    Debug.Log("FallingPlatform Found");
                    fallingPlatform.SetAggroStatus(true);
                }
            }
        }


        void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                if (_enemy != null)
                {
                   _enemy.SetAggroStatus(false);  
                }
                
                if (collumCollectible != null)
                {
                   collumCollectible.SetAggroStatus(false); 
                }

                if (fallingPlatform != null)
                {
                    fallingPlatform.SetAggroStatus(false);
                }
                
            }
        }
    }   
    
}
    

