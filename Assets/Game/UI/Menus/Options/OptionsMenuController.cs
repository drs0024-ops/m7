using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MessagePipe;
using Game.Core.Messages;
using Game.Gameplay.Player;
using Game.Core.Interfaces;
using Game.Core.Enums;
using VContainer.Unity;
using VContainer;

namespace Game.UI
{
    /// <summary>
    /// Self-contained controller for the Options panel (lives in Bootstrap scene).
    /// Root GameObject must be ACTIVE in the prefab — visibility is managed via _panelOpen.
    /// Opens/closes via OptionsOpenRequested / OptionsCloseRequested messages.
    /// Handles its own Escape key: sub-panel Escape closes sub-panel,
    /// category-level Escape publishes OptionsCloseRequested.
    /// </summary>
    public class OptionsMenuController : MonoBehaviour, IDisposable
    {
        private const string BLOCK = "█";

        #region Serialized

        [Header("Category List")]
        [SerializeField] private TextMeshProUGUI[] _categoryTexts;
        [SerializeField] private GameObject[] _subPanels;
        [SerializeField] private GameObject _categoryListPanel;

        [Header("Panel")]
        [SerializeField] private GameObject _panelContent;

        [Header("Dependencies")]
        [Inject] private InputManager _inputManager;
        [Inject] private IAudioManager _audio;
        [Inject] private IPublisher<OptionsCloseRequested> _closePublisher;
        [Inject] private IPublisher<OptionsPanelStateChanged> _stateChangedPublisher;
        [Inject] private ISubscriber<OptionsOpenRequested> _openSub;
        [Inject] private ISubscriber<OptionsCloseRequested> _closeSub;

        [Header("Blink")]
        [SerializeField] private float _blinkInterval = 0.5f;

        #endregion

        #region Fields

        private string[] _categoryOriginals;
        private int _selectedIndex;
        private bool _panelOpen;
        private bool _subPanelOpen;
        private bool _disposed;

        private bool _upHeld;
        private bool _downHeld;
        private float _blinkTimer;
        private bool _cursorVisible = true;

        private readonly List<IDisposable> _subscriptions = new(2);

        #endregion

        #region Properties

        public bool IsSubPanelOpen => _subPanelOpen;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_categoryTexts == null || _categoryTexts.Length == 0)
            {
                Debug.LogError("[OptionsMenu] _categoryTexts not assigned!", this);
                enabled = false;
                return;
            }

            if (_subPanels == null || _subPanels.Length != _categoryTexts.Length)
            {
                Debug.LogError("[OptionsMenu] _subPanels length must match _categoryTexts!", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < _subPanels.Length; i++)
                _subPanels[i].SetActive(false);

            _categoryOriginals = new string[_categoryTexts.Length];
            for (int i = 0; i < _categoryTexts.Length; i++)
            {
                _categoryOriginals[i] = _categoryTexts[i].text;
                _categoryTexts[i].text = string.Empty;
            }

            _panelOpen = false;
            SetVisualActive(false);
        }

        private void Start()
        {
            if (!enabled) return;

            _subscriptions.Add(_openSub.Subscribe(_ => OpenPanel()));
            _subscriptions.Add(_closeSub.Subscribe(_ => ClosePanel()));
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
                if (_subPanelOpen)
                    CloseSubPanel();
                else
                {
                    _audio.PlaySFX(SoundType.MenuClick);
                    _closePublisher.Publish(default);
                }
                return;
            }

            if (_subPanelOpen)
            {
                _upHeld = up;
                _downHeld = down;
                return;
            }

            if (up && !_upHeld)
                MoveSelection(-1);
            else if (down && !_downHeld)
                MoveSelection(1);

            _upHeld = up;
            _downHeld = down;

            if (_inputManager.ConfirmWasPressed)
                OpenSubPanel();
        }

        #endregion

        #region Navigation

        private void MoveSelection(int direction)
        {
            int count = _categoryTexts.Length;
            _selectedIndex = ((_selectedIndex + direction) % count + count) % count;
            RebuildCursor();
        }

        private void RebuildCursor()
        {
            for (int i = 0; i < _categoryTexts.Length; i++)
            {
                string cursor = (i == _selectedIndex && _cursorVisible) ? " " + BLOCK : string.Empty;
                _categoryTexts[i].text = _categoryOriginals[i] + cursor;
            }
        }

        #endregion

        #region Sub Panel

        private void OpenSubPanel()
        {
            _subPanels[_selectedIndex].SetActive(true);
            _subPanelOpen = true;
        }

        private void CloseSubPanel()
        {
            _subPanels[_selectedIndex].SetActive(false);
            _subPanelOpen = false;
            RebuildCursor();
        }

        #endregion

        #region Show / Hide

        private void OpenPanel()
        {
            if (_disposed || _panelOpen) return;
            _panelOpen = true;
            ResetState();
            SetVisualActive(true);
            _stateChangedPublisher.Publish(new OptionsPanelStateChanged(true));
        }

        private void ClosePanel()
        {
            if (_disposed || !_panelOpen) return;
            _panelOpen = false;
            for (int i = 0; i < _subPanels.Length; i++)
                _subPanels[i].SetActive(false);
            _subPanelOpen = false;
            SetVisualActive(false);
            _stateChangedPublisher.Publish(new OptionsPanelStateChanged(false));
        }

        private void SetVisualActive(bool active)
        {
            _panelContent.SetActive(active);
        }

        private void ResetState()
        {
            _selectedIndex = 0;
            _subPanelOpen = false;
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