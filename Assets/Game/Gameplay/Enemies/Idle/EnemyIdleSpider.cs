using UnityEngine;

namespace Game.Gameplay.Enemies
{
    
[CreateAssetMenu(fileName = "Idle-Stand Still", menuName = "Enemy Logic/Idle Logic/Stand Still")]
public class EnemyIdleSpider : EnemyIdleSOBase
{
    //public override void Initialze(GameObject gameObject, Enemy enemy)
    //{
    //    base.Initialze(gameObject, enemy);
    //}

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        Debug.Log("IdleBaseSO DoOnEnterLogic");
    }

    public override void DoUpdateLogic()
    {
        base.DoUpdateLogic();
        enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoFixedUpdateLogic()
    {
        base.DoFixedUpdateLogic();
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


