using UnityEngine;
using TMPro;
using VContainer;
using MessagePipe;
using Game.Core.Messages;
using Game.Gameplay.Player;
using Game.Gameplay.Save;

namespace Game.UI
{
    public class VideoSettingsUI : MonoBehaviour
    {
        [System.Serializable]
        public class SettingRow
        {
            public string label;
            public TextMeshProUGUI valueText;
        }
        

        private enum RowType { Cycle, Toggle, Slider }

        #region Fields

        [Header("Dependencies")]
        [Inject] private InputManager _inputManager;
        [Inject] private SettingsSavable _settings;

        [Header("Settings")]
        [SerializeField] private SettingRow[] _rows;
        [SerializeField] private int _barCharCount = 20;
        [SerializeField] private float _blinkInterval = 0.5f;
        [SerializeField] private int[] _defaultIndices = { 1, 1, 0, 0, 0, 0, 3 };
        [SerializeField] private float _defaultCRTSlider = 0.5f;

        private IPublisher<SaveRequest> _savePublisher;
        private IPublisher<CRTIntensityChanged> _crtIntensityPublisher;

        private string[][] _options;
        private int[] _valueIndices;
        private RowType[] _rowTypes;

        private int _selectedRow;
        private bool _enabled;

        private bool _upHeld, _downHeld, _leftHeld, _rightHeld;
        private float _blinkTimer;
        private bool _cursorVisible = true;

        private float _sliderValue; // CRT Effect (0-1)

        #endregion

        #region Injection

        [Inject]
        public void Construct(
            IPublisher<SaveRequest> savePublisher,
            IPublisher<CRTIntensityChanged> crtIntensityPublisher)
        {
            _savePublisher = savePublisher;
            _crtIntensityPublisher = crtIntensityPublisher;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_rows == null || _rows.Length == 0)
            {
                Debug.LogError("[VideoSettings] _rows not assigned!", this);
                return;
            }

            int count = _rows.Length;
            _options = new string[count][];
            _valueIndices = new int[count];
            _rowTypes = new RowType[count];

            // ── Row 0: Display Mode (Cycle) ──
            _options[0] = new[] { "FULL", "BORDERLESS", "WINDOW" };
            _rowTypes[0] = RowType.Cycle;
            _valueIndices[0] = 1; // default Borderless

            // ── Row 1: VSync (Toggle) ──
            _options[1] = new[] { "ON", "OFF" };
            _rowTypes[1] = RowType.Toggle;
            _valueIndices[1] = 0;

            // ── Row 2: Screen Shake (Toggle) ──
            _options[2] = new[] { "ON", "OFF" };
            _rowTypes[2] = RowType.Toggle;
            _valueIndices[2] = 0;

            // ── Row 3: CRT Effect (Slider) ──
            _options[3] = new[] { "" };
            _rowTypes[3] = RowType.Slider;
            _sliderValue = 0.5f;

            // ── Row 4: Colorblind (Cycle) ──
            _options[4] = new[] { "OFF", "DEUTERANOPIA", "PROTANPIA", "TRITANOPIA" };
            _rowTypes[4] = RowType.Cycle;
            _valueIndices[4] = 0;

            // ── Row 5: Resolution (Cycle - dynamic) ──
            _rowTypes[5] = RowType.Cycle;
            _valueIndices[5] = 0;
            BuildResolutionOptions();

            // ── Row 6: Refresh Rate (Cycle - dynamic) ──
            _rowTypes[6] = RowType.Cycle;
            _valueIndices[6] = 0;

            for (int i = 0; i < _valueIndices.Length; i++)
                _valueIndices[i] = i < _defaultIndices.Length ? _defaultIndices[i] : 0;

            _sliderValue = _defaultCRTSlider;

            BuildRefreshRateOptions();

            _selectedRow = 0;
        }

        private void OnEnable()
        {
            _enabled = true;
            _upHeld = _downHeld = _leftHeld = _rightHeld = false;
            _blinkTimer = 0f;
            _cursorVisible = true;
            RebuildAll();
        }

        private void OnDisable()
        {
            if (_enabled)
                _savePublisher.Publish(new SaveRequest("VideoSettings", false));
            _enabled = false;
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

            // Row navigation (edge-detected)
            if (up && !_upHeld)
                SelectRow(_selectedRow - 1);
            else if (down && !_downHeld)
                SelectRow(_selectedRow + 1);

            // Value adjustment
            if (left && !_leftHeld)
                AdjustValue(-1);
            else if (right && !_rightHeld)
                AdjustValue(+1);

            _upHeld = up;
            _downHeld = down;
            _leftHeld = left;
            _rightHeld = right;

            // Save (Enter)
            if (_inputManager.ConfirmWasPressed)
                Save();

            // Blink cursor
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
                UpdateRow(prev);
                UpdateRow(_selectedRow);
                Debug.Log($"[VideoSettings] Row: {_selectedRow}");
            }
        }

        private void AdjustValue(int direction)
        {
            switch (_rowTypes[_selectedRow])
            {
                case RowType.Cycle:
                    int count = _options[_selectedRow].Length;
                    _valueIndices[_selectedRow] = (_valueIndices[_selectedRow] + direction + count) % count;
                    UpdateRow(_selectedRow);
                    ApplyRow(_selectedRow);
                    break;

                case RowType.Toggle:
                    _valueIndices[_selectedRow] ^= 1;
                    UpdateRow(_selectedRow);
                    ApplyRow(_selectedRow);
                    break;

                case RowType.Slider:
                    _sliderValue = Mathf.Clamp01(_sliderValue + direction * 0.05f);
                    UpdateRow(_selectedRow);
                    ApplyRow(_selectedRow);
                    break;
            }
        }

        private void Save()
        {
            _savePublisher.Publish(new SaveRequest("VideoSettings", false));
            Debug.Log("[VideoSettings] Saved.");
        }

        #endregion

        #region Apply

        private static readonly FullScreenMode[] _displayModes =
        {
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.FullScreenWindow,   // ← this IS "Borderless"
            FullScreenMode.Windowed
        };


        private void ApplyRow(int row)
        {
            switch (row)
            {
                case 0: // Display Mode
                    Screen.fullScreenMode = _displayModes[_valueIndices[row]];
                    break;

                case 1: // VSync
                    Application.targetFrameRate = _valueIndices[row] == 0 ? -1 : 144;
                    break;

                case 3: // CRT Effect
                    _crtIntensityPublisher.Publish(new CRTIntensityChanged(_sliderValue));
                    break;

                case 5: // Resolution
                    ApplyResolution();
                    break;

                case 6: // Refresh Rate
                    ApplyRefreshRate();
                    break;
            }

            _settings.SetVideo(_valueIndices[0], _valueIndices[1], _valueIndices[2], _sliderValue, _valueIndices[4], _valueIndices[5], _valueIndices[6]);

        }

        private void ApplyResolution()
        {
            if (_options[5] == null || _valueIndices[5] >= _options[5].Length) return;
            var res = Screen.resolutions[_valueIndices[5]];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        }   

        private void ApplyRefreshRate()
        {
            if (_options[5] == null || _valueIndices[5] >= _options[5].Length) return;
            if (_options[6] == null || _valueIndices[6] >= _options[6].Length) return;

            var res = Screen.resolutions[_valueIndices[5]];

            // "Monitor" = let OS pick native rate
            if (_valueIndices[6] == 3)
            {
                Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
                return;
            }

            int hz = int.Parse(_options[6][_valueIndices[6]].Replace("Hz", ""));
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode,
                new RefreshRate { numerator = (uint)hz, denominator = 1 });
        }  

        #endregion

        #region Dynamic Options

        private void BuildResolutionOptions()
        {
            var resolutions = Screen.resolutions;
            var names = new string[resolutions.Length];
            for (int i = 0; i < resolutions.Length; i++)
                names[i] = $"{resolutions[i].width}x{resolutions[i].height}";

            _options[5] = names;

            // Default to current resolution
            int current = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width == Screen.currentResolution.width
                    && resolutions[i].height == Screen.currentResolution.height)
                {
                    current = i;
                    break;
                }
            }
            _valueIndices[5] = current;
        }

        private void BuildRefreshRateOptions()
        {
            _options[6] = new[] { "60Hz", "120Hz", "144Hz", "Monitor" };

            int current = 0;
            int rate = (int)Screen.currentResolution.refreshRateRatio.numerator;
            if (rate == 120) current = 1;
            else if (rate == 144) current = 2;
            else if (rate > 144) current = 3; // "Monitor" = native rate
            _valueIndices[6] = current;
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

            if (_rowTypes[row] == RowType.Slider)
            {
                int filled = Mathf.RoundToInt(_sliderValue * _barCharCount);
                val = $"[ {new string('█', filled)}{new string('░', _barCharCount - filled)} ] {Mathf.RoundToInt(_sliderValue * 100f):D2}";
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