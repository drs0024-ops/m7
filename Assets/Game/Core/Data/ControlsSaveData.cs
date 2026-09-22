using System;
using Game.Core.Interfaces;

namespace Game.Core.Data
{
    [Serializable]
    public struct ControlsSaveData : ISaveData
    {
        public string SaveId => "ControlsSettings";
        public string[] BindingPaths; // 8 entries: [up, down, left, right, jump, run, attack, interact]
    }
}
