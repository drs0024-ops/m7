using UnityEngine;
using VContainer;
using MessagePipe;
using Game.Core.Messages;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Publishes a CameraShakeRequest when triggered externally (e.g., by combat, terrain impact).
    /// </summary>
    public class CameraShakeTrigger : MonoBehaviour
    {
        #region Dependencies

        [Tooltip("The shake profile to use.")]
        [SerializeField] private ScreenShakeProfile _profile;
        [Tooltip("Multiplies the profile's impact force.")]
        [SerializeField] private float _forceMultiplier = 1f;

        private IPublisher<CameraShakeRequest> _shakePublisher;

        #endregion

        #region Public API

        [Inject]
        public void Construct(IPublisher<CameraShakeRequest> shakePublisher)
        {
            _shakePublisher = shakePublisher;
        }

        /// <summary>
        /// Called by gameplay code to trigger a shake with this trigger's profile.
        /// </summary>
        public void TriggerShake()
        {
            _shakePublisher.Publish(new CameraShakeRequest(_profile, _forceMultiplier));
        }

        #endregion
    }
}   