using UnityEngine;
using VContainer;
using MessagePipe;
using Game.Core.Enums;
using Game.Core.Messages;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Pans the camera by a fixed offset when the player enters the trigger zone.
    /// Resets to zero on exit.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CameraPanTrigger : MonoBehaviour
    {
        #region Dependencies

        [SerializeField] private PanDirection _direction;
        [SerializeField] private float _distance = 3f;
        [SerializeField] private float _duration = 0.35f;
        [SerializeField] private LayerMask _playerLayer;

        private IPublisher<CameraPanRequest> _panPublisher;

        #endregion

        #region Public API

        [Inject]
        public void Construct(IPublisher<CameraPanRequest> panPublisher)
        {
            _panPublisher = panPublisher;
        }

        #endregion

        #region Internal

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            _panPublisher.Publish(new CameraPanRequest(GetOffset(), _duration));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            _panPublisher.Publish(new CameraPanRequest(Vector2.zero, _duration));
        }

        private bool IsPlayer(Collider2D other)
        {
            return (_playerLayer.value & (1 << other.gameObject.layer)) > 0;
        }

        private Vector2 GetOffset()
        {
            return _direction switch
            {
                PanDirection.Up => Vector2.up * _distance,
                PanDirection.Down => Vector2.down * _distance,
                PanDirection.Left => Vector2.left * _distance,
                PanDirection.Right => Vector2.right * _distance,
                _ => Vector2.zero
            };
        }

        #endregion
    }
}   