using System;
using System.Collections.Generic;
using Game.Core.Data;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Achievements
{
    /// <summary>
    /// Tracks unlocked achievements. Integrates with the save system via ISaveable.
    /// Debounces save requests by 1 second after each unlock.
    /// </summary>
    public class AchievementManager : IStartable, ITickable, IDisposable, ISaveable
    {
        private const string SAVE_ID = "Achievement";
        private const float SaveDelay = 1.0f;

        #region Dependencies

        private readonly IPublisher<SaveRequest> _saveRequestPublisher;
        private readonly IPublisher<AchievementUnlocked> _achievementUnlockedPublisher;
        private readonly AchievementConfigSO _config;
        private readonly ISaveableRegistry _registry;

        #endregion

        #region State

        private readonly List<string> _unlockedAchievements = new();
        private bool _isSavePending;
        private bool _disposed;
        private float _saveTimer;

        #endregion

        #region Construction

        public AchievementManager(
            AchievementConfigSO config,
            ISaveableRegistry registry,
            IPublisher<SaveRequest> saveRequestPublisher,
            IPublisher<AchievementUnlocked> achievementUnlockedPublisher)
        {
            _config = config;
            _registry = registry;
            _saveRequestPublisher = saveRequestPublisher;
            _achievementUnlockedPublisher = achievementUnlockedPublisher;
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            if (_disposed) return;

            _registry.Register(this);

            if (_config.achievementList == null)
            {
                Debug.LogError("[AchievementManager] AchievementList is missing in Config SO!");
                return;
            }
        }

        #endregion

        #region ITickable

        void ITickable.Tick()
        {
            if (_disposed || !_isSavePending) return;

            _saveTimer -= Time.deltaTime;
            if (_saveTimer <= 0f)
            {
                _isSavePending = false;
                ForceSave();
            }
        }

        #endregion

        #region Public API

        public string SaveId => SAVE_ID;

        public void UnlockAchievement(string achievementId)
        {
            if (_disposed) return;
            if (_unlockedAchievements.Contains(achievementId)) return;

            var achievement = _config.achievementList.GetAchievement(achievementId);
            if (string.IsNullOrEmpty(achievement.AchievementID))
            {
                Debug.LogWarning($"[AchievementManager] Unknown ID: {achievementId}");
                return;
            }

            _unlockedAchievements.Add(achievementId);
            _achievementUnlockedPublisher.Publish(new AchievementUnlocked(achievement));

            RequestSave();
        }

        public bool IsAchievementUnlocked(string id) => _unlockedAchievements.Contains(id);
        public Achievement GetAchievement(string id) => _config.achievementList.GetAchievement(id);
        public IReadOnlyList<string> UnlockedAchievements => _unlockedAchievements.AsReadOnly();

        #endregion

        #region ISaveable

        public ISaveData GetSaveData() => new AchievementSaveData
        {
            SaveId = SAVE_ID,
            UnlockedAchievementIDs = _unlockedAchievements.ToArray()
        };

        public void LoadFromData(ISaveData data)
        {
            if (data is not AchievementSaveData saveData) return;

            _unlockedAchievements.Clear();

            if (saveData.UnlockedAchievementIDs == null) return;

            for (int i = 0; i < saveData.UnlockedAchievementIDs.Length; i++)
            {
                string id = saveData.UnlockedAchievementIDs[i];
                var achievement = _config.achievementList.GetAchievement(id);
                if (!string.IsNullOrEmpty(achievement.AchievementID))
                    _unlockedAchievements.Add(id);
                else
                    Debug.LogWarning($"[AchievementManager] Saved achievement '{id}' not found in config.");
            }
        }

        #endregion

        #region Save Debounce

        private void RequestSave()
        {
            if (_isSavePending) return;
            _isSavePending = true;
            _saveTimer = SaveDelay;
        }

        private void ForceSave()
        {
            _saveRequestPublisher.Publish(new SaveRequest("AchievementManager", false));
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _registry.Unregister(this);

            if (_isSavePending)
            {
                _isSavePending = false;
                try
                {
                    ForceSave();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AchievementManager] Failed to save on dispose: {e}");
                }
            }
        }

        #endregion
    }
}   