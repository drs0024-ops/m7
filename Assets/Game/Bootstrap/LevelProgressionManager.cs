using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine.SceneManagement;
using Game.Core.Enums;
using Game.Core.Messages;
using VContainer.Unity;
using Game.Core;
using Game.Core.Data;
using Game.Gameplay.Level;
using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Tracks level/scene progression. Responds to LevelComplete/Restart/LoadLevel signals
    /// and orchestrates scene transitions via ISceneTransitionDirector.
    /// Holds cross-scene level state (coins, health) for snapshot injection.
    /// </summary>
    public class LevelProgressionManager : IStartable, ILevelProgression, ISaveable, IDisposable
    {
        #region Dependencies

        private readonly ISubscriber<LevelCompleteSignal> _levelCompleteSub;
        private readonly ISubscriber<RestartLevelSignal> _restartSub;
        private readonly ISubscriber<LoadLevelSignal> _loadLevelSub;
        private readonly IPublisher<GameCompletedSignal> _gameCompletedPublisher;
        private readonly IPublisher<SceneLoadedSignal> _sceneLoadedPublisher;
        private readonly IPublisher<LevelProgressionChangedSignal> _progressionPublisher;
        private readonly LevelConfigSO _config;
        private readonly ISceneTransitionDirector _transitionDirector;
        private readonly ISaveableRegistry _saveableRegistry;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new(3);

        private int _currentLevelIndex;
        private int _currentSceneInLevelIndex;
        private bool _isInitialized;
        private bool _isLoading;
        private bool _hasActiveLevel;
        private bool _disposed;

        #endregion

        #region Level State (cross-scene, persists within a level)

        private int _coinsCollected;
        private float _currentHealth;

        public int CoinsCollected => _coinsCollected;
        public float CurrentHealth => _currentHealth;

        #endregion

        #region Public API

        public int CurrentLevelIndex => _currentLevelIndex;
        public int CurrentSceneInLevelIndex => _currentSceneInLevelIndex;
        public string SaveId => "LevelProgression";

        public LevelProgressionManager(
            LevelConfigSO config,
            ISceneTransitionDirector transitionDirector,
            ISubscriber<LevelCompleteSignal> levelCompleteSub,
            ISubscriber<RestartLevelSignal> restartSub,
            ISubscriber<LoadLevelSignal> loadLevelSub,
            IPublisher<GameCompletedSignal> gameCompletedPublisher,
            IPublisher<SceneLoadedSignal> sceneLoadedPublisher,
            IPublisher<LevelProgressionChangedSignal> progressionPublisher,
            ISaveableRegistry saveableRegistry)
        {
            if (config == null || config.allLevels == null || config.allLevels.Count == 0)
                throw new InvalidOperationException("LevelConfigSO is missing or empty.");

            _config = config;
            _transitionDirector = transitionDirector;
            _levelCompleteSub = levelCompleteSub;
            _restartSub = restartSub;
            _loadLevelSub = loadLevelSub;
            _gameCompletedPublisher = gameCompletedPublisher;
            _sceneLoadedPublisher = sceneLoadedPublisher;
            _progressionPublisher = progressionPublisher;
            _saveableRegistry = saveableRegistry;

            _currentLevelIndex = config.startingLevelIndex;
        }

        /// <summary>
        /// Called by scene code when coins change or health changes.
        /// Updates the snapshot that will be injected into the next scene.
        /// </summary>
        public void UpdateState(int coinsCollected, float currentHealth)
        {
            _coinsCollected = coinsCollected;
            _currentHealth = currentHealth;
        }

        /// <summary>
        /// Resets level-scoped state. Called when a level is completed.
        /// </summary>
        public void CompleteLevel()
        {
            _coinsCollected = 0;
            _currentHealth = 0f;
        }

        public ISaveData GetSaveData()
        {
            var level = _hasActiveLevel && _currentLevelIndex >= 0 && _currentLevelIndex < _config.allLevels.Count
                ? _config.allLevels[_currentLevelIndex]
                : null;

            return new LevelProgressionSaveData
            {
                levelName = level?.levelName ?? string.Empty,
                scenes = level != null ? new List<SceneField>(level.scenes) : new List<SceneField>()
            };
        }

        public void LoadFromData(ISaveData data)
        {
            if (data is not LevelProgressionSaveData progression || string.IsNullOrEmpty(progression.levelName))
                return;

            for (int i = 0; i < _config.allLevels.Count; i++)
            {
                if (_config.allLevels[i].levelName != progression.levelName) continue;
                _currentLevelIndex = i;
                _currentSceneInLevelIndex = 0;
                _hasActiveLevel = true;
                return;
            }

            Debug.LogWarning($"[LevelProgression] Saved level '{progression.levelName}' is not in the current LevelConfig.");
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            if (_isInitialized) return;

            _subscriptions.Add(_levelCompleteSub.Subscribe(OnLevelComplete));
            _subscriptions.Add(_restartSub.Subscribe(OnRestartLevel));
            _subscriptions.Add(_loadLevelSub.Subscribe(OnLoadLevelRequested));
            _saveableRegistry.Register(this);

            _isInitialized = true;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();
            _isInitialized = false;
        }

        #endregion

        #region Scene Flow

        public async UniTask StartLevelByName(string levelName)
        {
            if (_disposed) return;
            if (_isLoading) return;

            for (int i = 0; i < _config.allLevels.Count; i++)
            {
                if (_config.allLevels[i].levelName == levelName)
                {
                    await StartLevel(i);
                    return;
                }
            }

            Debug.LogError($"[LevelProgression] Level '{levelName}' not found in config.");
        }

        public async UniTask LoadNextScene()
        {
            if (_disposed) return;
            if (_isLoading) return;

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
                    CompleteLevel();
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
            if (_disposed) return;
            if (_isLoading) return;
            if (levelIndex < 0 || levelIndex >= _config.allLevels.Count) return;

            _currentLevelIndex = levelIndex;
            _currentSceneInLevelIndex = 0;
            _hasActiveLevel = true;
            CompleteLevel();
            await LoadSpecificScene(_config.allLevels[_currentLevelIndex].scenes[0]);
        }

        #endregion

        #region Message Handlers

        private void OnLevelComplete(LevelCompleteSignal msg)
        {
            if (_disposed) return;
            LoadNextScene().Forget();
        }

        private void OnRestartLevel(RestartLevelSignal msg)
        {
            if (_disposed) return;
            if (_isLoading) return;
            if (_currentLevelIndex < 0 || _currentLevelIndex >= _config.allLevels.Count) return;

            _currentSceneInLevelIndex = 0;
            CompleteLevel();
            LoadSpecificScene(_config.allLevels[_currentLevelIndex].scenes[0]).Forget();
        }

        private void OnLoadLevelRequested(LoadLevelSignal msg)
        {
            if (_disposed) return;
            StartLevel(msg.LevelIndex).Forget();
        }

        #endregion

        #region Internal

        private async UniTask LoadSpecificScene(SceneField sceneField)
        {
            if (_disposed) return;
            if (_isLoading) return;
            if (sceneField == null || string.IsNullOrEmpty(sceneField.SceneName)) return;

            var existing = SceneManager.GetSceneByName(sceneField.SceneName);
            if (existing.isLoaded)
            {
                _sceneLoadedPublisher.Publish(new SceneLoadedSignal(sceneField.SceneName));
                return;
            }

            _isLoading = true;

            try
            {
                _progressionPublisher.Publish(new LevelProgressionChangedSignal(
                    _currentLevelIndex, _currentSceneInLevelIndex, sceneField.SceneName));

                var snapshot = new LevelStateSnapshot(_coinsCollected, _currentHealth);

                string source = SceneManager.GetActiveScene().name;

                var result = await _transitionDirector.StartTransition(
                    sceneField, null, false, DoorToSpawnAt.None, false, snapshot, source);

                if (result.Success)
                {
                    _sceneLoadedPublisher.Publish(new SceneLoadedSignal(sceneField.SceneName));
                }
                else if (!result.Cancelled)
                {
                    Debug.LogError($"[LevelProgression] Transition to '{sceneField.SceneName}' failed: {result.FailureReason}");
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        #endregion
    }
}   