using UnityEngine;
using TMPro;
using VContainer;
using MessagePipe;
using Game.Core.Interfaces;
using Game.Core.Messages;
using Game.Gameplay.Player;
using Game.Gameplay.Save;

namespace Game.UI
{
    public class AudioSettingsUI : MonoBehaviour
    {
        [System.Serializable]
        public class AudioRow
        {
            public string label;
            public TextMeshProUGUI barText;
            public TextMeshProUGUI valueText;
        }

        #region Fields

        [Header("Defaults")]
        [SerializeField] private float[] _defaultValues = { 1f, 0.7f, 1f };

        [Header("Dependencies")]
        [Inject] private InputManager _inputManager;
        [Inject] private IAudioMixer _mixer;
        [Inject] private SettingsSavable _settings;

        [Header("Settings")]
        [SerializeField] private AudioRow[] _rows;
        [SerializeField] private int _barCharCount = 20;
        [SerializeField] private float _step = 0.05f;
        private float _blinkTimer;
        private bool _cursorVisible = true;
        [SerializeField] private float _blinkInterval = 0.5f;

        private IPublisher<SaveRequest> _savePublisher;

        private int _selectedRow;
        private float[] _values;

        private bool _upHeld, _downHeld, _leftHeld, _rightHeld;
        private bool _enabled;

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
                Debug.LogError("[AudioSettingsUI] _rows is not assigned!", this);
                return;
            }

            _values = new float[_rows.Length];
            for (int i = 0; i < _values.Length; i++)

            _values[i] = i < _defaultValues.Length ? _defaultValues[i] : 0f;
            _selectedRow = 0;
        }

        private void OnEnable()
        {
            _enabled = true;
            _upHeld = _downHeld = _leftHeld = _rightHeld = false;
            if (_mixer != null)
                ApplySavedValues();
            else
            {
                for (int i = 0; i < _rows.Length; i++)
                    UpdateBar(i);
            }
        }

        private void OnDisable()
        {
            _enabled = false;
            if (_enabled)
                _savePublisher.Publish(new SaveRequest("AudioSettings", false));
            _enabled = false;
        }

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

            // Value adjustment (continuous while held)
            if (left)
                AdjustValue(-_step);
            if (right)
                AdjustValue(+_step);

            _upHeld = up;
            _downHeld = down;
            _leftHeld = left;
            _rightHeld = right;

            // Blink cursor
            _blinkTimer += Time.unscaledDeltaTime;
            if (_blinkTimer >= _blinkInterval)
            {
                _blinkTimer = 0f;
                _cursorVisible = !_cursorVisible;
                UpdateBar(_selectedRow);
            }

            // Save (Enter)
            if (_inputManager.ConfirmWasPressed)
                Save();
        }   

        #endregion

        #region Save
        private void Save()
        {
            _savePublisher.Publish(new SaveRequest("AudioSettings", false));
            Debug.Log("[AudioSettings] Saved.");
        }
                #endregion
        #region Input Actions

        private void SelectRow(int index)
        {
            int prev = _selectedRow;
            _selectedRow = Mathf.Clamp(index, 0, _rows.Length - 1);

            if (prev != _selectedRow)
            {
                UpdateBar(prev);       // ← remove cursor from old row
                UpdateBar(_selectedRow); // ← add cursor to new row
            }
        }

        private void AdjustValue(float delta)
        {
            _values[_selectedRow] = Mathf.Clamp01(_values[_selectedRow] + delta);
            UpdateBar(_selectedRow);
            ApplyToMixer();
            _settings.SetAudio(_values[0], _values[1], _values[2]);
        }

        #endregion

        #region Mixer

        private void ApplyToMixer()
        {
            _mixer.MasterVolume = _values[0];
            _mixer.MusicVolume  = _values[1];
            _mixer.SFXVolume    = _values[2];
        }

        public void ApplySavedValues()
        {
            _values[0] = _mixer.MasterVolume;
            _values[1] = _mixer.MusicVolume;
            _values[2] = _mixer.SFXVolume;

            for (int i = 0; i < _rows.Length; i++)
                UpdateBar(i);

            //UpdateCursor(); // depreciated 9/13/26, moved to the end of value text
        }

        #endregion

        #region UI

        /* Depreciated 9/13/2026
        private void UpdateCursor()
        {
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].cursorText.text = (i == _selectedRow) ? ">" : " ";
        }
        */
        private void UpdateBar(int row)
        {
            int filled = Mathf.RoundToInt(_values[row] * _barCharCount);
            _rows[row].barText.text =
                $"[ {new string('█', filled)}{new string('░', _barCharCount - filled)} ]";

            string val = Mathf.RoundToInt(_values[row] * 100f).ToString("D2");
            _rows[row].valueText.text = (row == _selectedRow && _cursorVisible) ? $"{val} █" : val;
        }   

        #endregion

    }
}   