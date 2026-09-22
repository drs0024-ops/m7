using UnityEngine;

namespace Game.Gameplay.Enemies
{
    
    
    public class EnemyChaseSOBase : ScriptableObject
    {
        protected Enemy enemy;
        protected Transform transform;
        protected GameObject gameObject;
        protected EnemyAttack Attack;

        protected Transform playerTransform;

        public virtual void Initialize(GameObject gameObject, Enemy enemy)
        {
            this.gameObject = gameObject;
            transform = gameObject.transform;
            this.enemy = enemy;
        }
        
        public virtual void DoEnterLogic() { }
        public virtual void DoExitLogic() { ResetValues(); }
        
        public virtual void DoUpdateLogic()
        {
            if (enemy.IsWithinStrickingDistance)
            {
                // Only find player once if not already set, or update if needed
                if (playerTransform == null)
                    playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
                    
                // enemy.root.Machine.Sequencer.RequestTansition(enemy.currentState, Attack);
            }
        }

        public virtual void DoFixedUpdateLogic()
        {
            // Fallback if no transform passed (legacy support)
            if (playerTransform != null)
                enemy.ChasePlayer(playerTransform.position);
            else
                enemy.ChasePlayer(playerTransform.position);
        }

        // Updated to accept player position directly
        public virtual void DoFixedUpdateLogic(Transform playerTransform)
        {
            if (playerTransform == null) return;

            // Calculate direction to player
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            
            // Move enemy towards player
            // Option 1: Simple move (adjust speed as needed)
            float moveSpeed = enemy.EnemyStatsBaseInstance.MaxRunSpeed; // Ensure Enemy has a MoveSpeed property
            transform.position += (Vector3)direction * moveSpeed * Time.fixedDeltaTime;

            // Option 2: If using Rigidbody2D (uncomment if applicable)
            // if (enemy.Rb != null) 
            // {
            //     enemy.Rb.velocity = direction * moveSpeed;
            // }

            // Optional: Flip sprite to face player
            // if (direction.x < 0) transform.localScale = new Vector3(-1, 1, 1);
            // else transform.localScale = new Vector3(1, 1, 1);
        }

        public virtual void DoUpdateLogic(Transform playerTransform)
        {
            if (playerTransform == null) return;

            // Do NOT set IsWithinStrikingDistance here - it's handled by collision
            // Let the collision event trigger the state transition
        }   

        public virtual void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType) { }
        public virtual void ResetValues() { }
    }    
}