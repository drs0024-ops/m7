using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using Game.Gameplay.Player;
using Game.Gameplay.Save;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.Bootstrap
{
    /// <summary>
    /// Text-based skip prompt shown during intro video.
    /// Uses InputManager.ConfirmWasPressed + blinking cursor (same pattern as Pause/Options).
    /// Only visible when a save exists. Lives in Bootstrap (persistent UI).
    /// </summary>
    public class VideoSkipButton : MonoBehaviour, IDisposable
    {
        #region Dependencies

        private InputManager _inputManager;
        private SaveManager _saveManager;
        private ISubscriber<GameStateChanged> _stateSub;
        private ISubscriber<VideoFinished> _finishedSub;
        private IPublisher<VideoSkipRequested> _skipPublisher;

        #endregion

        #region References

        [Header("UI")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _text;

        [Header("Cursor")]
        [SerializeField] private float _blinkInterval = 0.5f;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new();
        private bool _isVisible;
        private bool _cursorVisible = true;
        private float _blinkTimer;
        private bool _disposed;

        #endregion

        #region Construction

        [Inject]
        private void Construct(
            InputManager inputManager,
            SaveManager saveManager,
            ISubscriber<GameStateChanged> stateSub,
            ISubscriber<VideoFinished> finishedSub,
            IPublisher<VideoSkipRequested> skipPublisher)
        {
            _inputManager = inputManager;
            _saveManager = saveManager;
            _stateSub = stateSub;
            _finishedSub = finishedSub;
            _skipPublisher = skipPublisher;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_text == null)
            {
                Debug.LogError("[VideoSkipButton] _text is not assigned!", this);
                return;
            }

            _panel.SetActive(false);
        }

        private void Start()
        {
            _subscriptions.Add(_stateSub.Subscribe(OnStateChanged));
            _subscriptions.Add(_finishedSub.Subscribe(_ => Hide()));
        }

        private void Update()
        {
            if (!_isVisible || _disposed) return;

            _blinkTimer += Time.unscaledDeltaTime;
            if (_blinkTimer >= _blinkInterval)
            {
                _blinkTimer = 0f;
                _cursorVisible = !_cursorVisible;
                UpdateText();
            }

            if (_inputManager.ConfirmWasPressed)
                Skip();
        }

        #endregion

        #region Message Handlers

        private void OnStateChanged(GameStateChanged msg)
        {
            if (_disposed) return;

            bool shouldShow = msg.State == GameState.IntroVideo
                              && _saveManager.HasSave;

            if (shouldShow) Show();
            else Hide();
        }

        #endregion

        #region Internal

        private void Show()
        {
            if (_isVisible) return;
            _isVisible = true;
            _blinkTimer = 0f;
            _cursorVisible = true;
            _panel.SetActive(true);
            UpdateText();
        }

        private void Hide()
        {
            if (!_isVisible) return;
            _isVisible = false;
            _panel.SetActive(false);
        }

        private void UpdateText()
        {
            _text.text = _cursorVisible ? "Skip █" : "Skip ";
        }

        private void Skip()
        {
            if (_disposed) return;
            Hide();
            _skipPublisher.Publish(VideoSkipRequested.Default);
        }

        #endregion

        #region Cleanup

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