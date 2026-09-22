using Game.Core.Enums;
using UnityEngine;

namespace Game.Gameplay.Audio
{
	[System.Serializable]
    public class AudioLibraryEntry
    {
        public SoundType soundType;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        public bool loop = false;
        [Range(0.5f, 2f)]
        public float pitchMin = 1f;
        [Range(0.5f, 2f)]
        public float pitchMax = 1f;
    }

}

