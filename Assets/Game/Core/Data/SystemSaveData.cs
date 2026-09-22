using System;
using Game.Core.Interfaces;

namespace Game.Core.Data
{
    [Serializable]
    public struct SystemSaveData : ISaveData
    {
        public string SaveId => "SystemSettings";
        public int InputDevice;
        public int TextSpeed;
        public int AutoSave;
        public int Difficulty;
        public int Language;
        public int ReduceFlashing;
        public int ShowFPS;
        public int CRTMode;
    }
}
