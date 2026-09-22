using UnityEngine;

namespace Game.Gameplay.World
{
    public class MoveOnTouchPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 _velocity;
        [SerializeField] private bool _stopWhenPlayerExits = true;

        private bool _isMoving;
        private Transform _parentedPlayer;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;

            _isMoving = true;
            _parentedPlayer = collision.transform.root;
            _parentedPlayer.SetParent(transform);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;
            if (_parentedPlayer != collision.transform.root) return;

            _parentedPlayer.SetParent(null);
            _parentedPlayer = null;

            if (_stopWhenPlayerExits)
                _isMoving = false;
        }

        private void FixedUpdate()
        {
            if (!_isMoving) return;
            transform.position += _velocity * Time.fixedDeltaTime;
        }
    }
}   