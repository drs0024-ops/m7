using UnityEngine;
using UnityEngine.Audio;
using Game.Core.Interfaces;

namespace Game.Gameplay.Audio
{
    /// <summary>
    /// Bridges normalized (0–1) volume values to the Unity AudioMixer.
    /// Each property setter pushes immediately to the mixer graph.
    /// GameVolume controls the duckable game audio bus (music + SFX).
    /// </summary>
    public class AudioMixerMaster : MonoBehaviour, IAudioMixer
    {
        #region Dependencies

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private string _masterParam = "MasterVolume";
        [SerializeField] private string _musicParam = "MusicVolume";
        [SerializeField] private string _sfxParam = "SFXVolume";
        [SerializeField] private string _gameParam = "GameVolume";

        #endregion

        #region State

        private const float MIN_DB = -80f;
        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private float _gameVolume = 1f;

        #endregion

        #region IAudioMixer

        public float MasterVolume
        {
            get => _masterVolume;
            set { _masterVolume = Mathf.Clamp01(value); Set(_masterParam, _masterVolume); }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set { _musicVolume = Mathf.Clamp01(value); Set(_musicParam, _musicVolume); }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Mathf.Clamp01(value); Set(_sfxParam, _sfxVolume); }
        }

        #endregion

        #region Game Audio Duck

        /// <summary>
        /// Normalized volume for the game audio bus (music + SFX combined).
        /// Set to ~0.15 during cutscenes, back to 1.0f after.
        /// </summary>
        public float GameVolume
        {
            get => _gameVolume;
            set { _gameVolume = Mathf.Clamp01(value); Set(_gameParam, _gameVolume); }
        }

        public void DuckGameAudio(float targetVolume, float fadeDuration)
        {
            if (_mixer == null) return;

            float currentDb = ToDecibels(_gameVolume);
            float targetDb = ToDecibels(targetVolume);

            LeanTween.value(this.gameObject, (float v) =>
            {
                _mixer.SetFloat(_gameParam, v);
                _gameVolume = FromDecibels(v);
            }, currentDb, targetDb, fadeDuration)
                .setEase(LeanTweenType.linear);
        }

        public void RestoreGameAudio(float fadeDuration)
        {
            if (_mixer == null) return;

            float currentDb = ToDecibels(_gameVolume);
            float targetDb = ToDecibels(1f);

            LeanTween.value(this.gameObject, (float v) =>
            {
                _mixer.SetFloat(_gameParam, v);
                _gameVolume = FromDecibels(v);
            }, currentDb, targetDb, fadeDuration)
                .setEase(LeanTweenType.linear);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_mixer == null)
            {
                Debug.LogError("[AudioMixerMaster] _mixer is not assigned!", this);
                return;
            }

            ApplyAll();
        }

        #endregion

        #region Internal

        private void ApplyAll()
        {
            Set(_masterParam, _masterVolume);
            Set(_musicParam, _musicVolume);
            Set(_sfxParam, _sfxVolume);
            Set(_gameParam, _gameVolume);
        }

        private void Set(string param, float normalized)
        {
            if (_mixer == null) return;
            _mixer.SetFloat(param, ToDecibels(normalized));
        }

        private static float ToDecibels(float normalized)
        {
            if (normalized <= 0.001f) return MIN_DB;
            return 20f * Mathf.Log10(Mathf.Clamp01(normalized));
        }

        private static float FromDecibels(float db)
        {
            if (db <= MIN_DB) return 0f;
            return Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
        }

        #endregion
    }
}   