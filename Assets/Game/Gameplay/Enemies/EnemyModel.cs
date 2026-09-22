using UnityEngine;
using Game.Core.Interfaces;

namespace Game.Gameplay.Enemies
{
    
    public class EnemyModel : MonoBehaviour, ITriggerCheckable
    {
        private Enemy enemy;
        public Animator animator;

        public bool ShouldBeDamaging {get; private set;} = false;


        public bool IsAggroed { get; set; }
        public bool IsWithinStrickingDistance { get; set;}

        void Start()
        {
            enemy = GetComponentInParent<Enemy>();
            animator = GetComponent<Animator>();
        }

        public void SetAggroStatus(bool isAggored)
        {
            IsAggroed = isAggored;
            // call animation for chase
        }

        public void SetStrikingDistanceBool(bool isWithinStrikingDistance)
        {
            IsWithinStrickingDistance = isWithinStrikingDistance;
            // call animation for attack
        }


        #region Animation Triggers

        public void ShouldBeDamagingToTrue () 
        {
            ShouldBeDamaging = true;
            //enemy.ShouldBeAttacking = true;
            
        }

        public void ShouldBeDamagingToFalse () 
        {
            ShouldBeDamaging = false;
            //enemy.ShouldBeAttacking = false;
        }

        #endregion
    }
    

}


