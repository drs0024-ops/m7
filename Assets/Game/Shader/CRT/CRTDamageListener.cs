using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using Game.Core.Messages;
using Game.Gameplay.Camera;
using System;

namespace Game.UI
{
    public class CRTDamageListener : MonoBehaviour, System.IDisposable
    {
        [Header("References")]
        [SerializeField] private CRTTransitionController _crtController;
        [SerializeField] private CRTCollapseController _collapseController;
        [SerializeField] private ScreenShakeProfile _damageShakeProfile;

        [Header("On-Hit Flinch")]
        [SerializeField] private float _shakeSeverityScale = 3f;
        [SerializeField] private float _rgbSplitAmount = 0.003f;
        [SerializeField] private float _rgbSplitDuration = 0.12f;

        [Header("Sustained Degradation")]
        [SerializeField] private float _idleFailure = 0.15f;
        [SerializeField] private float _maxFailure = 1.5f;

        [Header("Conscience Pulse")]
        [SerializeField] private float _pulseStart = 0.3f;
        [SerializeField] private float _pulseFull = 0.05f;

        private readonly List<IDisposable> _subscriptions = new();
        private IPublisher<CameraShakeRequest> _shakePublisher;
        private float _maxHealth;

        private void Awake()
        {
            _shakePublisher = GlobalMessagePipe.GetPublisher<CameraShakeRequest>();
        }

        private void Start()
        {
            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<PlayerDamaged>()
                .Subscribe(OnPlayerDamaged));
            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<EntityHealthChanged>()
                .Subscribe(OnHealthChanged));
            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<PlayerDied>()
                .Subscribe(OnPlayerDied));
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnPlayerDamaged(PlayerDamaged msg)
        {
            if (_crtController == null || _damageShakeProfile == null) return;

            float severity = _maxHealth > 0f ? msg.DamageAmount / _maxHealth : 0f;
            _shakePublisher.Publish(new CameraShakeRequest(severity * _shakeSeverityScale, _damageShakeProfile));
            _crtController.PlayRGBSplit(_rgbSplitAmount, _rgbSplitDuration);
        }

        private void OnHealthChanged(EntityHealthChanged msg)
        {
            if (_crtController == null) return;

            _maxHealth = msg.MaxHealth;
            float hpRatio = msg.MaxHealth > 0f ? msg.CurrentHealth / msg.MaxHealth : 0f;

            _crtController.SetFailureAmount(Mathf.Lerp(_idleFailure, _maxFailure, 1f - hpRatio));
            _crtController.SetConsciencePulse(Mathf.SmoothStep(_pulseStart, _pulseFull, hpRatio));
        }

        private void OnPlayerDied(PlayerDied msg)
        {
            if (_collapseController == null) return;

            _collapseController.OnComplete = () => { /* death menu, audio stop, etc. */ };
            _collapseController.PlayCollapse();
        }

        public void ResetAll()
        {
            _crtController?.SetFailureAmount(_idleFailure);
            _crtController?.SetConsciencePulse(0f);
            _collapseController?.ResetCollapse();
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }
    }
}   