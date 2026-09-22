using UnityEngine;

namespace Game.Gameplay.Enemies
{
    
    public class EnemyAttackSOBase : ScriptableObject
    {
        protected Enemy enemy;
        protected Transform transform;
        protected GameObject gameObject;

        protected Transform playerTransform;

        public virtual void Initialze(GameObject gameObject, Enemy enemy)
        {
            this.gameObject = gameObject;
            transform = gameObject.transform;
            this.enemy = enemy;
        }
        
        public virtual void DoEnterLogic () { }
        public virtual void DoExitLogic () { ResetValues(); }
        public virtual void DoUpdateLogic () { playerTransform = GameObject.FindGameObjectWithTag("Player").transform; }
        public virtual void DoFixedUpdateLogic () { }
        public virtual void DoAnimationTriggerEventLogic (Enemy.AnimationTriggerType triggerType) { }
        public virtual void ResetValues () { }
    }           
  
}

