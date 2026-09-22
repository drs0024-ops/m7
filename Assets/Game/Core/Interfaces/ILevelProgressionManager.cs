using Game.Core.Interfaces;
/// <summary>
/// Contract for the Level Progression System.
/// Handles level state, scene transitions, and persistence.
/// </summary>

namespace Game.Core.Interfaces
{
    public interface ILevelProgressionManager : ISaveable
    {
        /// <summary>
        /// The name of the scene pending load (for SceneRouter consumption).
        /// </summary>
        string TargetSceneName { get; }

        /// <summary>
        /// Loads the next scene in the sequence, handling level transitions automatically.
        /// </summary>
        void LoadNextScene();

        /// <summary>
        /// Starts a specific level by index, resetting scene progress.
        /// </summary>
        /// <param name="levelIndex">The 0-based index of the level to start.</param>
        void StartLevel(int levelIndex);

        /// <summary>
        /// Gets the current level index.
        /// </summary>
        int CurrentLevelIndex { get; }

        /// <summary>
        /// Gets the current scene index within the active level.
        /// </summary>
        int CurrentSceneInLevelIndex { get; }
    }   
}