using UnityEngine;
using VContainer;
using MessagePipe;
using Game.Core.Enums;
using Game.Core.Messages;

namespace Game.Gameplay.Camera
{
    [RequireComponent(typeof(Collider2D))]
    public class CameraZoneTrigger : MonoBehaviour
    {
        [SerializeField] private CameraSwitchDataSO _switchData;
        [SerializeField] private LayerMask _playerLayer;

        private IPublisher<CameraSwitchRequest> _switchPublisher;

        [Inject]
        public void Construct(IPublisher<CameraSwitchRequest> switchPublisher)
        {
            _switchPublisher = switchPublisher;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!((_playerLayer.value & (1 << other.gameObject.layer)) > 0)) return;

            float direction = other.attachedRigidbody?.linearVelocity.x ?? 1f;
            if (Mathf.Approximately(direction, 0f)) direction = 1f;

            CameraMode mode = direction > 0 ? _switchData.cameraOnRight : _switchData.cameraOnLeft;

            _switchPublisher.Publish(new CameraSwitchRequest(mode, new Vector2(direction, 0f)));
        }
    }
}   