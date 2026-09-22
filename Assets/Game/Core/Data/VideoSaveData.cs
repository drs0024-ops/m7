using System;
using Game.Core.Interfaces;

namespace Game.Core.Data
{
    [Serializable]
    public struct VideoSaveData : ISaveData
    {
        public string SaveId => "VideoSettings";

        public bool HasPlayed { get; set; }

        public int DisplayMode;
        public int VSync;
        public int ScreenShake;
        public float CRTSlider;
        public int Colorblind;
        public int Resolution;
        public int RefreshRate;
    }
}