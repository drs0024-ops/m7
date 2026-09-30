using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core.Audio;
using Game.Core.Enums;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Audio
{
    /// <summary>
    /// High-level audio manager. Routes music and SFX through AudioView,
    /// handles time-scale ducking, and exposes the IAudioManager interface.
    /// </summary>
    public class AudioManager : IAudioManager, IStartable, IDisposable
    {
        #region Dependencies

        private readonly AudioConfigSO _config;
        private readonly AudioView _view;
        private readonly IAudioMixer _mixer;
        private readonly ISubscriber<TimeScalePause> _pauseSub;
        private readonly ISubscriber<TimeScaleResume> _resumeSub;
        private readonly ISubscriber<StopMusic> _stopMusicSub;
        private readonly ISubscriber<PlayMusic> _playMusicSub;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new(4);
        private readonly Dictionary<MusicType, List<MusicTrack>> _musicTracks = new();
        private readonly Dictionary<SoundType, AudioClip> _clips = new(16);
        private readonly Dictionary<SoundType, bool> _sfxLoops = new(16);
        private readonly Dictionary<SoundType, (float min, float max)> _sfxPitchRanges = new(16);

        private AudioSource[] _activeMusicSources = Array.Empty<AudioSource>();
        private MusicType _currentMusicType = MusicType.None;
        private int _musicGeneration;
        private float _preDuckMusicVolume = 1f;
        private bool _disposed;

        #endregion

        #region Construction

        public AudioManager(
            AudioConfigSO config,
            AudioView view,
            IAudioMixer mixer,
            ISubscriber<TimeScalePause> pauseSub,
            ISubscriber<TimeScaleResume> resumeSub,
            ISubscriber<StopMusic> stopMusicSub,
            ISubscriber<PlayMusic> playMusicSub)
        {
            _config = config;
            _view = view;
            _mixer = mixer;
            _pauseSub = pauseSub;
            _resumeSub = resumeSub;
            _stopMusicSub = stopMusicSub;
            _playMusicSub = playMusicSub;
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            if (_disposed) return;

            InitializeClips();
            SetupMixerGroups();

            _subscriptions.Add(_pauseSub.Subscribe(OnTimeScalePause));
            _subscriptions.Add(_resumeSub.Subscribe(OnTimeScaleResume));
            _subscriptions.Add(_stopMusicSub.Subscribe(_ => StopMusic()));
            _subscriptions.Add(_playMusicSub.Subscribe(OnPlayMusic));
        }

        #endregion

        #region IAudioManager: Music

        public MusicType CurrentMusic => _currentMusicType;

        public bool IsMusicPlaying(MusicType type)
        {
            if (type != _currentMusicType) return false;
            for (int i = 0; i < _activeMusicSources.Length; i++)
                if (_activeMusicSources[i] != null && _activeMusicSources[i].isPlaying)
                    return true;
            return false;
        }

        public void PlayMusic(MusicType type)
        {
            if (_disposed) return;
            if (type == _currentMusicType) return;
            if (!_musicTracks.TryGetValue(type, out var tracks) || tracks == null) return;

            _currentMusicType = type;
            int generation = ++_musicGeneration;
            _view.StopMusic();
            PlayMusicTracks(tracks, generation).Forget();
        }

        public void PlayMusic(AudioClip clip, bool restart = false)
        {
            if (_disposed) return;
            if (clip == null) return;
            ++_musicGeneration;
            _view.StopMusic();
            _currentMusicType = MusicType.None;
            _view.PlayMusic(clip, loop: true);
        }

        public void StopMusic()
        {
            ++_musicGeneration;
            _view.StopMusic();
            _currentMusicType = MusicType.None;
            _activeMusicSources = Array.Empty<AudioSource>();
        }

        #endregion

        #region IAudioManager: SFX

        public void PlaySFX(SoundType soundType)
        {
            if (_disposed) return;

            if (_clips.TryGetValue(soundType, out var clip))
            {
                if (clip == null)
                {
                    Debug.LogWarning($"[Audio] SFX '{soundType}' has no clip and no fallback.");
                    return;
                }

                bool loop = _sfxLoops.TryGetValue(soundType, out var l) && l;
                float pitch = _sfxPitchRanges.TryGetValue(soundType, out var range)
                    ? UnityEngine.Random.Range(range.min, range.max)
                    : 1f;
                _view.PlaySFX(clip, 1f, loop, pitch);
            }
            else if (_config.fallbackClip != null)
                _view.PlaySFX(_config.fallbackClip, 1f, false, 1f);
            else
                Debug.LogWarning($"[Audio] SFX '{soundType}' not found.");
        }

        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (_disposed) return;
            if (clip == null) return;
            _view.PlaySFX(clip, volume);
        }

        public void StopSFX(SoundType soundType)
        {
            if (_disposed) return;

            if (_clips.TryGetValue(soundType, out var clip) && clip != null)
                _view.StopSFX(clip);
        }

        public void StopAllSFX()
        {
            if (_disposed) return;
            _view.StopAllSFX();
        }

        #endregion

        #region Internal: Music

        private async UniTask PlayMusicTracks(List<MusicTrack> tracks, int generation)
        {
            _activeMusicSources = new AudioSource[tracks.Count];

            for (int i = 0; i < tracks.Count; i++)
            {
                if (generation != _musicGeneration) return;
                if (tracks[i].clip == null) continue;

                if (tracks[i].delay > 0f)
                    await UniTask.Delay((int)(tracks[i].delay * 1000), ignoreTimeScale: true);

                if (generation != _musicGeneration) return;

                _activeMusicSources[i] = _view.PlayMusic(tracks[i].clip, tracks[i].loop, tracks[i].volume);
            }
        }

        #endregion

        #region Internal: Setup

        private void InitializeClips()
        {
            _clips.Clear();
            _musicTracks.Clear();
            _sfxLoops.Clear();
            _sfxPitchRanges.Clear();

            if (_config.audioClips != null)
            {
                for (int i = 0; i < _config.audioClips.Count; i++)
                {
                    var entry = _config.audioClips[i];
                    _clips[entry.soundType] = entry.clip ?? _config.fallbackClip;
                    _sfxLoops[entry.soundType] = entry.loop;
                }
            }

            if (_config.musicClips != null)
            {
                for (int i = 0; i < _config.musicClips.Count; i++)
                    _musicTracks[_config.musicClips[i].type] = _config.musicClips[i].tracks;
            }
        }

        private void SetupMixerGroups()
        {
            var mixer = _config.audioMixer;
            if (mixer == null) return;

            var sfx = mixer.FindMatchingGroups("SFX");
            var music = mixer.FindMatchingGroups("Music");

            if (sfx.Length > 0)
                _view.SetSfxOutputGroup(sfx[0]);
            if (music.Length > 0)
                _view.SetMusicOutputGroup(music[0]);
        }

        #endregion

        #region Message Handlers

        private void OnTimeScalePause(TimeScalePause _)
        {
            _preDuckMusicVolume = _mixer.MusicVolume;
            _mixer.MusicVolume = _config.musicDuckVolume;
        }

        private void OnTimeScaleResume(TimeScaleResume _)
        {
            _mixer.MusicVolume = _preDuckMusicVolume;
        }

        private void OnPlayMusic(PlayMusic msg) => PlayMusic(msg.Clip, msg.Restart);

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}   