using UnityEngine;

namespace Game.Gameplay.Parallax
{
    [DefaultExecutionOrder(100)]
    [ExecuteInEditMode]
    public class ParallaxCamera : MonoBehaviour
    {
        public delegate void ParallaxCameraDelegate(Vector3 deltaMovement);
        public ParallaxCameraDelegate onCameraTranslate;

        [SerializeField] private bool _useParallax;

        private Vector3 _oldPosition;

        private void Start()
        {
            _oldPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (!_useParallax) return;

            Vector3 currentPosition = transform.position;
            if (currentPosition != _oldPosition)
            {
                onCameraTranslate?.Invoke(currentPosition - _oldPosition);
                _oldPosition = currentPosition;
            }
        }
    }
}   