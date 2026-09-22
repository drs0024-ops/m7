namespace Game.Core.Enums
{
    [System.Flags]
    public enum GamePhase : long
    {
        None        = 0L,
        Letter      = 1L << 0,
        Menu        = 1L << 1,
        Gameplay    = 1L << 2,
        Climax      = 1L << 3,
        Ending      = 1L << 4,
        // bits 5–13 reserved
        PostCredits = 1L << 14,
        Cinematic   = 1L << 15,
        Loading     = 1L << 16,

        Playable = Gameplay | Climax | Ending
    }
}   