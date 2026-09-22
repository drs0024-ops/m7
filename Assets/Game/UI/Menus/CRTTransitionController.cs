using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Full-screen CRT visual effect overlay (lives in Bootstrap scene).
    /// Hidden by default — shown via CRTStateRequested messages.
    /// States: Hidden, MenuIdle, Transition (wipe animation), Injured, Dead.
    /// Root GameObject must be ACTIVE — visibility managed by enabling/disabling RawImages.
    /// </summary>
    public class CRTTransitionController : MonoBehaviour, IDisposable
    {
        #region Constants

        private const float OVERSHOOT = 1.15f;

        #endregion

        #region Fields

        [Header("Presets")]
        [SerializeField] private CRTShaderPreset _idlePreset;
        [SerializeField] private CRTShaderPreset _transitionPreset;
        [SerializeField] private CRTShaderPreset _injuredPreset;
        [SerializeField] private CRTShaderPreset _deadPreset;

        [Header("References")]
        [SerializeField] private RawImage _overlayRawImage;
        [SerializeField] private RawImage _rollBarRawImage;
        [SerializeField] private Material _overlayMaterial;
        [SerializeField] private Material _rollBarMaterial;

        private IPublisher<ScreenSwapped> _swappedPublisher;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;
        private bool _configValid;

        private enum AnimState { Idle, Ramping, Swap, Settling }
        private AnimState _animState;
        private float _animStateStartTime;

        private CRTState _currentState = CRTState.Hidden;

        private float _retraceY;
        private float _retraceY2;
        private float _tearY;
        private float _tearPauseTimer;
        private float _activeTearHeight;
        private float _tearFontSizeOverride = -1f;
        private float _rollBarY;
        private float _rollBarIntensity;

        private float _tearSpeed;
        private CRTScrollDirection _tearDirection;
        private float _retraceSpeed;
        private float _retraceSpeed2;
        private float _retraceHeight;
        private CRTScrollDirection _retraceDirection;
        private float _transitionDuration;
        private float _rollBarIdleIntensity;
        private float _rollBarTransitionIntensity;

        private float _lastAppliedRollBarY = float.NegativeInfinity;
        private float _lastAppliedRollBarIntensity = float.NegativeInfinity;

        private Coroutine _rgbSplitRoutine;

        #endregion

        #region Properties

        public bool IsTransitioning => _animState != AnimState.Idle;

        public float RetraceHeight
        {
            get => _retraceHeight;
            set
            {
                _retraceHeight = Mathf.Clamp(value, 0.0001f, 0.1f);
                _overlayMaterial.SetFloat("_RetraceWidth", _retraceHeight);
            }
        }

        public float RetraceOpacity
        {
            get => _idlePreset.retraceOpacity;
            set
            {
                _idlePreset.retraceOpacity = Mathf.Clamp01(value);
                _overlayMaterial.SetFloat("_RetraceOpacity", value);
            }
        }

        public float TearFontSize
        {
            get => _tearFontSizeOverride >= 0f ? _tearFontSizeOverride : _idlePreset.tearFontSize;
            set
            {
                _tearFontSizeOverride = Mathf.Clamp(value, 1f, 128f);
                _activeTearHeight = FontSizeToNormalized(_tearFontSizeOverride);
                _overlayMaterial.SetFloat("_TearHeight", _activeTearHeight);
            }
        }

        public float TearOpacity
        {
            get => _idlePreset.tearOpacity;
            set
            {
                _idlePreset.tearOpacity = Mathf.Clamp01(value);
                _overlayMaterial.SetFloat("_TearOpacity", value);
            }
        }

        public float TearLeadingOpacity
        {
            get => _idlePreset.tearLeadingOpacity;
            set
            {
                _idlePreset.tearLeadingOpacity = Mathf.Clamp01(value);
                ApplyTearEdges(_idlePreset);
            }
        }

        public float TearTrailingOpacity
        {
            get => _idlePreset.tearTrailingOpacity;
            set
            {
                _idlePreset.tearTrailingOpacity = Mathf.Clamp01(value);
                ApplyTearEdges(_idlePreset);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _configValid = _overlayMaterial != null && _rollBarMaterial != null
                           && _overlayRawImage != null && _rollBarRawImage != null
                           && _idlePreset != null && _transitionPreset != null;

            if (!_configValid)
            {
                Debug.LogError($"[CRT] Missing required references or presets on {gameObject.name}.", this);
                return;
            }

            SyncMotionParams(_idlePreset);
            _activeTearHeight = FontSizeToNormalized(_idlePreset.tearFontSize);

            _retraceY = 0.9f;
            _retraceY2 = 0.7f;
            _tearY = -_activeTearHeight;
            _tearPauseTimer = 0f;
            _rollBarY = 0.9f;
            _rollBarIntensity = 0.3f;
            _animState = AnimState.Idle;

            _overlayMaterial.SetVector("_ScreenSize", new Vector4(Screen.width, Screen.height, 0, 0));

            SetOverlayVisible(false);
        }

        private void Start()
        {
            if (!_configValid) return;

            _swappedPublisher = GlobalMessagePipe.GetPublisher<ScreenSwapped>();

            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<CRTStateRequested>()
                .Subscribe(OnStateRequested));

            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<CRTSettingsChanged>()
                .Subscribe(OnSettingsChanged));
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }

        #endregion

        #region Update

        private void Update()
        {
            if (_disposed || !_configValid) return;
            if (_currentState == CRTState.Hidden) return;

            UpdateScrolling();

            switch (_animState)
            {
                case AnimState.Idle:     UpdateIdle();     break;
                case AnimState.Ramping:  UpdateRamping();  break;
                case AnimState.Swap:     UpdateSwap();     break;
                case AnimState.Settling: UpdateSettling(); break;
            }
        }

        private void UpdateScrolling()
        {
            float retraceDelta = _retraceSpeed * Time.deltaTime;
            if (_retraceDirection == CRTScrollDirection.Down)
                _retraceY -= retraceDelta;
            else
                _retraceY += retraceDelta;

            if (_retraceY < -_retraceHeight) _retraceY = 1f + _retraceHeight;
            if (_retraceY > 1f + _retraceHeight) _retraceY = -_retraceHeight;
            _overlayMaterial.SetFloat("_RetraceY", _retraceY);

            float retraceDelta2 = _retraceSpeed2 * Time.deltaTime;
            if (_retraceDirection == CRTScrollDirection.Down)
                _retraceY2 -= retraceDelta2;
            else
                _retraceY2 += retraceDelta2;

            if (_retraceY2 < -_retraceHeight) _retraceY2 = 1f + _retraceHeight;
            if (_retraceY2 > 1f + _retraceHeight) _retraceY2 = -_retraceHeight;
            _overlayMaterial.SetFloat("_RetraceY2", _retraceY2);

            if (_tearPauseTimer > 0f)
            {
                _tearPauseTimer -= Time.deltaTime;
            }
            else
            {
                float tearDelta = _tearSpeed * Time.deltaTime;
                if (_tearDirection == CRTScrollDirection.Up)
                    _tearY += tearDelta;
                else
                    _tearY -= tearDelta;

                if (_tearDirection == CRTScrollDirection.Up)
                {
                    if (_tearY > 1f + _activeTearHeight)
                    {
                        _tearY = -_activeTearHeight;
                        _tearPauseTimer = UnityEngine.Random.Range(_idlePreset.tearPauseMin, _idlePreset.tearPauseMax);
                    }
                }
                else
                {
                    if (_tearY < -_activeTearHeight)
                    {
                        _tearY = 1f + _activeTearHeight;
                        _tearPauseTimer = UnityEngine.Random.Range(_idlePreset.tearPauseMin, _idlePreset.tearPauseMax);
                    }
                }
            }
            _overlayMaterial.SetFloat("_TearY", _tearY);
        }

        private void UpdateIdle()
        {
            _rollBarIntensity = 0.3f;
            ApplyRollBar();
        }

        private void UpdateRamping()
        {
            float elapsed = Time.unscaledTime - _animStateStartTime;
            float rampDuration = _transitionDuration * 0.5f;

            if (elapsed < rampDuration)
            {
                float t = elapsed / rampDuration;
                _rollBarY = Mathf.Lerp(0.9f, 0.5f, t);
                _rollBarIntensity = t * t;
                ApplyRollBar();
            }
            else
            {
                _animState = AnimState.Swap;
                _animStateStartTime = Time.unscaledTime;
                _rollBarY = 0.5f;
                _rollBarIntensity = 1f;
                ApplyRollBar();
            }
        }

        private void UpdateSwap()
        {
            _rollBarY = 0.5f;
            _rollBarIntensity = 1f;
            ApplyRollBar();

            _swappedPublisher?.Publish(default);

            _animState = AnimState.Settling;
            _animStateStartTime = Time.unscaledTime;
        }

        private void UpdateSettling()
        {
            float elapsed = Time.unscaledTime - _animStateStartTime;
            float settleDuration = _transitionDuration * 0.5f;

            if (elapsed < settleDuration)
            {
                _rollBarY = 0.1f;
                _rollBarIntensity = OVERSHOOT;
                ApplyRollBar();
            }
            else
            {
                _animState = AnimState.Idle;
                ApplyIdleMaterialState();
            }
        }

        #endregion

        #region State Handling

        private void OnStateRequested(CRTStateRequested msg)
        {
            if (!_configValid) return;

            _currentState = msg.State;

            if (msg.State == CRTState.Hidden)
            {
                SetOverlayVisible(false);
                return;
            }

            SetOverlayVisible(true);

            switch (msg.State)
            {
                case CRTState.MenuIdle:
                    ApplyIdleMaterialState();
                    break;

                case CRTState.Transition:
                    TriggerTransition();
                    break;

                case CRTState.TakingDamage:
                    if (_injuredPreset != null)
                        ApplyPreset(_injuredPreset);
                    break;

                case CRTState.Death:
                    if (_deadPreset != null)
                        ApplyPreset(_deadPreset);
                    break;
            }
        }

        private void SetOverlayVisible(bool visible)
        {
            if (_overlayRawImage != null) _overlayRawImage.enabled = visible;
            if (_rollBarRawImage != null) _rollBarRawImage.enabled = visible;
        }

        #endregion

        #region Transition

        private void TriggerTransition()
        {
            if (_animState != AnimState.Idle || !_configValid) return;

            _rollBarY = 0.9f;
            _rollBarIntensity = 0f;
            SetRollBarVisible(true);
            _animState = AnimState.Ramping;
            _animStateStartTime = Time.unscaledTime;

            SyncMotionParams(_transitionPreset);
            _activeTearHeight = _tearFontSizeOverride >= 0f
                ? FontSizeToNormalized(_tearFontSizeOverride)
                : FontSizeToNormalized(_transitionPreset.tearFontSize);
            _transitionPreset.ApplyTo(_overlayMaterial, _rollBarMaterial);
            _overlayMaterial.SetFloat("_TearHeight", _activeTearHeight);
        }

        #endregion

        #region Public API

        public void SetRollBarVisible(bool visible)
        {
            if (_rollBarRawImage != null)
                _rollBarRawImage.enabled = visible;
        }

        public void SetBarColor(Color tint)
        {
            if (_rollBarMaterial != null)
                _rollBarMaterial.SetColor("_Tint", tint);
        }

        public void SetScreenTint(Color tint)
        {
            if (_overlayMaterial != null)
                _overlayMaterial.SetColor("_PhosphorColor", tint);
        }

        public void SetTearColor(Color tint)
        {
            _idlePreset.tearColor = tint;
            _transitionPreset.tearColor = tint;
            _overlayMaterial.SetColor("_TearColor", tint);
        }

        public void SetFailureAmount(float value)
        {
            if (_overlayMaterial == null) return;
            _overlayMaterial.SetFloat("_FailureAmount", Mathf.Clamp(value, 0f, 2f));
        }

        public void SetConsciencePulse(float value)
        {
            if (_overlayMaterial == null) return;
            _overlayMaterial.SetFloat("_ConsciencePulse", Mathf.Clamp01(value));
        }

        public void PlayRGBSplit(float amount, float duration)
        {
            if (_overlayMaterial == null) return;
            if (_rgbSplitRoutine != null)
                StopCoroutine(_rgbSplitRoutine);
            _rgbSplitRoutine = StartCoroutine(RGBSplitRoutine(amount, duration));
        }

        public void ForceApplyIdle()
        {
            if (!_configValid) return;
            ApplyIdleMaterialState();
        }

        #endregion

        #region Message Handlers

        private void OnSettingsChanged(CRTSettingsChanged msg)
        {
            if (!_configValid) return;

            CRTSettings s = msg.Settings;

            _idlePreset.failureAmount = s.idleIntensity;
            _idlePreset.transitionOpacity = s.transitionOpacity;
            _idlePreset.tearColor = s.tearColor;
            _idlePreset.tearSpeed = s.tearSpeed;
            _idlePreset.tearDirection = s.tearDirection;
            _idlePreset.tearOpacity = s.idleTearOpacity;
            _idlePreset.tearFontSize = s.tearFontSize;
            _idlePreset.tearLeadingOpacity = s.tearLeadingOpacity;
            _idlePreset.tearTrailingOpacity = s.tearTrailingOpacity;
            _idlePreset.retraceSpeed = s.retraceSpeed;
            _idlePreset.retraceSpeed2 = s.retraceSpeed2;
            _idlePreset.retraceHeight = s.retraceHeight;
            _idlePreset.retraceOpacity = s.retraceOpacity;
            _idlePreset.retraceDirection = s.retraceDirection;
            _idlePreset.retraceIntensity = s.idleRetraceIntensity;
            _idlePreset.rollBarIdleIntensity = s.rollBarIdleIntensity;
            _idlePreset.rollBarTransitionIntensity = s.rollBarTransitionIntensity;
            _idlePreset.transitionDuration = s.transitionDuration;

            _transitionPreset.failureAmount = s.transitionIntensity;
            _transitionPreset.transitionOpacity = s.transitionOpacity;
            _transitionPreset.tearColor = s.tearColor;
            _transitionPreset.tearSpeed = s.tearSpeed;
            _transitionPreset.tearDirection = s.tearDirection;
            _transitionPreset.tearOpacity = s.transitionTearOpacity;
            _transitionPreset.tearFontSize = s.tearFontSize;
            _transitionPreset.tearLeadingOpacity = s.tearLeadingOpacity;
            _transitionPreset.tearTrailingOpacity = s.tearTrailingOpacity;
            _transitionPreset.retraceSpeed = s.retraceSpeed;
            _transitionPreset.retraceSpeed2 = s.retraceSpeed2;
            _transitionPreset.retraceHeight = s.retraceHeight;
            _transitionPreset.retraceOpacity = s.retraceOpacity;
            _transitionPreset.retraceDirection = s.retraceDirection;
            _transitionPreset.retraceIntensity = s.transitionRetraceIntensity;
            _transitionPreset.rollBarIdleIntensity = s.rollBarIdleIntensity;
            _transitionPreset.rollBarTransitionIntensity = s.rollBarTransitionIntensity;
            _transitionPreset.transitionDuration = s.transitionDuration;

            if (_currentState == CRTState.MenuIdle && _animState == AnimState.Idle)
                ApplyIdleMaterialState();
        }

        #endregion

        #region Helpers

        private float FontSizeToNormalized(float fontSize)
        {
            return fontSize / Screen.height;
        }

        private void SyncMotionParams(CRTShaderPreset preset)
        {
            _tearSpeed = preset.tearSpeed;
            _tearDirection = preset.tearDirection;
            _retraceSpeed = preset.retraceSpeed;
            _retraceSpeed2 = preset.retraceSpeed2;
            _retraceHeight = preset.retraceHeight;
            _retraceDirection = preset.retraceDirection;
            _transitionDuration = preset.transitionDuration;
            _rollBarIdleIntensity = preset.rollBarIdleIntensity;
            _rollBarTransitionIntensity = preset.rollBarTransitionIntensity;
        }

        private void ApplyIdleMaterialState()
        {
            SyncMotionParams(_idlePreset);
            _activeTearHeight = _tearFontSizeOverride >= 0f
                ? FontSizeToNormalized(_tearFontSizeOverride)
                : FontSizeToNormalized(_idlePreset.tearFontSize);
            _idlePreset.ApplyTo(_overlayMaterial, _rollBarMaterial);
            _overlayMaterial.SetFloat("_TearHeight", _activeTearHeight);
        }

        private void ApplyPreset(CRTShaderPreset preset)
        {
            SyncMotionParams(preset);
            preset.ApplyTo(_overlayMaterial, _rollBarMaterial);
            _overlayMaterial.SetFloat("_TearHeight", _activeTearHeight);
        }

        private void ApplyTearEdges(CRTShaderPreset preset)
        {
            _overlayMaterial.SetFloat("_TearLeadingOpacity", preset.tearLeadingOpacity);
            _overlayMaterial.SetFloat("_TearTrailingOpacity", preset.tearTrailingOpacity);
        }

        private void ApplyRollBar()
        {
            float max = _animState == AnimState.Idle ? _rollBarIdleIntensity : _rollBarTransitionIntensity;
            float scaled = _rollBarIntensity * max;

            if (Mathf.Approximately(_rollBarY, _lastAppliedRollBarY)
                && Mathf.Approximately(scaled, _lastAppliedRollBarIntensity))
                return;

            _lastAppliedRollBarY = _rollBarY;
            _lastAppliedRollBarIntensity = scaled;
            _rollBarMaterial.SetFloat("_Y", _rollBarY);
            _rollBarMaterial.SetFloat("_Intensity", scaled);
        }

        private IEnumerator RGBSplitRoutine(float amount, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - (elapsed / duration);
                _overlayMaterial.SetFloat("_RGBSplit", amount * t);
                yield return null;
            }
            _overlayMaterial.SetFloat("_RGBSplit", 0f);
            _rgbSplitRoutine = null;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}   