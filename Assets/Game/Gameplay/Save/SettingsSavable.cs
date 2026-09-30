using System;
using Game.Core.Interfaces;
using Game.Core.Data;
using Game.Gameplay.Player;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Gameplay.Save
{
    public class SettingsSavable : ISaveable, IStartable, IDisposable
    {
        private const string SAVE_ID = "Settings";

        private readonly IAudioMixer _mixer;
        private readonly InputManager _inputManager;
        private readonly ISaveableRegistry _registry;

        // Audio
        private float _masterVolume = 1f;
        private float _musicVolume = 0.7f;
        private float _sfxVolume = 1f;

        // Video
        private int _displayMode = 1;
        private int _vsync = 1;
        private int _screenShake = 0;
        private float _crtSlider = 0.5f;
        private int _colorblind = 0;
        private int _resolution = 0;
        private int _refreshRate = 3;

        // System
        private int _inputDevice = 0;
        private int _textSpeed = 1;
        private int _autoSave = 0;
        private int _difficulty = 1;
        private int _language = 0;
        private int _reduceFlashing = 1;
        private int _showFPS = 1;
        private int _crtMode = 0;

        public int GetTextSpeed() => _textSpeed;
        
        // Controls (8 binding paths)
        private string[] _bindingPaths = {
            "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d",
            "<Keyboard>/space", "<Keyboard>/shift", "<Keyboard>/leftCtrl", "<Keyboard>/o"
        };

        private bool _disposed;
        
        public SettingsSavable(IAudioMixer mixer, InputManager inputManager, ISaveableRegistry registry)
        {
            _mixer = mixer;
            _inputManager = inputManager;
            _registry = registry;
        }

        #region IStartable

        void IStartable.Start()
        {
            _registry.Register(this);
        }
        
        #endregion

        public string SaveId => SAVE_ID;

        #region ISaveable

        public ISaveData GetSaveData()
        {
            // Update audio from live mixer state
            _masterVolume = _mixer.MasterVolume;
            _musicVolume = _mixer.MusicVolume;
            _sfxVolume = _mixer.SFXVolume;

            return new SettingsSaveData
            {
                MasterVolume = _masterVolume,
                MusicVolume = _musicVolume,
                SfxVolume = _sfxVolume,
                DisplayMode = _displayMode,
                VSync = _vsync,
                ScreenShake = _screenShake,
                CRTSlider = _crtSlider,
                Colorblind = _colorblind,
                Resolution = _resolution,
                RefreshRate = _refreshRate,
                InputDevice = _inputDevice,
                TextSpeed = _textSpeed,
                AutoSave = _autoSave,
                Difficulty = _difficulty,
                Language = _language,
                ReduceFlashing = _reduceFlashing,
                ShowFPS = _showFPS,
                CRTMode = _crtMode,
                BindingPaths = _bindingPaths
            };
        }

        public void LoadFromData(ISaveData data)
        {
            if (data is not SettingsSaveData s) return;

            // Audio
            _masterVolume = s.MasterVolume;
            _musicVolume = s.MusicVolume;
            _sfxVolume = s.SfxVolume;
            _mixer.MasterVolume = _masterVolume;
            _mixer.MusicVolume = _musicVolume;
            _mixer.SFXVolume = _sfxVolume;

            // Video
            _displayMode = s.DisplayMode;
            _vsync = s.VSync;
            _screenShake = s.ScreenShake;
            _crtSlider = s.CRTSlider;
            _colorblind = s.Colorblind;
            _resolution = s.Resolution;
            _refreshRate = s.RefreshRate;

            // System
            _inputDevice = s.InputDevice;
            _textSpeed = s.TextSpeed;
            _autoSave = s.AutoSave;
            _difficulty = s.Difficulty;
            _language = s.Language;
            _reduceFlashing = s.ReduceFlashing;
            _showFPS = s.ShowFPS;
            _crtMode = s.CRTMode;

            // Controls
            if (s.BindingPaths != null && s.BindingPaths.Length == 8)
            {
                _bindingPaths = s.BindingPaths;
                ApplyControlOverrides();
            }
        }

        #endregion

        #region Public API (called by panels on change)

        public void SetAudio(float master, float music, float sfx)
        {
            _masterVolume = master;
            _musicVolume = music;
            _sfxVolume = sfx;
        }

        public void SetVideo(int display, int vsync, int shake, float crt, int colorblind, int res, int rate)
        {
            _displayMode = display;
            _vsync = vsync;
            _screenShake = shake;
            _crtSlider = crt;
            _colorblind = colorblind;
            _resolution = res;
            _refreshRate = rate;
        }

        public void SetSystem(int input, int text, int auto, int diff, int lang, int flash, int fps, int crt)
        {
            _inputDevice = input;
            _textSpeed = text;
            _autoSave = auto;
            _difficulty = diff;
            _language = lang;
            _reduceFlashing = flash;
            _showFPS = fps;
            _crtMode = crt;
        }

        public void SetControls(string[] paths)
        {
            if (paths != null && paths.Length == 8)
                _bindingPaths = paths;
        }

        public string[] GetBindingPaths() => _bindingPaths;
        public float GetCRTSlider() => _crtSlider;
        public int GetCRTMode() => _crtMode;

        #endregion

        #region Controls Apply

        private void ApplyControlOverrides()
        {
            var actions = _inputManager.PlayerInput.actions;
            var playerMap = actions.FindActionMap("Player");

            // Move composite (4 sub-bindings)
            var moveAction = playerMap?.FindAction("Move");
            if (moveAction != null)
            {
                string[] subNames = { "up", "down", "left", "right" };
                for (int i = 0; i < 4; i++)
                {
                    for (int b = 0; b < moveAction.bindings.Count; b++)
                    {
                        var binding = moveAction.bindings[b];
                        if (binding.isPartOfComposite && binding.name == subNames[i])
                        {
                            moveAction.ApplyBindingOverride(b, _bindingPaths[i]);
                            break;
                        }
                    }
                }
            }

            // Simple actions (4)
            string[] actionNames = { "Jump", "Run", "Attack", "Interact" };
            for (int i = 0; i < actionNames.Length; i++)
            {
                var action = playerMap?.FindAction(actionNames[i]);
                if (action == null) continue;

                for (int b = 0; b < action.bindings.Count; b++)
                {
                    if (action.bindings[b].path.StartsWith("<Keyboard>"))
                    {
                        action.ApplyBindingOverride(b, _bindingPaths[4 + i]);
                        break;
                    }
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _registry.Unregister(this);
        }

        #endregion
    }
}   