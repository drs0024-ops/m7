using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Data;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Save
{
    public class SaveManager : IStartable, IDisposable, ITickable
    {
        private const int SAVE_LOAD_BUDGET_MS = 50;

        private IPublisher<SaveCompleted> _saveCompletedPublisher;
        private IPublisher<SaveFailed> _saveFailedPublisher;
        private IPublisher<LoadCompleted> _loadCompletedPublisher;
        private ISubscriber<SaveRequest> _saveRequestSub;
        private ISubscriber<LoadRequest> _loadRequestSub;
        private IPublisher<LoadFailed> _loadFailedPublisher;

        private readonly ISaveableRegistry _registry;
        private readonly SaveConfigSO _config;

        private bool _isSavePending;
        private float _saveTimer;
        private const float SAVE_DELAY = 1.0f;
        public bool HasSave => CurrentLevelName != null;

        private SaveDataContainer _currentSaveData = new();
        private string SavePath => Path.Combine(Application.persistentDataPath, _config.saveFileName);

        private readonly List<IDisposable> _subscriptions = new();
        private CancellationTokenSource _cts;
        private bool _disposed;

        private const int CurrentSaveVersion = 1;

        /// <summary>
        /// The level scene name from the most recently loaded save. Null if no save loaded.
        /// </summary>
        public string CurrentLevelName { get; private set; }

        [Inject]
        public SaveManager(ISaveableRegistry registry, SaveConfigSO config)
        {
            _registry = registry;
            _config = config;
        }

        #region IStartable

        void IStartable.Start()
        {
            _cts = new CancellationTokenSource();

            _saveCompletedPublisher = GlobalMessagePipe.GetPublisher<SaveCompleted>();
            _saveFailedPublisher = GlobalMessagePipe.GetPublisher<SaveFailed>();
            _loadCompletedPublisher = GlobalMessagePipe.GetPublisher<LoadCompleted>();
            _saveRequestSub = GlobalMessagePipe.GetSubscriber<SaveRequest>();
            _loadRequestSub = GlobalMessagePipe.GetSubscriber<LoadRequest>();
            _loadFailedPublisher = GlobalMessagePipe.GetPublisher<LoadFailed>();   

            _subscriptions.Add(_saveRequestSub.Subscribe(OnSaveRequested));
            _subscriptions.Add(_loadRequestSub.Subscribe(_ => OnLoadRequested()));
        }

        #endregion

        #region ITickable

        public void Tick()
        {
            if (_disposed || !_isSavePending || Time.timeScale < 0.01f) return;

            _saveTimer -= Time.deltaTime;
            if (_saveTimer <= 0f)
            {
                _isSavePending = false;
                PerformSave();
            }
        }

        #endregion

        #region Message Handlers

        private void OnSaveRequested(SaveRequest msg)
        {
            if (_disposed) return;

            if (msg.Immediate)
            {
                _isSavePending = false;
                _saveTimer = 0f;
                PerformSave();
            }
            else
            {
                RequestSave();
            }
        }

        private void OnLoadRequested()
        {
            if (_disposed) return;
            LoadAsync().Forget();
        }

        #endregion

        #region Public API

        public void RequestSave()
        {
            if (_disposed) return;
            _isSavePending = true;
            _saveTimer = SAVE_DELAY;
        }

        public async UniTask LoadAsync()
        {
            if (_disposed) return;

            try
            {
                string decrypted = await LoadAsyncCore(_cts.Token);
                if (_disposed) return;

                _currentSaveData = JsonUtility.FromJson<SaveDataContainer>(decrypted);
                _currentSaveData.RebuildDictionary();

                if (_currentSaveData.version < CurrentSaveVersion)
                {
                    Debug.Log($"[SaveManager] Migrating save from v{_currentSaveData.version} → v{CurrentSaveVersion}");
                    SaveMigration.Migrate(_currentSaveData, _currentSaveData.version, CurrentSaveVersion);
                    _currentSaveData.RebuildDictionary();
                }

                if (_currentSaveData.TryGetValue("LevelProgression", out string json))
                {
                    var progression = JsonUtility.FromJson<LevelProgressionSaveData>(json);
                    CurrentLevelName = progression.levelName;
                }
                else
                {
                    CurrentLevelName = null;
                }

                _loadCompletedPublisher.Publish(LoadCompleted.Default);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e}");
                if (!_disposed)
                    _loadFailedPublisher.Publish(new LoadFailed(e.Message));
            }
        }   

        #endregion

        #region Core Logic

        private void PerformSave()
        {
            if (_disposed) return;
            _isSavePending = false;  // ← add
            _saveTimer = 0;          // ← add
            SaveAsyncCore(_cts.Token).Forget();
        }
        private async UniTask SaveAsyncCore(CancellationToken token)
        {
            try
            {
                await SaveAsync(token);
                if (_disposed) return;
                _saveCompletedPublisher.Publish(SaveCompleted.Default);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e}");
                if (!_disposed)
                    _saveFailedPublisher.Publish(new SaveFailed(e.Message));
            }
        }

        private async UniTask SaveAsync(CancellationToken token)
        {
            if (_currentSaveData == null) _currentSaveData = new SaveDataContainer();
            _currentSaveData.SaveDataChunks.Clear();

            var savables = _registry.GetAll();
            for (int i = 0; i < savables.Count; i++)
            {
                var savable = savables[i];
                if (savable == null) continue;

                var data = savable.GetSaveData();
                if (data != null && !string.IsNullOrEmpty(data.SaveId))
                {
                    string json = JsonUtility.ToJson(data);
                    _currentSaveData.SetValue(data.SaveId, json);
                }
            }

            _currentSaveData.version = CurrentSaveVersion;
            string jsonContainer = JsonUtility.ToJson(_currentSaveData);
            string encrypted = Encrypt(jsonContainer);

            string path = SavePath;

            await UniTask.RunOnThreadPool(() =>
            {
                File.WriteAllText(path, encrypted);
            }, cancellationToken: token);
        }   
        
        private async UniTask<string> LoadAsyncCore(CancellationToken token)
        {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
            var sw = System.Diagnostics.Stopwatch.StartNew();
        #endif

            string path = SavePath;

            string encrypted = await UniTask.RunOnThreadPool(() =>
            {
                return File.ReadAllText(path);
            }, cancellationToken: token);

            string result = Decrypt(encrypted);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
            sw.Stop();
            long ms = sw.ElapsedMilliseconds;
            Debug.Log($"[Perf] SaveLoad took {ms}ms");
            if (ms > SAVE_LOAD_BUDGET_MS)
                Debug.LogWarning($"[Perf][WARN] SaveLoad took {ms}ms (budget: {SAVE_LOAD_BUDGET_MS}ms)");
        #endif   

            return result;
        }   

        private static ISaveData DeserializeSaveData(string json, Type targetType)
        {
            try
            {
                MethodInfo method = typeof(JsonUtility).GetMethod("FromJson", new[] { typeof(string) });
                MethodInfo genericMethod = method.MakeGenericMethod(targetType);
                return genericMethod.Invoke(null, new object[] { json }) as ISaveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Deserialize failed for {targetType.Name}: {e}");
                return null;
            }
        }

        #endregion

        #region Encryption (Placeholder)

        private string Encrypt(string json) => json;
        private string Decrypt(string json) => json;

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 1. Flush pending save (synchronous, <1ms)
            if (_isSavePending)
            {
                _isSavePending = false;
                try
                {
                    SaveSync();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveManager] Failed to save on dispose: {e}");
                }
            }

            // 2. Dispose subscriptions
            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();

            // 3. Cancel + dispose CTS
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            // 4. Release save data references
            _currentSaveData = null;
        }   

        private void SaveSync()
        {
            try
            {
                if (_currentSaveData == null) _currentSaveData = new SaveDataContainer();
                _currentSaveData.SaveDataChunks.Clear();

                var savables = _registry.GetAll();
                for (int i = 0; i < savables.Count; i++)
                {
                    var savable = savables[i];
                    if (savable == null) continue;

                    var data = savable.GetSaveData();
                    if (data != null && !string.IsNullOrEmpty(data.SaveId))
                        _currentSaveData.SetValue(data.SaveId, JsonUtility.ToJson(data));
                }

                _currentSaveData.version = CurrentSaveVersion;
                File.WriteAllText(SavePath, Encrypt(JsonUtility.ToJson(_currentSaveData)));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Final save failed: {e}");
            }
        }   

        #endregion
    }
}   

#region Pre VContainer version
/*
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

/// <summary>
/// Centralized manager for handling game data persistence (Save/Load).
/// Implements a Singleton pattern to persist across scenes using DontDestroyOnLoad.
/// Uses an early execution order (-50) to ensure availability before other managers initialize.
/// 
/// Responsibilities:
/// - Aggregates save data from all registered <see cref="ISavable"/> components.
/// - Serializes data into a modular JSON structure with encryption.
/// - Handles asynchronous file I/O to prevent frame rate spikes.
/// - Broadcasts save completion events to update UI or trigger state changes.
/// - Provides a helper to retrieve the saved scene name without full deserialization.
/// 
/// Usage:
/// Attach to a persistent GameObject (e.g., "GameManager").
/// Ensure ISavable components register themselves on Awake/OnEnable.
/// Assign Event Channels in the Inspector.
/// </summary>
/// 
/// Centralized manager for saving and loading game state.
/// Registers as ISaveManager in the ServiceLocator.
/// </summary>

[DefaultExecutionOrder(-100)] // Run BEFORE everything else besides ServiceLocator(default is 0)
public class SaveManager : MonoBehaviour, ISaveManager
{
    #region Configuration

    [Header("Events")]
    [SerializeField] private  SaveRequestedEventChannel saveRequestedEventChannel;
    [SerializeField] private VoideEventChannel onSaveCompleteGlobalEvent;

    [Header("Settings")]
    [SerializeField] private string saveFileName = "save.save";

    #endregion

    #region Private State

    private SaveDataContainer _currentSaveData = new SaveDataContainer();
    private readonly List<ISaveable> _savables = new List<ISaveable>();
    private ServiceLocator _serviceLocator;

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Do NOT access ServiceLocator here if you can avoid it.
        // If you must, use the static property directly, not a cached variable.
        Debug.Log($"[SaveManager] Awake() - Time: {Time.realtimeSinceStartup}");

     
    }

    void Start()
    {
        // 1. Access Static Property Directly (Triggers Lazy Load if needed)
        _serviceLocator = ServiceLocator.Instance;

        // Single Null Check
        if (_serviceLocator == null)
        {
            Debug.LogError("[SaveManager] ServiceLocator instance not found!", this);
            enabled = false;
            return;
        }

        // Register Self
        _serviceLocator.Register<ISaveManager>(this);
        _serviceLocator.Register<SaveManager>(this);
        
        Debug.Log("[SaveManager] Successfully initialized.");
    }

    private void OnDestroy()
    {
        if (_serviceLocator != null)
        {
            _serviceLocator.Unregister<ISaveManager>();
            _serviceLocator.Unregister<SaveManager>();
        }
    }

    void OnDisable()
    {
   
    }

    #endregion

    #region Event Subscriptions


    // 1. Keep the Event Handler private (called only by the Event System)
    private void HandleSaveRequest()
    {
        // Fire and forget via event is okay here, but better to call the public method
        RequestSave(); 
    }

    // 2. Create a Public API method (Safe, awaitable, callable by UI/Code)
    public async void RequestSave()
    {
        Debug.Log("[SaveManager] Save initiated.");
        await SaveAsync();
    }

    #endregion

    #region Registration System

    public void Register(ISaveable saveable)
    {
        if (saveable != null && !_savables.Contains(saveable))
            _savables.Add(saveable);
    }

    public void Unregister(ISaveable saveable)
    {
        if (saveable != null)
            _savables.Remove(saveable);
    }

    #endregion

    #region Interface Implementation - Properties

    public string CurrentSceneName
    {
        get
        {
            if (_currentSaveData == null || _currentSaveData.SaveDataChunks == null)
                return null;

            if (_currentSaveData.SaveDataChunks.TryGetValue("GameLevel", out string json))
            {
                var levelData = JsonUtility.FromJson<GameLevelManagerSaveData>(json);
                return levelData.CurrentSceneName;
            }
            return null;
        }
    }

    #endregion

    #region Interface Implementation - Save/Load

    public async Task SaveAsync()
    {
        _currentSaveData.SaveDataChunks ??= new Dictionary<string, string>();
        _currentSaveData.SaveDataChunks.Clear();

        foreach (var savable in _savables)
        {
            if (savable == null) continue;

            var data = savable.GetSaveData();
            if (data != null)
            {
                string json = JsonUtility.ToJson(data);
                _currentSaveData.SaveDataChunks[data.SaveId] = json;
            }
        }

        try
        {
            string json = JsonUtility.ToJson(_currentSaveData);
            string encrypted = await Task.Run(() => Encrypt(json));
            await File.WriteAllTextAsync(SavePath, encrypted);

            Debug.Log("[SaveManager] Save completed successfully.");
            //onSaveEventChannel?.ConfirmSave();
            onSaveCompleteGlobalEvent?.Raise();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save failed: {e.Message}");
        }
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("[SaveManager] No save file found.");
            return;
        }

        try
        {
            string encrypted = File.ReadAllText(SavePath);
            string json = Decrypt(encrypted);
            var loadedData = JsonUtility.FromJson<SaveDataContainer>(json);

            if (loadedData?.SaveDataChunks == null)
            {
                Debug.LogError("[SaveManager] Loaded save data is corrupt.");
                return;
            }

            _currentSaveData = loadedData;

            foreach (var savable in _savables)
            {
                if (savable == null) continue;

                var templateData = savable.GetSaveData();
                if (templateData != null && _currentSaveData.SaveDataChunks.TryGetValue(templateData.SaveId, out string dataJson))
                {
                    var specificData = JsonUtility.FromJson(dataJson, templateData.GetType());
                    if (specificData is ISaveData validData)
                    {
                        savable.LoadFromData(validData);
                    }
                }
            }
            Debug.Log("[SaveManager] Load completed successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Load failed: {e.Message}");
        }
    }

    public void SaveData(ISaveData data)
    {
        if (data == null)
        {
            Debug.LogError("[SaveManager] Attempted to save null data.");
            return;
        }

        _currentSaveData.SaveDataChunks ??= new Dictionary<string, string>();
        string json = JsonUtility.ToJson(data);
        _currentSaveData.SaveDataChunks[data.SaveId] = json;

        _ = SaveAsync(); 
        Debug.Log($"[SaveManager] Manually saved data chunk: {data.SaveId}");
    }

    public bool GetData<T>(out T data) where T : struct, ISaveData
    {
        data = default;

        if (_currentSaveData == null || _currentSaveData.SaveDataChunks == null)
            return false;

        T temp = default;
        string saveId = temp.SaveId;

        if (_currentSaveData.SaveDataChunks.TryGetValue(saveId, out string json))
        {
            data = JsonUtility.FromJson<T>(json);
            return true;
        }
        return false;
    }

    #endregion

    #region Encryption Helpers

    private string Encrypt(string json)
    {
        // TODO: Implement AES encryption for production
        return json; 
    }

    private string Decrypt(string json)
    {
        // TODO: Implement AES decryption for production
        return json;
    }

    #endregion
}
*/
#endregion

#region  Prior Version 7/5/2026
/*
public class SaveManager : MonoBehaviour
{
    #region Singleton & Initialization

    /// <summary>
    /// Static singleton instance for global access.
    /// </summary>
    public static SaveManager Instance { get; private set; }

    private void Awake()
    {
        // Enforce Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Event Channels

    [Header("Events")]
    [SerializeField] private SaveEventChannel onSaveEventChannel;
    [SerializeField] private VoidEventChannel onSaveCompleteGlobalEvent;

    #endregion

    #region Configuration

    [Header("Settings")]
    [Tooltip("The filename used for the save file (stored in persistentDataPath).")]
    [SerializeField] private string saveFileName = "save.save";

    #endregion

    #region Private State

    private SaveDataContainer _currentSaveData = new SaveDataContainer();
    private readonly List<ISavable> _savables = new List<ISavable>();

    /// <summary>
    /// Full path to the save file combining persistent data path and filename.
    /// </summary>
    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    #endregion

    #region Event Subscriptions

    /// <summary>
    /// Subscribes to external save request events.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (onSaveEventChannel != null)
            onSaveEventChannel.OnSaveRequested += HandleSaveRequest;
    }

    /// <summary>
    /// Unsubscribes from events to prevent memory leaks.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (onSaveEventChannel != null)
            onSaveEventChannel.OnSaveRequested -= HandleSaveRequest;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Retrieves the scene name stored in the current save data.
    /// Returns null if no save data exists or the "GameLevel" chunk is missing.
    /// </summary>
    public string CurrentSceneName
    {
        get
        {
            if (_currentSaveData == null || _currentSaveData.SaveDataChunks == null)
                return null;

            if (_currentSaveData.SaveDataChunks.TryGetValue("GameLevel", out string json))
            {
                var levelData = JsonUtility.FromJson<GameLevelManagerSaveData>(json);
                return levelData.CurrentSceneName;
            }

            return null;
        }
    }

    #endregion

    #region Registration System

    /// <summary>
    /// Registers an object to be included in the save process.
    /// </summary>
    /// <param name="savable">The component implementing ISavable.</param>
    public void Register(ISavable savable)
    {
        if (savable != null && !_savables.Contains(savable))
            _savables.Add(savable);
    }

    /// <summary>
    /// Unregisters an object from the save process.
    /// </summary>
    /// <param name="savable">The component to remove.</param>
    public void Unregister(ISavable savable)
    {
        if (savable != null)
            _savables.Remove(savable);
    }

    #endregion

    #region Save Logic

    /// <summary>
    /// Handles incoming save requests from event channels.
    /// </summary>
    private async void HandleSaveRequest()
    {
        Debug.Log("[SaveManager] Save request received. Aggregating data...");
        await SaveAsync();
    }

    /// <summary>
    /// Asynchronously gathers data from all registered savables, serializes, encrypts, and writes to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        // 1. Gather Data (Main Thread)
        _currentSaveData.SaveDataChunks?.Clear();
        if (_currentSaveData.SaveDataChunks == null)
            _currentSaveData.SaveDataChunks = new Dictionary<string, string>();

        foreach (var savable in _savables)
        {
            if (savable == null) continue;

            var data = savable.GetSaveData();
            if (data != null)
            {
                string json = JsonUtility.ToJson(data);
                _currentSaveData.SaveDataChunks[data.SaveId] = json;
            }
        }

        Debug.Log("[SaveManager] Data aggregated. Serializing to disk...");

        try
        {
            // 2. Serialize & Encrypt (Background Thread)
            string json = JsonUtility.ToJson(_currentSaveData);
            string encrypted = await Task.Run(() => Encrypt(json));

            // 3. Write (Async I/O)
            await File.WriteAllTextAsync(SavePath, encrypted);

            Debug.Log("[SaveManager] Save completed successfully.");

            // 4. Notify Listeners
            onSaveEventChannel?.ConfirmSave();
            onSaveCompleteGlobalEvent?.Raise();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save failed: {e.Message}");
        }
    }

    #endregion

    #region Load Logic

    

    /// <summary>
    /// Synchronously loads data from disk, decrypts, and distributes to registered savables.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("[SaveManager] No save file found.");
            return;
        }

        try
        {
            string encrypted = File.ReadAllText(SavePath);
            string json = Decrypt(encrypted);
            var loadedData = JsonUtility.FromJson<SaveDataContainer>(json);

            if (loadedData?.SaveDataChunks == null)
            {
                Debug.LogError("[SaveManager] Loaded save data is corrupt or empty.");
                return;
            }

            _currentSaveData = loadedData;

            // Distribute data to listeners
            foreach (var savable in _savables)
            {
                if (savable == null) continue;

                var templateData = savable.GetSaveData(); // Get a populated or default instance to know the Type
                if (templateData != null && _currentSaveData.SaveDataChunks.TryGetValue(templateData.SaveId, out string dataJson))
                {
                    // Deserialize specifically into the type of the templateData
                    var specificData = JsonUtility.FromJson(dataJson, templateData.GetType());
                    
                    // Cast back to ISaveData to pass to LoadFromData
                    if (specificData is ISaveData validData)
                    {
                        savable.LoadFromData(validData);
                    }
                }
                
            }

            Debug.Log("[SaveManager] Load completed successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Load failed: {e.Message}");
        }
    }

    #endregion

    #region Manual Data Saving

    /// <summary>
    /// Manually saves a specific data chunk immediately.
    /// Updates the internal dictionary and triggers an asynchronous save to disk.
    /// </summary>
    public void SaveData(ISaveData data)
    {
        if (data == null)
        {
            Debug.LogError("[SaveManager] Attempted to save null data.");
            return;
        }

        // 1. Update the in-memory dictionary immediately
        if (_currentSaveData.SaveDataChunks == null)
            _currentSaveData.SaveDataChunks = new Dictionary<string, string>();

        string json = JsonUtility.ToJson(data);
        _currentSaveData.SaveDataChunks[data.SaveId] = json;

        // 2. Trigger an asynchronous save to disk
        // We use _ = to fire-and-forget the task, or you can await it if inside an async method
        _ = SaveAsync(); 
        
        Debug.Log($"[SaveManager] Manually saved data chunk: {data.SaveId}");
    }

    #endregion

    #region Encryption Helpers

    /// <summary>
    /// Encrypts the JSON string before writing to disk.
    /// TODO: Implement robust encryption (e.g., AES) instead of XOR for production.
    /// </summary>
    private string Encrypt(string json)
    {
        // Placeholder for XOR or AES logic
        return json; 
    }

    /// <summary>
    /// Decrypts the string read from disk.
    /// </summary>
    private string Decrypt(string json)
    {
        // Placeholder for XOR or AES logic
        return json;
    }

    /// <summary>
    /// Retrieves a specific save data chunk by type from the currently loaded data.
    /// Returns true if found, false otherwise.
    /// </summary>
    public bool GetData<T>(out T data) where T : struct, ISaveData
    {
        data = default(T);

        if (_currentSaveData == null || _currentSaveData.SaveDataChunks == null)
            return false;

        // Create a temporary instance to get the SaveId key
        // Note: This assumes T has a parameterless constructor or default values are fine for ID retrieval
        T temp = default(T); 
        string saveId = temp.SaveId; 

        if (_currentSaveData.SaveDataChunks.TryGetValue(saveId, out string json))
        {
            data = JsonUtility.FromJson<T>(json);
            return true;
        }

        return false;
    }

    #endregion
}

/// <summary>
/// Helper wrapper for dictionary deserialization if needed.
/// Ensures compatibility with the ISaveData interface during load operations.
/// </summary>
[Serializable]
public class SaveDataWrapper : ISaveData
{
    public string SaveId => "Wrapper";
    // Add fields as needed or use a generic dictionary approach depending on implementation
}   

*/
#endregion

#region Prior version 6/28/2026
/*
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayerStuff;

/// <summary>
/// A centralized, static system for managing game persistence.
/// </summary>
/// <remarks>
/// <para><strong>Key Features:</strong></para>
/// <list type="bullet">
/// <item><description>Asynchronous I/O to prevent frame hitches.</description></item>
/// <item><description>Background thread serialization/encryption to avoid main-thread CPU spikes.</description></item>
/// <item><description>Automatic backup management for data integrity.</description></item>
/// </list>
/// <para><strong>Usage:</strong> Register <see cref="ISavable"/> components to include them in the save data.</para>
/// </remarks>
public static class SaveSystem
{
    #region Configuration & State

    [Header("Event Channels")]
    [SerializeField] private static VoidEventSO onSaveGameCompleteEvent;

    private static SaveData _saveData = new SaveData();
    private static readonly List<ISavable> savables = new List<ISavable>();

    // File paths stored in persistent data directory
    private static readonly string SaveFilePath = Path.Combine(Application.persistentDataPath, "save.save");
    private static readonly string BackupFilePath = SaveFilePath + ".bak";

    // Simple XOR key for obfuscation (Not secure encryption, prevents casual editing)
    private const char EncryptionKey = 'K';

    /// <summary>
    /// Global access to the current save data container.
    /// </summary>
    public static SaveData Data => _saveData;

    #endregion

    #region Registration

    /// <summary>
    /// Registers an <see cref="ISavable"/> component to be included in save/load operations.
    /// </summary>
    /// <param name="savable">The component implementing ISavable.</param>
    public static void Register(ISavable savable)
    {
        if (!savables.Contains(savable))
            savables.Add(savable);
    }

    /// <summary>
    /// Unregisters an <see cref="ISavable"/> component.
    /// </summary>
    /// <param name="savable">The component to remove.</param>
    public static void Unregister(ISavable savable) => savables.Remove(savable);

    #endregion

    #region Save Logic

    /// <summary>
    /// Saves the current game state to disk asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    /// <remarks>
    /// Serialization and encryption are offloaded to a background thread using <see cref="Task.Run"/>
    /// to ensure the main Unity thread remains unblocked.
    /// </remarks>
    public static async Task Save()
    {
        // 1. Gather data from all registered components (Must happen on main thread if accessing Unity objects)
        HandleSaveData();
        
        // Capture data locally to ensure thread safety during background processing
        var dataToSave = _saveData;

        Debug.Log($"[SaveSystem] Preparing to save scene: '{dataToSave.CurrentSceneName}'");

        try
        {
            // 2. Offload CPU-bound work (Serialization + Encryption) to background thread
            string encryptedJson = await Task.Run(() =>
            {
                string json = JsonUtility.ToJson(dataToSave);
                return Encrypt(json);
            });

            // 3. Create Backup (Synchronous but fast for small files)
            if (File.Exists(SaveFilePath))
            {
                File.Copy(SaveFilePath, BackupFilePath, true);
            }

            // 4. Non-blocking disk write
            await File.WriteAllTextAsync(SaveFilePath, encryptedJson);
            
            Debug.Log($"✅ GAME SAVED: {dataToSave.CurrentSceneName}");
            
            // 5. Notify listeners
            onSaveGameCompleteEvent?.Raise();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
            AttemptRestoreBackup();
            throw; // Re-throw to allow upstream handling if needed
        }
    }

    #endregion

    #region Load Logic

    /// <summary>
    /// Loads the game state from disk synchronously.
    /// </summary>
    /// <remarks>
    /// Attempts to load the primary save file first, then falls back to the backup if corruption is detected.
    /// </remarks>
    public static void Load()
    {
        string[] pathsToTry = { SaveFilePath, BackupFilePath };

        foreach (string path in pathsToTry)
        {
            if (!File.Exists(path)) continue;

            try
            {
                string encryptedJson = File.ReadAllText(path);
                string json = Decrypt(encryptedJson);

                if (string.IsNullOrEmpty(json)) 
                    throw new System.Exception("Empty save data.");

                _saveData = JsonUtility.FromJson<SaveData>(json);
                
                // Validate data integrity
                if (_saveData.Equals(default(SaveData))) 
                    continue;

                SanitizeData();
                HandleLoadData();
                
                Debug.Log($"Game loaded successfully from: {path}");
                return; // Exit on success
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Load failed from {path}: {e.Message}");
                // Continue to next path (backup)
            }
        }
        Debug.LogWarning("No valid save file found. Using default data.");
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Collects data from all registered <see cref="ISavable"/> components.
    /// </summary>
    private static void HandleSaveData()
    {
        foreach (var savable in savables) 
            savable?.SaveTo(_saveData);
    }

    /// <summary>
    /// Distributes loaded data to all registered <see cref="ISavable"/> components.
    /// </summary>
    private static void HandleLoadData()
    {
        foreach (var savable in savables) 
            savable?.LoadFrom(_saveData);
    }

    /// <summary>
    /// Ensures data integrity by initializing null arrays/lists to empty defaults.
    /// </summary>
    private static void SanitizeData()
    {
        if (_saveData.AchievementData.unlockedAchievements == null)
            _saveData.AchievementData.unlockedAchievements = System.Array.Empty<string>();
    }

    /// <summary>
    /// Attempts to restore the backup file if the primary save is corrupted.
    /// </summary>
    private static void AttemptRestoreBackup()
    {
        if (File.Exists(BackupFilePath))
        {
            FileInfo info = new FileInfo(BackupFilePath);
            // Validate backup size to avoid restoring empty/garbage files
            if (info.Length > 10)
            {
                File.Copy(BackupFilePath, SaveFilePath, true);
                Debug.LogWarning("Save corrupted. Successfully restored from backup.");
                return;
            }
        }
        Debug.LogError("Backup restore failed or missing.");
    }

    /// <summary>
    /// Simple XOR encryption with index mixing to prevent pattern analysis.
    /// </summary>
    private static string Encrypt(string json)
    {
        char[] chars = json.ToCharArray();
        for (int i = 0; i < chars.Length; i++) 
            chars[i] = (char)(chars[i] ^ EncryptionKey ^ (char)i);
        return new string(chars);
    }

    /// <summary>
    /// Decrypts the XOR encrypted string (symmetric operation).
    /// </summary>
    private static string Decrypt(string json)
    {
        char[] chars = json.ToCharArray();
        for (int i = 0; i < chars.Length; i++) 
            chars[i] = (char)(chars[i] ^ EncryptionKey ^ (char)i);
        return new string(chars);
    }

    #endregion
}

#region Data Structures

[System.Serializable]
public struct AchievementSaveData 
{ 
    public string[] unlockedAchievements; 
}

[System.Serializable]
public struct SaveData
{
    public string CurrentSceneName;
    public PlayerSaveData PlayerData;
    public PlayerUpgradesSaveData PlayerUpgradesSaveData;
    public GameLevelManagerSaveData GamelevelManagerData;
    public AchievementSaveData AchievementData;
}

#endregion

#region Extensions

/// <summary>
/// Extension methods for <see cref="Task"/> to handle fire-and-forget scenarios safely.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Executes a task asynchronously and logs any exceptions without blocking.
    /// </summary>
    public static async void Forget(this Task task)
    {
        try 
        { 
            await task; 
        }
        catch (System.Exception e) 
        { 
            Debug.LogError($"Background task failed: {e.Message}"); 
        }
    }
}

#endregion   
*/
#endregion

#region Prior version 6/17/2026
/*
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using PlayerStuff;
using System.Threading.Tasks;

/// <summary>
/// A centralized system for saving and loading game data.
/// Uses a static class pattern for global access and manages a list of ISavable components.
/// Data is serialized to JSON, encrypted, and saved asynchronously to prevent frame hitches.
/// </summary>
public static class SaveSystem
{
    [Header("Event Channels")]
    [SerializeField] private static VoidEventSO onSaveGameCompleteEvent;
    private static SaveData _saveData = new SaveData();
    private static readonly string SaveFilePath = Path.Combine(Application.persistentDataPath, "save.save");
    private static readonly string BackupFilePath = SaveFilePath + ".bak";

    // Encryption Key (Should ideally be stored securely or obfuscated further in a real release)
    private const char EncryptionKey = 'K';

    /// <summary>
    /// The central data container for the entire game.
    /// </summary>
    public static SaveData Data => _saveData;

    private static readonly List<ISavable> savables = new List<ISavable>();

    /// <summary>
    /// Registers an ISavable component so it can participate in save/load operations.
    /// </summary>
    public static void Register(ISavable savable)
    {
        if (!savables.Contains(savable))
            savables.Add(savable);
    }

    /// <summary>
    /// Unregisters an ISavable component.
    /// </summary>
    public static void Unregister(ISavable savable) => savables.Remove(savable);

    /// <summary>
    /// Saves the current game state to disk asynchronously.
    /// Creates a backup before overwriting to prevent data corruption.
    /// FIX 1: Changed from 'async void' to 'async Task' to allow proper error propagation and awaiting.
    /// </summary>
    // In SaveSystem.cs
    public static async System.Threading.Tasks.Task Save()
    {
        // 1. Gather data (Must be on main thread if accessing Unity objects)
        HandleSaveData();
        
        // Capture data locally to avoid threading issues
        var dataToSave = _saveData; 

        // 2. Offload CPU-heavy work to a background thread
        string encryptedJson = await System.Threading.Tasks.Task.Run(() =>
        {
            string json = JsonUtility.ToJson(dataToSave);
            return Encrypt(json);
        });

        try
        {
            if (File.Exists(SaveFilePath))
                File.Copy(SaveFilePath, BackupFilePath, true);

            // 3. Write to disk (Already non-blocking)
            await File.WriteAllTextAsync(SaveFilePath, encryptedJson);
            
            Debug.Log($"✅ GAME SAVED TO DISK: {dataToSave.CurrentSceneName}");
            onSaveGameCompleteEvent?.Raise();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
            AttemptRestoreBackup();
            throw;
        }
    }  

    /// <summary>
    /// Loads the game state from disk.
    /// Handles decryption, deserialization, and distributes data to components.
    /// FIX 2: Replaced recursive Load() call with an iterative approach to prevent stack overflow.
    /// </summary>
   public static void Load()
{
    string[] pathsToTry = { SaveFilePath, BackupFilePath };

    foreach (string path in pathsToTry)
    {
        if (!File.Exists(path))
            continue;

        try
        {
            string encryptedJson = File.ReadAllText(path);
            string json = Decrypt(encryptedJson);

            if (string.IsNullOrEmpty(json) || json.Trim() == "")
                throw new System.Exception("Save file is empty or corrupted.");

            _saveData = JsonUtility.FromJson<SaveData>(json);
            
            // NEW: Explicitly check if deserialization resulted in a default struct
            // This catches cases where JSON was "{}" or invalid but didn't throw an exception
            if (_saveData.Equals(default(SaveData)))
            {
                Debug.LogWarning($"Save file at {path} contained no valid data (default struct).");
                continue; // Try the next file (backup)
            }

            SanitizeData();
            HandleLoadData();

            Debug.Log($"Game loaded successfully from: {path}");
            return; 
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load from {path}: {e.Message}");
            // Continue to backup
        }
    }

    Debug.LogWarning("No valid save file found. Starting new game with default data.");
    // Optional: Initialize _saveData to a known good default here if needed
    // _saveData = new SaveData { ... }; 
}   

    /// <summary>
    /// Collects data from all registered ISavable components.
    /// </summary>
    private static void HandleSaveData()
    {
        foreach (var savable in savables)
        {
            // Null-conditional operator prevents crashes if a savable was destroyed but not unregistered
            savable?.SaveTo(_saveData);
        }
    }

    /// <summary>
    /// Distributes data to all registered ISavable components.
    /// </summary>
    private static void HandleLoadData()
    {
        foreach (var savable in savables)
        {
            savable?.LoadFrom(_saveData);
        }
    }

    /// <summary>
    /// FIX 5: Sanitizes data after loading to prevent null reference exceptions.
    /// Unity's JsonUtility does not initialize arrays inside structs if they are missing from JSON.
    /// </summary>
    private static void SanitizeData()
    {
        // Ensure arrays are never null, even if missing from the save file
        if (_saveData.AchievementData.unlockedAchievements == null)
            _saveData.AchievementData.unlockedAchievements = System.Array.Empty<string>();

        // Add similar checks here for other structs containing lists/arrays if needed
        // Example: if (_saveData.PlayerData.Inventory == null) _saveData.PlayerData.Inventory = new Item[0];
    }

    /// <summary>
    /// Attempts to restore the backup file if the main save fails.
    /// FIX 3: Added validation and error handling to prevent restoring a corrupted backup.
    /// </summary>
    private static void AttemptRestoreBackup()
    {
        if (File.Exists(BackupFilePath))
        {
            try
            {
                // Validate backup size before restoring (prevent restoring empty/garbage files)
                FileInfo backupInfo = new FileInfo(BackupFilePath);
                if (backupInfo.Length < 10) // Arbitrary minimum size for a valid JSON save
                {
                    Debug.LogError("Backup file is too small to be valid. Discarding backup.");
                    return;
                }

                File.Copy(BackupFilePath, SaveFilePath, true);
                Debug.LogWarning("Save corrupted. Successfully restored from backup.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to restore backup: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("Save failed and no backup file exists!");
        }
    }

    #region Encryption Helpers

    /// <summary>
    /// Simple XOR encryption to prevent casual editing of save files.
    /// FIX 4: Improved encryption by mixing the key with the index to break repeating patterns.
    /// </summary>
    private static string Encrypt(string json)
    {
        char[] chars = json.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            // XOR with Key AND the index position. 
            // This prevents simple frequency analysis attacks that work on standard single-key XOR.
            chars[i] = (char)(chars[i] ^ EncryptionKey ^ (char)i);
        }
        return new string(chars);
    }

    /// <summary>
    /// Decrypts the XOR encrypted string.
    /// Must use the exact same logic as Encrypt (XOR is symmetric).
    /// </summary>
    private static string Decrypt(string encryptedJson)
    {
        char[] chars = encryptedJson.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            // Reverse the operation: XOR with Key AND the index position
            chars[i] = (char)(chars[i] ^ EncryptionKey ^ (char)i);
        }
        return new string(chars);
    }

    #endregion
}


/// <summary>
/// Serializable data container for achievement-related data.
/// </summary>
[System.Serializable]
public struct AchievementSaveData
{
    public string[] unlockedAchievements;
}

/// <summary>
/// Serializable data container for the entire game state.
/// Contains sub-structs for different game systems.
/// Note: Ensure all nested structs (PlayerSaveData, etc.) are also [Serializable] and use public fields.
/// </summary>
[System.Serializable]
public struct SaveData
{
    // Store the active scene name
    public string CurrentSceneName;

    public PlayerSaveData PlayerData;
    public PlayerUpgradesSaveData PlayerUpgradesSaveData;
    public GameLevelManagerSaveData GamelevelManagerData;
    public AchievementSaveData AchievementData;
}   

public static class TaskExtensions
{
    /// <summary>
    /// Safely fires and forgets a Task, logging any exceptions to prevent silent crashes.
    /// Usage: SaveSystem.Save().Forget();
    /// </summary>
    public static async void Forget(this Task task)
    {
        try
        {
            await task;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Background save failed: {e.Message}");
        }
    }
}
*/
#endregion