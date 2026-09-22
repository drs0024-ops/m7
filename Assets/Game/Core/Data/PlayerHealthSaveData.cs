using Game.Core.Interfaces;

[System.Serializable]
    public class PlayerHealthSaveData : ISaveData
    {
        public string SaveId = "PlayerHealth";
        public float CurrentHealth;
        public bool IsInvincible;

        string ISaveData.SaveId => SaveId;
    }