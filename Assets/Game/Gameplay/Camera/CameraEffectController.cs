using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using VContainer;
using MessagePipe;
using VContainer.Unity;
using System;

namespace Game.Gameplay.Camera
{
    public class CameraEffectController : MonoBehaviour, IInitializable, IDisposable
    {
        [SerializeField] private float _globalShakeForce = 1f;
        [SerializeField] private float _fallPanAmount = 0.25f;
        [SerializeField] private float _fallYPanTime = 0.35f;

        private readonly ISubscriber<CameraShakeRequest> _shakeSub;
        private readonly ISubscriber<CameraDampingRequest> _dampingSub;
        private readonly List<IDisposable> _disposables = new List<IDisposable>(2);

        private readonly CameraSwitcher _switcher;
        private CinemachineImpulseSource _impulseSource;

        private float _normalDampingY;
        private bool _dampingInitialized;
        private int _dampingTweenId = -1;

        public bool IsLerpingYDamping { get; private set; }

        [Inject]
        public CameraEffectController(
            CameraSwitcher switcher,
            CinemachineImpulseSource impulseSource,
            ISubscriber<CameraShakeRequest> shakeSub,
            ISubscriber<CameraDampingRequest> dampingSub)
        {
            _switcher = switcher;
            _impulseSource = impulseSource;
            _shakeSub = shakeSub;
            _dampingSub = dampingSub;
        }

        public void Initialize()
        {
            _disposables.Add(_shakeSub.Subscribe(HandleShake));
            _disposables.Add(_dampingSub.Subscribe(HandleDamping));
        }

        private void Start()
        {
            CacheNormalDamping();
        }

        private void CacheNormalDamping()
        {
            var composer = _switcher.CurrentComposer;
            if (composer != null)
            {
                _normalDampingY = composer.Damping.y;
                _dampingInitialized = true;
            }
        }

        private void HandleShake(CameraShakeRequest req)
        {
            if (_impulseSource == null) return;

            if (req.Profile != null)
                TriggerShakeFromProfile(req.Profile, _impulseSource);
            else
                TriggerShake(_impulseSource, req.ForceMultiplier);
        }

        private void HandleDamping(CameraDampingRequest req)
        {
            if (!_dampingInitialized) CacheNormalDamping();
            if (_dampingInitialized) LerpYDamping(req.IsFalling);
        }

        public void TriggerShake(CinemachineImpulseSource source, float multiplier = 1f)
        {
            if (source == null) return;
            source.GenerateImpulseWithForce(_globalShakeForce * multiplier);
        }

        public void TriggerShakeFromProfile(ScreenShakeProfile profile, CinemachineImpulseSource source)
        {
            if (source == null || profile == null) return;

            var def = source.ImpulseDefinition;
            def.ImpulseDuration = profile.impulseDuration;
            def.CustomImpulseShape = profile.impulseCurve;
            def.AmplitudeGain = profile.amplitudeGain;
            def.FrequencyGain = profile.frequencyGain;

            source.ImpulseDefinition = def;
            source.GenerateImpulseWithForce(profile.impactForce);
        }

        public void LerpYDamping(bool isFalling)
        {
            var composer = _switcher.CurrentComposer;
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

        public void Dispose()
        {
            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();

            if (_dampingTweenId != -1) LeanTween.cancel(_dampingTweenId);
        }

        private void OnDisable()
        {
            if (_dampingTweenId != -1) LeanTween.cancel(_dampingTweenId);
        }
    }
}   