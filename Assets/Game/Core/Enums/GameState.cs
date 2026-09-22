namespace Game.Core.Enums
{
    /// <summary>
    /// Global game state machine states.
    /// </summary>
    public enum GameState
    {
        Boot,
        IntroVideo,
        MainMenu,
        Loading,
        Gameplay,
        Paused,
        GameOver,
        QuitGame
    }
}