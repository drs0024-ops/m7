using Game.Core.Enums;
/// <summary>
/// Contract for the Game State System.
/// Allows swapping state implementations without changing consumer code (Input, UI, etc.).
/// </summary>

namespace Game.Core.Interfaces
{
    public interface IGameStateMachine
    {
        /// <summary>
        /// Gets the current active game state.
        /// </summary>
        GameState CurrentState { get; }

        /// <summary>
        /// Request a transition to a new state (triggers events, time scale changes, etc.).
        /// </summary>
        void RequestState(GameState newState);

        /// <summary>
        /// Forcefully set the state without triggering transitions or broadcasts.
        /// Useful for syncing state after modals or loading screens.
        /// </summary>
        void SilentlySetState(GameState newState);

        bool IsInState(GameState newState);

        public void ForceState(GameState newState);
    }  
} 