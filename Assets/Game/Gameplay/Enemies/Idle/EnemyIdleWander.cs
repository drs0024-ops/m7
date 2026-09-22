using UnityEngine;

namespace Game.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "Idle-Wander", menuName = "Enemy Logic/Idle Logic/Wander")]

public class EnemyIdleWander : EnemyIdleSOBase
{
    //public override void Initialze(GameObject gameObject, Enemy enemy)
    //{
      //  base.Initialze(gameObject, enemy);
   // }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        //Debug.Log("IdleBaseSO DoOnEnterLogic");
    }

    public override void DoUpdateLogic()
    {
        base.DoUpdateLogic();
        enemy.MoveEnemy();
    }

    public override void DoFixedUpdateLogic()
    {
        base.DoFixedUpdateLogic();
        //enemy.WayPointWander();
        
        //enemy.currentVelocity.x = enemy.RB.linearVelocityX;

        //enemy.PatrollPauseAtWayPoint(5, 2);
        //Debug.Log("IDLE SO FIXED UPDATE DELTA TIME: " + Time.deltaTime);
    }

   // public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
   //{
   //     base.DoAnimationTriggerEventLogic(triggerType);
   // }

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

