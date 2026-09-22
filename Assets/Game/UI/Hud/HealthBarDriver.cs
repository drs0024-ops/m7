using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.UI
{
    public class HealthBarDriver : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private Material _material;
        [SerializeField] private float _lerpSpeed = 4f;

        private ISubscriber<EntityHealthChanged> _healthSub;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;
        private float _targetHealth = 1f;

        public HealthBarDriver() { }

        private void Awake()
        {
            if (_material == null)
            {
                var img = GetComponent<UnityEngine.UI.Image>();
                if (img != null) _material = img.material;
            }
        }

        void IStartable.Start()
        {
            _healthSub = GlobalMessagePipe.GetSubscriber<EntityHealthChanged>();
            _subscriptions.Add(_healthSub.Subscribe(OnHealthChanged));

            if (_material != null)
                _material.SetFloat("_Health", 1f);
        }

        private void Update()
        {
            if (_material == null || _disposed) return;
            float current = _material.GetFloat("_Health");
            if (Mathf.Abs(current - _targetHealth) > 0.001f)
                _material.SetFloat("_Health", Mathf.Lerp(current, _targetHealth, _lerpSpeed * Time.deltaTime));
        }

        private void OnHealthChanged(EntityHealthChanged msg)
        {
            _targetHealth = msg.MaxHealth > 0f ? msg.CurrentHealth / msg.MaxHealth : 0f;
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