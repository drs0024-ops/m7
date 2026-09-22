/// <summary>
/// Contract for the core Game Manager.
/// Defines essential game flow operations accessible globally.
/// </summary>

namespace Game.Core.Interfaces
{
    public interface IGameManager
    {
        /// <summary>
        /// Starts or restarts the game session.
        /// </summary>
        void StartGame();

        /// <summary>
        /// Ends the current game session (Game Over).
        /// </summary>
        void EndGame();

        /// <summary>
        /// Pauses the game logic.
        /// </summary>
        void PauseGame();

        /// <summary>
        /// Resumes the game from a paused state.
        /// </summary>
        void ResumeGame();
    }  
} 