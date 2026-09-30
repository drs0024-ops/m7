using UnityEngine;
using TMPro;
using VContainer;
using MessagePipe;
using Game.Core.Messages;
using Game.Gameplay.Player;
using Game.Gameplay.Save;

namespace Game.UI
{
    /// <summary>
    /// System settings sub-panel: input device, text speed, auto-save,
    /// difficulty, language, reduce flashing, show FPS, CRT mode, delete save.
    /// Activated/deactivated by OptionsMenuController.
    /// </summary>
    public class SystemSettingsUI : MonoBehaviour
    {
        [System.Serializable]
        public class SettingRow
        {
            public string label;
            public TextMeshProUGUI valueText;
        }

        private enum RowType { Cycle, Toggle, Action }

        #region Fields

        [Header("Dependencies")]
        [Inject] private InputManager _inputManager;
        [Inject] private IPublisher<DeleteSaveRequested> _deleteSavePublisher;


        [Header("Settings")]
        [SerializeField] private SettingRow[] _rows;
        [SerializeField] private float _blinkInterval = 0.5f;
        [SerializeField] private int[] _defaultIndices = { 0, 1, 0, 1, 0, 1, 1, 0 };

        private IPublisher<SaveRequest> _savePublisher;
        private IPublisher<CRTModeChanged> _crtModePublisher;
        private IPublisher<CRTIntensityChanged> _crtIntensityPublisher;
        private IObjectResolver _container;

        private string[][] _options;
        private int[] _valueIndices;
        private RowType[] _rowTypes;
        private string[] _rowOriginals;

        private int _selectedRow;
        private bool _enabled;
        private bool _deleteConfirming;
        private bool _dirty;

        private bool _upHeld, _downHeld, _leftHeld, _rightHeld;
        private float _blinkTimer;
        private bool _cursorVisible = true;

        #endregion

        #region Injection

        [Inject]
        public void Construct(
            InputManager inputManager,
            IPublisher<SaveRequest> savePublisher,
            IPublisher<CRTModeChanged> crtModePublisher,
            IPublisher<CRTIntensityChanged> crtIntensityPublisher,
            IObjectResolver container)
        {
            _inputManager = inputManager;
            _savePublisher = savePublisher;
            _crtModePublisher = crtModePublisher;
            _crtIntensityPublisher = crtIntensityPublisher;
            _container = container;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_rows == null || _rows.Length == 0)
            {
                Debug.LogError("[SystemSettings] _rows not assigned!", this);
                enabled = false;
                return;
            }

            int count = _rows.Length;
            _options = new string[count][];
            _valueIndices = new int[count];
            _rowTypes = new RowType[count];
            _rowOriginals = new string[count];

            for (int i = 0; i < count; i++)
            {
                _rowOriginals[i] = _rows[i].label;
                _rows[i].valueText.text = string.Empty;
            }

            // ── Row 0: Input Device (Cycle) ──
            _options[0] = new[] { "KEYBOARD", "GAMEPAD", "AUTO" };
            _rowTypes[0] = RowType.Cycle;

            // ── Row 1: Text Speed (Cycle) ──
            _options[1] = new[] { "SLOW", "NORMAL", "FAST", "INSTANT" };
            _rowTypes[1] = RowType.Cycle;

            // ── Row 2: Auto-Save (Toggle) ──
            _options[2] = new[] { "ON", "OFF" };
            _rowTypes[2] = RowType.Toggle;

            // ── Row 3: Difficulty (Cycle) ──
            _options[3] = new[] { "EASY", "NORMAL", "HARD" };
            _rowTypes[3] = RowType.Cycle;

            // ── Row 4: Language (Cycle) ──
            _options[4] = new[] { "ENGLISH", "SPANISH", "FRENCH", "GERMAN", "JAPANESE" };
            _rowTypes[4] = RowType.Cycle;

            // ── Row 5: Reduce Flashing (Toggle) ──
            _options[5] = new[] { "ON", "OFF" };
            _rowTypes[5] = RowType.Toggle;

            // ── Row 6: Show FPS (Toggle) ──
            _options[6] = new[] { "ON", "OFF" };
            _rowTypes[6] = RowType.Toggle;

            // ── Row 7: CRT Effect (Cycle) ──
            _options[7] = new[] { "ALWAYS", "MENU ONLY", "OFF" };
            _rowTypes[7] = RowType.Cycle;

            // ── Row 8: Delete Save (Action) ──
            _options[8] = new[] { "[PRESS ENTER]" };
            _rowTypes[8] = RowType.Action;

            for (int i = 0; i < _valueIndices.Length; i++)
                _valueIndices[i] = i < _defaultIndices.Length ? _defaultIndices[i] : 0;

            _selectedRow = 0;
        }

        private void OnEnable()
        {
            if (!enabled) return;

            _enabled = true;
            _dirty = false;
            _upHeld = _downHeld = _leftHeld = _rightHeld = false;
            _blinkTimer = 0f;
            _cursorVisible = true;
            _deleteConfirming = false;
            RebuildAll();
        }

        private void OnDisable()
        {
            _enabled = false;
            if (_dirty)
                _savePublisher.Publish(new SaveRequest("SystemSettings", false));
        }

        #endregion

        #region Update

        private void Update()
        {
            if (!_enabled) return;

            float x = _inputManager.Movement.x;
            float y = _inputManager.Movement.y;

            bool up = y > 0.5f;
            bool down = y < -0.5f;
            bool left = x < -0.5f;
            bool right = x > 0.5f;

            if (up && !_upHeld)
                SelectRow(_selectedRow - 1);
            else if (down && !_downHeld)
                SelectRow(_selectedRow + 1);

            if (_deleteConfirming)
            {
                if (left && !_leftHeld)
                    _valueIndices[_selectedRow] = 0;
                else if (right && !_rightHeld)
                    _valueIndices[_selectedRow] = 1;

                if (_inputManager.ConfirmWasPressed)
                {
                    if (_valueIndices[_selectedRow] == 1)
                        ConfirmDelete();
                    _deleteConfirming = false;
                    _valueIndices[_selectedRow] = 0;
                    UpdateRow(_selectedRow);
                }

                if (_inputManager.EscapeWasPressed)
                {
                    _deleteConfirming = false;
                    _valueIndices[_selectedRow] = 0;
                    UpdateRow(_selectedRow);
                }
            }
            else
            {
                if (left && !_leftHeld)
                    AdjustValue(-1);
                else if (right && !_rightHeld)
                    AdjustValue(+1);

                if (_inputManager.ConfirmWasPressed)
                {
                    if (_rowTypes[_selectedRow] == RowType.Action)
                        EnterDeleteConfirm();
                    else
                        Save();
                }
            }

            _upHeld = up;
            _downHeld = down;
            _leftHeld = left;
            _rightHeld = right;

            _blinkTimer += Time.unscaledDeltaTime;
            if (_blinkTimer >= _blinkInterval)
            {
                _blinkTimer = 0f;
                _cursorVisible = !_cursorVisible;
                UpdateRow(_selectedRow);
            }
        }

        #endregion

        #region Input Actions

        private void SelectRow(int index)
        {
            int prev = _selectedRow;
            _selectedRow = Mathf.Clamp(index, 0, _rows.Length - 1);

            if (prev != _selectedRow)
            {
                _deleteConfirming = false;
                UpdateRow(prev);
                UpdateRow(_selectedRow);
            }
        }

        private void AdjustValue(int direction)
        {
            int count = _options[_selectedRow].Length;
            _valueIndices[_selectedRow] = (_valueIndices[_selectedRow] + direction + count) % count;
            _dirty = true;
            UpdateRow(_selectedRow);
            ApplyRow(_selectedRow);
        }

        private void EnterDeleteConfirm()
        {
            _deleteConfirming = true;
            _valueIndices[_selectedRow] = 0;
            _rows[_selectedRow].valueText.text = "Delete? N █";
        }

        private void ConfirmDelete()
        {
             _deleteSavePublisher.Publish(DeleteSaveRequested.Default);
        }

        private void Save()
        {
            _savePublisher.Publish(new SaveRequest("SystemSettings", false));
            _dirty = false;
        }

        #endregion

        #region Apply

        private void ApplyRow(int row)
        {
            if (row == 7)
            {
                switch (_valueIndices[row])
                {
                    case 0:
                        _crtModePublisher.Publish(new CRTModeChanged(false));
                        _crtIntensityPublisher.Publish(new CRTIntensityChanged(1f));
                        break;
                    case 1:
                        _crtModePublisher.Publish(new CRTModeChanged(true));
                        break;
                    case 2:
                        _crtIntensityPublisher.Publish(new CRTIntensityChanged(0f));
                        break;
                }
            }
            
            _container.Resolve<SettingsSavable>().SetSystem(
                _valueIndices[0], _valueIndices[1], _valueIndices[2], _valueIndices[3],
                _valueIndices[4], _valueIndices[5], _valueIndices[6], _valueIndices[7]);
        }

        #endregion

        #region UI

        private void RebuildAll()
        {
            for (int i = 0; i < _rows.Length; i++)
                UpdateRow(i);
        }

        private void UpdateRow(int row)
        {
            string val;

            if (row == 8 && _deleteConfirming)
            {
                val = _valueIndices[row] == 1 ? "Delete? Y" : "Delete? N";
            }
            else
            {
                val = _options[row][_valueIndices[row]];
            }

            _rows[row].valueText.text = (row == _selectedRow && _cursorVisible) ? $"{val} █" : val;
        }

        #endregion
    }
}   