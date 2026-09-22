// Core/Data/PlayerUpgradesSaveData.cs
using System;
using Game.Core.Interfaces;

namespace Game.Core.Data
{
    [Serializable]
    public struct PlayerUpgradesSaveData : ISaveData
    {
        public string SaveId => "PlayerUpgrades";
        public string[] UnlockedUpgradeIDs;
    } 
}  