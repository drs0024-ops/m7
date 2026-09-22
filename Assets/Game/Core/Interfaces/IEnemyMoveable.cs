
using UnityEngine;

namespace Game.Core.Interfaces
{
    public interface IEnemyMoveable
    {

        Rigidbody2D RB { get; set; }
        bool IsFacingRight { get; set;}
        void MoveEnemy(Vector2 velolicy);
        void CheckForLeftOrRightFacing(Vector2 velocity);
        
    }
}
