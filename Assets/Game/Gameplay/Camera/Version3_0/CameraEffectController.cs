using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using VContainer.Unity;
using MessagePipe;
using Game.Core.Messages;
using VContainer;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Handles camera shake (Cinemachine impulse) and Y-axis damping (fall pan).
    /// Reads the active composer from CameraConfig — no dependency on CameraSwitcher.
    /// </summary>
    public class CameraEffectController : MonoBehaviour, IDisposable
    {
        #region Dependencies

        [SerializeField] private float _globalShakeForce = 1f;
        [SerializeField] private float _fallPanAmount = 0.25f;
        [SerializeField] private float _fallYPanTime = 0.35f;

        private CameraConfig _config;
        private CinemachineImpulseSource _impulseSource;
        private ISubscriber<CameraShakeRequest> _shakeSub;
        private ISubscriber<CameraDampingRequest> _dampingSub;

        #endregion

        #region State

        private readonly List<IDisposable> _disposables = new List<IDisposable>(2);

        private float _normalDampingY;
        private bool _dampingInitialized;
        private int _dampingTweenId = -1;
        private bool _isDisposed;

        #endregion

        #region Public API

        public bool IsLerpingYDamping { get; private set; }

        [Inject]
        public void Construct(
            CameraConfig config,
            CinemachineImpulseSource impulseSource,
            ISubscriber<CameraShakeRequest> shakeSub,
            ISubscriber<CameraDampingRequest> dampingSub)
        {
            _config = config;
            _impulseSource = impulseSource;
            _shakeSub = shakeSub;
            _dampingSub = dampingSub;
        }

        public void Initialize()
        {
            _disposables.Add(_shakeSub.Subscribe(HandleShake));
            _disposables.Add(_dampingSub.Subscribe(HandleDamping));
            CacheNormalDamping();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();

            if (_dampingTweenId != -1) LeanTween.cancel(_dampingTweenId);
        }

        #endregion

        #region Message Handlers

        private void HandleShake(CameraShakeRequest req)
        {
            if (req.Profile != null)
                TriggerShakeFromProfile(req.Profile);
            else
                TriggerShake(req.ForceMultiplier);
        }

        private void HandleDamping(CameraDampingRequest req)
        {
            if (!_dampingInitialized) CacheNormalDamping();
            LerpYDamping(req.IsFalling);
        }

        #endregion

        #region Internal

        private void CacheNormalDamping()
        {
            var composer = _config.ActiveComposer;
            if (composer != null)
            {
                _normalDampingY = composer.Damping.y;
                _dampingInitialized = true;
            }
        }

        private void TriggerShake(float multiplier = 1f)
        {
            _impulseSource.GenerateImpulseWithForce(_globalShakeForce * multiplier);
        }

        private void TriggerShakeFromProfile(ScreenShakeProfile profile)
        {
            var def = _impulseSource.ImpulseDefinition;
            def.ImpulseDuration = profile.impulseDuration;
            def.CustomImpulseShape = profile.impulseCurve;
            def.AmplitudeGain = profile.amplitudeGain;
            def.FrequencyGain = profile.frequencyGain;

            _impulseSource.ImpulseDefinition = def;
            _impulseSource.GenerateImpulseWithForce(profile.impactForce);
        }

        private void LerpYDamping(bool isFalling)
        {
            var composer = _config.ActiveComposer;
            if (composer == null) return;

            float baseline = _dampingInitialized ? _normalDampingY : composer.Damping.y;
            float start = composer.Damping.y;
            float end = isFalling ? _fallPanAmount : baseline;

            if (_dampingTweenId != -1) LeanTween.cancel(_dampingTweenId);

            var tween = LeanTween.value(gameObject, (float val) =>
            {
                if (composer != null) composer.Damping.y = val;
            }, start, end, _fallYPanTime)
            .setEase(LeanTweenType.linear)
            .setOnStart(() => IsLerpingYDamping = true)
            .setOnComplete(() =>
            {
                if (composer != null) composer.Damping.y = end;
                IsLerpingYDamping = false;
            });

            _dampingTweenId = tween.id;
        }

        #endregion

        #region Cleanup

        private void OnDisable()
        {
            if (_dampingTweenId != -1) LeanTween.cancel(_dampingTweenId);
        }

        #endregion
    }
}   