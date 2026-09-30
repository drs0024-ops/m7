using System;
using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using Game.Core.Messages;
using Game.Gameplay.Camera;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{ 
    /// <summary>
    /// Listens for player damage/health events and drives CRT visual feedback:
    /// RGB split on hit, sustained failure degradation, and collapse on death.
    /// Also publishes CameraShakeRequest for gameplay shake.
    /// </summary>
    public class CRTDamageListener : MonoBehaviour, IStartable, IDisposable
    {
        #region Dependencies
 
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

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new();
        private IPublisher<CameraShakeRequest> _shakePublisher;
        private ISubscriber<PlayerDamaged> _playerDamagedSub;
        private ISubscriber<EntityHealthChanged> _healthChangedSub;
        private ISubscriber<PlayerDied> _playerDiedSub;
        private ISubscriber<DialogueStarted> _dialogueStartedSub;
        private ISubscriber<DialogueEnded> _dialogueEndedSub;
        private float _maxHealth;
        private bool _isDisposed;

        #endregion

    

        #region Public API

        /// <summary>
        /// Resets all CRT damage effects to idle state (call on level restart).
        /// </summary>
        public void ResetAll()
        {
            _crtController?.SetFailureAmount(_idleFailure);
            _crtController?.SetConsciencePulse(0f);
            _collapseController?.ResetCollapse();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        #endregion

        #region Internal

        [Inject]
        private void Inject(
            IPublisher<CameraShakeRequest> shakePublisher,
            ISubscriber<PlayerDamaged> playerDamagedSub,
            ISubscriber<EntityHealthChanged> healthChangedSub,
            ISubscriber<PlayerDied> playerDiedSub,
            ISubscriber<DialogueStarted> dialogueStartedSub,
            ISubscriber<DialogueEnded> dialogueEndedSub)
        {
            _shakePublisher = shakePublisher;
            _playerDamagedSub = playerDamagedSub;
            _healthChangedSub = healthChangedSub;
            _playerDiedSub = playerDiedSub;
            _dialogueStartedSub = dialogueStartedSub;
            _dialogueEndedSub = dialogueEndedSub;
        }

        void IStartable.Start()
        {
            _subscriptions.Add(_playerDamagedSub.Subscribe(OnPlayerDamaged));
            _subscriptions.Add(_healthChangedSub.Subscribe(OnHealthChanged));
            _subscriptions.Add(_playerDiedSub.Subscribe(OnPlayerDied));
            _subscriptions.Add(_dialogueStartedSub.Subscribe(OnDialogueStarted));
            _subscriptions.Add(_dialogueEndedSub.Subscribe(OnDialogueEnded));
        }

// Delete Awake() entirely   

        #region Hanlders

        private void OnDialogueStarted(DialogueStarted _)
        {
            if (_crtController == null) return;
            _crtController.SetFailureAmount(0f);
            _crtController.SetConsciencePulse(0f);
        }

        private void OnDialogueEnded(DialogueEnded _)
        {
            // Next EntityHealthChanged message will restore values automatically.
        }
        #endregion
        
        private void OnDestroy()
        {
            Dispose();
        }

        private void OnPlayerDamaged(PlayerDamaged msg)
        {
            if (_crtController == null || _damageShakeProfile == null) return;

            float severity = _maxHealth > 0f ? msg.DamageAmount / _maxHealth : 0f;
            _shakePublisher.Publish(new CameraShakeRequest(_damageShakeProfile, severity * _shakeSeverityScale));
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

        #endregion
    }
}   