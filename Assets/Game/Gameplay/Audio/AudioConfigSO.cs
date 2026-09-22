using UnityEngine.Audio;
using System.Collections.Generic;
using UnityEngine;
using Game.Gamplay.Audio;

namespace Game.Gameplay.Audio
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Game/Audio/Audio Config")]
    public class AudioConfigSO : ScriptableObject
    {
        public AudioMixer audioMixer;
        public AudioClip fallbackClip;
        public List<MusicEntry> musicClips = new();
        public List<AudioLibraryEntry> audioClips = new();
        
        [Range(0f, 1f)] public float musicDuckVolume = 0.1f;
    }

}


