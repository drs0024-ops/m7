using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using Game.Core.Enums;
using System;
using Game.Core;

namespace Game.Bootstrap
{
    public class SceneTransitionDirector
    {
        private readonly SceneTransitionOrchestrator _orchestrator;

        [Inject]
        public SceneTransitionDirector(SceneTransitionOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public async UniTask StartTransition(
            SceneField scene,
            Action onComplete,
            bool spawnAtDoor,
            DoorToSpawnAt door,
            bool fromRight)
        {
            if (scene == null || !scene.IsValid())
            {
                onComplete?.Invoke();
                return;
            }

            try
            {
                await _orchestrator.TransitionAsync(
                    scene.SceneName, onComplete, spawnAtDoor, door, fromRight);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransitionDirector] Failed: {e.Message}");
                onComplete?.Invoke();
            }
        }
    }
}    

/*
/// <summary>
/// Manages ALL scene operations: Heavy transitions (with fades/spawns) and Lightweight Overlays (Options).
/// Refactored for VContainer + SignalBus architecture.
/// </summary>
public class SceneTransitionDirector : MonoBehaviour, IStartable, IDisposable
{
    #region Dependencies

    private readonly SignalBus _signalBus;
 

    // Optional: Inject other managers if you want to remove SerializedField dependencies
    // private readonly IPlayerSpawnerManager _playerSpawner;
    // private readonly ISceneFadeManager _fadeManager;

    [Inject]
    public SceneTransitionDirector(SignalBus signalBus)
    {
        _signalBus = signalBus;

    }

    #endregion

    #region Configuration (Serialized Fields)

    [Header("Dependencies (Scene References)")]
    [Tooltip("Reference to the Fade Manager component in the scene.")]
    [SerializeField] private SceneFadeManager fadeManager; // Renamed from ScenFadeManager

    [Header("Settings")]
    [Tooltip("Name of the persistent scene that should NEVER be unloaded.")]
    [SerializeField] private string persistentSceneName = "PersistentManagers";

    [Header("Scene References")]
    [SerializeField] private SceneField mainMenuScene;

    #endregion

    #region Private State

    private Scene _previousSceneBeforeOptions;
    private bool _isTransitioning = false;

    #endregion

    #region Lifecycle (IStartable & IDisposable)

    private void Awake()
    {
        // VContainer ensures this component is unique if registered correctly.
        // No Singleton pattern needed.
        Debug.Log("[SceneTransitionDirector] Awake (VContainer Managed).");
    }

    public void Start()
    {
        Debug.Log("[SceneTransitionDirector] Started via VContainer.");
        // Subscribe to signals if needed (e.g., RequestTransitionSignal)
        // _signalBus.Subscribe<RequestTransitionSignal>(OnTransitionRequested);
    }

    public void Dispose()
    {
        // Unsubscribe from signals
        // _signalBus.Unsubscribe<RequestTransitionSignal>(OnTransitionRequested);
        Debug.Log("[SceneTransitionDirector] Disposed.");
    }

    #endregion

    #region Public API: Heavy Transitions

    /// <summary>
    /// Public API: Accepts SceneField for Inspector safety.
    /// Passes the object directly to the coroutine.
    /// </summary>
    public void StartTransition(SceneField scene, Action onComplete, bool spawnAtDoor, DoorTriggerInteraction.DoorToSpawnAt door, bool fromRight)
    {
        // 1. Validate Input
        if (scene == null || !scene.IsValid())
        {
            Debug.LogError("[TransitionDirector] Invalid SceneField provided. Aborting transition.");
            onComplete?.Invoke();
            return;
        }

        // 2. Ensure Active
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);

        // 3. Start Coroutine with SceneField Object
        // No need to extract string manually; the coroutine handles it.
        StartCoroutine(TransitionCoroutine(scene, onComplete, spawnAtDoor, door, fromRight));
    }

        #region Legacy for String delete if no longer using
        /*

    /// <summary>
    /// Internal Logic: Operates purely on strings. 
    /// Spawning is now decoupled via SignalBus.
    /// </summary>
    private IEnumerator TransitionCoroutine(string sceneName, Action onComplete, bool spawnAtDoor, DoorTriggerInteraction.DoorToSpawnAt door, bool fromRight)
    {
        _isTransitioning = true;

        // 1. Fade Out
        if (fadeManager != null)
        {
            fadeManager.StartFadeOut();
            while (fadeManager.IsFadingOut) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(0.5f); // Fallback if no fade manager
        }

        // 2. Load Scene
        // Note: For additive loads (recommended for transitions), use LoadSceneMode.Additive
        // and manually set the scene active. For single mode, this works as is.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f) yield return null;

        // Activate
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) yield return null;

        // 3. Set Active Scene (Critical for Additive loads)
        Scene newScene = SceneManager.GetSceneByName(sceneName);
        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
            // Fix EventSystem if needed here
        }

        // 4. 🟢 FIRE SIGNAL (Replaces direct spawn logic)
        // The PlayerSpawnerService in the NEW scene will handle the rest.
        if (spawnAtDoor)
        {
            _signalBus.Fire(new PlayerSpawnRequest
            {
                SpawnAtDoor = true,
                Door = door,
                FromRight = fromRight
            });
        }
        else
        {
            // Optional: Fire a default spawn request if you always want a player on transition
            // Or omit this block if some transitions shouldn't spawn a player.
            _signalBus.Fire(new PlayerSpawnRequest
            {
                SpawnAtDoor = false,
                ForceRespawn = false
            });
        }

        // 5. Fade In
        if (fadeManager != null)
        {
            fadeManager.StartFadeIn();
            while (fadeManager.IsFadingIn) yield return null;
        }

        // 6. Completion
        onComplete?.Invoke();
        
        // 7. Cleanup Old Scenes (Optional: depends on your unloading strategy)
        // UnloadOldScenes(sceneName); 

        _isTransitioning = false;
    }
       
        #endregion
    #endregion

    #region Public API: Options Overlay

    public IEnumerator LoadOptionsMenuOverlayAsync(SceneField optionsMenu, SceneField persistentScene)
    {
        _previousSceneBeforeOptions = SceneManager.GetActiveScene();
        Debug.Log($"[TransitionDirector] Options opened from: {_previousSceneBeforeOptions.name}");

        Scene scene = SceneManager.GetSceneByName(optionsMenu.SceneName);
        
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.Log($"[TransitionDirector] Loading Options Menu: {optionsMenu.SceneName}");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(optionsMenu.SceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone) yield return null;
        }
        else
        {
            Debug.LogWarning($"[TransitionDirector] Options scene '{optionsMenu.SceneName}' is already loaded.");
        }

        Debug.Log($"[TransitionDirector] Forcing Active Scene and EventSystem fix for Options.");
        //SceneLoaderService.SetActiveAndFixEventSystem(optionsMenu.SceneName, persistentScene.SceneName);

    }

    public IEnumerator CloseOptionsMenuAsync(SceneField optionsMenu, SceneField persistentScene, SceneField mainMenuFallback)
    {
        Debug.Log("[TransitionDirector] CloseOptionsMenuAsync called. Delegating unload to the authoritative state-router flow.");

        // This path is intentionally no longer responsible for unloading the Options scene.
        // The only allowed unload happens in GameStateSceneRouter.HandleMainMenuRequestAsync.
        if (optionsMenu != null && optionsMenu.IsValid())
        {
            Scene optionsScene = SceneManager.GetSceneByName(optionsMenu.SceneName);
            if (optionsScene.IsValid() && optionsScene.isLoaded)
            {
                Debug.LogWarning($"[TransitionDirector] Options scene '{optionsMenu.SceneName}' is still loaded; main-menu transition should own the unload.");
            }
        }

        Scene targetScene = SceneManager.GetSceneByName(mainMenuFallback.SceneName);
        if (!targetScene.IsValid() || !targetScene.isLoaded)
        {
            Debug.LogWarning("[TransitionDirector] MainMenu scene missing. Loading it without unloading the options overlay.");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuFallback.SceneName, LoadSceneMode.Additive);
            yield return asyncLoad;
            targetScene = SceneManager.GetSceneByName(mainMenuFallback.SceneName);
        }

        if (targetScene.IsValid() && targetScene.isLoaded)
        {
            SceneManager.SetActiveScene(targetScene);
            _signalBus.Fire(new OptionsClosedSignal());
        }
    }

    private FadePanelManager FindFaderInScene(Scene scene)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            FadePanelManager fader = root.GetComponentInChildren<FadePanelManager>(true);
            if (fader != null) return fader;
        }
        return null;
    }

    #endregion

    #region Internal Logic

    // <summary>
    /// Internal Logic: Operates on SceneField.
    /// Extracts string only when calling SceneManager API.
    /// </summary>
    private IEnumerator TransitionCoroutine(SceneField sceneField, Action onComplete, bool spawnAtDoor, DoorTriggerInteraction.DoorToSpawnAt door, bool fromRight)
    {
        _isTransitioning = true;
        
        // Extract string strictly for the API call
        string sceneName = sceneField.SceneName;
        
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[TransitionDirector] Scene name is empty at runtime. Aborting.");
            _isTransitioning = false;
            onComplete?.Invoke();
            yield break;
        }

        _signalBus.Fire(new SceneTransitionStartedSignal { TargetScene = sceneName });

        // 1. Fade Out
        if (fadeManager != null)
        {
            //fadeManager.StartFadeOut();
            //while (fadeManager.IsFadingOut) yield return null;
        }

        // 2. Load Additive
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncOp.allowSceneActivation = false;
        while (asyncOp.progress < 0.9f) yield return null;

        // 3. Activate Scene
        asyncOp.allowSceneActivation = true;
        while (!asyncOp.isDone) yield return null;

        // 4. Set Active Scene & Fix EventSystem
        //SceneLoaderService.SetActiveAndFixEventSystem(sceneName, persistentSceneName);

        // 5. 🟢 FIRE SIGNAL
        _signalBus.Fire(new PlayerSpawnRequest
        {
            SpawnAtDoor = spawnAtDoor,
            Door = door,
            FromRight = fromRight
        });

        // 6. Fade In
        if (fadeManager != null)
        {
            //fadeManager.StartFadeIn();
            //while (fadeManager.IsFadingIn) yield return null;
        }

        onComplete?.Invoke();
        yield return null;

        UnloadOldScenes(sceneName);
        _isTransitioning = false;

        _signalBus.Fire(new SceneTransitionCompletedSignal { TargetScene = sceneName });
    }

    private void UnloadOldScenes(string newSceneName)
    {
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--) 
        {
            Scene s = SceneManager.GetSceneAt(i);
            
            if (s.name == persistentSceneName || s.name == newSceneName || !s.IsValid()) continue;

            Debug.Log($"[TransitionDirector] Unloading old scene: {s.name}");
            SceneManager.UnloadSceneAsync(s);
        }
    }

    #endregion

    #region Main Menu

    public IEnumerator TransitionToMainMenuAsync(SceneField mainMenu, SceneField optionsMenu, SceneField persistentScene)
    {
        // Intentionally do not unload the Options scene here.
        // The authoritative flow in GameStateSceneRouter owns that unload once, after save completes.
        Scene mainMenuSceneRef = SceneManager.GetSceneByName(mainMenu.SceneName);
        if (!mainMenuSceneRef.IsValid() || !mainMenuSceneRef.isLoaded)
        {
            Debug.Log("[TransitionDirector] Loading Main Menu...");
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(mainMenu.SceneName, LoadSceneMode.Additive);
            yield return loadOp;
        }

        if (mainMenuSceneRef.IsValid() || SceneManager.GetSceneByName(mainMenu.SceneName).IsValid())
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(mainMenu.SceneName));
        }

        Debug.Log("[TransitionDirector] Main Menu transition complete.");
    }
    #endregion
}


//Pre VContainer verson 

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// Manages ALL scene operations: Heavy transitions (with fades/spawns) and Lightweight Overlays (Options).
/// </summary>
public class SceneTransitionDirector : MonoBehaviour
{
    #region Singleton & Initialization

    public static SceneTransitionDirector Instance { get; private set; }
    #endregion

    #region Unity lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Do NOT access ServiceLocator here if you can avoid it.
        // If you must, use the static property directly, not a cached variable.
        Debug.Log($"[SceneTransitionDirector] Awake() - Time: {Time.realtimeSinceStartup}");
    }

    void Start()
    {
        // Access Static Property Directly (Triggers Lazy Load if needed)
        _serviceLocator = ServiceLocator.Instance;

        // Single Null Check
        if (_serviceLocator == null)
        {
            Debug.LogError("[SceneTransitionDirector] ServiceLocator instance not found!", this);
            enabled = false;
            return;
        }

        // Register Self
        _serviceLocator.Register<SceneTransitionDirector>(this);
        
        Debug.Log("[SceneTransitionDirector] Successfully initialized.");
    }

    void OnDestroy()
    {
        if (_serviceLocator != null)
        {
            _serviceLocator.Unregister<SceneTransitionDirector>();
        }
    }

    #endregion

    #region Configuration

    [Header("Dependencies")]
    [Tooltip("Reference to the Fade Manager singleton or component.")]
    [SerializeField] private ScenFadeManager fadeManager;

    [Tooltip("Reference to PlayerSpawnerManager for spawn logic.")]
    [SerializeField] private PlayerSpawnerManager playerSpawner;

    [Header("Settings")]
    [Tooltip("Name of the persistent scene that should NEVER be unloaded.")]
    [SerializeField] private string persistentSceneName = "PersistentManagers";

    [Header("Scene References")]
    [SerializeField] private SceneField mainMenuScene;

    private ServiceLocator _serviceLocator;

    #endregion

    #region Events
    [Header("Event Channels")]
    [Tooltip("Listen for event to close Options Menu and return to previous scene.")]
    [SerializeField] private VoideEventChannel onOptionsClosedEvent;
    #endregion

    #region Private State
    private Scene _previousSceneBeforeOptions;
    #endregion


  

    #region Public API: Heavy Transitions

    /// <summary>
    /// Initiates a full scene transition with fades, spawn logic, and cleanup.
    /// </summary>
    public void StartTransition(SceneField sceneField, Action onComplete, bool spawnAtDoor, DoorTriggerInteraction.DoorToSpawnAt door, bool fromRight)
    {
        if (!sceneField.IsValid())
        {
            Debug.LogError($"[TransitionDirector] Invalid SceneField: {sceneField.SceneName}");
            onComplete?.Invoke();
            return;
        }

        // Ensure this object is active (fixes your initial error)
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);

        StartCoroutine(TransitionCoroutine(sceneField, onComplete, spawnAtDoor, door, fromRight));
    }

    #endregion

    #region Public API: Options Overlay

    public IEnumerator LoadOptionsMenuOverlayAsync(SceneField optionsMenu, SceneField persistentScene)
    {
        _previousSceneBeforeOptions = SceneManager.GetActiveScene();
        Debug.Log($"[TransitionDirector] Options opened from: {_previousSceneBeforeOptions.name}");

        Scene scene = SceneManager.GetSceneByName(optionsMenu.SceneName);
        
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.Log($"[TransitionDirector] Loading Options Menu: {optionsMenu.SceneName}");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(optionsMenu.SceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone) yield return null;
        }
        else
        {
            Debug.LogWarning($"[TransitionDirector] Options scene '{optionsMenu.SceneName}' is already loaded.");
        }

        Debug.Log($"[TransitionDirector] Forcing Active Scene and EventSystem fix for Options.");
        SceneLoaderService.SetActiveAndFixEventSystem(optionsMenu.SceneName, persistentScene.SceneName);
    }

    public IEnumerator CloseOptionsMenuAsync(SceneField optionsMenu, SceneField persistentScene, SceneField mainMenuFallback)
    {
        Debug.Log($"[TransitionDirector] Closing Options. Returning to: {_previousSceneBeforeOptions.name}");

        if (optionsMenu.IsValid())
        {
            Scene optionsScene = SceneManager.GetSceneByName(optionsMenu.SceneName);
            
            if (optionsScene.IsValid() && optionsScene.isLoaded)
            {
                FadePanelManager optionsFader = FindFaderInScene(optionsScene);
                
                if (optionsFader != null)
                {
                    Debug.Log("[TransitionDirector] Fading out Options panels...");
                    yield return optionsFader.FadeOutAllPanelsSequentialCoroutine();
                }

                Debug.Log($"[TransitionDirector] Unloading Options scene: {optionsMenu.SceneName}");
                AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(optionsMenu.SceneName);
                yield return asyncUnload;
            }
        }

        if (_previousSceneBeforeOptions.IsValid() && _previousSceneBeforeOptions.isLoaded)
        {
            Debug.Log($"[TransitionDirector] Restoring previous scene: {_previousSceneBeforeOptions.name}");
            SceneLoaderService.SetActiveAndFixEventSystem(_previousSceneBeforeOptions.name, persistentScene.SceneName);
            
            if (_previousSceneBeforeOptions.name == mainMenuFallback.SceneName)
            {
                yield return null; 
                onOptionsClosedEvent?.Raise();
            }
        }
        else
        {
            Debug.LogWarning("[TransitionDirector] Previous scene invalid. Fallback to MainMenu.");
            SceneLoaderService.SetActiveAndFixEventSystem(mainMenuFallback.SceneName, persistentScene.SceneName);
            yield return null;
            onOptionsClosedEvent?.Raise();
        }
    }

    private FadePanelManager FindFaderInScene(Scene scene)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            FadePanelManager fader = root.GetComponentInChildren<FadePanelManager>(true);
            if (fader != null) return fader;
        }
        return null;
    }

    #endregion

    #region Internal Logic

    private IEnumerator TransitionCoroutine(SceneField sceneField, Action onComplete, bool spawnAtDoor, DoorTriggerInteraction.DoorToSpawnAt door, bool fromRight)
    {
        // 1. Fade Out
        if (fadeManager != null)
        {
            fadeManager.StartFadeOut();
            while (fadeManager.IsFadingOut) yield return null;
        }

        // 2. Load Additive (Block Activation)
        Debug.Log($"[TransitionDirector] Loading '{sceneField.SceneName}'...");
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneField.SceneName, LoadSceneMode.Additive);
        asyncOp.allowSceneActivation = false;

        while (asyncOp.progress < 0.9f) yield return null;

        // 3. Activate Scene
        Debug.Log($"[TransitionDirector] Activating '{sceneField.SceneName}'...");
        asyncOp.allowSceneActivation = true;
        
        // Wait for the scene to be fully done (Awake called, objects instantiated)
        while (!asyncOp.isDone) yield return null;

        // 4. Set Active Scene & Fix EventSystem
        SceneLoaderService.SetActiveAndFixEventSystem(sceneField.SceneName, persistentSceneName);

        // 5. Spawn Logic (Execute AFTER scene is active)
        var spawner = playerSpawner != null ? playerSpawner : FindFirstObjectByType<PlayerSpawnerManager>();
        if (spawner != null)
        {
            if (spawnAtDoor) spawner.SpawnPlayerAtDoor(door, fromRight);
            else spawner.SpawnPlayerAtStart();
        }

        // 6. Fade In
        if (fadeManager != null)
        {
            fadeManager.StartFadeIn();
            while (fadeManager.IsFadingIn) yield return null;
        }

        // 7. Invoke Completion Callback
        // This allows IntroSceneLoader to run its logic (e.g., logging) BEFORE we destroy its scene
        onComplete?.Invoke();

        // 8. SAFETY YIELD: Give the callback a full frame to execute
        yield return null;

        // 9. Cleanup & Unload Old Scenes (NOW SAFE)
        // The new scene is active, faded in, and the caller has finished their work.
        UnloadOldScenes(sceneField.SceneName);
    }

    private void UnloadOldScenes(string newSceneName)
    {
        // FIX: Iterate backwards to avoid index shifting
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--) 
        {
            Scene s = SceneManager.GetSceneAt(i);
            
            // KEEP: Persistent, New Scene, and Invalid scenes
            if (s.name == persistentSceneName || s.name == newSceneName || !s.IsValid()) continue;

            Debug.Log($"[TransitionDirector] Unloading old scene: {s.name}");
            SceneManager.UnloadSceneAsync(s);
        }
    }

    #endregion

    #region Main menu

    public IEnumerator TransitionToMainMenuAsync(SceneField mainMenu, SceneField optionsMenu, SceneField persistentScene)
    {
        Scene optionsScene = SceneManager.GetSceneByName(optionsMenu.SceneName);
        if (optionsScene.IsValid() && optionsScene.isLoaded)
        {
            Debug.Log("[TransitionDirector] Unloading Options Menu...");
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(optionsMenu.SceneName);
            yield return unloadOp;
        }

        Scene mainMenuSceneRef = SceneManager.GetSceneByName(mainMenu.SceneName);
        if (!mainMenuSceneRef.IsValid() || !mainMenuSceneRef.isLoaded)
        {
            Debug.Log("[TransitionDirector] Loading Main Menu...");
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(mainMenu.SceneName, LoadSceneMode.Additive);
            yield return loadOp;
        }

        SceneLoaderService.SetActiveAndFixEventSystem(mainMenu.SceneName, persistentScene.SceneName);
        Debug.Log("[TransitionDirector] Main Menu transition complete.");
    }   
    #endregion
}   
*/ 