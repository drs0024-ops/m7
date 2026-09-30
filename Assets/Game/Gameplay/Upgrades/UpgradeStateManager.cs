using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Data;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Upgrades
{
    /// <summary>
    /// Tracks unlocked upgrades and active timed effects.
    /// Integrates with the save system via ISaveable.
    /// </summary>
    public class UpgradeStateManager : ITickable, IStartable, ISaveable, IDisposable
    {
        #region Dependencies

        private readonly ISubscriber<PlayerSpawned> _playerSpawnedSubscriber;
        private readonly IPublisher<UpgradeExpired> _expiredPublisher;
        private readonly UpgradeListObject _registry;
        private readonly ISaveableRegistry _saveableRegistry;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new(1);
        private readonly HashSet<string> _unlockedUpgradeIds = new();
        private readonly Dictionary<string, float> _activeTimedEffects = new();
        private Transform _currentPlayer;
        private bool _disposed;

        #endregion

        #region Public API

        public string SaveId => "PlayerUpgrades";
        public Transform CurrentPlayer => _currentPlayer;

        public bool HasUpgrade(string id) => _unlockedUpgradeIds.Contains(id);
        public bool IsEffectActive(string id) => _activeTimedEffects.ContainsKey(id);

        public float GetRemainingTime(string id)
        {
            if (!_activeTimedEffects.TryGetValue(id, out var expiry)) return 0f;
            return Mathf.Max(0f, expiry - Time.time);
        }

        public IReadOnlyCollection<string> UnlockedUpgradeIds => _unlockedUpgradeIds;

        public PlayerUpgrade GetUpgrade(string id) => _registry.GetUpgrade(id);

        public bool AddUpgrade(string upgradeId)
        {
            return _unlockedUpgradeIds.Add(upgradeId);
        }

        public void StartTimedEffect(string upgradeId, float duration)
        {
            _activeTimedEffects[upgradeId] = Time.time + duration;
        }

        public void LoadFromIds(string[] ids)
        {
            _unlockedUpgradeIds.Clear();
            _activeTimedEffects.Clear();

            for (int i = 0; i < ids.Length; i++)
            {
                if (_registry.GetUpgrade(ids[i]) != null)
                    _unlockedUpgradeIds.Add(ids[i]);
                else
                    Debug.LogWarning($"[UpgradeStateManager] Saved upgrade '{ids[i]}' not found.");
            }
        }

        public UpgradeStateManager(
            UpgradeListObject registry,
            ISaveableRegistry saveableRegistry,
            ISubscriber<PlayerSpawned> playerSpawnedSubscriber,
            IPublisher<UpgradeExpired> expiredPublisher)
        {
            _registry = registry;
            _saveableRegistry = saveableRegistry;
            _playerSpawnedSubscriber = playerSpawnedSubscriber;
            _expiredPublisher = expiredPublisher;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _saveableRegistry.Unregister(this);

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();

            _unlockedUpgradeIds.Clear();
            _activeTimedEffects.Clear();
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            _saveableRegistry.Register(this);
            _subscriptions.Add(_playerSpawnedSubscriber.Subscribe(OnPlayerSpawned));
        }

        #endregion

        #region ITickable

        void ITickable.Tick()
        {
            if (_disposed) return;

            float now = Time.time;
            var toRemove = new List<string>();

            foreach (var kvp in _activeTimedEffects)
            {
                if (kvp.Value <= now)
                    toRemove.Add(kvp.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                _activeTimedEffects.Remove(toRemove[i]);
                _expiredPublisher.Publish(new UpgradeExpired(toRemove[i]));
            }
        }

        #endregion

        #region ISaveable

        public ISaveData GetSaveData()
        {
            return new PlayerUpgradesSaveData
            {
                UnlockedUpgradeIDs = _unlockedUpgradeIds.ToArray()
            };
        }

        public void LoadFromData(ISaveData data)
        {
            if (data is not PlayerUpgradesSaveData saveData || saveData.UnlockedUpgradeIDs == null)
                return;
            LoadFromIds(saveData.UnlockedUpgradeIDs);
            Debug.Log($"[UpgradeStateManager] Loaded {_unlockedUpgradeIds.Count} upgrades.");
        }

        #endregion

        #region Message Handlers

        private void OnPlayerSpawned(PlayerSpawned message)
        {
            _currentPlayer = message.Player;
        }

        #endregion
    }
}   