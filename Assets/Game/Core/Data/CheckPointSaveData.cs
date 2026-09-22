using UnityEngine;
using Game.Core.Interfaces;
namespace Game.Core.Data
{
    [System.Serializable]
    public class CheckpointSaveData : ISaveData
    {
        public string SaveId = "Checkpoint";
        public string CheckpointId;
        public Vector3 Position;
        public bool IsValid;

        string ISaveData.SaveId => SaveId;
    }
}