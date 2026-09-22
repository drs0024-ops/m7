using System;
using System.Collections.Generic;
using Game.Core.Data;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Achievements
{
    public class AchievementManager : IStartable, ITickable, IDisposable, ISaveable
    {
        private IPublisher<SaveRequest> _saveRequestPublisher;
        private IPublisher<AchievementUnlocked> _achievementUnlockedPublisher;

        private readonly AchievementConfigSO _config;
        private readonly ISaveableRegistry _registry;
        private readonly List<string> _unlockedAchievements = new();
        private readonly List<IDisposable> _subscriptions = new();

        private bool _isSavePending;
        private bool _disposed;
        private float _saveTimer;
        private const float SaveDelay = 1.0f;

        [Inject]
        public AchievementManager(AchievementConfigSO config, ISaveableRegistry registry)
        {
            _config = config;
            _registry = registry;
        }

        void IStartable.Start()
        {
            _registry.Register(this);

            _saveRequestPublisher = GlobalMessagePipe.GetPublisher<SaveRequest>();
            _achievementUnlockedPublisher = GlobalMessagePipe.GetPublisher<AchievementUnlocked>();

            if (_config.achievementList == null)
            {
                Debug.LogError("[AchievementManager] AchievementList is missing in Config SO!");
                return;
            }
        }

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

        #region Public API

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

        public string SaveId => throw new NotImplementedException();

        #endregion

        #region ISaveable

        public ISaveData GetSaveData() => new AchievementSaveData
        {
            SaveId = "Achievement",
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
            if (_saveRequestPublisher != null)
                _saveRequestPublisher.Publish(new SaveRequest("AchievementManager", false));
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 1. Unregister first — prevents SaveManager from calling GetSaveData() on a half-destroyed object
            _registry.Unregister(this);

            // 2. Flush pending save (guard against exceptions during teardown)
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

            // 3. Dispose subscriptions (empty now, future-proof)
            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}


/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the unlocking and persistence of game achievements.
/// 
/// ARCHITECTURE CHANGES:
/// - Decoupled: Removed static SaveSystem dependency. Uses SaveEventChannel.
/// - Modular Save: Implements new ISavable (GetSaveData/LoadFromData) with AchievementSaveData struct.
/// - Robust Lifecycle: Handles save debouncing and application quit safely.
/// - Event-Driven: Relies on external systems (e.g., ObjectivesManager) to raise unlock events, 
///   but also provides a direct API for code-based unlocks.
/// </summary>
public class AchievementManager : MonoBehaviour, IAchievementManager
{
    #region Inspector Fields

    [Header("Configuration")]
    [Tooltip("Database of all possible achievements.")]
    [SerializeField] private AchievementList achievementList;

    [Header("Event Channels")]
    [Tooltip("Raised when an achievement is unlocked (for UI/Audio).")]
    [SerializeField] private AchievementEventSO onAchievementUnlockedEvent;

    [Tooltip("Triggers the SaveManager to save the game.")]
    [SerializeField] private SaveEventChannel onSaveRequestEvent;

    #endregion

    #region Private Fields

    private List<string> _unlockedAchievements = new List<string>();
    private bool _isSavePending = false;
    private ServiceLocator _serviceLocator;

    #endregion

    #region Public API (Read-Only)

    public IReadOnlyList<string> UnlockedAchievements => _unlockedAchievements.AsReadOnly();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (achievementList == null)
        {
            Debug.LogError("[AchievementManager] AchievementList is missing! Cannot initialize.");
            enabled = false;
            return;
        }

        // Do NOT access ServiceLocator here if you can avoid it.
        // If you must, use the static property directly, not a cached variable.
        Debug.Log($"[AchievementManager] Awake() - Time: {Time.realtimeSinceStartup}");
    }

    private void Start()
    {
        // Access Static Property Directly (Triggers Lazy Load if needed)
        _serviceLocator = ServiceLocator.Instance;

        // Single Null Check
        if (_serviceLocator == null)
        {
            Debug.LogError("[AchievementManager] ServiceLocator instance not found!", this);
            enabled = false;
            return;
        }

        // Register Self
        _serviceLocator.Register<IAchievementManager>(this);
        _serviceLocator.Register<AchievementManager>(this);
        
        Debug.Log("[AchievementManager] Successfully initialized.");
    }

    private void OnDestroy()
    {
        if (_serviceLocator != null)
        {
            _serviceLocator.Unregister<AchievementManager>();
            _serviceLocator.Unregister<IAchievementManager>();
        }
    }

    private void OnApplicationQuit()
    {
        if (_isSavePending)
        {
            StopAllCoroutines();
            ForceSave();
        }
    }

    #endregion

    #region IAchievementManager Implementation

    public void UnlockAchievement(string achievementId)
    {
        if (_unlockedAchievements.Contains(achievementId))
            return;

        Achievement achievement = achievementList.GetAchievement(achievementId);
        if (achievement == null)
        {
            Debug.LogWarning($"[AchievementManager] Attempted to unlock unknown ID: {achievementId}");
            return;
        }

        // 1. Update Data
        _unlockedAchievements.Add(achievementId);
        Debug.Log($"[AchievementManager] Unlocked: {achievement.name}");

        // 2. Raise Event for UI/Audio
        onAchievementUnlockedEvent?.Raise(achievement);

        // 3. Request Save (Debounced)
        RequestSave();
    }

    public bool IsAchievementUnlocked(string achievementId)
    {
        return _unlockedAchievements.Contains(achievementId);
    }

    public Achievement GetAchievement(string id) => achievementList.GetAchievement(id);

    #endregion

    #region ISaveable Implementation

    public string SaveId => "Achievements";

    public ISaveData GetSaveData()
    {
        return new AchievementSaveData
        {
            UnlockedAchievementIDs = _unlockedAchievements.ToArray()
        };
    }

    public void LoadFromData(ISaveData data)
    {
        if (data is not AchievementSaveData achievementData)
        {
            Debug.LogError("[AchievementManager] Received invalid data type for loading.");
            return;
        }

        _unlockedAchievements.Clear();

        if (achievementData.UnlockedAchievementIDs != null)
        {
            foreach (string id in achievementData.UnlockedAchievementIDs)
            {
                // Validate ID exists in achievementList before adding
                if (achievementList.GetAchievement(id) != null)
                {
                    _unlockedAchievements.Add(id);
                }
                else
                {
                    Debug.LogWarning($"[AchievementManager] Saved achievement '{id}' not found in list.");
                }
            }
        }

        Debug.Log($"[AchievementManager] Loaded {_unlockedAchievements.Count} achievements.");
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Debounces save requests to prevent disk spam when multiple achievements unlock rapidly.
    /// </summary>
    private void RequestSave()
    {
        if (_isSavePending) return;

        _isSavePending = true;
        StartCoroutine(SaveAfterDelay());
    }

    private IEnumerator SaveAfterDelay()
    {
        // Wait 1 second to batch multiple unlocks
        yield return new WaitForSeconds(1.0f);
        
        ForceSave();
        _isSavePending = false;
    }

    private void ForceSave()
    {
        if (onSaveRequestEvent != null)
        {
            onSaveRequestEvent.RequestSave();
            Debug.Log("[AchievementManager] Save requested via Event Channel.");
        }
        else
        {
            Debug.LogWarning("[AchievementManager] SaveEventChannel missing! Achievements will NOT be saved.");
        }
    }

    #endregion
}

/// <summary>
/// Modular save data chunk for Achievements.
/// </summary>
[System.Serializable]
public struct AchievementSaveData : ISaveData
{
    public string SaveId => "Achievements";
    public string[] UnlockedAchievementIDs;
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the unlocking, persistence, and visual notification of game achievements.
/// Implements ISavable to automatically integrate with the SaveSystem.
/// Uses a Singleton pattern to ensure global access and single-point save registration.
/// </summary>
public class AchievementManager : MonoBehaviour, ISavable
{
    #region Singleton & Lifecycle

    /// <summary>
    /// Static accessor for global access to the AchievementManager.
    /// </summary>
    public static AchievementManager Instance { get; private set; }

    private void Awake()
    {
        // Strict Singleton Enforcement
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[AchievementManager] Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Register with SaveSystem for automatic persistence
        SaveSystem.Register(this);
    }
    
    private void OnDestroy() 
    {
        if (Instance == this)
        {
            SaveSystem.Unregister(this);
            Instance = null;
        }
    }
    
    private void Start()
    {
        // REMOVE LoadAchievements() if SaveSystem automatically calls LoadFrom()
        // Only keep this if you have specific non-save initialization logic
        //LoadAchievements();

    }

    private void OnEnable()
    {
        // Subscribe to external unlock events (if any other system triggers achievements)
        onAchievementUnlockedEvent?.RegisterListener(OnAchievementUnlocked);

        // Initialize Notification UI state
        if (notificationPanel.canvasGroup != null)
        {
            notificationPanel.canvasGroup.alpha = notificationPanel.minAlpha;
            notificationPanel.canvasGroup.blocksRaycasts = false;
            notificationPanel.canvasGroup.interactable = false;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        onAchievementUnlockedEvent?.UnregisterListener(OnAchievementUnlocked);
    }

    #endregion

    #region Serialized Fields

    [Header("Configuration")]
    [Tooltip("Database of all possible achievements.")]
    [SerializeField] private AchievementList achievementList;
    
    [Tooltip("Event raised when an achievement is unlocked.")]
    [SerializeField] private AchievementEventSO onAchievementUnlockedEvent;
    [SerializeField] private VoidEventSO savegameEvent;

    [Header("Notification UI")]
    [Tooltip("The notification panel configuration (fade settings, canvas group).")]
    [SerializeField] private HUDPanel notificationPanel;
    
    [Tooltip("Text element for the achievement title.")]
    [SerializeField] private TextMeshProUGUI achievementNameText;
    
    [Tooltip("Text element for the achievement description.")]
    [SerializeField] private TextMeshProUGUI achievementDescriptionText;
    
    [Tooltip("Image element for the achievement icon.")]
    [SerializeField] private Image achievementIconImage;

    [Header("Behavior Settings")]
    [Tooltip("Duration in seconds to show the notification before fading out.")]
    [SerializeField] private float displayTime = 3f;

    #endregion

    #region Private Variables

    /// <summary>
    /// List of unique IDs for unlocked achievements.
    /// </summary>
    private List<string> unlockedAchievements = new List<string>();
    private bool isSavePending = false;

    #endregion

    #region Public Methods

    /// <summary>
    /// Attempts to unlock an achievement by its unique ID.
    /// Handles validation, event raising, UI notification, and saving.
    /// </summary>
    /// <param name="achievementID">The unique identifier of the achievement to unlock.</param>
    public void UnlockAchievement(string achievementID)
    {
        if (unlockedAchievements.Contains(achievementID)) return;
        
        Achievement achievement = achievementList.GetAchievement(achievementID);
        if (achievement == null) return;

        // 1. Update Data
        unlockedAchievements.Add(achievementID);
        
        // 2. Show UI Immediately (User feedback is priority)
        ShowNotification(achievement);

        // 3. Raise Event for OTHER systems (Audio, Analytics)
        // Do NOT rely on the event to save, do it here but debounced.
        onAchievementUnlockedEvent?.Raise(achievement);
        
        RequestSave(); // Use the debounced save
    }   

    /// <summary>
    /// Checks if a specific achievement has been unlocked.
    /// </summary>
    /// <param name="achievementID">The unique identifier of the achievement.</param>
    /// <returns>True if unlocked, false otherwise.</returns>
    public bool IsAchievementUnlocked(string achievementID)
    {
        return unlockedAchievements.Contains(achievementID);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Internal listener to ensure data consistency if the event is raised externally.
    /// </summary>
    /// <param name="achievement">The unlocked achievement data.</param>
    private void OnAchievementUnlocked(Achievement achievement)
    {
        if (!unlockedAchievements.Contains(achievement.AchievementID))
        {
            unlockedAchievements.Add(achievement.AchievementID);
            
            if (GameManager2.Instance != null)
            {
                GameManager2.Instance.SaveGame();
            }
        }
    }

    #endregion

    #region UI Notification Logic

    /// <summary>
    /// Displays the achievement notification panel with a fade-in animation.
    /// </summary>
    /// <param name="achievement">The data to display.</param>
    private void ShowNotification(Achievement achievement)
    {
        if (notificationPanel.canvasGroup == null)
        {
            Debug.LogWarning("[AchievementManager] Notification Panel CanvasGroup is missing.");
            return;
        }

        // Cancel any existing tween to prevent conflicts
        LeanTween.cancel(notificationPanel.canvasGroup.gameObject, false, TweenAction.CANVASGROUP_ALPHA);
        
        // Populate UI Elements
        if (achievementNameText) achievementNameText.text = achievement.Title;
        if (achievementDescriptionText) achievementDescriptionText.text = achievement.Description;
        if (achievementIconImage) achievementIconImage.sprite = achievement.Icon;

        // Fade In using panel-specific settings
        LeanTween.alphaCanvas(notificationPanel.canvasGroup, notificationPanel.maxAlpha, notificationPanel.fadeDuration)
            .setEase(notificationPanel.easeType)
            .setOnStart(() => {
                notificationPanel.canvasGroup.blocksRaycasts = true;
                notificationPanel.canvasGroup.interactable = true;
            })
            .setOnComplete(() => {
                // Schedule fade out after display time
                Invoke(nameof(HideNotification), displayTime);
            });
    }

    /// <summary>
    /// Fades out the notification panel and disables interaction.
    /// </summary>
    private void HideNotification()
    {
        if (notificationPanel.canvasGroup == null) return;

        LeanTween.cancel(notificationPanel.canvasGroup.gameObject, false, TweenAction.CANVASGROUP_ALPHA);
        
        LeanTween.alphaCanvas(notificationPanel.canvasGroup, notificationPanel.minAlpha, notificationPanel.fadeDuration)
            .setEase(notificationPanel.easeType)
            .setOnComplete(() => {
                notificationPanel.canvasGroup.blocksRaycasts = false;
                notificationPanel.canvasGroup.interactable = false;
            });
    }

    #endregion

    #region 

    private void RequestSave()
    {
        if (isSavePending) return; // Already waiting to save
        
        isSavePending = true;
        
        // Wait until end of frame or 1 second to batch multiple achievements
        StartCoroutine(SaveAfterDelay());
    }

    private System.Collections.IEnumerator SaveAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);
        
        // Optional: Add a null check if the event isn't assigned in the Inspector
        savegameEvent?.Raise(); 
        
        isSavePending = false;
    }

    /// <summary>
    /// Loads unlocked achievements from the SaveSystem.
    /// Called automatically by SaveSystem on game start.
    /// </summary>
    private void LoadAchievements()
    {
        if (SaveSystem.Data.AchievementData.unlockedAchievements != null)
        {
            unlockedAchievements = new List<string>(SaveSystem.Data.AchievementData.unlockedAchievements);
        }
    }

    /// <summary>
    /// Saves the current list of unlocked achievements to the SaveData object.
    /// </summary>
    /// <param name="saveData">The save data container to write to.</param>
    public void SaveTo(SaveData saveData)
    {
        saveData.AchievementData.unlockedAchievements = unlockedAchievements.ToArray();
    }

    /// <summary>
    /// Loads the list of unlocked achievements from the SaveData object.
    /// </summary>
    /// <param name="saveData">The save data container to read from.</param>
    public void LoadFrom(SaveData saveData)
    {
        if (saveData.AchievementData.unlockedAchievements != null)
        {
            unlockedAchievements = new List<string>(saveData.AchievementData.unlockedAchievements);
        }
        else
        {
            // Ensure list is initialized even if no data exists (prevents NullRef later)
            unlockedAchievements = new List<string>();
        }
    }

    private void OnApplicationQuit()
    {
        if (isSavePending)
        {
            StopAllCoroutines(); // Stop the delay
            savegameEvent?.Raise(); // Force save immediately
        }
    }

    #endregion


}   
*/