using UnityEngine;
using UnityEngine.Audio;
using Game.Core.Interfaces;

namespace Game.Gameplay.Audio
{
    /// <summary>
    /// Bridges normalized (0–1) volume values to the Unity AudioMixer.
    /// Each property setter pushes immediately to the mixer graph.
    /// </summary>
    public class AudioMixerMaster : MonoBehaviour, IAudioMixer
    {
        #region Dependencies

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private string _masterParam = "MasterVolume";
        [SerializeField] private string _musicParam = "MusicVolume";
        [SerializeField] private string _sfxParam = "SFXVolume";

        #endregion

        #region State

        private const float MIN_DB = -80f;
        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

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

        #endregion
    }
}   