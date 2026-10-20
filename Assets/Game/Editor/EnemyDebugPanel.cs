#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Bootstrap.Scopes;
using Game.Gameplay.Enemies.Runtime;
using Game.Gameplay.Player;
using MessagePipe;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal sealed class EnemyDebugPanel : IDisposable
    {
        private sealed class EnemyRow
        {
            public int Id;
            public string Name;
            public string TypeId;
            public string State;
            public float CurrentHealth;
            public float MaxHealth;
            public bool HasHealthSample;
        }

        private const int MaxLogEntries = 30;

        private readonly SortedDictionary<int, EnemyRow> _enemies = new();
        private readonly List<string> _events = new(MaxLogEntries);
        private readonly List<IDisposable> _subscriptions = new(3);
        private Vector2 _eventScrollPosition;
        private bool _connected;
        private bool _enabled;
        private string _status = "Enter Play Mode to connect.";
        private PlayerHealth _player;
        private float _playerCurrentHealth;
        private float _playerMaxHealth;
        private bool _hasHealthSample;

        public void Enable()
        {
            if (_enabled) return;
            _enabled = true;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void Refresh()
        {
            if (!Application.isPlaying) return;

            if (!_connected)
                TryConnect();

            RefreshEnemyHealth();
            RefreshPlayerHealth();
        }

        public void DrawContents(float availableWidth)
        {
            EditorGUILayout.LabelField("Status", Application.isPlaying ? _status : "Enter Play Mode to connect.");

            if (availableWidth >= 720f)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(availableWidth * 0.58f)))
                        DrawCombatStatus();
                    using (new EditorGUILayout.VerticalScope())
                        DrawEvents();
                }
            }
            else
            {
                DrawCombatStatus();
                DrawEvents();
            }
        }

        public void Dispose()
        {
            if (!_enabled) return;
            _enabled = false;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Disconnect();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode
                && state != PlayModeStateChange.EnteredEditMode)
                return;

            Disconnect();
            _enemies.Clear();
            _events.Clear();
            _player = null;
            _hasHealthSample = false;
            _status = "Enter Play Mode to connect.";
        }

        private void TryConnect()
        {
            if (UnityEngine.Object.FindFirstObjectByType<FacilitySceneLifetimeScope>() == null)
            {
                _status = "Waiting for FacilitySceneLifetimeScope.";
                return;
            }

            try
            {
                _subscriptions.Add(GlobalMessagePipe.GetSubscriber<EnemySpawned>().Subscribe(OnEnemySpawned));
                _subscriptions.Add(GlobalMessagePipe.GetSubscriber<EnemyDespawned>().Subscribe(OnEnemyDespawned));
                _subscriptions.Add(GlobalMessagePipe.GetSubscriber<EnemyStateChanged>().Subscribe(OnEnemyStateChanged));
                _connected = true;
                _status = "Connected to scene events.";
                SeedExistingEnemies();
            }
            catch (Exception exception)
            {
                Disconnect();
                _status = $"MessagePipe connection failed: {exception.GetType().Name}";
            }
        }

        private void SeedExistingEnemies()
        {
            EnemyEntity[] entities = UnityEngine.Object.FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < entities.Length; i++)
            {
                EnemyEntity entity = entities[i];
                if (entity != null)
                    AddOrUpdateEnemy(entity.InstanceId.Value, entity.Definition, entity.name);
            }
        }

        private void OnEnemySpawned(EnemySpawned message)
        {
            AddOrUpdateEnemy(
                message.InstanceId.Value,
                message.Definition,
                FindEnemyObjectName(message.InstanceId.Value));

            if (_enemies.TryGetValue(message.InstanceId.Value, out EnemyRow row))
                AddEvent($"{row.Name} spawned ({row.TypeId}).");
        }

        private void OnEnemyDespawned(EnemyDespawned message)
        {
            if (!_enemies.TryGetValue(message.InstanceId.Value, out EnemyRow row)) return;

            AddEvent($"{row.Name} despawned.");
            _enemies.Remove(message.InstanceId.Value);
        }

        private void OnEnemyStateChanged(EnemyStateChanged message)
        {
            if (!_enemies.TryGetValue(message.InstanceId.Value, out EnemyRow row))
            {
                row = new EnemyRow
                {
                    Id = message.InstanceId.Value,
                    Name = FindEnemyObjectName(message.InstanceId.Value),
                    TypeId = "Enemy"
                };
                _enemies.Add(row.Id, row);
            }

            row.State = message.CurrentStateId;
            AddEvent($"{row.Name}: {message.PreviousStateId} -> {message.CurrentStateId}.");
        }

        private void AddOrUpdateEnemy(int id, EnemyDefinition definition, string objectName)
        {
            if (definition == null) return;

            if (!_enemies.TryGetValue(id, out EnemyRow row))
            {
                row = new EnemyRow { Id = id };
                _enemies.Add(id, row);
            }

            row.Name = string.IsNullOrEmpty(objectName) ? definition.TypeId : objectName;
            row.TypeId = definition.TypeId;
            if (!string.IsNullOrEmpty(row.State)) return;

            try
            {
                row.State = definition.StateGraph.GetInitialLeafStateId();
            }
            catch (Exception)
            {
                row.State = "Invalid definition";
            }
        }

        private static string FindEnemyObjectName(int instanceId)
        {
            EnemyEntity[] entities = UnityEngine.Object.FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < entities.Length; i++)
                if (entities[i] != null && entities[i].InstanceId.Value == instanceId)
                    return entities[i].name;

            return $"Enemy {instanceId}";
        }

        private void RefreshEnemyHealth()
        {
            EnemyEntity[] entities = UnityEngine.Object.FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < entities.Length; i++)
            {
                EnemyEntity entity = entities[i];
                if (entity == null || !_enemies.TryGetValue(entity.InstanceId.Value, out EnemyRow row))
                    continue;

                if (entity.Definition == null || !entity.TryGetCurrentHealth(out float currentHealth))
                {
                    row.HasHealthSample = false;
                    continue;
                }

                float maxHealth = entity.Definition.Stats.MaxHealth;
                if (row.HasHealthSample && currentHealth < row.CurrentHealth)
                    AddEvent($"{row.Name} took {row.CurrentHealth - currentHealth:0.##} damage ({row.CurrentHealth:0.##} -> {currentHealth:0.##}).");

                row.CurrentHealth = currentHealth;
                row.MaxHealth = maxHealth;
                row.HasHealthSample = true;
            }
        }

        private void RefreshPlayerHealth()
        {
            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            if (player == null)
            {
                _player = null;
                _hasHealthSample = false;
                return;
            }

            try
            {
                float current = player.CurrentHealth;
                float maximum = player.MaxHealth;
                if (player != _player)
                    _hasHealthSample = false;

                if (_hasHealthSample && current < _playerCurrentHealth)
                    AddEvent($"Player took {_playerCurrentHealth - current:0.##} damage ({_playerCurrentHealth:0.##} -> {current:0.##}).");
                else if (_hasHealthSample && current > _playerCurrentHealth)
                    AddEvent($"Player healed ({_playerCurrentHealth:0.##} -> {current:0.##}).");

                _player = player;
                _playerCurrentHealth = current;
                _playerMaxHealth = maximum;
                _hasHealthSample = true;
            }
            catch (NullReferenceException)
            {
                _player = null;
                _hasHealthSample = false;
            }
        }

        private void DrawCombatStatus()
        {
            EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Health",
                _player != null && _hasHealthSample
                    ? $"{_playerCurrentHealth:0.##} / {_playerMaxHealth:0.##}"
                    : "Player not found or not initialized.");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Enemies ({_enemies.Count})", EditorStyles.boldLabel);
            if (_enemies.Count == 0)
            {
                EditorGUILayout.LabelField("No active enemies.");
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Name / Type", GUILayout.MinWidth(150f));
                GUILayout.Label("State", GUILayout.MinWidth(80f));
                GUILayout.Label("HP", GUILayout.Width(95f));
                GUILayout.Label("ID", GUILayout.Width(55f));
            }

            foreach (EnemyRow row in _enemies.Values)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{row.Name} ({row.TypeId})", GUILayout.MinWidth(150f));
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(row.State) ? "-" : row.State, GUILayout.MinWidth(80f));
                    EditorGUILayout.LabelField(
                        row.HasHealthSample ? $"{row.CurrentHealth:0.##} / {row.MaxHealth:0.##}" : "-",
                        GUILayout.Width(95f));
                    EditorGUILayout.LabelField(row.Id.ToString(), GUILayout.Width(55f));
                }
            }
        }

        private void DrawEvents()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Recent Events", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", GUILayout.Width(55f)))
                    _events.Clear();
            }

            _eventScrollPosition = EditorGUILayout.BeginScrollView(_eventScrollPosition);
            for (int i = _events.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(_events[i], EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }

        private void AddEvent(string message)
        {
            _events.Add($"{Time.time:0.0}s  {message}");
            if (_events.Count > MaxLogEntries)
                _events.RemoveAt(0);
        }

        private void Disconnect()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();

            _subscriptions.Clear();
            _connected = false;
        }
    }
}
#endif