# M7 — Unity 2D Platformer (Continuation Prompt)

## Project
- Unity 6.3 LTS, URP
- VContainer 1.19, MessagePipe 1.8.2, UniTask, LeanTween
- Genre: 2D vintage/creepy platformer ("M7" — synthetic optics HUD)
- Tone: Creepy with moments of fear. NOT strict horror.
- All messaging via GlobalMessagePipe. No ISignalBus, no GameEventSO, no Unity Events for system-to-system.

## Folder Structure & Namespaces

Core: Assets/Game/Core/ → namespace Game.Core.*
Bootstrap: Assets/Game/Bootstrap/ → namespace Game.Bootstrap
Gameplay: Assets/Game/Gameplay/ → namespace Game.Gameplay.*
UI: Assets/Game/UI/ → namespace Game.UI

Subfolders do NOT affect namespace.

## MessagePipe Pattern (GOLD STANDARD)

For MonoBehaviours (scene-placed or runtime-spawned):
- Publishers/Subscribers: NOT readonly, cached in IStartable.Start() via GlobalMessagePipe.Get*()
- Subscribe in IStartable.Start()
- Storage: List<IDisposable> _subscriptions
- Unsubscribe: IDisposable.Dispose() with _disposed guard
- Safety net: OnDestroy() calls Dispose() if !_disposed
- Parameterless: .Publish(default). Data-carrying: .Publish(new T(data))

For VContainer services (non-MonoBehaviour):
- Empty or service-only constructor (NO IPublisher/ISubscriber params)
- Cache via GlobalMessagePipe in IStartable.Start()
- Fields are NOT readonly

NEVER:
- IPublisher<T>/ISubscriber<T> as constructor params
- GlobalMessagePipe.Get*() in constructor (MonoBehaviour or service)
- System.Threading.Tasks (use UniTask)
- Coroutines for async logic (UniTask only; coroutines only for scene loading)
- LeanTween.alpha(Image/CanvasGroup) — always use .gameObject
- ISignalBus or GameEventSO

## VContainer Setup (ProjectLifetimeScope)

builder.RegisterMessagePipe(o => o.EnableCaptureStackTrace = true);
builder.RegisterBuildCallback(resolver => {
    GlobalMessagePipe.SetProvider(resolver.AsServiceProvider());
});

## M7 HUD (Synthetic Optics)

### Design Philosophy
- HUD is M7's internal display. Minimal. Mechanical. Not sleek sci-fi.
- Invisible when working, visible when failing.
- No minimap. No health bar. No inventory. No labels. No tooltips. No boxes. No borders.
- HUD font: Sans-serif monospace (IBM Plex Mono / JetBrains Mono). Small.
- HUD color: #E0F0FF at 80% opacity.
- World font (artifact text only): Serif, typewriter-style. Warm amber (#FFD080).
- All fades: 0.3s ease-in-out. Typewriter: 30ms/char.
- The absence of UI IS the confirmation.

### HUD Hierarchy

HUD (Canvas, Screen Space - Overlay)
│ Components: Canvas, CanvasScaler(1920×1080, 0.5), GraphicRaycaster, CanvasGroup,
│ HudController, HudManager, FlinchHUDController, HUDPauseController, ObjectivePulseController
│
├── GuidanceText (bottom-center)
│ Components: CanvasGroup, TextMeshProUGUI(#E0F0FF, 80%), GuidanceTextController
│ └── CodeFragment (INACTIVE) → TextMeshProUGUI(monospace, slightly smaller)
│
├── SignalBars (top-right)
│ Components: CanvasGroup, SignalBarsController, HorizontalLayoutGroup(2px)
│ ├── Bar1 → Image(8×12px, #E0F0FF, alpha 0)
│ ├── Bar2 → Image
│ ├── Bar3 → Image
│ └── Bar4 → Image
│
├── OrbCount (bottom-right)
│ Components: CanvasGroup, TextMeshProUGUI(#E0F0FF, 80%), OrbCountController
│ (color flash: Humanoid=#FFD080, NonHumanoid=#80FFD0, 0.5s fade back)
│
└── VignetteOverlay (full-screen stretch)
Components: CanvasGroup, DamageVignetteController
├── Vignette → Image(radial gradient, transparent center→red edges, alpha 0)
└── Noise → Image(static texture, alpha 0)


### HUD Messages (Game.Core.Messages)
- GuidanceReceivedMessage { string Text; float Duration; }
- GuidanceSilentMessage { }
- SignalStrengthChangedMessage { int Bars; } // 0-4
- OrbCollectedMessage { OrbType Type; int NewTotal; }
- PlayerDamagedMessage { float DamageAmount; bool IsNearDeath; }
- CodeFragmentDisplayedMessage { string Fragment; }
- FlinchTriggeredMessage { }
- GamePhaseChangedMessage { GamePhase NewPhase; }
- OrbPickedUp { OrbType Type; } // internal, consumed by OrbCounter only
- StaminaChanged { float Current; float Max; }
- LevelTimerUpdated { float Time; }
- ObjectiveProgress { int Collected; int Total; }

### Enums
```csharp
[Flags] enum GamePhase { None=0, Letter=1, Gameplay=2, Climax=4, Ending=8, PostCredits=16 }
enum OrbType { Humanoid, NonHumanoid }

HUD Classes (all in Game.UI)
Class	Role
HudController	Root. Subscribes GamePhaseChangedMessage. Shows/hides via HudManager.
HudManager	Generic panel fade. ShowPanel/HidePanel/ShowPanels/HidePanels. Auto-hide.
GuidanceTextController	Typewriter(30ms). Code fragment overlay(2s). Fade out on silent.
SignalBarsController	4 bars. Flicker on change(0.05s).
OrbCountController	Number + color flash by type.
DamageVignetteController	Normal: 0.4→0(0.5s). Near-death: 0.6+hold+noise 1-frame.
FlinchHUDController	IsFrozen flag. Flicker(0.1s). Freeze 1.5s.
HUDPauseController	Escape→timeScale=0+fade out. Unpause→300ms handshake+fade in. Long-press(2s)→Application.Quit(). Uses InputManager.
ObjectivePulseController	Climax→CodeFragment. Signal 0→1→re-trigger guidance.

MainMenu (Merged with Options)
Hierarchy
MainMenu (Canvas)
│  Components: Canvas, CanvasScaler, GraphicRaycaster, MenuMaster
│
├── MainMenuPanel (EMPTY)
│   Components: CanvasGroup, BootTextController
│   └── BootText → TextMeshProUGUI (monospace, #E0F0FF, 80%, Rich Text ON)
│
└── OptionsPanel (EMPTY, INACTIVE)
    Components: CanvasGroup(alpha 0)
    ├── OptionsBackground → Image(#000, 70%)
    ├── OptionsText → TMP("CONFIGURATION")
    ├── VolumeSlider → Slider + TMP
    ├── MusicSlider → Slider + TMP
    ├── SFXSlider → Slider + TMP
    ├── ControllerRemap → TMP + Button("REMAP")
    └── BackButton → Button + TMP("< BACK")

MenuMaster
Replaces old MainMenuView
Subscribes GamePhaseChangedMessage (shows on MainMenu, hides on Gameplay/Climax/Ending)
Button routing: NewGame→IntroVideo, LoadGame→Loading, Options→panel toggle, Exit→QuitGame
Options: SetActive + CanvasGroup alpha fade (0.3s). No scene load.
Escape during Options: closes panel
BootTextController: types lines(30ms/char, 0.8s pause). Up/Down navigates. Enter confirms. > blinks at 1Hz on selected line. Skip on click.
BootTextController lines
[0] "M7 OPTICS v2.4.1"
[1] "SIGNAL: ACTIVE"
[2] "SUBJECT: PRESENT"
[3] ""
[4] "> NEW SESSION"
[5] "> RESUME SESSION"
[6] "> CONFIGURATION"
[7] "> TERMINATE"

Game Flow
GameState Enum (Final)
enum GameState { Boot, IntroVideo, MainMenu, Loading, Gameplay, Paused, GameOver, QuitGame }

(OptionsMenu, Playing, NewGame, LoadSavedGame were removed)

New Game Flow
"NEW SESSION" → SetState(IntroVideo)
    → SceneRouter → HandleIntroAsync()
    → Intro video plays
    → VideoFinished → GameFlowSystem.OnVideoFinished()
        → GamePhaseController.SetPhase(Gameplay)
        → HUD fades in

Load Game Flow
"RESUME SESSION" → LoadRequest → GameFlowSystem.OnLoadAsync()
    → SaveManager.LoadAsync()
    → LevelLoader.LoadLevelAsync()
    → SetState(Gameplay)
    → GamePhaseController.SetPhase(Gameplay)
    → HUD fades in immediately

Death Flow
PlayerDied → GameFlowSystem → SetState(GameOver) + SetPhase(Ending) → HUD hidden

Key Classes
Class	Role
GameStateMachine	Sole publisher of GameStarted/Paused/Resumed + TimeScale + GameStateChanged
GamePhaseController	Owns GamePhase. Publishes GamePhaseChangedMessage.
GameFlowSystem	Orchestrates NewGame/Load/Pause/Resume/Death. Calls LevelLoader, SaveManager, GamePhaseController. Subscribes VideoFinished, PlayerDied, LoadRequest.
GameStateSceneRouter	Routes state to scene transitions (MainMenu, Intro, Quit). No OptionsMenu case.
SceneTransitionOrchestrator	Fade out→swap→fade in.
SceneLoaderService	Load/Unload/Activate. FixDuplicateEventSystems.
SceneCleanupService	Unloads obsolete scenes.
SceneFadeManager	FadeOutAsync/FadeInAsync (LeanTween + UniTaskCompletionSource).
LevelLoader	Creates LevelLifetimeScope, loads scenes, spawns HUD, calls InjectGameObject.

Gameplay Systems
Class	Namespace	Role
InputManager	Game.Gameplay.Player	MonoBehaviour. InputSystem. Exposes snapshot props. Publishes InputBackPressed. Has EscapeWasPressed, EscapeIsHeld, MoveWasPressedUp/Down, ConfirmWasPressed.
PlayerSpawnerService	Game.Gameplay.Player	Spawns/respawns. Lazy-caches doors.
PlayerStateDriverShell	Game.Gameplay.Player	MonoBehaviour wrapper. Caches publishers in [Inject] Initialize(). Drives PlayerController.
PlayerController	Game.Gameplay.Player	Owns Ctx + StateMachine. Handles BouncePlatformHit.
PlayerContext	Game.Gameplay.Player	Pure data.
PlayerDependencies	Game.Gameplay.Player	Pure data.
PlayerHSMBuilder	Game.Gameplay.Player	Factory. No GlobalMessagePipe calls (receives publishers as params).
PlayerHealth	Game.Gameplay.Player	IDamagable + ISaveable + IStartable. Publishes EntityHealthChanged, PlayerDamaged, PlayerDied. Registers with ISaveableRegistry.
PlayerUpgradesManager	Game.Gameplay.Upgrades	IStartable + ITickable + ISaveable. Replaces old UpgradeOrchestrator + UpgradeStateManager. Publishes CodeFragmentDisplayedMessage on pickup.
AchievementManager	Game.Gameplay.Achievements	IStartable + ITickable + ISaveable. SILENT — no HUD feedback. Publishes AchievementUnlocked (data only).
ObjectiveTracker	Game.Gameplay.Achievements	IStartable + IDisposable. SILENT. Tracks progress. Publishes NextAchievementRevealed / AchievementUnlocked.
CheckpointManager	Game.Gameplay.World	MonoBehaviour + ISaveable. [SerializeField] ICheckpoint[]. Defers Activate() to IStartable.Start().
OrbPickup	Game.Gameplay.World	Trigger. Publishes OrbPickedUp. Destroys self.
OrbCounter	Game.Gameplay.World	IStartable + ISaveable. Subscribes OrbPickedUp, increments, publishes OrbCollectedMessage with NewTotal.
SaveManager	Game.Gameplay.Save	ISaveableRegistry pattern. Collects all saveables.
ISaveableRegistry	Game.Core.Interfaces	Mutable list. Register/Unregister/GetAll.

Save System
SaveManager uses ISaveableRegistry (NOT IEnumerable in constructor)
Level-scoped saveables register/unregister in IStartable.Start() / Dispose()
SaveManager.LoadAsync() runs BEFORE scene loads → LoadFromData must NOT touch scene objects
CheckpointManager defers visual Activate() to IStartable.Start()
What Was DELETED (do not recreate)
NotificationView, NotificationPanel, NotificationRequested, NotificationData, NotificationType
ObjectivesManager, ObjectiveItem
PauseManager, PauseMenuTabs, PauseMenu prefab
MainMenuView, MainMenuFader, MenuNavigationHighlighter
GameManagerService, GameManagerInitialized
CloseOptionsMenuRequested, OptionsClosed messages
Options scene (merged into MainMenu)
GameState.OptionsMenu enum value
SceneRegistry.OptionsMenuScene
PopMenuAsync (if nothing else calls it)
UpgradeOrchestrator, UpgradeStateManager (replaced by PlayerUpgradesManager)
GetNotificationData() on PlayerUpgrade
IsComplete, Icon on Achievement struct
Registration
ProjectLifetimeScope
GameStateMachine: Singleton, AsSelf + As
GamePhaseController: Singleton, AsSelf + As
GameFlowSystem: Singleton, AsImplementedInterfaces + AsSelf
GameStateSceneRouter: Singleton, AsSelf + AsImplementedInterfaces
SceneTransitionOrchestrator: Singleton, AsSelf + AsImplementedInterfaces
SceneLoaderService: Singleton, AsSelf + AsImplementedInterfaces
SceneCleanupService: Singleton, AsSelf
SaveManager: Singleton, AsSelf + AsImplementedInterfaces
SaveableRegistry: Singleton, As
LevelLoader: RegisterComponent
SceneFadeManager: RegisterComponent
InputManager: RegisterComponentInHierarchy (or on player prefab)
EventSystem: RegisterInstance (dedup + DontDestroyOnLoad)
LevelLifetimeScope
PlayerPrefabReference: Scoped
PlayerFactory: Scoped, As
PlayerSpawnerService: Scoped, AsSelf + As + As
PlayerUpgradesManager: Scoped, AsImplementedInterfaces + AsSelf
AchievementManager: Scoped, AsImplementedInterfaces + AsSelf
ObjectiveTracker: Scoped, AsSelf + As + As
CheckpointManager: RegisterComponentInHierarchy, AsImplementedInterfaces + AsSelf
OrbCounter: Scoped, AsSelf + AsImplementedInterfaces
CurrencyManager: Singleton, AsSelf + As + As
InvisibilityInputHandler: RegisterComponentInHierarchy
InvisibilityVisualPresenter: RegisterComponentInHierarchy
Key Design Decisions
GameManagerService ELIMINATED. GameStateMachine is sole publisher of GameStarted/Paused/Resumed
GamePhaseController separate from GameStateMachine (phase drives HUD visibility, state drives game logic)
No NotificationView. Upgrades → CodeFragmentDisplayedMessage. Achievements → SILENT.
No PauseMenu. Pause = timeScale 0 + Canvas fade. Long-press Escape = quit.
OptionsMenu is a panel in MainMenu scene (no separate scene, no scene load)
MenuMaster + BootTextController replace MainMenuView
OrbPickup → OrbPickedUp (internal) → OrbCounter → OrbCollectedMessage (HUD)
PlayerUpgradesManager is a single class (replaced Orchestrator + StateManager)
Achievement struct is immutable data. Progress lives in ObjectiveTracker.
VideoPlayerMaster only publishes VideoFinished. Phase transition owned by GameFlowSystem.
HUDPauseController uses InputManager.EscapeWasPressed (single input source).
What's Remaining / TODO
[ ] BombUpgradeHandler review
[ ] InvisibilityInputHandler / InvisibilityVisualPresenter review
[ ] CurrencyManager review
[ ] PlayerHSM states (GroundedState, AirborneState, etc.)
[ ] SignalStrength publisher (SignalTracker — proximity-based, ITickable)
[ ] GuidanceReceivedMessage publisher (narrative/dialogue system)
[ ] FlinchTriggeredMessage publisher (glitch system)
[ ] CodeFragmentDisplayedMessage publisher (narrative system)
[ ] GameOver flow (death screen → respawn or menu)
[ ] Ending / PostCredits phase handling
[ ] AES encryption in SaveManager (currently placeholder)
[ ] OptionsPanel wiring (sliders → AudioManager, controller remap)
[ ] BootTextController: verify InputManager has MoveWasPressedUp/Down, ConfirmWasPressed
[ ] MenuMaster: verify _bootText.IsSelecting property exists
[ ] MainMenu scene: create Background + FilmGrain renderers
[ ] HUD scene: create VignetteGradient + NoiseStatic textures
[ ] HUD scene: import IBM Plex Mono font + create TMP Font Asset
Instructions for Continuation
Ask me which class/task to work on next (or wait for me to paste it)
For each class I provide:
a. Define strongly-typed readonly struct messages in Game.Core.Messages (if new)
b. Refactor publishers to use cached GlobalMessagePipe.GetPublisher().Publish(...)
c. Refactor subscribers to use cached GlobalMessagePipe.GetSubscriber().Subscribe(...) with List storage
d. Ensure all async code uses UniTask (no System.Threading.Tasks)
e. Provide exact, compilable code
f. State the file path and namespace
STOP after each class and wait for "Ready for next step"
Do NOT suggest removing GlobalMessagePipe until I confirm 100% migration
Keep every file under 200 lines
Add a one-line comment at the top of every file explaining what it does
Do NOT use Coroutines for async logic (UniTask only; coroutines only for scene loading)
Do NOT use Animator for HUD (LeanTween only)
Do NOT add labels, tooltips, minimap, health bar, or traditional pause menu to HUD
Do NOT create NotificationView, NotificationPanel, PauseMenu, or any toast/popup system
LeanTween.alpha() takes GameObject, NOT Image or CanvasGroup — always use .gameObject
The tone is "creepy with moments of fear" — NOT strict horror. UI should feel calm, not punitive.