using System.Collections.Generic;
using Game.Core.Audio;
using Game.Core.Enums;

namespace Game.Gamplay.Audio {

	[System.Serializable]
    public class MusicEntry
    {
        public MusicType type;
        public List<MusicTrack> tracks = new();
    }
}