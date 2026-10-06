namespace Game.Core.Interfaces
{
    /// <summary>
    /// Read-only view of upgrade unlock state.
    /// Allows consumers to query current state without coupling to the writer.
    /// </summary>
    public interface IUpgradeState
    {
        bool IsUnlocked(string id);
        int GetCount(string id);
    }
}
