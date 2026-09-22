using UnityEngine;

namespace Game.Gameplay.World
{
    public class PassThroughPlatform : MonoBehaviour
    {
        [SerializeField] private Collider2D _platformCollider;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("NoBumpObject")) return;

            if (_platformCollider != null)
                Physics2D.IgnoreCollision(_platformCollider, collision, true);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.CompareTag("NoBumpObject")) return;

            if (_platformCollider != null)
                Physics2D.IgnoreCollision(_platformCollider, collision, false);
        }
    }
}   