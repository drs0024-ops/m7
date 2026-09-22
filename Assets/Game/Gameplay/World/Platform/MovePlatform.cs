using UnityEngine;

namespace Game.Gameplay.World
{
    public class MovePlatform : MonoBehaviour
    {
        [SerializeField] private Transform _platform;
        [SerializeField] private Transform _startingPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField] private float _speed = 1.5f;
        [SerializeField] private bool _stopWhenPlayerRides = false;

        private int _direction = 1;
        private bool _isMoving;
        private Transform _parentedPlayer;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;

            if (_stopWhenPlayerRides)
            {
                _isMoving = true;
                _parentedPlayer = collision.transform.root;
                _parentedPlayer.SetParent(transform);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (!_stopWhenPlayerRides) return;
            if (!collision.gameObject.CompareTag("Player")) return;
            if (_parentedPlayer != collision.transform.root) return;

            _parentedPlayer.SetParent(null);
            _parentedPlayer = null;
        }

        private void Update()
        {
            if (_stopWhenPlayerRides && _parentedPlayer != null) return;

            Vector2 target = CurrentMovementTarget();
            _platform.position = Vector2.Lerp(_platform.position, target, _speed * Time.deltaTime);

            if (Vector2.Distance(target, (Vector2)_platform.position) <= 0.1f)
                _direction *= -1;
        }

        private Vector2 CurrentMovementTarget()
        {
            return _direction == 1 ? _startingPoint.position : _endPoint.position;
        }

        private void OnDrawGizmos()
        {
            if (_platform == null || _startingPoint == null || _endPoint == null) return;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(_platform.position, _startingPoint.position);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_platform.position, _endPoint.position);
        }
    }
}   