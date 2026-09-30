using UnityEngine;
using VContainer;
using MessagePipe;
using Game.Core.Enums;
using Game.Core.Messages;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Direction-aware camera switch trigger. Fires on player entry,
    /// selecting the target mode based on horizontal velocity.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CameraZoneTrigger : MonoBehaviour
    {
        #region Dependencies

        [Tooltip("Defines which camera mode to switch to based on entry direction.")]
        [SerializeField] private CameraSwitchDataSO _switchData;
        [SerializeField] private LayerMask _playerLayer;

        private IPublisher<CameraSwitchRequest> _switchPublisher;

        #endregion

        #region Public API

        [Inject]
        public void Construct(IPublisher<CameraSwitchRequest> switchPublisher)
        {
            _switchPublisher = switchPublisher;
        }

        #endregion

        #region Internal

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;

            float dir = other.attachedRigidbody?.linearVelocity.x ?? 1f;
            if (Mathf.Approximately(dir, 0f)) dir = 1f;

            CameraMode mode = dir > 0 ? _switchData.cameraOnRight : _switchData.cameraOnLeft;
            _switchPublisher.Publish(new CameraSwitchRequest(mode));
        }

        private bool IsPlayer(Collider2D other)
        {
            return (_playerLayer.value & (1 << other.gameObject.layer)) > 0;
        }

        #endregion
    }
}   