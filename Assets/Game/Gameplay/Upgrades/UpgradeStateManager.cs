using System.Collections.Generic;
using System.Linq;
using MessagePipe;
using Game.Core.Messages;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Core.Interfaces;
using System;
using Game.Core.Data;
/// <summary>
/// Tracks unlocked upgrades and active timed effects.
/// Uses tick-based expiration. No publishing — pure state.
/// </summary>

namespace Game.Gameplay.Upgrades
{
    public class UpgradeStateManager : ITickable, IStartable, ISaveable, IDisposable
    {
        private ISubscriber<PlayerSpawned> _playerSpawnedSubscriber;
        private readonly UpgradeListObject _registry;
        private readonly ISaveableRegistry _saveableRegistry;
        private readonly List<IDisposable> _subscriptions = new();

        private IPublisher<UpgradeExpired> _expiredPublisher;

        private readonly HashSet<string> _unlockedUpgradeIds = new();
        private readonly Dictionary<string, float> _activeTimedEffects = new();
        private Transform _currentPlayer;

        private bool _disposed;

        [Inject]
        public UpgradeStateManager(UpgradeListObject registry, ISaveableRegistry saveableRegistry)
        {
            _registry = registry;
            _saveableRegistry = saveableRegistry;
        }

        void IStartable.Start()
        {
            _saveableRegistry.Register(this);
            _playerSpawnedSubscriber = GlobalMessagePipe.GetSubscriber<PlayerSpawned>();
            _subscriptions.Add(_playerSpawnedSubscriber.Subscribe(OnPlayerSpawned));

            _expiredPublisher = GlobalMessagePipe.GetPublisher<UpgradeExpired>();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _saveableRegistry.Unregister(this);

            foreach (var sub in _subscriptions) sub?.Dispose();
            _subscriptions.Clear();

            _unlockedUpgradeIds.Clear();
            _activeTimedEffects.Clear();
        }

        void ITickable.Tick()
        {
            if (_disposed) return;

            float now = Time.time;
            var expired = new List<string>();

            foreach (var kvp in _activeTimedEffects)
            {
                if (kvp.Value <= now)
                    expired.Add(kvp.Key);
            }

            for (int i = 0; i < expired.Count; i++)
            {
                _activeTimedEffects.Remove(expired[i]);
                _expiredPublisher.Publish(new UpgradeExpired(expired[i]));
            }
        }

        private void OnPlayerSpawned(PlayerSpawned message)
        {
            _currentPlayer = message.Player;
        }

        #region ISaveable

        public string SaveId => "PlayerUpgrades";

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

        #region Public API

        public bool HasUpgrade(string id) => _unlockedUpgradeIds.Contains(id);

        public bool IsEffectActive(string id) => _activeTimedEffects.ContainsKey(id);

        public float GetRemainingTime(string id)
        {
            if (!_activeTimedEffects.TryGetValue(id, out var expiry)) return 0f;
            return Mathf.Max(0f, expiry - Time.time);
        }

        public string[] UnlockedUpgradeIds => _unlockedUpgradeIds.ToArray();

        public Transform CurrentPlayer => _currentPlayer;

        public PlayerUpgrade GetUpgrade(string id) => _registry.GetUpgrade(id);

        public bool AddUpgrade(string upgradeId)
        {
            if (_unlockedUpgradeIds.Contains(upgradeId)) return false;
            _unlockedUpgradeIds.Add(upgradeId);
            return true;
        }

        public void StartTimedEffect(string upgradeId, float duration)
        {
            _activeTimedEffects[upgradeId] = Time.time + duration;
        }

        public void LoadFromIds(string[] ids)
        {
            _unlockedUpgradeIds.Clear();
            _activeTimedEffects.Clear();

            foreach (var id in ids)
            {
                if (_registry.GetUpgrade(id) != null)
                    _unlockedUpgradeIds.Add(id);
                else
                    Debug.LogWarning($"[UpgradeStateManager] Saved upgrade '{id}' not found.");
            }
        }

        #endregion

    }

}

 