using System.Threading.Tasks;

/// <summary>
/// Contract for the Save System.
/// Handles aggregation, serialization, encryption, and persistence of game state.
/// </summary>
namespace Game.Core.Interfaces
{
    public interface ISaveManager
    {
        /// <summary>
        /// Gets the scene name stored in the current loaded save data.
        /// </summary>
        string CurrentSceneName { get; }

        /// <summary>
        /// Asynchronously saves all registered ISavable components to disk.
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Synchronously loads data from disk and distributes it to registered ISavable components.
        /// </summary>
        void Load();

        /// <summary>
        /// Manually saves a specific data chunk immediately and triggers a full async save.
        /// </summary>
        void SaveData(ISaveData data);

        /// <summary>
        /// Retrieves a specific save data chunk by type from the currently loaded data.
        /// Returns true if found, false otherwise.
        /// </summary>
        bool GetData<T>(out T data) where T : struct, ISaveData;
    }   
}