# Complete Flow: Gameplay → Pause → Options → Pause → Resume

## 1. Player Presses Escape in Gameplay
- **Input Detection**: `GameStateInputHandler` detects `Escape`.
  - Action: `stateMachine.RequestState(GameState.Paused)`.
- **State Change**: `GameStateMachine` sets `CurrentState = Paused`.
  - Event: Raises `onTimeScalePauseEvent` (Time freezes).
- **UI Response**: `PauseManager` (listening to state change or input):
  - Action: Calls `OpenPauseMenu()` (Fades in Canvas).

## 2. Player Clicks "Options" in Pause Menu
- **Input Detection**: `PauseManager.OnOptionsButtonClicked()`.
  - Event: Raises `openSettingsFromPauseEvent` with `OptionsSourse.PauseMenu`.
- **Context Management**: `OptionsMenuContextManager.HandleOptionsOpen(PauseMenu)`.
  - Action: Sets `_currentState = PauseMenu`.
  - Event: Raises `onOptionsOpenedEventChannel`.
- **State Request**: `GameStateInputHandler` → `stateMachine.RequestState(GameState.OptionsMenu)`.
- **Scene Routing**: `GameStateSceneRouter.OnStateChange(OptionsMenu)`.
  - Action: Saves `_gameStateBeforeOptions = GameState.Paused`.
  - Action: Calls `transitionDirector.LoadOptionsMenuOverlayAsync()`.
- **Scene Transition**: `SceneTransitionDirector`.
  - Capture: `_previousSceneBeforeOptions = GameplayScene`.
  - Load: Loads `OptionsScene` **additively**.
  - Activate: Sets `OptionsScene` as **active** (for UI input).

## 3. Player Clicks "Back" in Options Menu
- **Input Detection**: `OptionsManager.OnBackClicked()`.
  - Event: Raises `onOptionsBackRequetEventChannel`.
- **Context Logic**: `OptionsMenuContextManager.OnOptionsBackRequested()` → Calls `HandleBackLogic()`.
  - Event: Raises **ONLY** `closeOptionsRequestEvent`.
- **Handler Start**: `GameStateSceneRouter.OnCloseOptionsRequested()` → Starts `CloseOptionsMenuHandler()`.
  - Yield: `transitionDirector.CloseOptionsMenuAsync()`.
- **Scene Transition**: `SceneTransitionDirector.CloseOptionsMenuAsync()`.
  - Unload: Unloads `OptionsScene`.
  - Restore: Restores `GameplayScene` as **active**.
  - *Note*: Does **NOT** raise `onOptionsClosedEvent` (source was Pause, not MainMenu).
- **State Restoration**: `GameStateSceneRouter` resumes.
  - Action: Calls `stateMachine.SilentlySetState(GameState.Paused)`.
- **Result**: Options closed, `GameplayScene` active, **Pause Menu still visible**, Time still paused.

## 4. Player Clicks "Resume" in Pause Menu
- **Input Detection**: `PauseManager.OnResumeButtonClicked()`.
  - Event: Raises `onResumeRequested`.
  - *Requirement*: A listener (e.g., `GameStateInputHandler` or `PauseListener`) must catch this to call `stateMachine.RequestState(GameState.Playing)`.
- **State Change**: `GameStateMachine`.
  - Action: Sets `CurrentState = Playing`.
  - Event: Raises `onTimeScaleResumeEvent`.
- **UI Response**: `PauseManager` (listening to state change or specific event).
  - Action: Calls `ClosePauseMenu()`.   

# Typical Gameplay Flow

## 1. Main Menu → New Game
- **Trigger**: User selects "New Game".
- **Action**: `SceneRouter` requests `GameState.NewGame`.
- **Director Actions**:
  1. Loads `IntroScene` (or `GameplayScene`) additively.
  2. Unloads `MainMenuScene`.
  3. Sets `GameplayScene` as active.

## 2. Gameplay → Pause
- **Trigger**: Input triggers `GameState.Paused`.
- **Action**: `PauseManager` (in `GameplayScene`) enables Canvas.
- **Note**: No scene load/unload occurs.

## 3. Pause → Options
- **Trigger**: Input triggers `GameState.OptionsMenu`.
- **Director Actions**:
  1. Loads `OptionsScene` additively over `GameplayScene`.
  2. Sets `OptionsScene` as active (for UI input).
- **State**: `GameplayScene` (with Pause Menu) remains loaded but inactive.

## 4. Options → Pause (Back)
- **Trigger**: Input triggers "Back".
- **Director Actions**:
  1. Unloads `OptionsScene`.
  2. Restores `GameplayScene` as active.
- **Result**: `PauseManager` Canvas becomes visible again.

## 5. Pause → Resume
- **Trigger**: Input triggers `GameState.Playing`.
- **Action**: `PauseManager` disables Canvas.
- **Note**: No scene load/unload occurs.

## 6. Level Complete → Next Level
- **Trigger**: Level completion logic.
- **Action**: `SceneRouter` requests `GameState.LoadSavedGame` (or similar).
- **Director Actions**:
  1. Loads `NextLevelScene` additively.
  2. Unloads `CurrentGameplayScene`.
  3. Sets `NextLevelScene` as active.   



DETAILS 
Complete Flow: Gameplay → Pause → Options → Pause → Resume
Player presses Escape in Gameplay:
GameStateInputHandler detects Escape → stateMachine.RequestState(GameState.Paused).
GameStateMachine sets CurrentState = Paused → Raises onTimeScalePauseEvent (Time freezes).
PauseManager (listening to onEscapePressed or similar) → Calls OpenPauseMenu() (Fades in Canvas). 
Player clicks "Options" in Pause Menu:
PauseManager.OnOptionsButtonClicked() → Raises openSettingsFromPauseEvent with OptionsSourse.PauseMenu.
OptionsMenuContextManager.HandleOptionsOpen(PauseMenu) → Sets _currentState = PauseMenu.
OptionsMenuContextManager → Raises onOptionsOpenedEventChannel.
GameStateInputHandler → stateMachine.RequestState(GameState.OptionsMenu).
GameStateSceneRouter.OnStateChange(OptionsMenu):
Saves _gameStateBeforeOptions = GameState.Paused.
Calls transitionDirector.LoadOptionsMenuOverlayAsync().
SceneTransitionDirector:
Captures _previousSceneBeforeOptions = GameplayScene.
Loads OptionsScene additively.
Sets OptionsScene as active (for UI input). 
Player clicks "Back" in Options Menu:
OptionsManager.OnBackClicked() → Raises onOptionsBackRequetEventChannel.
OptionsMenuContextManager.OnOptionsBackRequested() → Calls HandleBackLogic().
HandleBackLogic() → Raises ONLY closeOptionsRequestEvent.
GameStateSceneRouter.OnCloseOptionsRequested() → Starts CloseOptionsMenuHandler().
CloseOptionsMenuHandler():
Yields transitionDirector.CloseOptionsMenuAsync().
SceneTransitionDirector.CloseOptionsMenuAsync():
Unloads OptionsScene.
Restores GameplayScene as active.
(Does NOT raise onOptionsClosedEvent because source was Pause, not MainMenu).
GameStateSceneRouter resumes → Calls stateMachine.SilentlySetState(GameState.Paused).
Result: Options closed, Gameplay scene active, Pause Menu still visible, Time still paused. 
Player clicks "Resume" in Pause Menu:
PauseManager.OnResumeButtonClicked() → Raises onResumeRequested.
(You need a listener, e.g., GameStateInputHandler or a dedicated PauseListener, to catch this and call stateMachine.RequestState(GameState.Playing)).
GameStateMachine → Sets CurrentState = Playing → Raises onTimeScaleResumeEvent.
PauseManager (listening to state change or a specific event) → Calls ClosePauseMenu(). 



Requirement	Current Implementation	
Load Gameplay Additively	TransitionCoroutine uses LoadSceneMode.Additive with allowSceneActivation = false. 	Correct (Prevents premature execution). 
Wait for Load Complete	Coroutine waits for asyncOp.progress >= 0.9f then subscribes to SceneManager.sceneLoaded. 	Correct (Ensures assets are ready). 
Spawn & Activate	sceneLoaded callback handles spawning, then SceneLoaderService.SetActiveAndFixEventSystem sets the new scene active. 	Correct (Ensures input/lighting context). 
Unload Previous Gameplay	UnloadOldScenes iterates all scenes and unloads anything that isn't PersistentManagers or the NewScene. 	Correct (Cleans memory safely). 
Pause/Options Overlay	Options loads additively over the active Gameplay scene; Pause is a Canvas in the Gameplay scene. 	Correct (No scene unload required for overlays). 