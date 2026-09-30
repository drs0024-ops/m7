
public class SaveSignals
{

}

namespace Game.Core.Messages
{
    public readonly struct SaveRequest
    {
        public string Source { get; }
        public bool Immediate { get; }
        public SaveRequest(string source, bool immediate = false)
        {
            Source = source;
            Immediate = immediate;
        }
    }

    public readonly struct LoadRequest
    {
        public static LoadRequest Default => new();
    }

    public readonly struct SaveCompleted
    {
        public static SaveCompleted Default => new();
    }

    public readonly struct SaveFailed
    {
        public string Error { get; }
        public SaveFailed(string error) => Error = error;
    }

    public readonly struct LoadCompleted
    {
        public static LoadCompleted Default => new();
    }

    public readonly struct LoadFailed
    {
        public string Error { get; }
        public LoadFailed(string error) => Error = error;
    }

    /// <summary>
    /// Request to delete the save file. Published by UI, handled by SaveManager.
    /// </summary>
    public readonly struct DeleteSaveRequested
    {
        public static readonly DeleteSaveRequested Default = new();
    }
}