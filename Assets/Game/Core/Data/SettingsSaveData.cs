using System;
using Game.Core.Interfaces;

namespace Game.Core.Data
{
    [Serializable]
    public struct SettingsSaveData : ISaveData
    {
        public string SaveId => "Settings";

        // Audio
        public float MasterVolume;
        public float MusicVolume;
        public float SfxVolume;

        // Video
        public int DisplayMode;
        public int VSync;
        public int ScreenShake;
        public float CRTSlider;
        public int Colorblind;
        public int Resolution;
        public int RefreshRate;

        // System
        public int InputDevice;
        public int TextSpeed;
        public int AutoSave;
        public int Difficulty;
        public int Language;
        public int ReduceFlashing;
        public int ShowFPS;
        public int CRTMode;

        // Controls
        public string[] BindingPaths;
    }
}   