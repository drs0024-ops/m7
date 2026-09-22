using UnityEngine;
using TMPro;
using VContainer;
using MessagePipe;
using Game.Core.Messages;
using Game.Gameplay.Player;
using UnityEngine.InputSystem;
using Game.Gameplay.Save;

namespace Game.UI
{
    public class ControlsSettingsUI : MonoBehaviour
    {
        [System.Serializable]
        public class SettingRow
        {
            public string label;
            public TextMeshProUGUI valueText;
        }

        private enum RowType { Remap, Action }
        private enum RebindState { None, Waiting, Confirming }

        #region Fields

        [Header("Dependencies")]
        [Inject] private InputManager _inputManager;
        [Inject] private SettingsSavable _settings;

        [Header("Settings")]
        [SerializeField] private SettingRow[] _rows;
        [SerializeField] private float _blinkInterval = 0.5f;

        private IPublisher<SaveRequest> _savePublisher;

        private string[] _actionNames;
        private string[] _subBindingNames;
        private RowType[] _rowTypes;
        private string[] _currentDisplay;
        private string[] _bindingPaths =
        {
            "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d",
            "<Keyboard>/space", "<Keyboard>/shift", "<Keyboard>/leftCtrl", "<Keyboard>/o"
        };

        private int _selectedRow;
        private bool _enabled;
        private RebindState _rebindState;
        private string _pendingKey;

        private bool _upHeld, _downHeld;
        private float _blinkTimer;
        private bool _cursorVisible = true;

        #endregion

        #region Injection

        [Inject]
        public void Construct(IPublisher<SaveRequest> savePublisher)
        {
            _savePublisher = savePublisher;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_rows == null || _rows.Length == 0)
            {
                Debug.LogError("[Controls] _rows not assigned!", this);
                return;
            }

            int count = _rows.Length;
            _actionNames = new string[count];
            _subBindingNames = new string[count];
            _rowTypes = new RowType[count];
            _currentDisplay = new string[count];

            _actionNames[0] = "Move";  _subBindingNames[0] = "up";    _rowTypes[0] = RowType.Remap;
            _actionNames[1] = "Move";  _subBindingNames[1] = "down";  _rowTypes[1] = RowType.Remap;
            _actionNames[2] = "Move";  _subBindingNames[2] = "left";  _rowTypes[2] = RowType.Remap;
            _actionNames[3] = "Move";  _subBindingNames[3] = "right"; _rowTypes[3] = RowType.Remap;
            _actionNames[4] = "Jump";  _rowTypes[4] = RowType.Remap;
            _actionNames[5] = "Run";   _rowTypes[5] = RowType.Remap;
            _actionNames[6] = "Attack"; _rowTypes[6] = RowType.Remap;
            _actionNames[7] = "Interact"; _rowTypes[7] = RowType.Remap;
            _actionNames[8] = null;    _rowTypes[8] = RowType.Action;

            _selectedRow = 0;
            _rebindState = RebindState.None;
        }

        private void OnEnable()
        {
            _enabled = true;
            _upHeld = _downHeld = false;
            _blinkTimer = 0f;
            _cursorVisible = true;
            _rebindState = RebindState.None;
            _pendingKey = null;

            if (_inputManager != null)
            {
                LoadCurrentBindings();
                RebuildAll();
            }
        }

        private void OnDisable()
        {
            if (_enabled)
                _savePublisher.Publish(new SaveRequest("ControlsSettings", false));
            _enabled = false;
        }

        #endregion

        #region Update

        private void Update()
        {
            if (!_enabled) return;

            // Blink (always runs)
            _blinkTimer += Time.unscaledDeltaTime;
            if (_blinkTimer >= _blinkInterval)
            {
                _blinkTimer = 0f;
                _cursorVisible = !_cursorVisible;
                UpdateRow(_selectedRow);
            }

            // Rebind states
            if (_rebindState == RebindState.Waiting)
            {
                HandleRebindWaiting();
                return;
            }

            if (_rebindState == RebindState.Confirming)
            {
                HandleRebindConfirming();
                return;
            }

            // Normal navigation
            float y = _inputManager.Movement.y;
            bool up = y > 0.5f;
            bool down = y < -0.5f;

            if (up && !_upHeld)
                SelectRow(_selectedRow - 1);
            else if (down && !_downHeld)
                SelectRow(_selectedRow + 1);

            _upHeld = up;
            _downHeld = down;

            if (_inputManager.ConfirmWasPressed)
            {
                if (_rowTypes[_selectedRow] == RowType.Remap)
                    EnterRebindMode();
                else
                    ResetAll();
            }
        }

        #endregion

        #region Rebind

        private void EnterRebindMode()
        {
            _rebindState = RebindState.Waiting;
            _pendingKey = null;
            // Display stays as current key (e.g., "W █") — player just presses new key
            Debug.Log($"[Controls] Rebinding: row {_selectedRow}");
        }

        private void HandleRebindWaiting()
        {
            if (_inputManager.EscapeWasPressed)
            {
                ExitRebindMode();
                return;
            }

            var kb = Keyboard.current;
            if (kb == null) return;

            foreach (var button in kb.allKeys)
            {
                if (!button.wasPressedThisFrame) continue;
                if (button.name == "enter" || button.name == "escape") continue;

                _pendingKey = button.name;
                _rebindState = RebindState.Confirming;
                UpdateRow(_selectedRow);
                Debug.Log($"[Controls] Pending: '{_pendingKey}'");
                return;
            }
        }

        private void HandleRebindConfirming()
        {
            if (_inputManager.ConfirmWasPressed)
            {
                AssignBinding(_selectedRow, _pendingKey);
                ExitRebindMode();
                return;
            }

            if (_inputManager.EscapeWasPressed)
            {
                ExitRebindMode();
                return;
            }
        }

        private void ExitRebindMode()
        {
            _rebindState = RebindState.None;
            _pendingKey = null;
            UpdateRow(_selectedRow);
        }

        private void AssignBinding(int row, string keyName)
        {
            string bindingPath = $"<Keyboard>/{keyName}";
            var actions = _inputManager.PlayerInput.actions;
            var playerMap = actions.FindActionMap("Player");
            var action = playerMap?.FindAction(_actionNames[row]);

            if (action == null) return;

            if (_subBindingNames[row] != null)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var b = action.bindings[i];
                    if (b.isPartOfComposite && b.name == _subBindingNames[row])
                    {
                        action.ApplyBindingOverride(i, bindingPath);
                        _currentDisplay[row] = FormatKeyName(keyName);
                        _bindingPaths[row] = bindingPath;
                        Debug.Log($"[Controls] Row {row} → {keyName}");
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var b = action.bindings[i];
                    if (b.path.StartsWith("<Keyboard>"))
                    {
                        action.ApplyBindingOverride(i, bindingPath);
                        _currentDisplay[row] = FormatKeyName(keyName);
                        _bindingPaths[row] = bindingPath;
                        Debug.Log($"[Controls] Row {row} → {keyName}");
                        break;
                    }
                }
            }

            _settings.SetControls(_bindingPaths);
        }

        private void ResetAll()
        {
            var actions = _inputManager.PlayerInput.actions;
            var playerMap = actions.FindActionMap("Player");
            playerMap.RemoveAllBindingOverrides();

            _bindingPaths = new[]
            {
                "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d",
                "<Keyboard>/space", "<Keyboard>/shift", "<Keyboard>/leftCtrl", "<Keyboard>/o"
            };

            LoadCurrentBindings();
            RebuildAll();
            _settings.SetControls(_bindingPaths);
            Debug.Log("[Controls] Reset to defaults.");
        }

        #endregion

        #region Navigation

        private void SelectRow(int index)
        {
            int prev = _selectedRow;
            _selectedRow = Mathf.Clamp(index, 0, _rows.Length - 1);

            if (prev != _selectedRow)
            {
                UpdateRow(prev);
                UpdateRow(_selectedRow);
            }
        }

        #endregion

        #region Binding Helpers

        private void LoadCurrentBindings()
        {
            for (int i = 0; i < _actionNames.Length; i++)
            {
                if (_actionNames[i] == null)
                {
                    _currentDisplay[i] = "PRESS ENTER";
                    continue;
                }
                _currentDisplay[i] = FormatPath(_bindingPaths[i]);
            }
        }

        private static string FormatPath(string path)
        {
            if (!path.StartsWith("<Keyboard>/")) return path;
            return FormatKeyName(path.Substring("<Keyboard>/".Length));
        }

        private static string FormatKeyName(string raw)
        {
            switch (raw)
            {
                case "space": return "Space";
                case "leftShift": return "L-Shift";
                case "rightShift": return "R-Shift";
                case "leftCtrl": return "L-Ctrl";
                case "rightCtrl": return "R-Ctrl";
                case "leftAlt": return "L-Alt";
                case "rightAlt": return "R-Alt";
                case "escape": return "Esc";
                case "leftArrow": return "←";
                case "rightArrow": return "→";
                case "upArrow": return "↑";
                case "downArrow": return "↓";
                case "tab": return "Tab";
                case "enter": return "Enter";
                default:
                    if (raw.Length == 1) return raw.ToUpper();
                    return raw;
            }
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

            if (_rebindState == RebindState.Waiting && row == _selectedRow)
                val = $"{_currentDisplay[row]} [ENTER NEW KEY]";
            else if (_rebindState == RebindState.Confirming && row == _selectedRow)
                val = $"{_currentDisplay[row]} → {FormatKeyName(_pendingKey)}";
            else if (_rowTypes[row] == RowType.Action)
                val = "PRESS ENTER";
            else
                val = _currentDisplay[row];

            _rows[row].valueText.text = (row == _selectedRow && _cursorVisible) ? $"{val} █" : val;
        }

        #endregion
    }
}   