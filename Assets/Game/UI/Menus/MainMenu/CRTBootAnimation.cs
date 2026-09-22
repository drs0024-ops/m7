using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class CRTBootAnimation : MonoBehaviour
    {
        [Header("CRT")]
        [SerializeField] private RawImage _crtImage;
        [SerializeField] private Image _blackOverlay;

        [Header("Timing")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _duration = 0.4f;
        [Range(0f, 2f)]
        [SerializeField] private float _postDelay = 0.3f;

        public float PostDelay => _postDelay;

        private Material _bootMaterial;
        private int _progressID;

        private void Awake()
        {
            _bootMaterial = _crtImage.material;
            _progressID = Shader.PropertyToID("_BootProgress");
            _bootMaterial.SetFloat(_progressID, 0f);  // start hidden (shader shows nothing)

            // Ensure overlay is visible on start
            if (_blackOverlay != null)
            {
                _blackOverlay.color = Color.black;
                _blackOverlay.gameObject.SetActive(true);
            }
        }

        public async UniTask Play()
        {
            // Instantly hide the black overlay
            if (_blackOverlay != null)
                _blackOverlay.gameObject.SetActive(false);

            // Run the shader animation
            _bootMaterial.SetFloat(_progressID, 0f);

            float t = 0f;
            while (t < _duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / _duration);
                float eased = 1f - (1f - p) * (1f - p);
                _bootMaterial.SetFloat(_progressID, eased);
                await UniTask.Yield();
            }

            _bootMaterial.SetFloat(_progressID, 1f);
        }
    }
}   