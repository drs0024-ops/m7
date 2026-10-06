using UnityEngine;

namespace Game.Core.Interfaces
{
    /// <summary>
    /// Read/write interface for checkpoint spawn position queries.
    /// Decouples PlayerSpawnerService from the concrete CheckpointManager.
    /// </summary>
    public interface ICheckpointProvider
    {
        GameObject gameObject { get; }
        Vector3 GetSpawnPosition();
        void ForceSpawnAt(Vector3 position);
    }
}
