using UnityEngine;

namespace Game.Gameplay.Enemies
{
[CreateAssetMenu(fileName = "Chase-Direct Chase", menuName = "Enemy Logic/Chase Logic/Direct Chase")]


    public class EnemyChaseDirectToPlayer : EnemyChaseSOBase {
        private bool stop {get; set; } = false;

        //public override void Initialize(GameObject gameObject, Enemy enemy)
        //{
          //  base.Initialize(gameObject, enemy);
       // }

        public override void DoEnterLogic()
        {
            base.DoEnterLogic();
        }

        public override void DoUpdateLogic()
        {
            base.DoUpdateLogic();
            
        }

        public override void DoFixedUpdateLogic()
        {
            base.DoFixedUpdateLogic();
            if (!stop)
            {   
                stop = true;
            }
            // Pass the player transform directly
            enemy.ChasePlayer(enemy.PlayerTransform.position);
        }

        //public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
        //{
           // base.DoAnimationTriggerEventLogic(triggerType);
        //}

        public override void ResetValues()
        {
            base.ResetValues();
        }

        public override void DoExitLogic()
        {
            base.DoExitLogic();
        }
    }	
}

