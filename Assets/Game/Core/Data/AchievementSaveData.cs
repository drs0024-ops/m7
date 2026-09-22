using Game.Core.Interfaces;
namespace Game.Core.Data
{

    // --- Save Data Struct (POCO, NOT registered in DI) ---
    [System.Serializable]
    public class AchievementSaveData : ISaveData
    {
        public string SaveId = "Achievement";
        public string[] UnlockedAchievementIDs;

        string ISaveData.SaveId => SaveId;
    } 

}   