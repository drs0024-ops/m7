namespace Game.Core.Interfaces
{
    public interface ISaveable
    {
        string SaveId { get; }
        ISaveData GetSaveData();
        void LoadFromData(ISaveData data);
    }
}