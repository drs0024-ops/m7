using Game.Core.Enums;
using UnityEngine;


public class AudioSignals  {}

namespace Game.Core.Messages
{
    public readonly struct StopMusic
    {
        public static StopMusic Default => new();
    }

    public readonly struct PlayMusic
    {
        public AudioClip Clip { get; }
        public bool Restart { get; }
        public PlayMusic(AudioClip clip, bool restart = false)
        {
            Clip = clip;
            Restart = restart;
        }
    }

    public readonly struct SetVolume
    {
        public float Volume { get; }
        public SetVolume(float volume) => Volume = volume;
    }

    // Channel payload — value type, no AudioClip reference
public readonly struct AudioEvent
{
    public readonly SoundType Type;
    public readonly float Volume;

    public AudioEvent(SoundType type, float volume = 1f)
    {
        Type = type;
        Volume = volume;
    }
}

public readonly struct MusicEvent
{
    public readonly MusicType Type;
    public readonly bool Restart;

    public MusicEvent(MusicType type, bool restart = false)
    {
        Type = type;
        Restart = restart;
    }
}
}