namespace Game.Core.Interfaces
{
    /// <summary>Supplies frame time without coupling pure models to Unity.</summary>
    public interface IFrameTimeProvider
    {
        /// <summary>Delta time for the current frame.</summary>
        float DeltaTime { get; }
    }
}