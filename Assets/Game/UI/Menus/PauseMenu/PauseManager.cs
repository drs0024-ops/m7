using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Messages;
using MessagePipe;
using TMPro;
using UnityEngine;
using Game.Gameplay.Player;
using VContainer;

namespace Game.UI
{
    /// <summary>
    /// UI controller for the Pause Menu overlay (lives in Bootstrap scene).
    /// Root GameObject must be ACTIVE in the prefab — visibility managed via _panelOpen.
    /// Self-contained navigation (up/down/enter/escape) with blinking cursor.
    /// Instant show/hide, no fade.
    /// </summary>
    public class PauseManager : MonoBehaviour, IDisposable
    {
        private const string BLOCK = "█";

        #region Serialized

        [Header("Menu Items")]
        [SerializeField] private TextMeshProUGUI[] _lineTexts;

        [Header("Cursor")]
        [SerializeField] private float _blinkInterval = 0.5f;

        [Header("Dependencies")]
        [Inject] private InputManager _inputManager;
        [Inject] private SceneRegistry _sceneRegistry;
        [Inject] private IPublisher<ResumeGameRequested> _resumePublisher;
        [Inject] private IPublisher<NavigateToMenu> _navigateToMenuPublisher;
        [Inject] private IPublisher<OptionsOpenRequested> _optionsOpenPublisher;
        [Inject] private ISubscriber<GamePaused> _pausedSub;
        [Inject] private ISubscriber<GameResumed> _resumedSub;
        [Inject] private ISubscriber<GameStarted> _startedSub;

        #endregion

        #region Fields

        private string[] _lineFullText;
        private int _selectedIndex;
        private bool _panelOpen;
        private bool _upHeld;
        private bool _downHeld;
        private float _blinkTimer;
        private bool _cursorVisible = true;
        private bool _disposed;
        private readonly List<IDisposable> _subscriptions = new(3);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_lineTexts == null || _lineTexts.Length == 0)
            {
                Debug.LogError("[PauseManager] _lineTexts not assigned!", this);
                enabled = false;
                return;
            }

            _lineFullText = new string[_lineTexts.Length];
            for (int i = 0; i < _lineTexts.Length; i++)
            {
                _lineFullText[i] = _lineTexts[i].text;
                _lineTexts[i].text = string.Empty;
            }

            _panelOpen = false;
            SetVisualActive(false);
        }

        private void Start()
        {
            if (!enabled) return;

            _subscriptions.Add(_pausedSub.Subscribe(_ => OpenPauseMenu()));
            _subscriptions.Add(_resumedSub.Subscribe(_ => ClosePauseMenu()));
            _subscriptions.Add(_startedSub.Subscribe(_ => ClosePauseMenu()));
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Update

        private void Update()
        {
            if (_disposed || !_panelOpen) return;

            float y = _inputManager.Movement.y;
            bool up = y > 0.5f;
            bool down = y < -0.5f;

            _blinkTimer += Time.unscaledDeltaTime;
            if (_blinkTimer >= _blinkInterval)
            {
                _blinkTimer = 0f;
                _cursorVisible = !_cursorVisible;
                RebuildCursor();
            }

            if (_inputManager.EscapeWasPressed)
            {
                _resumePublisher.Publish(default);
                return;
            }

            if (up && !_upHeld)
                MoveSelection(-1);
            else if (down && !_downHeld)
                MoveSelection(1);

            _upHeld = up;
            _downHeld = down;

            if (_inputManager.ConfirmWasPressed)
                ExecuteSelection();
        }

        #endregion

        #region Navigation

        private void MoveSelection(int direction)
        {
            int count = _lineTexts.Length;
            _selectedIndex = ((_selectedIndex + direction) % count + count) % count;
            RebuildCursor();
        }

        private void RebuildCursor()
        {
            for (int i = 0; i < _lineTexts.Length; i++)
            {
                string cursor = (i == _selectedIndex && _cursorVisible) ? " " + BLOCK : string.Empty;
                _lineTexts[i].text = _lineFullText[i] + cursor;
            }
        }

        #endregion

        #region Actions

        private void ExecuteSelection()
        {
            switch (_selectedIndex)
            {
                case 0:
                    _resumePublisher.Publish(default);
                    break;
                case 1:
                    _optionsOpenPublisher.Publish(default);
                    break;
                case 2:
                    _navigateToMenuPublisher.Publish(new NavigateToMenu(_sceneRegistry.GameLoopScene));
                    break;
            }
        }

        #endregion

        #region Show / Hide

        private void OpenPauseMenu()
        {
            if (_disposed || _panelOpen) return;
            _panelOpen = true;
            ResetState();
            SetVisualActive(true);
        }

        private void ClosePauseMenu()
        {
            if (_disposed || !_panelOpen) return;
            _panelOpen = false;
            SetVisualActive(false);
        }

        private void SetVisualActive(bool active)
        {
            for (int i = 0; i < _lineTexts.Length; i++)
                _lineTexts[i].gameObject.SetActive(active);
        }

        private void ResetState()
        {
            _selectedIndex = 0;
            _upHeld = _downHeld = false;
            _blinkTimer = 0f;
            _cursorVisible = true;
            RebuildCursor();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}   