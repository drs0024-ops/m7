using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Messages;
using Game.Gameplay.Player;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    public class HUDPauseController : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private CanvasGroup _hudCanvasGroup;
        [SerializeField] private Image _blackFadeImage;
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private float _handshakeDelay = 0.3f;
        [SerializeField] private float _longPressDuration = 1.2f;
        [SerializeField] private float _pulseDuration = 0.2f;

        [Header("Audio")]
        [SerializeField] private AudioSource _holdChargeSource;
        [SerializeField] private AudioSource _holdCompleteSource;
        [SerializeField] private AudioSource _holdReleaseSource;

        [Inject] private InputManager _inputManager;
        [Inject] private HudState _hudState;

        private ISubscriber<GamePaused> _pausedSub;
        private ISubscriber<GameResumed> _resumedSub;
        private readonly List<IDisposable> _subscriptions = new();

        private bool _isPaused;
        private bool _isHolding;
        private bool _disposed;
        private float _holdTimer;
        private int _tweenId = -1;
        private int _pulseTweenId = -1;

        public HUDPauseController() { }

        void IStartable.Start()
        {
            _blackFadeImage.color = new Color(0, 0, 0, 0f);

            _pausedSub = GlobalMessagePipe.GetSubscriber<GamePaused>();
            _subscriptions.Add(_pausedSub.Subscribe(_ => Pause().Forget()));

            _resumedSub = GlobalMessagePipe.GetSubscriber<GameResumed>();
            _subscriptions.Add(_resumedSub.Subscribe(_ => Resume().Forget()));
        }

        private void Update()
        {
            if (_disposed || !_isPaused || _hudState.IsFrozen) return;

            if (_inputManager.EscapeIsHeld)
            {
                if (!_isHolding)
                {
                    _isHolding = true;
                    _holdTimer = 0f;
                    StartHoldFeedback();
                }

                _holdTimer += Time.unscaledDeltaTime;
                UpdateHoldFeedback();

                if (_holdTimer >= _longPressDuration)
                    CompleteQuit();
            }
            else
            {
                if (_isHolding) CancelHold();
                _isHolding = false;
                _holdTimer = 0f;
            }
        }

        private async UniTaskVoid Pause()
        {
            _isPaused = true;
            _isHolding = false;
            _holdTimer = 0f;

            if (_tweenId != -1) LeanTween.cancel(_tweenId);

            _tweenId = LeanTween.alpha(_hudCanvasGroup.gameObject, 0f, _fadeDuration)
                .setEase(LeanTweenType.easeInOutQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => _tweenId = -1).id;
        }

        private async UniTaskVoid Resume()
        {
            _isPaused = false;
            _isHolding = false;
            _holdTimer = 0f;

            await UniTask.Delay((int)(_handshakeDelay * 1000f), ignoreTimeScale: true);
            if (_disposed) return;

            if (_tweenId != -1) LeanTween.cancel(_tweenId);

            _tweenId = LeanTween.alpha(_hudCanvasGroup.gameObject, 1f, _fadeDuration)
                .setEase(LeanTweenType.easeInOutQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => _tweenId = -1).id;
        }

        private void StartHoldFeedback()
        {
            if (_pulseTweenId != -1) LeanTween.cancel(_pulseTweenId);

            _pulseTweenId = LeanTween.alpha(_hudCanvasGroup.gameObject, 0.15f, _pulseDuration * 0.5f)
                .setEase(LeanTweenType.linear)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    if (_disposed) return;
                    LeanTween.alpha(_hudCanvasGroup.gameObject, 0f, _pulseDuration * 0.5f)
                        .setEase(LeanTweenType.linear)
                        .setIgnoreTimeScale(true)
                        .setOnComplete(() => _pulseTweenId = -1);
                }).id;

            if (_holdChargeSource != null)
            {
                _holdChargeSource.pitch = 1.0f;
                _holdChargeSource.Play();
            }
        }

        private void UpdateHoldFeedback()
        {
            if (_holdChargeSource != null && _holdChargeSource.isPlaying)
                _holdChargeSource.pitch = Mathf.Lerp(1.0f, 1.4f, _holdTimer / _longPressDuration);
        }

        private void CancelHold()
        {
            if (_holdChargeSource != null)
                _holdChargeSource.Stop();

            if (_holdReleaseSource != null)
                _holdReleaseSource.Play();
        }

        private void CompleteQuit()
        {
            _isHolding = false;

            if (_holdChargeSource != null)
                _holdChargeSource.Stop();

            if (_holdCompleteSource != null)
                _holdCompleteSource.Play();

            LeanTween.alpha(_blackFadeImage.gameObject, 1f, 0.5f)
                .setEase(LeanTweenType.easeInOutQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => Application.Quit());
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_tweenId != -1) LeanTween.cancel(_tweenId);
            if (_pulseTweenId != -1) LeanTween.cancel(_pulseTweenId);

            if (_holdChargeSource != null) _holdChargeSource.Stop();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
    }
}   