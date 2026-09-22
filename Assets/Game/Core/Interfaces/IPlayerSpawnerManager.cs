
using Game.Core.Enums;
using UnityEngine;

namespace Game.Core.Interfaces
{
    /// <summary>
    /// Contract for the Player Spawning System.
    /// Handles instantiation, positioning, and upgrade re-application.
    /// </summary>
    public interface IPlayerSpawnerManager
    {
        void SpawnPlayerAtDoor(DoorToSpawnAt targetDoor, bool playerEnteredFromRight);

        IPlayerStateDriver GetPlayer();

        void RespawnPlayer();
        void SpawnPlayer();
        void SpawnPlayerAtStart();
        void SpawnPlayerAtCheckpoint();
        void ForceSpawnPlayer(Vector3 position);
    }
}