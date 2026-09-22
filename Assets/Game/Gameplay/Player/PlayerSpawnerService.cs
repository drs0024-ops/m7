using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using Game.Gameplay.World;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Player
{
    public class PlayerSpawnerService : IStartable, IDisposable
    {
        // NOT readonly — deferred pattern
        private ISubscriber<PlayerSpawnRequest> _spawnRequestSub;
        private IPublisher<PlayerSpawned> _playerSpawnedPublisher;

        private readonly List<IDisposable> _disposables = new();

        private readonly CheckpointManager _checkpointManager;
        private readonly CharacterSceneConfig _config;
        private readonly IPlayerFactory _playerFactory;
        private readonly IObjectResolver _objectResolver;
        private readonly Transform _startSpawnPoint;

        private PlayerStateDriverShell _currentPlayer;
        private bool _hasSpawnedInitialPlayer;
        private Dictionary<DoorToSpawnAt, Transform> _doorSpawnMap;
        private float _doorSpawnOffset = 3f;
        private bool _disposed;

        public PlayerSpawnerService(
            CheckpointManager checkpointManager,
            CharacterSceneConfig config,
            IPlayerFactory playerFactory,
            IObjectResolver objectResolver,
            [Key(SpawnKeys.StartSpawn)] Transform startSpawnPoint)
        {
            _checkpointManager = checkpointManager;
            _config = config;
            _playerFactory = playerFactory;
            _objectResolver = objectResolver;
            _startSpawnPoint = startSpawnPoint;
        }

        void IStartable.Start()
        {
            _spawnRequestSub = GlobalMessagePipe.GetSubscriber<PlayerSpawnRequest>();
            _playerSpawnedPublisher = GlobalMessagePipe.GetPublisher<PlayerSpawned>();

            _disposables.Add(_spawnRequestSub.Subscribe(HandleSpawnRequest));

            if (_config.SpawnPlayerOnStart && !_hasSpawnedInitialPlayer)
            {
                _hasSpawnedInitialPlayer = true;
                SpawnPlayer();
            }
        }

        #region Spawning

        public void RespawnPlayer()
        {
            if (_disposed) return;
            DestroyCurrentPlayer();
            SpawnPlayer();
        }

        public void SpawnPlayer()
        {
            if (_disposed) return;
            InstantiatePlayer(ResolveSpawnPosition());
        }

        private void SpawnPlayerAtDoor(DoorToSpawnAt targetDoor, bool fromRight)
        {
            EnsureDoorsCached();

            if (_doorSpawnMap != null && _doorSpawnMap.TryGetValue(targetDoor, out var target) && target != null)
            {
                var pos = target.position;
                pos.x += fromRight ? -_doorSpawnOffset : _doorSpawnOffset;
                InstantiatePlayer(pos);
                return;
            }

            Debug.LogWarning($"[PlayerSpawner] Door {targetDoor} invalid. Spawning default.");
            SpawnPlayer();
        }

        private void InstantiatePlayer(Vector3 position)
        {
            DestroyCurrentPlayer();

            _currentPlayer = _playerFactory.CreatePlayer(position, Quaternion.identity);

            if (_currentPlayer == null)
            {
                Debug.LogError("[PlayerSpawner] Factory returned null!");
                return;
            }

            _objectResolver.InjectGameObject(_currentPlayer.gameObject);
            _playerSpawnedPublisher.Publish(new PlayerSpawned(_currentPlayer.transform));
        }

        private void DestroyCurrentPlayer()
        {
            if (_currentPlayer != null)
            {
                UnityEngine.Object.Destroy(_currentPlayer.gameObject);
                _currentPlayer = null;
            }
        }

        #endregion

        #region Position Resolution

        private Vector3 ResolveSpawnPosition()
        {
            if (_config.ForcedSpawnPosition.HasValue && _config.ForcedSpawnPosition.Value != Vector3.zero)
                return _config.ForcedSpawnPosition.Value;

            if (_checkpointManager != null && _checkpointManager.GetSpawnPosition() is Vector3 checkpoint)
                return checkpoint;

            return _config.DefaultStartPosition;
        }

        private void EnsureDoorsCached()
        {
            if (_doorSpawnMap != null) return;

            _doorSpawnMap = new Dictionary<DoorToSpawnAt, Transform>();
            var allDoors = UnityEngine.Object.FindObjectsByType<DoorTriggerInteraction>(FindObjectsSortMode.None);

            for (int i = 0; i < allDoors.Length; i++)
            {
                if (!_doorSpawnMap.ContainsKey(allDoors[i].CurrentDoorPosition))
                    _doorSpawnMap[allDoors[i].CurrentDoorPosition] = allDoors[i].transform;
            }
        }

        #endregion

        #region Message Handlers

        private void HandleSpawnRequest(PlayerSpawnRequest request)
        {
            if (_disposed) return;

            if (_hasSpawnedInitialPlayer
                && !request.ForceRespawn
                && !request.UseCustomPosition
                && !request.SpawnAtDoor)
                return;

            if (!request.ForceRespawn)
                _hasSpawnedInitialPlayer = true;

            if (request.UseCustomPosition)
                InstantiatePlayer(request.CustomPosition);
            else if (request.SpawnAtDoor)
                SpawnPlayerAtDoor(request.Door, request.FromRight);
            else if (request.ForceRespawn)
                RespawnPlayer();
            else
                SpawnPlayer();
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var d in _disposables) d?.Dispose();
            _disposables.Clear();

            DestroyCurrentPlayer();
            _doorSpawnMap?.Clear();
        }

        #endregion
    }
}   

/* Prior to VContainer version
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Manages player instantiation, positioning, and upgrade synchronization.
/// Fully decoupled via SignalBus.
/// </summary>
public class PlayerSpawnerManager : MonoBehaviour, IPlayerSpawnerManager, IStartable, IDisposable
{
    [Inject] private ICheckpointManager _checkpointManager;
    [Inject] private SignalBus _signalBus;

    [Header("Spawn Offset")]
    [SerializeField] private float doorSpawnOffset = 3f;
    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;

    private Dictionary<DoorTriggerInteraction.DoorToSpawnAt, Transform> _doorSpawnMap;
    private bool _doorsCached = false;
    private GameObject _currentPlayerInstance;
    private readonly PlayerStateDriverShell _cachedPlayer;

    public PlayerStateDriverShell GetPlayer() => _cachedPlayer;
    
    
    public void Start()
    {
        // Add code if needed
    }

    public void Dispose() { }

    // --- Public API ---

    public void SpawnPlayerAtDoor(DoorTriggerInteraction.DoorToSpawnAt targetDoor, bool playerEnteredFromRight)
    {
        EnsureDoorsCached();
        if (_doorSpawnMap.TryGetValue(targetDoor, out Transform targetTransform))
        {
            Vector3 spawnPosition = targetTransform.position;
            spawnPosition.x += playerEnteredFromRight ? -doorSpawnOffset : doorSpawnOffset;
            InstantiatePlayer(spawnPosition);
        }
    }

    public void RespawnPlayer() => InstantiatePlayer(_checkpointManager?.GetSpawnPosition() ?? Vector3.zero);
    public void SpawnPlayerAtStart() => InstantiatePlayer(_checkpointManager?.GetSpawnPosition() ?? Vector3.zero);
    public void SpawnPlayerAtCheckpoint() => InstantiatePlayer(_checkpointManager?.GetSpawnPosition() ?? Vector3.zero);
    public void ForceSpawnPlayer(Vector3 position) => InstantiatePlayer(position);

    public void SpawnPlayer()
    {
        var sceneReq = FindFirstObjectByType<SceneRequirements>();
        if (sceneReq != null && !sceneReq.RequiresPlayer) return;
        InstantiatePlayer(_checkpointManager?.GetSpawnPosition() ?? Vector3.zero);
    }

    // --- Private Helpers ---

    private void EnsureDoorsCached()
    {
        if (_doorsCached) return;
        _doorSpawnMap = new Dictionary<DoorTriggerInteraction.DoorToSpawnAt, Transform>();
        DoorTriggerInteraction[] allDoors = FindObjectsByType<DoorTriggerInteraction>(FindObjectsSortMode.None);
        foreach (var door in allDoors)
        {
            if (!_doorSpawnMap.ContainsKey(door.CurrentDoorPosition))
                _doorSpawnMap[door.CurrentDoorPosition] = door.transform;
        }
        _doorsCached = true;
    }

    private void InstantiatePlayer(Vector3 position)
    {
        if (playerPrefab == null) return;

        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");

        if (existingPlayer != null)
        {
            // Teleport existing
            existingPlayer.transform.position = position;

            //Fire a signal instead of calling the manager directly
            _signalBus.Fire(new PlayerSpawnedSignal { Player = _currentPlayerInstance });
        
        }
        else
        {
            // Spawn New
            _currentPlayerInstance = Instantiate(playerPrefab, position, Quaternion.identity);

            // CRITICAL: Fire signal for Camera and other systems
            _signalBus.Fire(new PlayerSpawnedSignal { Player = _currentPlayerInstance });
        }
    }
}




using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Concrete implementation of IPlayerSpawnerManager.
/// Manages player instantiation, positioning, and upgrade synchronization.
/// </summary>
public class PlayerSpawnerManager : MonoBehaviour, IPlayerSpawnerManager
{
    [Header("Spawn Offset")]
    [SerializeField] private float doorSpawnOffset = 3f;

    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;

    private ServiceLocator _serviceLocator;

    // Lazy caching for doors
    private Dictionary<DoorTriggerInteraction.DoorToSpawnAt, Transform> doorSpawnMap;
    private bool _doorsCached = false;

    private void Awake()
    {
        // Do NOT access ServiceLocator here if you can avoid it.
        // If you must, use the static property directly, not a cached variable.
        Debug.Log($"[PlayerSpawnManager] Awake() - Time: {Time.realtimeSinceStartup}");
        
        

        // 1. Check Scene Requirements Immediately
        var sceneReq = FindFirstObjectByType<SceneRequirements>();
        bool needsPlayer = (sceneReq == null) || sceneReq.RequiresPlayer;

        if (needsPlayer)
        {
            SpawnPlayer();
        }
        
        // 2. Register as BOTH concrete class and interface
        if (_serviceLocator != null)
        {
            _serviceLocator.Register<PlayerSpawnerManager>(this);
            _serviceLocator.Register<IPlayerSpawnerManager>(this);
        }
    }

    private void OnDestroy()
    {
        if (_serviceLocator != null)
        {
            _serviceLocator.Unregister<PlayerSpawnerManager>();
            _serviceLocator.Unregister<IPlayerSpawnerManager>();
        }
    }

    #region IPlayerSpawnerManager Implementation

    public void SpawnPlayerAtDoor(DoorTriggerInteraction.DoorToSpawnAt targetDoor, bool playerEnteredFromRight)
    {
        EnsureDoorsCached();

        if (doorSpawnMap.TryGetValue(targetDoor, out Transform targetTransform))
        {
            Vector3 spawnPosition = targetTransform.position;
            spawnPosition.x += playerEnteredFromRight ? -doorSpawnOffset : doorSpawnOffset;
            
            InstantiatePlayer(spawnPosition);
        }
        else
        {
            Debug.LogError($"[PlayerSpawnerManager] No door found for spawn position: {targetDoor}");
        }
    }

    public void RespawnPlayer()
    {
        // Resolve via Interface instead of static Instance
        var checkpointManager = _serviceLocator?.Get<ICheckpointManager>();
        Vector3 spawnPos = checkpointManager?.GetSpawnPosition() ?? Vector3.zero;
        InstantiatePlayer(spawnPos);
    }

    public void SpawnPlayerAtStart()
    {
        // Assuming ICheckpointManager also exposes a Start position, or fallback to Vector3.zero
        // If GameLevelManager is separate, resolve ILevelProgressionManager or similar
        var checkpointManager = _serviceLocator?.Get<ICheckpointManager>();
        
        // Fallback logic if specific 'Start' method isn't in ICheckpointManager
        // You may need to add GetStartSpawnPosition() to ICheckpointManager if strictly decoupling
        Vector3 spawnPos = checkpointManager?.GetSpawnPosition() ?? Vector3.zero; 
        InstantiatePlayer(spawnPos);
    }

    public void SpawnPlayerAtCheckpoint()
    {
        var checkpointManager = _serviceLocator?.Get<ICheckpointManager>();
        Vector3 spawnPos = checkpointManager?.GetSpawnPosition() ?? Vector3.zero;
        InstantiatePlayer(spawnPos);
    }

    public void ForceSpawnPlayer(Vector3 position)
    {
        InstantiatePlayer(position);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Caches doors ONLY when needed (Lazy Loading) to improve level load times.
    /// </summary>
    private void EnsureDoorsCached()
    {
        if (_doorsCached) return;

        doorSpawnMap = new Dictionary<DoorTriggerInteraction.DoorToSpawnAt, Transform>();
        
        // Use FindObjectsByType with None sort mode for performance
        DoorTriggerInteraction[] allDoors = FindObjectsByType<DoorTriggerInteraction>(FindObjectsSortMode.None);
        
        foreach (var door in allDoors)
        {
            if (!doorSpawnMap.ContainsKey(door.CurrentDoorPosition))
            {
                doorSpawnMap[door.CurrentDoorPosition] = door.transform;
            }
        }
        _doorsCached = true;
    }

    /// <summary>
    /// Unified method to spawn or teleport the player.
    /// Ensures upgrades are always applied/re-applied via Interface.
    /// </summary>
    private void InstantiatePlayer(Vector3 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawnerManager] PlayerPrefab is missing in Inspector!");
            return;
        }

        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        
        // Resolve Upgrades Manager via Interface
        var upgradesManager = _serviceLocator?.Get<IPlayerUpgradesManager>();

        if (existingPlayer != null)
        {
            // Teleport
            existingPlayer.transform.position = position;
            
            // Re-apply upgrades
            upgradesManager?.InitializePlayerUpgrades(); 
        }
        else
        {
            // Spawn New
            GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
            
            // Initialize upgrades for the new player
            // Note: If InitializePlayerUpgrades finds the player by Tag internally, this works.
            // Otherwise, you might pass the player reference directly if you refactor that method.
            upgradesManager?.InitializePlayerUpgrades(); 
        }
    }   

    /// <summary>
    /// Initial spawn logic triggered by Awake.
    /// </summary>
    private void SpawnPlayer()
    {
        var sceneReq = FindFirstObjectByType<SceneRequirements>();
        if (sceneReq != null && !sceneReq.RequiresPlayer) return;

        var checkpointManager = _serviceLocator?.Get<ICheckpointManager>();
        Vector3 spawnPosition = checkpointManager?.GetSpawnPosition() ?? Vector3.zero;
        
        InstantiatePlayer(spawnPosition);
    }

    #endregion
}   
*/