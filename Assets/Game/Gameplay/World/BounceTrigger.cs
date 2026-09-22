
using UnityEngine;

namespace Game.Gameplay.World
{
    public class BounceTrigger : MonoBehaviour
    {
        private BouncePlatform _bouncePlatform;

        private void Awake()
        {
            _bouncePlatform = GetComponentInParent<BouncePlatform>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;
            _bouncePlatform?.OnPlayerEnteredBounceZone();
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;
            _bouncePlatform?.OnPlayerExitedBounceZone();
        }
    }
}   