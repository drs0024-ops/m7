using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.UI
{
    /// <summary>
    /// Drives the ProgressiveVignette shader.
    /// Base intensity is inversely proportional to health (low HP = strong vignette).
    /// Pulses briefly on PlayerDamaged.
    /// </summary>
    public class VignetteDriver : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private Material _vignetteMaterial;

        [Header("Tuning")]
        [Tooltip("How much of the health range maps to vignette (0..1). 0.5f means vignette starts at 50% HP.")]
        [SerializeField] private float _startThreshold = 0.5f;

        [Tooltip("How quickly the base vignette lerps toward its target.")]
        [SerializeField] private float _lerpSpeed = 2f;

        [Tooltip("Extra intensity added on each hit (decays over _pulseDecayTime).")]
        [SerializeField] private float _pulseAmount = 0.3f;

        [Tooltip("Seconds for the pulse to decay back to base.")]
        [SerializeField] private float _pulseDecayTime = 0.8f;

        private ISubscriber<EntityHealthChanged> _healthSub;
        private ISubscriber<PlayerDamaged> _damagedSub;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        private float _baseTarget;   // driven by health
        private float _current;      // what the shader sees
        private float _pulse;        // decaying hit-flash
        private float _pulseTimer;

        public VignetteDriver() { }

        private void Awake()
        {
            if (_vignetteMaterial == null)
            {
                var img = GetComponent<UnityEngine.UI.Image>();
                if (img != null && img.material != null)
                    _vignetteMaterial = img.material;
            }
        }

        void IStartable.Start()
        {
            _healthSub = GlobalMessagePipe.GetSubscriber<EntityHealthChanged>();
            _damagedSub = GlobalMessagePipe.GetSubscriber<PlayerDamaged>();

            _subscriptions.Add(_healthSub.Subscribe(OnHealthChanged));
            _subscriptions.Add(_damagedSub.Subscribe(OnDamaged));

            if (_vignetteMaterial != null)
                _vignetteMaterial.SetFloat("_Damage", 0f);
        }

        private void Update()
        {
            if (_vignetteMaterial == null || _disposed) return;

            // Decay pulse
            if (_pulse > 0f)
            {
                _pulseTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_pulseTimer / _pulseDecayTime);
                _pulse = _pulseAmount * (1f - t);
                if (t >= 1f) { _pulse = 0f; _pulseTimer = 0f; }
            }

            // Lerp base toward target
            _current = Mathf.Lerp(_current, _baseTarget + _pulse, _lerpSpeed * Time.deltaTime);

            if (Mathf.Abs(_current - _vignetteMaterial.GetFloat("_Damage")) > 0.001f)
                _vignetteMaterial.SetFloat("_Damage", Mathf.Clamp01(_current));
        }

        private void OnHealthChanged(EntityHealthChanged msg)
        {
            if (msg.MaxHealth <= 0f) return;
            float hp = msg.CurrentHealth / msg.MaxHealth;

            // 0 at full health, 1 at zero health — but only kicks in below threshold
            if (hp >= _startThreshold)
                _baseTarget = 0f;
            else
            {
                float range = 1f - _startThreshold;
                _baseTarget = (1f - hp) / range; // 0 at threshold → 1 at zero HP
            }
        }

        private void OnDamaged(PlayerDamaged _)
        {
            _pulse = _pulseAmount;
            _pulseTimer = 0f;
        }

        /// <summary>
        /// Override the base target externally (e.g., scripted events).
        /// Pass -1 to revert to health-driven behavior.
        /// </summary>
        public void SetDamage(float value)
        {
            if (value < 0f) return; // -1 = revert, handled by next health message
            _baseTarget = Mathf.Clamp01(value);
        }

        public void SetVignetteColor(Color c)
        {
            if (_vignetteMaterial != null)
                _vignetteMaterial.SetColor("_VignetteColor", c);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
    }
}   