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

        [Header("Settings")]
        [SerializeField] private AudioRow[] _rows;
        [SerializeField] private int _barCharCount = 20;
        [SerializeField] private float _step = 0.05f;
        [SerializeField] private float _blinkInterval = 0.5f;

        private InputManager _inputManager;
        private IAudioMixer _mixer;
        private IPublisher<SaveRequest> _savePublisher;
        private IObjectResolver _container;

        private float _blinkTimer;
        private bool _cursorVisible = true;

        private int _selectedRow;
        private float[] _values;

        private bool _upHeld, _downHeld, _leftHeld, _rightHeld;
        private bool _enabled;

        #endregion

        #region Injection

        [Inject]
        private void Construct(
            InputManager inputManager,
            IAudioMixer mixer,
            IPublisher<SaveRequest> savePublisher,
            IObjectResolver container)
        {
            _inputManager = inputManager;
            _mixer = mixer;
            _savePublisher = savePublisher;
            _container = container;
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
            _savePublisher.Publish(new SaveRequest("AudioSettings", false));
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

            if (up && !_upHeld)
                SelectRow(_selectedRow - 1);
            else if (down && !_downHeld)
                SelectRow(_selectedRow + 1);

            if (left)
                AdjustValue(-_step);
            if (right)
                AdjustValue(+_step);

            _upHeld = up;
            _downHeld = down;
            _leftHeld = left;
            _rightHeld = right;

            _blinkTimer += Time.unscaledDeltaTime;
            if (_blinkTimer >= _blinkInterval)
            {
                _blinkTimer = 0f;
                _cursorVisible = !_cursorVisible;
                UpdateBar(_selectedRow);
            }

            if (_inputManager.ConfirmWasPressed)
                Save();
        }

        #endregion

        #region Save

        private void Save()
        {
            _savePublisher.Publish(new SaveRequest("AudioSettings", false));
        }

        #endregion

        #region Input Actions

        private void SelectRow(int index)
        {
            int prev = _selectedRow;
            _selectedRow = Mathf.Clamp(index, 0, _rows.Length - 1);

            if (prev != _selectedRow)
            {
                UpdateBar(prev);
                UpdateBar(_selectedRow);
            }
        }

        private void AdjustValue(float delta)
        {
            _values[_selectedRow] = Mathf.Clamp01(_values[_selectedRow] + delta);
            UpdateBar(_selectedRow);
            ApplyToMixer();

            _container.Resolve<SettingsSavable>()
                .SetAudio(_values[0], _values[1], _values[2]);
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
        }

        #endregion

        #region UI

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