Here's a PROJECT.md for your documentation folder:

# BoneYardEscape — Project Documentation

**Engine:** Unity 6.3 LTS
**Language:** C#
**Genre:** 2D Platformer (single player, upgrades, objectives, collectibles)
**Last Updated:** September 22, 2026

---

## Tech Stack

| Library | Version | Purpose |
|---------|---------|---------|
| VContainer | 1.19 | Dependency Injection |
| MessagePipe | 8.2 | Global message bus (no singletons) |
| UniTask | — | Async/await |
| LeanTween | — | UI animations |
| NUnit (Test Framework) | 1.6.0 | Unit tests |

---

## Assembly Structure

| Asmdef | Namespace | Contents |
|--------|-----------|----------|
| `Core` | `Game.Core` | Enums, interfaces, shared data, messages |
| `Bootstrap` | `Game.Bootstrap` | Flow orchestration, state machines, scene loading |
| `Gameplay` | `Game.Gameplay` | Gameplay systems (save, player, audio, video) |
| `UI` | `Game.UI` | UI MonoBehaviours |
| `Editor` | `Game.Editor` | Editor-only scripts (`UNITY_EDITOR`) |
| `Tests.EditMode` | `Game.Tests` | Unit tests (`UNITY_INCLUDE_TESTS`) |

---

## Scene Structure

| Index | Scene | Purpose |
|-------|-------|---------|
| 0 | Bootstrap | Persistent. UI Canvas, managers, camera, cutscene player |
| 1 | IntroScene | Intro video (VideoPlayerMaster) |
| 2 | MainMenu | MenuMaster, BootTextController, CRTBootAnimation |
| 3+ | GameLoop / Levels | Gameplay content, LevelLoader targets |

**Rule:** Bootstrap is always loaded. All other scenes load/unload additively.

---

## VContainer Scope Hierarchy

ProjectLifetimeScope (Bootstrap, persistent)
├── All flow systems (GameFlowSystem, GameStateMachine, SceneLoader, etc.)
├── Audio (AudioManager, AudioView, AudioMixerMaster)
├── Save (SaveManager, ISaveableRegistry)
├── Cutscenes (CutsceneSelector, CutscenePlayer)
├── Analytics (AnalyticsReporter, IAnalyticsReporter)
└── UI Controllers (LoadingPanel, Pause, Options, CRT, etc.)

LevelLifetimeScope (per-level, child of Project)
├── Player (PlayerFactory, PlayerSpawnerService)
├── Upgrades (UpgradeStateManager, UpgradeOrchestrator, handlers)
├── Achievements (ObjectiveTracker, CheckpointManager)
├── Currency (CurrencyManager)
└── Scene-specific components


**Lifetime rules:**
- `Lifetime.Scoped` — dies with its scope (most systems)
- `Lifetime.Singleton` — survives scope disposal (use sparingly, prefer Scoped)
- `Lifetime.Transient` — new instance per resolve (factories)

---

## Message Bus (MessagePipe)

All inter-system communication via `GlobalMessagePipe`. No direct class-to-class references for game flow.

**Initialization (ProjectLifetimeScope):**
```csharp
builder.RegisterMessagePipe(o => o.EnableCaptureStackTrace = true);
builder.RegisterBuildCallback(resolver =>
    GlobalMessagePipe.SetProvider(resolver.AsServiceProvider()));

Key Messages:

Message	Direction	Purpose
GameStateChanged	GameStateMachine → all	State transition occurred
GamePhaseChangedMessage	GamePhaseController → all	Phase changed
StartGameRequested	UI → GameFlowSystem	New Game selected
PauseGameRequested / ResumeGameRequested	Input → GameFlowSystem	Pause/Resume
LoadRequest	UI → GameFlowSystem	Load Game selected
VideoFinished	VideoPlayerMaster → GameFlowSystem	Intro/cutscene done
VideoSkipRequested	UI → VideoPlayerMaster/CutscenePlayer	Skip video
SceneTransitionStarted / Completed	Orchestrator → all	Transition lifecycle
SceneLoaded / SceneUnloaded / SceneActivated	SceneLoader → all	Scene lifecycle
NavigateToMenu	GameFlowSystem → Router	Return to menu
SaveRequest / SaveCompleted / SaveFailed	SaveManager → all	Save lifecycle
LoadCompleted / LoadFailed	SaveManager → all	Load lifecycle
PlayerDied	Gameplay → GameFlowSystem	Player death
ClimaxTriggered / ClimaxCompleted	Gameplay → GameFlowSystem	Climax sequence
SceneLoadProgress	SceneLoader → LoadingPanel	Progress bar update
TimeScalePause / TimeScaleResume	GameStateMachine → Audio	Music ducking

Message pattern:

public readonly struct SomeMessage
{
    public SomeProperty { get; }
    public SomeMessage(params) { ... }
}

Code Conventions
Class Structure
namespace Game.XXX
{
    /// <summary>
    /// One-line description.
    /// </summary>
    public class ClassName : IStartable, IDisposable
    {
        #region Dependencies
        // Injected fields, publishers, subscribers
        #endregion

        #region State
        // Private state fields
        #endregion

        #region Construction
        [Inject]
        public ClassName(Deps) { ... }
        #endregion

        #region IStartable
        void IStartable.Start() { ... }
        #endregion

        #region Public API
        #endregion

        #region Message Handlers
        #endregion

        #region Internal
        #endregion

        #region Cleanup
        public void Dispose() { ... }
        #endregion
    }
}

Rules
No singletons. No FindObjectOfType. No DontDestroyOnLoad.
No FindObjectsByType at runtime. Editor-only is fine.
_disposed guard at top of every public method.
CancellationTokenSource for all async work, cancelled in Dispose().
Subscriptions stored in List<IDisposable>, disposed in Dispose().
IStartable.Start() is explicitly implemented (not public).
#if UNITY_EDITOR || DEVELOPMENT_BUILD for all debug/perf code.
Fail-fast DI: no null guards on required [SerializeField] references.
XML doc summary on every class.
DI Registration Pattern
// Plain C#:
builder.Register<ClassName>(Lifetime.Scoped)
    .As<IStartable>()
    .As<IDisposable>();

// MonoBehaviour in scene:
builder.RegisterComponentInHierarchy<ClassName>()
    .AsImplementedInterfaces();

// MonoBehaviour from prefab/scene reference:
builder.RegisterComponent(myReference);

// ScriptableObject:
builder.RegisterInstance(myAsset);

Game Flow
States
MainMenu → Loading → Gameplay → Paused → Gameplay
                         ↓
                     GameOver → MainMenu
MainMenu → IntroVideo → Loading → Gameplay
MainMenu → QuitGame

Boot Sequence
1. Unity loads Bootstrap (index 0)
2. ProjectLifetimeScope.Awake() → builds container
3. IStartable.Start() fires on all registered services
4. GameFlowSystem reads GameFlowConfig.initialState
5. ForceState(initialState) → router loads correct scene
6. If IntroVideo: video plays → VideoFinished → MainMenu
7. If MainMenu: InitialLoadAsync → NavigateToMenu → MainMenu loads

Transition Pattern (DRY)
Guard (CanTransitionTo Loading)
  → ForceState(Loading)
  → await loadAction()
  → TransitionTo(targetState)
  → SetPhase(targetPhase)
  → SwitchTo(targetState)
  → [on failure] ForceState(recoveryState)

Recovery
Gameplay/Paused → recovers to MainMenu (scene may be corrupted)
IntroVideo/MainMenu/Loading → recovers to previous state (scene intact)
QuitGame → recovers to MainMenu
Save System
Format: JSON + encryption (placeholder), key-value string dict
Versioning: int version field in SaveDataContainer
Current version: 1
Migration: SaveMigration.Migrate(data, fromVersion, toVersion) — chain of if (v < N) blocks
ISaveable: any system that needs to persist implements ISaveable, registers in ISaveableRegistry
Save trigger: SaveRequest message (immediate or delayed 1s)
Load trigger: LoadRequest message
Audio Architecture
Layer	Class	Responsibility
Orchestration	AudioManager	WHAT plays, WHEN. Subscribes to messages.
Execution	AudioView	AudioSource pools. Raw clip playback.
Mixing	AudioMixerMaster	Normalized (0–1) → dB. Pushes to AudioMixer.
Settings UI	AudioSettingsUI	Text-based bars. Talks to IAudioMixer for volume.

MusicType enum: None, MainMenu, Settings, Pause, Gameplay, Climax, GameOver, Ending

Volume flow: AudioSettingsUI → IAudioMixer.MusicVolume = x → AudioMixerMaster setter → _mixer.SetFloat(param, dB)

Video / Cutscene System
Class	Purpose
VideoPlayerMaster	Intro video. ISaveable (HasPlayed). External audio support.
CutscenePlayer	Reusable full-screen cutscene. PlayAsync(clip) → UniTask.
CutsceneSelector	Maps context → VideoClip from CutscenePoolSO.
CutscenePoolSO	ScriptableObject. Pools for NewGame, per-Level.
VideoSkipButton	Bootstrap UI. Bl