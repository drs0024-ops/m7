using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using VContainer.Unity;

namespace Game.UI
{
    /// <summary>
    /// Drives the EKG Trace shader's _PulseSpeed and _PulseAmplitude
    /// based on a 0→1 health value.
    /// At full HP: speed=1, amplitude=1 (normal steady beat).
    /// At critical HP: speed≈0.4, amplitude≈0.5 (slow, weak, erratic).
    /// </summary>
    public class EKGDriver : MonoBehaviour, IStartable, IDisposable
    {
        [Header("Shader")] 
        [SerializeField] private Material _ekgMaterial;

        [Header("Health Range")]
        [SerializeField] private float _criticalThreshold = 0.3f;
        [SerializeField] private float _minPulseSpeed = 0.4f;
        [SerializeField] private float _maxPulseSpeed = 1.0f;
        [SerializeField] private float _minAmplitude = 0.5f;
        [SerializeField] private float _maxAmplitude = 1.0f;

        private float _healthPercent = 1f;
        private bool _disposed;
        private readonly List<IDisposable> _subscriptions = new();

        void IStartable.Start()
        {
            if (_ekgMaterial == null)
            {
                var rawImage = GetComponent<RawImage>();
                if (rawImage != null && rawImage.material != null)
                    _ekgMaterial = rawImage.material;
            }
        }

        public void SetHealth(float healthPercent)
        {
            _healthPercent = Mathf.Clamp01(healthPercent);
            UpdateShader();
        }

        private void UpdateShader()
        {
            if (_ekgMaterial == null) return;

            // Lerp: full HP → max values; critical → min values
            float t = 1f - Mathf.Clamp01((_healthPercent - _criticalThreshold) / (1f - _criticalThreshold));
            // t = 0 at full HP, t = 1 at critical

            float speed = Mathf.Lerp(_maxPulseSpeed, _minPulseSpeed, t);
            float amplitude = Mathf.Lerp(_maxAmplitude, _minAmplitude, t);

            // Add erratic jitter at low health
            if (t > 0.5f)
            {
                float jitter = (UnityEngine.Random.value - 0.5f) * 0.1f * t;
                amplitude += (UnityEngine.Random.value - 0.5f) * 0.05f * t;
            }

            _ekgMaterial.SetFloat("_PulseSpeed", speed);
            _ekgMaterial.SetFloat("_PulseAmplitude", amplitude);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
    }
}   