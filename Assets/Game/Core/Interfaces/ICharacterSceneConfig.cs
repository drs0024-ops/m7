using System;
using UnityEngine;

namespace Game.Core.Interfaces
{
    /// <summary>
    /// Read-only view of character spawn configuration.
    /// Allows PlayerSpawnerService to be tested with a mock config.
    /// </summary>
    public interface ICharacterSceneConfig
    {
        bool SpawnPlayerOnStart { get; }
        Vector3 DefaultStartPosition { get; }
        Nullable<Vector3> ForcedSpawnPosition { get; }
        float DoorSpawnOffset { get; }
    }
}
