# Scene System Flow Updated 8/18/2026

## Architecture Overview

This document describes the additive scene management system for a Unity project using **VContainer** for Dependency Injection and **UniTask** for async/await operations.

### Core Principles

- **Bootstrap Scene**: Persistent for the application lifetime. Holds the root `LifetimeScope` and core managers. 
- **Additive Stack**: All other scenes (MainMenu, Options, Gameplay) are loaded/unloaded additively. 
- **Explicit Scene Tracking**: A `Dictionary<string, Scene>` in `SceneLoaderService` tracks loaded scene handles to prevent ambiguity with same-named scenes. 
- **Signal-Driven Navigation**: UI components fire signals; `GameStateSceneRouter` interprets them and delegates to the appropriate transition method.
- **Atomic Transitions**: Scene activation always precedes unloading to prevent void states.

---

## Scene Hierarchy & VContainer Scopes

Bootstrap (Persistent, Build Index 0) ├── Root LifetimeScope │ ├── GameStateSceneRouter (IStartable, IDisposable) │ ├── GameStateMachine │ ├── SceneLoaderService │ ├── SceneTransitionOrchestrator │ ├── SceneFadeService │ ├── SceneCleanupService │ └── ISignalBus │ ├── MainMenu (Additive) │ └── MainMenu LifetimeScope (child of Root) │ ├── MainMenuView │ └── ... │ ├── OptionsMenu (Additive) │ └── OptionsMenu LifetimeScope (child of Root) │ ├── OptionsView │ ├── OptionsNavigationHandler │ └── ... │ └── Gameplay Scenes (Additive) └── Level LifetimeScope (child of Root) ├── SceneTransitionDirector ├── Player └── ...


---

## Component Responsibilities

| Component | SRP | Scope |
|-----------|-----|-------|
| `GameStateMachine` | Tracks current `GameState`, fires `GameStateChangedSignal` | Bootstrap |
| `GameStateSceneRouter` | Routes state changes to scene operations (Push/Pop/Replace) | Bootstrap |
| `SceneLoaderService` | Low-level `LoadSceneAsync`/`UnloadSceneAsync` with handle tracking | Bootstrap |
| `SceneTransitionOrchestrator` | Coordinates Fade → Load/Unload → Fade sequences | Bootstrap |
| `SceneFadeService` | Visual fade in/out via Canvas/Camera | Bootstrap |
| `SceneCleanupService` | Purges obsolete scenes after level transitions | Bootstrap |
| `OptionsNavigationHandler` | Interprets Options "Back" button, fires navigation signals | OptionsMenu |
| `SceneTransitionDirector` | Entry point for heavy level transitions (doors, start game) | Level |

---

## Signal Flow

### Opening Options Menu

UI Button → GameStateMachine.RequestState(OptionsMenu) → GameStateChangedSignal(OptionsMenu) → GameStateSceneRouter.OnStateChanged → HandleOptionsMenuRequest() 1. Guard: IsSceneLoaded("OptionsMenu")? → Skip if true 2. Record: _gameStateBeforeOptions = CurrentState 3. Sync: Ensure StateMachine is in OptionsMenu 4. Load: SceneLoaderService.LoadSceneAdditiveAsync("OptionsMenu")


### Closing Options Menu (Return to Main Menu)

UI Button → OptionsBackRequestedSignal → OptionsNavigationHandler.OnBackRequested → ReturnToMainMenuRequestedSignal → GameStateSceneRouter.OnReturnToMainMenuRequested 1. Check: StateMachine.CurrentState == MainMenu? → YES: Call HandleMainMenuRequestAsync() directly → NO: StateMachine.RequestState(MainMenu) 2. HandleMainMenuRequestAsync() a. Guard: _isTransitioning? → Skip b. Check: IsSceneLoaded("OptionsMenu")? → YES: PATH A → Orchestrator.PopMenuAsync("OptionsMenu") → NO: PATH B → Orchestrator.TransitionAsync("MainMenu") 3. PopMenuAsync("OptionsMenu") a. FadeOutAsync() b. SceneLoaderService.ReturnToMainMenuFromOptionsAsync("OptionsMenu") i. Ensure MainMenu loaded (dual check: dict + SceneManager) ii. ActivateScene("MainMenu") iii. FixDuplicateEventSystems() iv. UnloadSceneAsync("OptionsMenu") [by handle] c. FadeInAsync() d. Fire OptionsClosedSignal


### Heavy Level Transition (e.g., New Game)

UI Button / Door Trigger → GameStateMachine.RequestState(NewGame) → GameStateChangedSignal(NewGame) → GameStateSceneRouter.OnStateChanged → HandleNewGameStartTransitionAsync() → Orchestrator.TransitionAsync("NewGameScene") 1. FadeOutAsync() 2. LoadSceneAdditiveAsync("NewGameScene") 3. FixDuplicateEventSystems() 4. SceneCleanupService.UnloadObsoleteScenesAsync() 5. FadeInAsync()


---

## Critical Patterns & Guards

### 1. Scene Handle Tracking (`SceneLoaderService`)

```csharp
private readonly Dictionary<string, Scene> _loadedScenes = new();

public bool IsSceneLoaded(string sceneName)
    => _loadedScenes.TryGetValue(sceneName, out Scene s) && s.isLoaded;

Why: SceneManager.GetSceneByName returns only one of multiple same-named scenes. Tracking handles ensures the correct copy is unloaded.
Guard: LoadSceneAdditiveAsync checks IsSceneLoaded before loading to prevent duplicates. 
2. Dual-Check for Pre-Existing Scenes
// In ReturnToMainMenuFromOptionsAsync:
bool mainMenuInManager = SceneManager.GetSceneByName(name).isLoaded;
bool mainMenuInDict = IsSceneLoaded(name);

if (!mainMenuInManager && !mainMenuInDict)
    await LoadSceneAdditiveAsync(name);
else if (!mainMenuInDict)
    _loadedScenes[name] = SceneManager.GetSceneByName(name); // Register

Why: MainMenu may be loaded by Bootstrap before SceneLoaderService exists, so it's missing from the dictionary. 
3. Activate Before Unload
SceneManager.SetActiveScene(targetScene);  // FIRST
await UnloadSceneAsync(sourceScene);        // SECOND

Why: Unity refuses to unload the active scene if it would leave no active scene. Always shift context first.
4. Re-Entrancy Guard
private bool _isOptionsLoading = false;

private void HandleOptionsMenuRequest()
{
    if (_isOptionsLoading) return;
    if (_loader.IsSceneLoaded(sceneName)) return;
    _isOptionsLoading = true;
    // ... load ...
    _isOptionsLoading = false;
}

Why: Prevents double-loading if GameStateChangedSignal fires twice (e.g., VContainer entry point duplication).
5. State Machine Bypass
// In OnReturnToMainMenuRequested:
if (_stateMachine.CurrentState == GameState.MainMenu)
    _ = HandleMainMenuRequestAsync(); // Direct call
else
    _stateMachine.RequestState(GameState.MainMenu); // Normal path

Why: If Options was loaded additively without a state change, the state machine is still at MainMenu. RequestState(MainMenu) would be a no-op. Bypassing ensures the scene unload logic still executes.
6. Bootstrap Protection
// In SceneCleanupService:
if (scene.name == "Bootstrap") continue; // NEVER unload

Why: Bootstrap holds the root DI scope. Unloading it destroys the entire application state. 
EventSystem Hygiene
After every scene activation, FixDuplicateEventSystems() is called:

Find all EventSystem components in the scene.
Keep the one in DontDestroyOnLoad or Bootstrap.
Destroy all others (not just disable) to free memory and prevent input conflicts.
Debugging Checklist
Symptom	Likely Cause	Fix
Scene loads twice	Duplicate signal or missing IsSceneLoaded guard	Add re-entrancy flag + dictionary check
Scene won't unload	Active scene not switched before unload	Call SetActiveScene first
isDone == true but isLoaded == true	Second copy of same-named scene exists	Track handles, unload by handle
Bootstrap unloaded	SceneCleanupService lacks persistent scene guard	Add if (name == "Bootstrap") continue
State change not firing	GameStateMachine already in target state	Bypass with direct method call
Input conflicts after transition	Duplicate EventSystem not destroyed	Call FixDuplicateEventSystems() after activation

File Structure
Assets/Project/Scripts/
├── Core/
│   ├── GameState/
│   │   ├── GameStateSceneRouter.cs
│   │   ├── GameStateMachine.cs
│   │   └── GameState.cs
│   ├── SceneManagerment/
│   │   └── VContainerSceneSystem/
│   │       └── Refactored/
│   │           ├── SceneLoaderService.cs
│   │           ├── SceneTransitionOrchestrator.cs
│   │           ├── SceneFadeService.cs
│   │           ├── SceneFadeManager.cs
│   │           └── SceneCleanupService.cs
│   └── Events/
│       └── SignalBus/
│           └── SignalBus.cs
├── UI/
│   └── Menus/
│       ├── MainMenu/
│       │   └── VContainerVersion/
│       │       └── MainMenuView.cs
│       └── Options/
│           └── VContainerVersion/
│               ├── OptionsView.cs
│               └── OptionsNavigationHandler.cs
└── Systems/
    └── Camera/
        └── NewCameraSystem/
            └── CamDebug.cs