using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Gameplay.Audio
{
    /// <summary>
    /// Low-level audio executor. Manages AudioSource pools for music and SFX.
    /// Knows nothing about MusicType/SoundType — works with raw AudioClips.
    /// Volume and routing are handled by IAudioMixer.
    /// </summary>
    public class AudioView : MonoBehaviour
    {
        #region Dependencies

        [Header("Audio Configuration")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private int _sfxPoolSize = 8;

        #endregion

        #region State

        private readonly List<AudioSource> _musicPool = new();
        private int _musicIndex;
        private AudioMixerGroup _musicOutputGroup;

        private AudioSource[] _sfxPool;
        private int _sfxIndex;
        private float _lastStopTime = -10f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_audioMixer == null)
            {
                Debug.LogError("[AudioView] AudioMixer is missing!", this);
                return;
            }

            _sfxPool = new AudioSource[_sfxPoolSize];
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var go = new GameObject($"Sfx_{i}");
                go.transform.SetParent(transform);
                _sfxPool[i] = go.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
                _sfxPool[i].loop = false;
            }
        }

        private void Update()
        {
            if (_lastStopTime > 0f && Time.unscaledTime - _lastStopTime > 2f)
            {
                for (int i = _musicPool.Count - 1; i >= 0; i--)
                {
                    if (_musicPool[i] == null || !_musicPool[i].isPlaying)
                    {
                        if (_musicPool[i] != null)
                            Destroy(_musicPool[i].gameObject);
                        _musicPool.RemoveAt(i);
                    }
                }
                _lastStopTime = -10f;
            }
        }   

        #endregion

        #region Public API: SFX

        public void PlaySFX(AudioClip clip, float volume = 1f, bool loop = false, float pitch = 1f)
        {
            var source = _sfxPool[_sfxIndex];
            _sfxIndex = (_sfxIndex + 1) % _sfxPool.Length;

            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.loop = loop;
            source.Play();
        }

        public void StopSFX(AudioClip clip)
        {
            for (int i = 0; i < _sfxPool.Length; i++)
            {
                if (_sfxPool[i].clip == clip && _sfxPool[i].isPlaying)
                    _sfxPool[i].Stop();
            }
        }

        public void StopAllSFX()
        {
            for (int i = 0; i < _sfxPool.Length; i++)
            {
                if (_sfxPool[i] != null && _sfxPool[i].isPlaying)
                    _sfxPool[i].Stop();
            }
        }   

        #endregion

        #region Public API: Music

        public AudioSource PlayMusic(AudioClip clip, bool loop, float volume = 1f)
        {
            var source = GetOrCreateMusicSource();

            source.clip = clip;
            source.loop = loop;
            source.volume = volume;
            source.Play();
            return source;
        }

        public void StopMusic()
        {
            for (int i = 0; i < _musicPool.Count; i++)
            {
                if (_musicPool[i] != null && _musicPool[i].isPlaying)
                    _musicPool[i].Stop();
            }

            _lastStopTime = Time.unscaledTime;
        }

        #endregion

        #region Public API: Routing

        public void SetSfxOutputGroup(AudioMixerGroup group)
        {
            for (int i = 0; i < _sfxPool.Length; i++)
                _sfxPool[i].outputAudioMixerGroup = group;
        }

        public void SetMusicOutputGroup(AudioMixerGroup group)
        {
            _musicOutputGroup = group;
            for (int i = 0; i < _musicPool.Count; i++)
                _musicPool[i].outputAudioMixerGroup = group;
        }

        #endregion

        #region Internal

        private AudioSource GetOrCreateMusicSource()
        {
            for (int i = 0; i < _musicPool.Count; i++)
            {
                var source = _musicPool[(_musicIndex + i) % _musicPool.Count];
                if (!source.isPlaying)
                {
                    _musicIndex = (_musicIndex + i + 1) % _musicPool.Count;
                    return source;
                }
            }

            var go = new GameObject($"Music_{_musicPool.Count}");
            go.transform.SetParent(transform);
            var newSource = go.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            _musicPool.Add(newSource);

            if (_musicOutputGroup != null)
                newSource.outputAudioMixerGroup = _musicOutputGroup;

            _musicIndex = (_musicIndex + 1) % _musicPool.Count;
            return newSource;
        }

        #endregion
    }
}   