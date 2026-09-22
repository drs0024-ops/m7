using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Owns the current GameState and validates transitions via a state table.
    /// Publishes all state-related messages (GameStateChanged, GameStarted, GamePaused,
    /// GameResumed, TimeScalePause/Resume). Does NOT decide WHEN to transition.
    /// </summary>
    public class GameStateMachine : IStartable
    {
        #region Dependencies

        private IPublisher<TimeScalePause> _pausePublisher;
        private IPublisher<TimeScaleResume> _resumePublisher;
        private IPublisher<GameStateChanged> _stateChangedPublisher;
        private IPublisher<GameStarted> _gameStartedPublisher;
        private IPublisher<GamePaused> _gamePausedPublisher;
        private IPublisher<GameResumed> _gameResumedPublisher;

        #endregion

        #region State

        private GameState _currentState = GameState.MainMenu;

        public GameState CurrentState => _currentState;

        #endregion

        #region Transition Table

        private static readonly Dictionary<GameState, HashSet<GameState>> _validTransitions = new()
        {
            [GameState.MainMenu]   = new() { GameState.Loading, GameState.IntroVideo, GameState.QuitGame },
            [GameState.Loading]    = new() { GameState.Gameplay, GameState.MainMenu },
            [GameState.Gameplay]   = new() { GameState.Paused, GameState.GameOver, GameState.Loading },
            [GameState.Paused]     = new() { GameState.Gameplay, GameState.MainMenu },
            [GameState.GameOver]   = new() { GameState.MainMenu },
            [GameState.IntroVideo] = new() { GameState.MainMenu, GameState.Loading },
            [GameState.QuitGame]   = new() { },
        };

        #endregion

        #region Construction

        [Inject]
        public GameStateMachine() { }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            _pausePublisher = GlobalMessagePipe.GetPublisher<TimeScalePause>();
            _resumePublisher = GlobalMessagePipe.GetPublisher<TimeScaleResume>();
            _stateChangedPublisher = GlobalMessagePipe.GetPublisher<GameStateChanged>();
            _gameStartedPublisher = GlobalMessagePipe.GetPublisher<GameStarted>();
            _gamePausedPublisher = GlobalMessagePipe.GetPublisher<GamePaused>();
            _gameResumedPublisher = GlobalMessagePipe.GetPublisher<GameResumed>();

   
        }

        #endregion

        #region Public API

        public bool IsInState(GameState state) => _currentState == state;

        public bool CanTransitionTo(GameState target)
        {
            return _validTransitions.TryGetValue(_currentState, out var allowed)
                   && allowed.Contains(target);
        }

        /// <summary>
        /// Validates and executes a state transition. Returns false (and logs) if invalid.
        /// </summary>
        public bool TransitionTo(GameState target)
        {
            if (_currentState == target) return true;

            if (!CanTransitionTo(target))
            {
                Debug.LogWarning($"[StateMachine] Invalid transition: {_currentState} → {target}");
                return false;
            }

            ExecuteTransition(target);
            return true;
        }

        /// <summary>
        /// Forces a state without validation. Use ONLY for startup and error recovery.
        /// </summary>
        public void ForceState(GameState target)
        {
            if (_currentState == target) return;
            ExecuteTransition(target);
        }

        #endregion

        #region Internal

        private void ExecuteTransition(GameState target)
        {
            GameState previous = _currentState;
            _currentState = target;

            if (ShouldPause(target))
                _pausePublisher.Publish(default);
            else if (ShouldPause(previous))
                _resumePublisher.Publish(default);

            if (previous == GameState.MainMenu && target == GameState.Gameplay)
                _gameStartedPublisher.Publish(default);

            if (target == GameState.Paused)
                _gamePausedPublisher.Publish(default);

            if (previous == GameState.Paused && target == GameState.Gameplay)
                _gameResumedPublisher.Publish(default);

            _stateChangedPublisher.Publish(new GameStateChanged(target));
        }

        private static bool ShouldPause(GameState state)
        {
            return state == GameState.Paused
                || state == GameState.QuitGame;
        }

        #endregion
    }
}   

/*
using UnityEngine;
using VContainer;

public class GameStateMachine 
{
    private readonly ISignalBus _signalBus;
    private GameState _currentState;

    public GameState CurrentState => _currentState;

    [Inject]
    public GameStateMachine(ISignalBus signalBus)
    {
        _signalBus = signalBus;
        _currentState = GameState.MainMenu; // Default
    }

    public void Setup()
    {
        // Optional: Fire initial state if UI needs to sync immediately on boot
        // _signalBus.Fire(new GameStateChangedSignal(_currentState));
    }

    public void Cleanup() { }

    // --- Required Methods ---

    public bool IsInState(GameState state)
    {
        return _currentState == state;
    }

    /// <summary>
    /// Forces the state immediately without broadcasting signals.
    /// Used by GameFlowSystem for initialization and recovery.
    /// </summary>
    public void ForceState(GameState newState)
    {
        if (_currentState == newState) return;
        
        Debug.Log($"[StateMachine] FORCE State: {_currentState} -> {newState} (No Signals)");
        _currentState = newState;
        
        // No signals fired here
        // Manually handle TimeScale if needed, but typically ForceState implies 
        // we are setting up context, so we might NOT want to pause/resume yet.
        // If you DO want time to update immediately, call internal time logic here.
    }

    /// <summary>
    /// Alias for ForceState or internal setter. 
    /// Useful if you want to distinguish "Internal Set" vs "External Force".
    /// </summary>
    public void SilentlySetState(GameState newState)
    {
        ForceState(newState);
    }

    // --- Standard Public API ---

    public void RequestState(GameState newState)
    {
        SetState(newState);
    }

    /// <summary>
    /// Core state transition logic.
    /// Handles TimeScale side-effects and broadcasts the state change via SignalBus.
    /// </summary>
    /// <param name="newState">The target state to transition to.</param>
    /// <param name="forceRaise">If true, fires the signal even if already in this state (useful for re-initialization).</param>
    private void SetState(GameState newState, bool forceRaise = false)
    {
        // 1. Guard: Prevent redundant transitions unless explicitly forced
        if (_currentState == newState && !forceRaise)
        {
            Debug.LogWarning($"[StateMachine] BLOCKED: Already in state '{newState}'. Use forceRaise=true to override.");
            return;
        }

        Debug.Log($"[StateMachine] TRANSITION: {_currentState} → {newState} (Force: {forceRaise})");
        _currentState = newState;

        // 2. Handle TimeScale Side-Effects
        //    Pause states freeze gameplay; active states resume it.
        bool shouldPause = newState is GameState.Paused or GameState.OptionsMenu or GameState.QuitGame;
        
        if (shouldPause)
        {
            Debug.Log("[StateMachine] Pausing TimeScale.");
            _signalBus.Fire(new TimeScalePauseSignal());
        }
        else
        {
            Debug.Log("[StateMachine] Resuming TimeScale.");
            _signalBus.Fire(new TimeScaleResumeSignal());
        }

        // 3. Broadcast State Change to all subscribers (SceneRouter, UI, etc.)
        _signalBus.Fire(new GameStateChangedSignal(newState));
        Debug.Log($"[StateMachine] GameStateChangedSignal fired for '{newState}'.");
    }   
}

#region Signal Definitions
public struct TimeScalePauseSignal { }
public struct TimeScaleResumeSignal { }
#endregion   


 Prior versions

#region prior version 6/28/2026
using UnityEngine;

/// <summary>
/// Centralized controller for global application flow using an Event-Driven State Machine.
/// </summary>
/// <remarks>
/// <para><strong>Pattern:</strong> Separates state <em>requests</em> (inputs) from state <em>execution</em> (logic).</para>
/// <para><strong>Flow:</strong> UI/Input → Request Event → <see cref="SetState"/> → State Change Event → <see cref="OnStateChanged"/> (Logic).</para>
/// </remarks>
public class GameStateController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.MainMenu;

    [Header("Input Events (Requests)")]
    [Tooltip("Raised by OptionsContextManager when Options are opened")]
    [SerializeField] private VoidEventSO onOptionsOpenedEvent;
    
    [Tooltip("Raised by OptionsContextManager to return to Main Menu")]
    [SerializeField] private VoidEventSO showMainMenuRequestedFromOptions;
    
    [Tooltip("Raised by OptionsContextManager to return to Pause")]
    [SerializeField] private VoidEventSO returnToPauseFromOptions;

    [SerializeField] private VoidEventSO onEscapePressed;
    //private VoidEventSO onExitToMainMenuRequested; not currently using, need confirm then remove
    [SerializeField] private GameStateEventSO onQuitRequested;

    [Header("Output Events")]
    [SerializeField] private GameStateEventSO onGameStateChange;
    //private VoidEventSO pauseGameEvent; Not currently using, need to confirm and remove
    //private VoidEventSO unPauseGameEvent;

    #endregion

    #region Initialization

    private void OnEnable()
    {
        // Subscribe to Input Events
        onOptionsOpenedEvent.OnEventRaised += OnOptionsOpened;
        returnToPauseFromOptions.OnEventRaised += OnReturnToPauseFromOptions;
        showMainMenuRequestedFromOptions.OnEventRaised += OnReturnToMainMenuFromOptions;

        onEscapePressed.OnEventRaised += OnPauseRequested;
        //onExitToMainMenuRequested.OnEventRaised += OnExitToMainMenuRequested;
        onQuitRequested.OnRaised += OnQuitRequested;
        
        // Subscribe to State Change Event (Execution Loop)
        onGameStateChange.OnRaised += OnStateChanged;
        
        // Initialize state
        SetState(currentState, forceRaise: true);
    }

    private void OnDisable()
    {
        onOptionsOpenedEvent.OnEventRaised -= OnOptionsOpened;
        returnToPauseFromOptions.OnEventRaised -= OnReturnToPauseFromOptions;
        showMainMenuRequestedFromOptions.OnEventRaised -= OnReturnToMainMenuFromOptions;

        onEscapePressed.OnEventRaised -= OnPauseRequested;
        //onExitToMainMenuRequested.OnEventRaised -= OnExitToMainMenuRequested;
        onQuitRequested.OnRaised -= OnQuitRequested;
        onGameStateChange.OnRaised -= OnStateChanged;
    }

    #endregion

    #region State Management

    /// <summary>
    /// Requests a transition to a new game state.
    /// </summary>
    /// <param name="newState">The target state.</param>
    /// <param name="forceRaise">If true, raises the event even if the state hasn't changed.</param>
    public void SetState(GameState newState, bool forceRaise = false)
    {
        Debug.Log("[GameStateController] State set as: " + newState);
        if (currentState == newState && !forceRaise) 
            return;

        /* need to confirm if still using
        // Handle immediate pause/unpause events for listeners
        if (newState == GameState.Paused) 
            pauseGameEvent?.Raise();
        else if (newState == GameState.Playing && currentState == GameState.Paused) 
            unPauseGameEvent?.Raise();

       

        currentState = newState;
        onGameStateChange?.Raise(currentState);
    }

    #endregion

    #region State Execution Logic

    /// <summary>
    /// Executes the logic associated with the new state (TimeScale, Scene Loading).
    /// </summary>
    /// <remarks>
    /// This method is decoupled from the decision to change state. It purely reacts to the change.
    /// </remarks>
    private void OnStateChanged(GameState newState)
    {
        Debug.Log($"[State] Executing logic for: {newState}");
        
        // 1. Handle TimeScale Globally
        // Options and Paused stop time. MainMenu usually stops time (or keeps it 0 if coming from pause).
        if (newState == GameState.Paused || newState == GameState.OptionsMenu || newState == GameState.QuitGame)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;

        // 2. Execute State-Specific Logic
        switch (newState)
        {
            case GameState.MainMenu:
                // Delegate to GameManager to ensure Save -> Load sequence
                Debug.Log("[GameStateController] call GM LoadMainMenu()");
                if (GameManager.Instance == null)
                {
                    Debug.Log("[GameStateController] GM is null");
                    
                }
                GameManager.Instance?.LoadMainMenu();
                break;

            case GameState.NewGame:
                GameManager.Instance?.OnStartNewGameEvent();
                break;

            case GameState.LoadSavedGame:
                GameManager.Instance?.OnLoadSavedGameEvent();
                break;

            case GameState.OptionsMenu:
                // Load Options Additively. 
                // State remains 'Options' until user clicks Back.
                SceneSwapManager.Instance?.LoadOptionsMenu();
                break;
            
            case GameState.QuitGame:
                GameManager.Instance?.OnGameQuitEvent();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
                break;
                
            case GameState.Paused:
                // Pause logic (e.g., show pause UI) handled by UI listeners
                break;
                
            case GameState.Playing:
                // Resume logic handled by UI listeners
                break;
                
            // Note: No cases for "ReturnTo..." here anymore. 
            // Those are handled in the Input Handlers which call the SwapManager directly, 
            // then set the state back to MainMenu/Paused.
        }
    }

    #endregion

    #region Input Handlers

     // Triggered when OptionsContext says "Open Options"
    private void OnOptionsOpened()
        => SetState(GameState.OptionsMenu);

    // Triggered when OptionsContext says "Go back to Main Menu"
    private void OnReturnToMainMenuFromOptions()
    {
        // 1. Perform the Unload/Restore action directly
        SceneSwapManager.Instance?.UnloadOptionsAndReturnToMainMenu();
        
        // 2. Transition State back to MainMenu
        SetState(GameState.MainMenu);
    }
    
    // Triggered when OptionsContext says "Go back to Pause"
    private void OnReturnToPauseFromOptions()
    {
        // 1. Perform the Unload/Restore action directly
        SceneSwapManager.Instance?.UnloadOptionsAndReturnToPause();
        
        // 2. Transition State back to Paused (TimeScale stays 0)
        SetState(GameState.Paused);
    }

    private void OnPauseRequested()
    {
        if (currentState == GameState.OptionsMenu)
        {
            // Tell OptionsContext to close itself
  
            return; 
        }

        if (currentState == GameState.Playing)
            SetState(GameState.Paused);
        else if (currentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    private void OnExitToMainMenuRequested() 
        => SetState(GameState.MainMenu);

    private void OnQuitRequested(GameState gameState)
    {
        if (gameState != GameState.QuitGame) return;

        SetState(GameState.QuitGame);
        
    }


    #endregion
}

#endregion




#region PRIOR VERSION 6/17/2026
/*
using UnityEngine;

/// <summary>
/// Centralized controller for global application flow and game lifecycle management.
/// Acts as the single source of truth for <see cref="GameState"/>.
/// </summary>
/// <remarks>
/// <para><strong>Pattern:</strong> Event-Driven State Machine.</para>
/// <para><strong>Flow:</strong> Inputs (UI/InputSystem) -> Call SetState() -> Raise Event -> OnStateChanged() executes logic (TimeScale/Scenes).</para>
/// </remarks>
public class GameStateController : MonoBehaviour
{
    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.BootstrapState;

    [Header("Subscribed Events (Inputs)")]
    // These events trigger a state CHANGE REQUEST
    [SerializeField] private VoidEventSO onEscapePressed;
    [SerializeField] private VoidEventSO onIntroVideoFinished;
    [SerializeField] private VoidEventSO onStartNewGameRequestedEvent;
    [SerializeField] private VoidEventSO onLoadSavedGameRequested; // Added for completeness
    [SerializeField] private VoidEventSO onResumeRequested;
    [SerializeField] private VoidEventSO onSettingsRequested;
    [SerializeField] private VoidEventSO onExitToMainMenuRequested; 
    [SerializeField] private VoidEventSO onQuitRequested;

    [Header("Events Raised (Outputs)")]
    // Notifies listeners that the state HAS changed and logic is executing
    [SerializeField] private GameStateEventSO onGameStateChange;
    [SerializeField] private VoidEventSO pauseGameEvent;
    [SerializeField] private VoidEventSO unPauseGameEvent;

    private void OnEnable()
    {
        // 1. Register for Input Events
        onEscapePressed?.RegisterListener(OnPauseRequested);
        onIntroVideoFinished?.RegisterListener(OnIntroVideoFinished);
        onStartNewGameRequestedEvent?.RegisterListener(OnStartNewGameRequest);
        onLoadSavedGameRequested?.RegisterListener(OnLoadSavedGameRequest);
        onResumeRequested?.RegisterListener(OnResumeRequested);
        onSettingsRequested?.RegisterListener(OnSettingsRequested);
        onExitToMainMenuRequested?.RegisterListener(OnExitToMainMenuRequested);
        onQuitRequested?.RegisterListener(OnQuitRequested);

        // 2. Register for the State Change Event (The "Reaction" loop)
        // NOTE: We listen to this to execute logic (TimeScale/Scenes) AFTER the state is decided.
        onGameStateChange?.RegisterListener(OnStateChanged);

        // 3. Force initial state setup (ensures TimeScale is correct on startup)
        SetState(currentState, forceRaise: true);
    }

    private void OnDisable()
    {
        onEscapePressed?.UnregisterListener(OnPauseRequested);
        onIntroVideoFinished?.UnregisterListener(OnIntroVideoFinished);
        onStartNewGameRequestedEvent?.UnregisterListener(OnStartNewGameRequest);
        onLoadSavedGameRequested?.UnregisterListener(OnLoadSavedGameRequest);
        onResumeRequested?.UnregisterListener(OnResumeRequested);
        onSettingsRequested?.UnregisterListener(OnSettingsRequested);
        onExitToMainMenuRequested?.UnregisterListener(OnExitToMainMenuRequested);
        onQuitRequested?.UnregisterListener(OnQuitRequested);
        
        onGameStateChange?.UnregisterListener(OnStateChanged);
    }
    
    private void OnBootstrapFinished()
    {
        // This triggers your existing OnStateChanged logic

        SetState(GameState.MainMenu);
    }

    private void OnPauseRequested()
    {
        if (currentState == GameState.Playing)
            SetState(GameState.Paused);
    }

    /// <summary>
    /// Requests a state transition.
    /// </summary>
    public void SetState(GameState newState, bool forceRaise = false)
    {
        if (currentState == newState && !forceRaise) return;

        GameState previousState = currentState;

        // 1. Raise Specific Pause/Unpause events immediately based on transition
        if (newState == GameState.Paused)
        {
            pauseGameEvent?.Raise();
        }
        else if (newState == GameState.Playing && previousState == GameState.Paused)
        {
            unPauseGameEvent?.Raise();
        }

        // 2. Update internal state BEFORE raising the main event 
        // (So OnStateChanged reads the correct new state)
        currentState = newState;

        // 3. Notify all listeners (including OnStateChanged) to execute logic
        onGameStateChange?.Raise(currentState);
    }

    /// <summary>
    /// Executes the logic associated with the new state (TimeScale, Scene Loading).
    /// This is decoupled from the decision to change state.
    /// </summary>
    private void OnStateChanged(GameState newState)
    {
        Debug.Log($"[GameStateController] Executing logic for: {newState}");
        Debug.Log($"[GameStateController] BEFORE Switch: Time.timeScale = {Time.timeScale} | New State = {newState}");

        switch (newState)
        {
            case GameState.BootstrapState:
                Time.timeScale = 1f;
                // Do NOT load Main Menu here. Wait for explicit transition.
                // Managers are now ready, but let the game decide when to show the menu.
                break;

            case GameState.MainMenu:
                Time.timeScale = 1f; // Keep time normal for menu animations
                if (GameManager.Instance?.SceneSwapManager != null)
                {
                    // Load Main Menu ADDITIVELY so managers persist
                    GameManager.Instance.SceneSwapManager.LoadMainMenu();
                }
                break;

            case GameState.NewGame:
            Time.timeScale = 1f;
            if (GameManager.Instance?.SceneSwapManager != null)
            {
                // 1. Unload Menu First
                GameManager.Instance.SceneSwapManager.UnloadMainMenu();
                
                // 2. Then Load Level (ensure your LoadLevel method uses Additive or Single as needed)
                // If using Single, it unloads everything else automatically. 
                // If using Additive, you MUST unload the menu first (as done above).
                GameManager.Instance.SceneSwapManager.LoadLevel("Level_01"); 
            }
            break;

            case GameState.LoadSavedGame:
                Time.timeScale = 1f;
                if (GameManager.Instance?.SceneSwapManager != null)
                {
                    GameManager.Instance.SceneSwapManager.UnloadMainMenu();
                    GameManager.Instance.SceneSwapManager.StartCoroutine(
                        GameManager.Instance.SceneSwapManager.LoadSavedGame()
                    );
                }
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                // No scene changes here. This state is for gameplay logic.
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                // Load Pause Menu ADDITIVELY (overlay)
                // GameManager.Instance?.SceneSwapManager?.LoadPauseMenuAdditive();
                break;

            case GameState.ExitingToMainMenu:
                Time.timeScale = 1f;
                if (GameManager.Instance?.SceneSwapManager != null)
                {
                    // Unloads the current level dynamically
                    GameManager.Instance.SceneSwapManager.UnloadCurrentLevel(); 
                    
                    // Reloads the Main Menu
                    GameManager.Instance.SceneSwapManager.LoadMainMenu();
                }
                break;

            case GameState.QuitGame:
                Time.timeScale = 0f;
                GameManager.Instance?.OnGameQuitEvent();
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
                break;

            // Handle other states (Settings, GameOver, etc.) as UI-only or similar logic

            
        }
        
        Debug.Log($"[GameStateController] AFTER Switch: Time.timeScale = {Time.timeScale}");
    }   

    #region Input Handlers (State Requestors)

    private void OnIntroVideoFinished()
    {
        // If intro was a video overlay in the same scene, transition directly to Playing
        // If it was a scene, SceneSwapManager usually handles the callback to SetState(GameState.Playing)
        if (GameManager.Instance?.SceneSwapManager == null) 
        {
            SetState(GameState.Playing);
        }
    }

    public void OnStartNewGameRequest() => SetState(GameState.NewGame);
    
    public void OnLoadSavedGameRequest() => SetState(GameState.LoadSavedGame);

    public void OnResumeRequested() => SetState(GameState.Playing);

    public void OnSettingsRequested() => SetState(GameState.Settings);

    public void OnExitToMainMenuRequested() => SetState(GameState.MainMenu);

    public void OnQuitRequested() => SetState(GameState.QuitGame);

    #endregion

    public bool IsGameplayActive() => currentState == GameState.Playing;
    public GameState GetCurrentState() => currentState;
}


*/
