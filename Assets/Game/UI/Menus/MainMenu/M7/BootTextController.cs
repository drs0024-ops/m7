using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Enums;
using Game.Core.Interfaces;
using Game.Gameplay.Player;
using TMPro;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    public class BootTextController : MonoBehaviour, IStartable, IDisposable
    {
        #region Serialized

        [Header("Boot text for _titleText")]
        [SerializeField] private string _freshBootTitle = "SYSTEM BOOT: CONCIENCENESS TRANSFER SUCCESSFUL";
        [SerializeField] private string _returnBootTitle = "SYSTEM BOOT: NORMAL";

        [Header("Title Reference")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Header("Catagory Refrences")]
        [SerializeField] private TextMeshProUGUI[] _lineTexts;

        [Header("Item States")]
        [SerializeField] private Color _enabledColor = Color.white;
        [SerializeField] private Color _disabledColor = new Color(1f, 1f, 1f, 0.35f);   

        [Header("Cursor")]
        [SerializeField] private string _cursorChar = "_";
        [SerializeField] private float _cursorBlinkRate = 0.5f;

        [Header("Typing")]
        [SerializeField] private float _charDelay = 0.04f;
        [SerializeField] private float _lineDelay = 0.15f;

        #endregion

        #region Fields

        private string[] _lineFullText;
        private int _selectedIndex;
        private bool _isTyping;
        private bool _isSelecting;
        private bool _disposed;
        private CancellationTokenSource _cts;
        private float _cursorTimer;

        [Inject] private IAudioManager _audio;
        [Inject] private InputManager _inputManager;

        #endregion

        #region Properties

        public bool IsSelecting => _isSelecting;
        public int SelectedIndex => _selectedIndex;

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            
        }

        

        #endregion

        #region Public API

        public void StartSequence(bool fromGameplay = false)
        {
            if (_disposed) return;

              // Lazy init — don't rely on IStartable.Start() timing
            if (_lineFullText == null || _lineFullText.Length != _lineTexts.Length)
            {
                _lineFullText = new string[_lineTexts.Length];
                for (int i = 0; i < _lineTexts.Length; i++)
                    _lineFullText[i] = _lineTexts[i].text;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _selectedIndex = 0;
            _isSelecting = false;

            // Re-read full text in case inspector changed
            for (int i = 0; i < _lineTexts.Length; i++)
            {
                if (_lineTexts[i].text.Length > 0)
                    _lineFullText[i] = _lineTexts[i].text;
            }

            if (_itemEnabled == null || _itemEnabled.Length != _lineTexts.Length)
            {
                _itemEnabled = new bool[_lineTexts.Length];
                for (int i = 0; i < _itemEnabled.Length; i++)
                    _itemEnabled[i] = true;
            }

            string title = fromGameplay ? _returnBootTitle : _freshBootTitle;

            if (fromGameplay)
            {
                _titleText.text = title;
                for (int i = 0; i < _lineTexts.Length; i++)
                    _lineTexts[i].text = _lineFullText[i];

                _isTyping = false;
                _isSelecting = true;
                RebuildSelection();
            }
            else
            {
                _titleText.text = string.Empty;
                for (int i = 0; i < _lineTexts.Length; i++)
                    _lineTexts[i].text = string.Empty;

                _isTyping = true;
                TypeAllLines(title, _cts.Token).Forget();
            }
        }
        private bool[] _itemEnabled;

        public void SetItemEnabled(int index, bool enabled)
        {
            if (index < 0 || index >= _itemEnabled.Length) return;
            _itemEnabled[index] = enabled;
            _lineTexts[index].color = enabled ? _enabledColor : _disabledColor;
        }

        public void SetReturnTitle(string title)
        {
            _returnBootTitle = title;
        }

        public void MoveSelection(int direction)
        {
            if (!_isSelecting || _disposed) return;

            int count = _lineTexts.Length;
            int next = _selectedIndex;

            for (int i = 0; i < count; i++)
            {
                next = ((next + direction) % count + count) % count;
                if (_itemEnabled[next]) break;
            }

            if (next != _selectedIndex)
            {
                _selectedIndex = next;
                _audio.PlaySFX(SoundType.MenuNavigate);
                RebuildSelection();
            }
        }

        public int GetSelectedAction() => _selectedIndex;

        #endregion

        #region Update (Cursor Blink)

        private void Update()
        {
            if (_disposed || !_isSelecting) return;

            // Cursor blink
            _cursorTimer += Time.unscaledDeltaTime;
            if (_cursorTimer >= _cursorBlinkRate)
            {
                _cursorTimer = 0f;
                RebuildSelection();
            }

            // Navigation
            if (_inputManager.MoveWasPressedUp)
                MoveSelection(-1);
            if (_inputManager.MoveWasPressedDown)
                MoveSelection(1);
        }

        private bool _cursorVisible = true;

        private void RebuildSelection()
        {
            _cursorVisible = !_cursorVisible;

            for (int i = 0; i < _lineTexts.Length; i++)
            {
                bool isSelected = (i == _selectedIndex && _cursorVisible && _itemEnabled[i]);
                string text = isSelected ? _lineFullText[i] + _cursorChar : _lineFullText[i];
                _lineTexts[i].text = text;
                _lineTexts[i].color = _itemEnabled[i] ? _enabledColor : _disabledColor;
            }
        }

        #endregion

        #region Typing

        private async UniTaskVoid TypeAllLines(string title, CancellationToken token)
        {
            try
            {
                // Type the title
                for (int i = 0; i < title.Length; i++)
                {
                    if (token.IsCancellationRequested) return;
                    _titleText.text = title.Substring(0, i + 1);
                    await UniTask.Delay((int)(_charDelay * 1000), cancellationToken: token);
                }

                await UniTask.Delay((int)(_lineDelay * 1000), cancellationToken: token);

                // Type each category line
                for (int i = 0; i < _lineFullText.Length; i++)
                {
                    if (token.IsCancellationRequested) return;

                    string line = _lineFullText[i];
                    for (int c = 0; c < line.Length; c++)
                    {
                        if (token.IsCancellationRequested) return;
                        _lineTexts[i].text = line.Substring(0, c + 1);
                        await UniTask.Delay((int)(_charDelay * 1000), cancellationToken: token);
                    }

                    _lineTexts[i].text = line;
                    await UniTask.Delay((int)(_lineDelay * 1000), cancellationToken: token);
                }

                _isTyping = false;
                _isSelecting = true;
                _cursorVisible = true;
                RebuildSelection();
            }
            catch (OperationCanceledException) { }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
        }

        void OnDestroy() => Dispose();

        #endregion
    }
}   