using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine.SceneManagement;
using VContainer;
using Game.Core.Enums;
using Game.Core.Messages;
using VContainer.Unity;
using System;
using Game.Core;
using Game.Gameplay.Level;

namespace Game.Bootstrap
{
    public class LevelProgressionManager : IStartable, IDisposable
    {
        private readonly ISubscriber<LevelCompleteSignal> _levelCompleteSub;
        private readonly ISubscriber<RestartLevelSignal> _restartSub;
        private readonly ISubscriber<LoadLevelSignal> _loadLevelSub;
        private readonly IPublisher<GameCompletedSignal> _gameCompletedPublisher;
        private readonly IPublisher<SceneLoadedSignal> _sceneLoadedPublisher;
        private readonly IPublisher<LevelProgressionChangedSignal> _progressionPublisher;

        private readonly LevelConfigSO _config;
        private readonly SceneTransitionDirector _transitionDirector;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>(3);

        private int _currentLevelIndex;
        private int _currentSceneInLevelIndex;
        private bool _isInitialized;
        private bool _isLoading;

        public int CurrentLevelIndex => _currentLevelIndex;
        public int CurrentSceneInLevelIndex => _currentSceneInLevelIndex;

        [Inject]
        public LevelProgressionManager(
            LevelConfigSO config,
            SceneTransitionDirector transitionDirector,
            ISubscriber<LevelCompleteSignal> levelCompleteSub,
            ISubscriber<RestartLevelSignal> restartSub,
            ISubscriber<LoadLevelSignal> loadLevelSub,
            IPublisher<GameCompletedSignal> gameCompletedPublisher,
            IPublisher<SceneLoadedSignal> sceneLoadedPublisher,
            IPublisher<LevelProgressionChangedSignal> progressionPublisher)
        {
            if (config == null || config.allLevels == null || config.allLevels.Count == 0)
                throw new System.InvalidOperationException("LevelConfigSO is missing or empty.");

            _config = config;
            _transitionDirector = transitionDirector;
            _levelCompleteSub = levelCompleteSub;
            _restartSub = restartSub;
            _loadLevelSub = loadLevelSub;
            _gameCompletedPublisher = gameCompletedPublisher;
            _sceneLoadedPublisher = sceneLoadedPublisher;
            _progressionPublisher = progressionPublisher;

            _currentLevelIndex = config.startingLevelIndex;
        }

        void IStartable.Start()
        {
            if (_isInitialized) return;

            _subscriptions.Add(_levelCompleteSub.Subscribe(OnLevelComplete));
            _subscriptions.Add(_restartSub.Subscribe(OnRestartLevel));
            _subscriptions.Add(_loadLevelSub.Subscribe(OnLoadLevelRequested));

            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();
            _isInitialized = false;
        }

        public async UniTask LoadNextScene()
        {
            if (_currentLevelIndex >= _config.allLevels.Count)
            {
                _gameCompletedPublisher.Publish(GameCompletedSignal.Default);
                return;
            }

            var currentLevel = _config.allLevels[_currentLevelIndex];
            int nextSceneIndex = _currentSceneInLevelIndex + 1;

            if (nextSceneIndex >= currentLevel.scenes.Count)
            {
                if (_currentLevelIndex + 1 < _config.allLevels.Count)
                {
                    _currentLevelIndex++;
                    _currentSceneInLevelIndex = 0;
                    await LoadSpecificScene(_config.allLevels[_currentLevelIndex].scenes[0]);
                }
                else
                {
                    _gameCompletedPublisher.Publish(GameCompletedSignal.Default);
                }
            }
            else
            {
                _currentSceneInLevelIndex = nextSceneIndex;
                await LoadSpecificScene(currentLevel.scenes[_currentSceneInLevelIndex]);
            }
        }

        public async UniTask StartLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= _config.allLevels.Count) return;

            _currentLevelIndex = levelIndex;
            _currentSceneInLevelIndex = 0;
            await LoadSpecificScene(_config.allLevels[_currentLevelIndex].scenes[0]);
        }

        private void OnLevelComplete(LevelCompleteSignal msg) => _ = LoadNextScene();

        private void OnRestartLevel(RestartLevelSignal msg)
        {
            _currentSceneInLevelIndex = 0;
            _ = LoadSpecificScene(_config.allLevels[_currentLevelIndex].scenes[0]);
        }

        private void OnLoadLevelRequested(LoadLevelSignal msg) => _ = StartLevel(msg.LevelIndex);   

        private async UniTask LoadSpecificScene(SceneField sceneField)
        {
            if (_isLoading) return;
            if (sceneField == null || string.IsNullOrEmpty(sceneField.SceneName)) return;
            if (SceneManager.GetSceneByName(sceneField.SceneName).isLoaded) return;

            _isLoading = true;

            try
            {
                _progressionPublisher.Publish(new LevelProgressionChangedSignal(
                    _currentLevelIndex, _currentSceneInLevelIndex, sceneField.SceneName));

                await _transitionDirector.StartTransition(
                    sceneField, null, false, DoorToSpawnAt.None, false);

                _sceneLoadedPublisher.Publish(new SceneLoadedSignal(sceneField.SceneName));
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}

/* Pre VContainer verson
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Concrete implementation of ILevelProgressionManager.
/// Manages level state, scene transitions, and persistence.
/// </summary>
public class LevelProgressionManager : MonoBehaviour, ILevelProgressionManager
{
    [Header("Level Configuration")]
    [SerializeField] private List<LevelProgressionSaveData> allLevels = new List<LevelProgressionSaveData>();
    
    [Header("State Tracking")]
    [SerializeField] private int currentLevelIndex = 0;
    [SerializeField] private int currentSceneInLevelIndex = 0;

    private string _pendingLoadSceneName;
    private ServiceLocator _serviceLocator;

    #region Public API (Read-Only)

    public string TargetSceneName => _pendingLoadSceneName;
    public int CurrentLevelIndex => currentLevelIndex;
    public int CurrentSceneInLevelIndex => currentSceneInLevelIndex;

    #endregion

    private void Awake()
    {
        // Validate Configuration
        if (allLevels == null || allLevels.Count == 0)
        {
            Debug.LogError("[LevelProgressionManager] No levels configured!");
            enabled = false;
            return;
        }

        // Do NOT access ServiceLocator here if you can avoid it.
        // If you must, use the static property directly, not a cached variable.
        Debug.Log($"[LevelProgressionManager] Awake() - Time: {Time.realtimeSinceStartup}");;
    }

    void Start()
    {
        // Access Static Property Directly (Triggers Lazy Load if needed)
        _serviceLocator = ServiceLocator.Instance;

        // Single Null Check
        if (_serviceLocator == null)
        {
            Debug.LogError("[LevelProgressionManager] ServiceLocator instance not found!", this);
            enabled = false;
            return;
        }

        // Register Self
        _serviceLocator.Register<ILevelProgressionManager>(this);
        _serviceLocator.Register<LevelProgressionManager>(this);
        
        Debug.Log("[LevelProgressionManager] Successfully initialized.");
    }

    private void OnDestroy()
    {
        if (_serviceLocator != null)
        {
            _serviceLocator.Unregister<LevelProgressionManager>();
            _serviceLocator.Unregister<ILevelProgressionManager>();
        }
    }

    #region ILevelProgressionManager Implementation

    public void LoadNextScene()
    {
        if (allLevels.Count == 0 || currentLevelIndex >= allLevels.Count) 
        {
            Debug.LogWarning("[LevelProgression] No levels available or index out of range.");
            return;
        }

        LevelProgressionSaveData currentLevel = allLevels[currentLevelIndex];
        
        // Safety check for null scene list
        if (currentLevel.scenes == null || currentLevel.scenes.Count == 0) return;

        int nextSceneIndex = currentSceneInLevelIndex + 1;

        if (nextSceneIndex >= currentLevel.scenes.Count)
        {
            // End of level: Move to next level
            if (currentLevelIndex + 1 < allLevels.Count)
            {
                currentLevelIndex++;
                currentSceneInLevelIndex = 0;
                LoadSpecificScene(allLevels[currentLevelIndex].firstScene);
            }
            else
            {
                Debug.Log("[LevelProgression] Reached end of all levels.");
            }
        }
        else
        {
            // Next scene in current level
            currentSceneInLevelIndex = nextSceneIndex;
            LoadSpecificScene(currentLevel.scenes[currentSceneInLevelIndex]);
        }
    }

    public void StartLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= allLevels.Count)
        {
            Debug.LogError($"[LevelProgression] Level index {levelIndex} out of range.");
            return;
        }

        currentLevelIndex = levelIndex;
        currentSceneInLevelIndex = 0;
        LoadSpecificScene(allLevels[currentLevelIndex].firstScene);
    }

    #endregion

    #region Internal Helpers

    private void LoadSpecificScene(SceneField sceneToLoad)
    {
        if (!sceneToLoad.IsValid()) 
        {
            Debug.LogError("[LevelProgression] Invalid SceneField provided.");
            return;
        }

        _pendingLoadSceneName = sceneToLoad.SceneName;

        // Attempt to resolve via ServiceLocator first, fallback to FindFirstObjectByType
        var transitionDirector = _serviceLocator?.Get<SceneTransitionDirector>() 
                                 ?? FindFirstObjectByType<SceneTransitionDirector>();

        if (transitionDirector != null)
        {
            transitionDirector.StartTransition(
                sceneToLoad,
                onComplete: () => Debug.Log($"[LevelProgression] Loaded: {sceneToLoad.SceneName}"),
                spawnAtDoor: false,
                door: DoorTriggerInteraction.DoorToSpawnAt.None,
                fromRight: false
            );
        }
        else
        {
            Debug.LogError("[LevelProgression] SceneTransitionDirector not found!");
        }
    }

    #endregion

    #region ISaveable Implementation

    public string SaveId => "LevelProgression";

    public ISaveData GetSaveData()
    {
        // Ensure we capture the current state accurately
        var currentLevelData = (allLevels.Count > 0 && currentLevelIndex < allLevels.Count) 
        ? allLevels[currentLevelIndex] 
        : default(LevelProgressionSaveData);

        return new LevelProgressionSaveData
        {
            levelName = SceneManager.GetActiveScene().name,
            scenes = currentLevelData.scenes != null 
                ? new List<SceneField>(currentLevelData.scenes) 
                : new List<SceneField>()
        };
    }

    public void LoadFromData(ISaveData data)
    {
        if (data is not LevelProgressionSaveData levelData)
        {
            Debug.LogError("[LevelProgressionManager] Received invalid data type for loading.");
            return;
        }

        _pendingLoadSceneName = levelData.levelName;

        // CRITICAL FIX: Ensure scenes is never null after deserialization
        if (levelData.scenes == null)
        {
            levelData.scenes = new List<SceneField>();
        }

        // Note: We do NOT change currentLevelIndex here automatically unless the save data 
        // explicitly stores the index. Usually, you match the scene name to find the index.
        // For now, we just prepare the pending load name as per original logic.
        
        if (levelData.scenes.Count > 0)
        {
            Debug.Log($"[LevelProgression] Loaded {levelData.scenes.Count} scenes. First: {levelData.firstScene}");
        }
        else
        {
            Debug.LogWarning("[LevelProgression] No scenes found in save data.");
        }
    }

    #endregion
}   
*/
