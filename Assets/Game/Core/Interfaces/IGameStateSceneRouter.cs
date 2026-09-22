using Game.Core.Enums;
/// <summary>
/// Contract for the Scene Routing System.
/// Responsible for loading/unloading scenes based on Game State transitions.
/// Decouples scene management logic from the GameStateMachine and UI.
/// </summary>

namespace Game.Core.Interfaces
{
    public interface IGameStateSceneRouter
    {
        /// <summary>
        /// Returns true if a scene transition is currently in progress.
        /// Use this to block new state requests until the transition completes.
        /// </summary>
        bool IsTransitioning { get; }

        void OnStateChange(GameState state);

        /// <summary>
        /// Requests a transition to the Main Menu.
        /// Handles saving game state before unloading the current level.
        /// </summary>
        void LoadMainMenu();

        /// <summary>
        /// Requests loading the Options Menu as an overlay.
        /// Automatically stores the previous state for restoration.
        /// </summary>
        void LoadOptionsMenu();

        /// <summary>
        /// Requests closing the Options Menu and restoring the previous game state.
        /// Handles the transition back to the underlying scene.
        /// </summary>
        void OnCloseOptionsRequested();


        /// <summary>
        /// Initiates the New Game flow.
        /// Typically loads the intro scene and initializes level progression.
        /// </summary>
        void HandleNewGameRequest();

        
        void HandleLoadSavedGameRequest();

        /// <summary>
        /// Initiates the Load Game flow.
        /// Retrieves save data, validates the target scene, and triggers the transition.
        /// </summary>
        void LoadSavedGame();

        void OnSaveComplete();
    }   
}