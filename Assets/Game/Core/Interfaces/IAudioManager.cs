using Game.Core.Enums;
using UnityEngine;

namespace Game.Core.Interfaces
{
    public interface IAudioManager
    {
        // --- Music ---
        void PlayMusic(MusicType type);
        void PlayMusic(AudioClip clip, bool restart = false);
        void StopMusic();

        // --- SFX ---
        void PlaySFX(SoundType soundType);
        void PlaySFX(AudioClip clip, float volume = 1.0f);
        void StopSFX(SoundType soundType);
        void StopAllSFX();

        // --- State ---
        bool IsMusicPlaying(MusicType type);
        MusicType CurrentMusic { get; }
    }
}   