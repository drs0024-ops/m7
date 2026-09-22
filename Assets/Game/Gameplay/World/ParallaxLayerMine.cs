using UnityEngine;

namespace Game.Gameplay.World
{
    [DefaultExecutionOrder(200)]
    public class ParallaxLayerMine : MonoBehaviour
    {
        [Range(0f, 100f)]
        public float parallaxSpeed = 100f;
        public bool lockY;

        private Transform _camTrans;
        private Vector3 _initialWorldPosition;
        private Vector3 _initialCamPosition;
        private bool _initialized;

        private void Awake()
        {
            TryInitialize();
        }

        private void OnEnable()
        {
            if (!_initialized)
                TryInitialize();
        }

        public void ResetCameraReference()
        {
            _initialized = false;
            TryInitialize();
        }

        private void TryInitialize()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            _camTrans = cam.transform;
            _initialCamPosition = _camTrans.position;
            _initialWorldPosition = transform.position;
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                TryInitialize();
                if (!_initialized) return;
            }

            Vector3 camOffset = _camTrans.position - _initialCamPosition;
            float factor = Mathf.Clamp01(1f - parallaxSpeed / 100f);

            float x = _initialWorldPosition.x + camOffset.x * factor;
            float y = lockY
                ? _initialWorldPosition.y
                : _initialWorldPosition.y + camOffset.y * factor;

            transform.position = new Vector3(x, y, _initialWorldPosition.z);
        }
    }
}   