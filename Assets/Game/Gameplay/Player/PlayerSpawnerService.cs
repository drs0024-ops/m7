using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Interfaces;
using Game.Core.Messages;
using Game.Gameplay.World;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Sole player spawner. Responds to PlayerSpawnRequest messages.
    /// Delegates actual instantiation to IPlayerFactory.
    /// Registered as an entry point in the scene scope.
    /// </summary>
    public class PlayerSpawnerService : IStartable, IDisposable
    {
        #region Dependencies

        private readonly ISubscriber<PlayerSpawnRequest> _spawnRequestSub;
        private readonly IPublisher<PlayerSpawned> _spawnedPublisher;
        private readonly IPlayerFactory _playerFactory;
        private readonly ICheckpointProvider _checkpointManager;
        private readonly ICharacterSceneConfig _config;

        #endregion

        #region State

        private readonly List<IDisposable> _disposables = new(1);
        private Dictionary<DoorToSpawnAt, Transform> _doorSpawnMap;
        private GameObject _currentPlayer;
        private bool _hasSpawnedInitial;
        private bool _disposed;

        #endregion

        #region Construction

        public PlayerSpawnerService(
            ISubscriber<PlayerSpawnRequest> spawnRequestSub,
            IPublisher<PlayerSpawned> spawnedPublisher,
            IPlayerFactory playerFactory,
            ICheckpointProvider checkpointManager,
            ICharacterSceneConfig config)
        {
            _spawnRequestSub = spawnRequestSub;
            _spawnedPublisher = spawnedPublisher;
            _playerFactory = playerFactory;
            _checkpointManager = checkpointManager;
            _config = config;
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            if (_disposed) return;

            _disposables.Add(_spawnRequestSub.Subscribe(OnSpawnRequest));

            if (_config.SpawnPlayerOnStart && !_hasSpawnedInitial)
            {
                // FIX #55: Set flag AFTER successful spawn, not before
                var spawned = SpawnAtCheckpointInternal();
                if (spawned)
                    _hasSpawnedInitial = true;
            }
        }

        #endregion

        #region Message Handler

        private void OnSpawnRequest(PlayerSpawnRequest request)
        {
            if (_disposed) return;

            if (request.UseCustomPosition)
            {
                SpawnAtPosition(request.CustomPosition, request.ForceRespawn);
            }
            else if (request.SpawnAtDoor && request.Door != DoorToSpawnAt.None)
            {
                SpawnAtDoor(request.Door, request.FromRight, request.ForceRespawn);
            }
            else
            {
                SpawnAtCheckpoint(request.ForceRespawn);
            }
        }

        #endregion

        #region Spawn Methods

        public void SpawnAtCheckpoint(bool forceRespawn = false)
        {
            if (_disposed) return;

            Vector3 position;
            if (_config.ForcedSpawnPosition.HasValue)
                position = _config.ForcedSpawnPosition.Value;
            else
                position = _checkpointManager.GetSpawnPosition();

            SpawnAtPosition(position, forceRespawn);
        }

        public void SpawnAtDoor(DoorToSpawnAt door, bool fromRight, bool forceRespawn = false)
        {
            if (_disposed) return;

            EnsureDoorsCached();

            if (_doorSpawnMap.TryGetValue(door, out var target) && target != null)
            {
                var pos = target.position;
                pos.x += fromRight ? -_config.DoorSpawnOffset : _config.DoorSpawnOffset;
                SpawnAtPosition(pos, forceRespawn);
            }
            else
            {
                Debug.LogWarning($"[PlayerSpawner] Door {door} not found. Falling back to checkpoint.");
                SpawnAtCheckpoint(forceRespawn);
            }
        }

        public void SpawnAtPosition(Vector3 position, bool forceRespawn = false)
        {
            if (_disposed) return;

            if (_currentPlayer != null && !forceRespawn)
            {
                var existingShell = _currentPlayer.GetComponentInChildren<PlayerStateDriverShell>(true);
                if (existingShell == null)
                {
                    Debug.LogError("[PlayerSpawner] Existing player is missing PlayerStateDriverShell.");
                    return;
                }

                existingShell.AlignFeetToGroundPosition(position);
                Debug.Log($"[Phase2 Verify] PlayerSpawnerService reused player at {position}.");
                return;
            }

            DestroyCurrentPlayer();

            var shell = _playerFactory.CreatePlayer(position, Quaternion.identity);
            if (shell == null)
            {
                Debug.LogError("[Phase2 Verify] PlayerSpawnerService failed: PlayerFactory returned null.");
                return;
            }

            _currentPlayer = shell.gameObject;
            _spawnedPublisher.Publish(new PlayerSpawned(shell.transform));
            Debug.Log($"[SpawnTrace] PlayerSpawnerService spawned '{_currentPlayer.name}' id={_currentPlayer.GetInstanceID()} scene='{_currentPlayer.scene.name}' root='{_currentPlayer.transform.root.name}' at {position}.", _currentPlayer);
        }

        public void Respawn()
        {
            SpawnAtCheckpoint(forceRespawn: true);
        }

        #endregion

        #region Internal

        private bool SpawnAtCheckpointInternal()
        {
            Vector3 position;
            if (_config.ForcedSpawnPosition.HasValue)
                position = _config.ForcedSpawnPosition.Value;
            else
                position = _checkpointManager.GetSpawnPosition();

            DestroyCurrentPlayer();

            var shell = _playerFactory.CreatePlayer(position, Quaternion.identity);
            if (shell == null)
            {
                Debug.LogError("[Phase2 Verify] PlayerSpawnerService initial spawn failed: PlayerFactory returned null.");
                return false;
            }

            _currentPlayer = shell.gameObject;
            _spawnedPublisher.Publish(new PlayerSpawned(shell.transform));
            Debug.Log($"[SpawnTrace] PlayerSpawnerService initial spawn succeeded: '{_currentPlayer.name}' id={_currentPlayer.GetInstanceID()} scene='{_currentPlayer.scene.name}' root='{_currentPlayer.transform.root.name}' at {position}.", _currentPlayer);
            return true;
        }

        private void DestroyCurrentPlayer()
        {
            if (_currentPlayer != null)
            {
                Debug.Log($"[SpawnTrace] PlayerSpawnerService destroying '{_currentPlayer.name}' id={_currentPlayer.GetInstanceID()} scene='{_currentPlayer.scene.name}'.", _currentPlayer);
                UnityEngine.Object.Destroy(_currentPlayer);
                _currentPlayer = null;
            }
        }

        private void EnsureDoorsCached()
        {
            if (_doorSpawnMap != null) return;

            // FIX #53: Filter by current scene to avoid picking up doors from other additive scenes
            var currentScene = _checkpointManager.gameObject.scene;
            _doorSpawnMap = new Dictionary<DoorToSpawnAt, Transform>();
            var allDoors = UnityEngine.Object.FindObjectsByType<DoorTriggerInteraction>(FindObjectsSortMode.None);

            for (int i = 0; i < allDoors.Length; i++)
            {
                if (allDoors[i].gameObject.scene != currentScene) continue;
                if (!_doorSpawnMap.ContainsKey(allDoors[i].CurrentDoorPosition))
                    _doorSpawnMap[allDoors[i].CurrentDoorPosition] = allDoors[i].transform; 
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();

            DestroyCurrentPlayer();
        }

        #endregion
    }
}   