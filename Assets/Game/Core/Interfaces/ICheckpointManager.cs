using UnityEngine;
/// <summary>
/// Contract for Checkpoint logic and player spawn positioning.
/// Inherits ISaveable to enforce save/load consistency across implementations.
/// </summary>

namespace Game.Core.Interfaces
{
    public interface ICheckpointManager : ISaveable
    {
        /// <summary>
        /// Called when the player triggers a checkpoint to update the spawn position.
        /// </summary>
        /// <param name="position">The world position of the new checkpoint.</param>
        void SetCheckpoint(Vector3 position);

        /// <summary>
        /// Retrieves the last recorded spawn position.
        /// Falls back to start position or Vector3.zero if no checkpoint is set.
        /// </summary>
        Vector3 GetSpawnPosition();

        /// <summary>
        /// Forces the player to spawn at a specific position immediately.
        /// Typically used after loading a saved game state.
        /// </summary>
        /// <param name="position">The target spawn position.</param>
        void ForceSpawnAt(Vector3 position);
    }  

}