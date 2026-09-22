using System;
using Game.Core.Interfaces;
/// <summary>
/// Modular save data chunk for Player Upgrades.
/// </summary>

namespace Game.Core.Data
{
    [Serializable]
    public struct AudioSaveData : ISaveData
    {
        public string SaveId => "AudioSettings"; // Unique ID for the SaveManager dictionary
        
        // Store float values
        public float MasterVolume;
        public float MusicVolume;
        public float SfxVolume;
    }  
}
