using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using TMPro;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Message-driven loading panel shown during scene transitions.
    /// Uses text-based progress bar (same style as AudioSettingsUI rows).
    /// Lives in Bootstrap (persistent). Hidden by default.
    /// </summary>
    public class LoadingPanelController : MonoBehaviour, IDisposable
    {
        #region Dependencies

        private ISubscriber<SceneTransitionStarted> _startedSub;
        private ISubscriber<SceneTransitionCompleted> _completedSub;
        private ISubscriber<SceneLoadProgress> _progressSub;

        #endregion

        #region References

        [Header("UI")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private TextMeshProUGUI _barText;
        [SerializeField] private TextMeshProUGUI _valueText;

		[Header("Cursor")]
		[SerializeField] private bool _showCursor = false;
		[SerializeField] private float _blinkInterval = 0.5f;

        [Header("Bar")]
        [SerializeField] private int _barCharCount = 20;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.25f;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new();
        private bool _isShowing;
        private bool _disposed;
		private float _blinkTimer;
		private bool _cursorVisible = true;
		private float _lastValue;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_barText == null || _valueText == null)
            {
                Debug.LogError("[LoadingPanel] _barText or _valueText is not assigned!", this);
                return;
            }

            _panel.SetActive(false);
            UpdateBar(0f);
        }

        private void Start()
        {
            _startedSub = GlobalMessagePipe.GetSubscriber<SceneTransitionStarted>();
            _completedSub = GlobalMessagePipe.GetSubscriber<SceneTransitionCompleted>();
            _progressSub = GlobalMessagePipe.GetSubscriber<SceneLoadProgress>();

            _subscriptions.Add(_startedSub.Subscribe(OnTransitionStarted));
            _subscriptions.Add(_completedSub.Subscribe(OnTransitionCompleted));
            _subscriptions.Add(_progressSub.Subscribe(OnProgress));
        }

		private void Update()
		{
			if (!_isShowing || !_showCursor) return;

			_blinkTimer += Time.unscaledDeltaTime;
			if (_blinkTimer >= _blinkInterval)
			{
				_blinkTimer = 0f;
				_cursorVisible = !_cursorVisible;
				UpdateBar(_lastValue);
			}
		}

        #endregion

        #region Message Handlers

        private void OnTransitionStarted(SceneTransitionStarted msg)
        {
            if (_disposed || _isShowing) return;
            _isShowing = true;

            if (_label != null)
                _label.text = "Loading";

            UpdateBar(0f);
            ShowPanel();
        }

        private void OnTransitionCompleted(SceneTransitionCompleted msg)
        {
            if (_disposed || !_isShowing) return;
            _isShowing = false;
            HidePanel();
        }

        private void OnProgress(SceneLoadProgress msg)
        {
            if (_disposed || _isShowing) return;
				_isShowing = true;
				_blinkTimer = 0f;
				_cursorVisible = true;

			if (_label != null)
				_label.text = "Loading";

			_lastValue = 0f;
			UpdateBar(0f);
			ShowPanel();
        }

        #endregion

        #region UI

        private void UpdateBar(float value)
        {
            int filled = Mathf.RoundToInt(value * _barCharCount);
			_barText.text =
				$"[ {new string('█', filled)}{new string('░', _barCharCount - filled)} ]";

			string val = Mathf.RoundToInt(value * 100f).ToString("D2");
			_valueText.text = (_showCursor && _cursorVisible) ? $"{val} █" : val;
        }

        #endregion

        #region Internal

        private void ShowPanel()
        {
            _panel.SetActive(true);
            CanvasGroup cg = _panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = _panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            LeanTween.alpha(_panel, 1f, _fadeDuration);
        }

        private void HidePanel()
        {
            CanvasGroup cg = _panel.GetComponent<CanvasGroup>();
            if (cg == null) return;

            var lt = LeanTween.alpha(_panel, 0f, _fadeDuration);
            lt.setOnComplete(() =>
            {
                if (!_disposed) _panel.SetActive(false);
            });
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