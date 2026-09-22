namespace Game.Core.Messages
{
    /// <summary>
    /// Published when the player skips a video (intro or cutscene).
    /// </summary>
    public readonly struct VideoSkipRequested
    {
        public static readonly VideoSkipRequested Default = default;
    }
}   