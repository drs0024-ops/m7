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
    public sealed class EnemyDebugWindow : EditorWindow
    {
        private sealed class EnemyRow
        {
            public int Id;
            public string Name;
            public string TypeId;
            public string State;
        }

        private const int MaxLogEntries = 30;

        private readonly SortedDictionary<int, EnemyRow> _enemies = new();
        private readonly List<string> _events = new(MaxLogEntries);
        private readonly List<IDisposable> _subscriptions = new(3);
        private Vector2 _scrollPosition;
        private bool _connected;
        private string _status = "Enter Play Mode to connect.";
        private PlayerHealth _player;
        private float _playerCurrentHealth;
        private float _playerMaxHealth;
        private bool _hasHealthSample;

        [MenuItem("Window/Game/Enemy Debug")]
        private static void Open()
        {
            var window = GetWindow<EnemyDebugWindow>("Enemy Debug");
            window.minSize = new Vector2(360f, 320f);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Disconnect();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode
                || state == PlayModeStateChange.EnteredEditMode)
            {
                Disconnect();
                _enemies.Clear();
                _events.Clear();
                _player = null;
                _hasHealthSample = false;
                _status = "Enter Play Mode to connect.";
            }

            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying)
            {
                Repaint();
                return;
            }

            if (!_connected)
                TryConnect();

            RefreshPlayerHealth();
            Repaint();
        }

        private void TryConnect()
        {
            if (FindFirstObjectByType<FacilitySceneLifetimeScope>() == null)
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
            EnemyEntity[] entities = FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < entities.Length; i++)
            {
                EnemyEntity entity = entities[i];
                if (entity == null)
                    continue;

                AddOrUpdateEnemy(entity.InstanceId.Value, entity.Definition, entity.name);
            }
        }

        private void OnEnemySpawned(EnemySpawned message)
        {
            string objectName = FindEnemyObjectName(message.InstanceId.Value);
            AddOrUpdateEnemy(message.InstanceId.Value, message.Definition, objectName);

            if (_enemies.TryGetValue(message.InstanceId.Value, out EnemyRow row))
                AddEvent($"{row.Name} spawned ({row.TypeId}).");
        }

        private void OnEnemyDespawned(EnemyDespawned message)
        {
            if (_enemies.TryGetValue(message.InstanceId.Value, out EnemyRow row))
            {
                AddEvent($"{row.Name} despawned.");
                _enemies.Remove(message.InstanceId.Value);
            }
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
            if (definition == null)
                return;

            if (!_enemies.TryGetValue(id, out EnemyRow row))
            {
                row = new EnemyRow { Id = id };
                _enemies.Add(id, row);
            }

            row.Name = string.IsNullOrEmpty(objectName) ? definition.TypeId : objectName;
            row.TypeId = definition.TypeId;

            if (string.IsNullOrEmpty(row.State))
            {
                try
                {
                    row.State = definition.StateGraph.GetInitialLeafStateId();
                }
                catch (Exception)
                {
                    row.State = "Invalid definition";
                }
            }
        }

        private static string FindEnemyObjectName(int instanceId)
        {
            EnemyEntity[] entities = FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < entities.Length; i++)
                if (entities[i] != null && entities[i].InstanceId.Value == instanceId)
                    return entities[i].name;

            return $"Enemy {instanceId}";
        }

        private void RefreshPlayerHealth()
        {
            PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
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

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Enemy Debug", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Status", Application.isPlaying ? _status : "Enter Play Mode to connect.");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
            if (_player != null && _hasHealthSample)
                EditorGUILayout.LabelField("Health", $"{_playerCurrentHealth:0.##} / {_playerMaxHealth:0.##}");
            else
                EditorGUILayout.LabelField("Health", "Player not found or not initialized.");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Enemies ({_enemies.Count})", EditorStyles.boldLabel);
            if (_enemies.Count == 0)
                EditorGUILayout.LabelField("No active enemies.");
            else
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Name / Type", GUILayout.MinWidth(150f));
                    GUILayout.Label("State", GUILayout.MinWidth(80f));
                    GUILayout.Label("ID", GUILayout.Width(55f));
                }

                foreach (EnemyRow row in _enemies.Values)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{row.Name} ({row.TypeId})", GUILayout.MinWidth(150f));
                        EditorGUILayout.LabelField(string.IsNullOrEmpty(row.State) ? "-" : row.State, GUILayout.MinWidth(80f));
                        EditorGUILayout.LabelField(row.Id.ToString(), GUILayout.Width(55f));
                    }
                }
            }

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Recent Events", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", GUILayout.Width(55f)))
                    _events.Clear();
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = _events.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(_events[i], EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif