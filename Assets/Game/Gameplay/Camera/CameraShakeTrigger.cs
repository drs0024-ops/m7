using UnityEngine;
using VContainer;
using MessagePipe;

namespace Game.Gameplay.Camera
{
    public class CameraShakeTrigger : MonoBehaviour
    {
        [SerializeField] private ScreenShakeProfile _profile;
        [SerializeField] private float _forceMultiplier = 1f;

        private IPublisher<CameraShakeRequest> _shakePublisher;

        [Inject]
        public void Construct(IPublisher<CameraShakeRequest> shakePublisher)
        {
            _shakePublisher = shakePublisher;
        }

        public void TriggerShake()
        {
            _shakePublisher.Publish(new CameraShakeRequest(_profile, _forceMultiplier));
        }
    }
}   