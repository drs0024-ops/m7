# Project Architecture & Onboarding

> **Purpose:** This document is for "future you." If you're reading this after a break,
> it should re-orient you in under 10 minutes. Every architectural decision is recorded
> with its rationale so you don't second-guess your past self.

---

## 1. Project Overview

- **Game:** 2D platformer, single player, upgrades, objectives, collectibles
- **Engine:** Unity 6.3 LTS, C#
- **Team:** Solo dev, small-studio quality bar
- **Target:** Steam

### Tech Stack

| Layer | Library | Version |
|-------|---------|---------|
| DI | VContainer | 1.19 |
| Messaging | MessagePipe (GlobalMessagePipe) | 8.2 |
| Async | UniTask | — |
| UI Animation | LeanTween | — |
| Camera | Cinemachine | 3.x |
| Input | Unity Input System | — |
| Testing | NUnit / Unity Test Framework | 1.6.0 |
| Rendering | URP (custom CRT shader) | — |

---

## 2. Assembly Structure

| Assembly | Namespace | Contents |
|----------|-----------|----------|
| `Core` | `Game.Core` | Shared data, enums, interfaces, messages |
| `Bootstrap` | `Game.Bootstrap` | Flow orchestration, state machines, scene loading, scopes |
| `Gameplay` | `Game.Gameplay` | Gameplay systems (save, player, audio, camera, world) |
| `UI` | `Game.UI` | UI MonoBehaviours (dialogue, settings, CRT, loading) |
| `Editor` | `Game.Editor` | Editor-only scripts (`defineConstraints: ["UNITY_EDITOR"]`) |
| `Tests.EditMode` | `Game.Tests` | Unit tests (`defineConstraints: ["UNITY_INCLUDE_TESTS"]`) |

**Dependency rule:** Core ← everything. Bootstrap ← Gameplay, UI. Gameplay ← Core. UI ← Core. Editor ← all. Tests ← all.

---

## 3. Scope Hierarchy

ProjectLifetimeScope (Bootstrap scene, persistent)
├── Flow: GameFlowSystem, GameStateMachine, SceneLoader, SceneTransitionOrchestrator
├── Audio: AudioManager, AudioView, AudioMixerMaster
├── Save: SaveManager, ISaveableRegistry (Lifetime.Singleton)
├── Cutscenes: CutsceneSelector, CutscenePlayer
├── Analytics: AnalyticsReporter
├── UI: LoadingPanel, Pause, Options, CRT, Dialogue
├── Input: InputManager (IInputSwitcher)
├── Camera: Physical Camera + CinemachineBrain (persistent)
└── Settings: SettingsSavable (Lifetime.Singleton)

└── FacilitySceneLifetimeScope (per-level scene)
├── Player: PlayerFactory, PlayerSpawnerService
├── Camera: CameraConfig, CameraSwitcher, CameraTargetManager,
│ CameraEffectController, CameraPanMover, SceneCameraSetup,
│ CameraFollowObject, CinemachineImpulseSource
├── World: NPC, DialogueSeenTracker, CheckpointManager
└── Triggers: CameraZoneTrigger, CameraPanTrigger, CameraShakeTrigger

    └── LevelLifetimeScope (per-level, child of Facility)
        ├── Upgrades: UpgradeStateManager, UpgradeOrchestrator, handlers
        ├── Achievements: ObjectiveTracker, CheckpointManager
        └── Currency: CurrencyManager


### Key Rules

| Rule | Why |
|------|-----|
| LevelLifetimeScope instantiated **after** `LoadSceneAsync` completes | Components must exist before `RegisterComponentInHierarchy` runs |
| `ISaveableRegistry` is `Lifetime.Singleton` in Project | Child scopes resolve it via parent |
| `RegisterComponentInHierarchy` only finds components in the **active** scene at build time | Additive scene components need explicit `RegisterComponent` |
| `DontDestroyOnLoad` only on the Bootstrap scene root | Everything else is managed by scope lifetime |

---

## 4. Core Patterns

### 4.1 DI Registration

```csharp
// Plain C# system:
builder.Register<ClassName>(Lifetime.Scoped)
    .As<IInterface>()
    .As<IInitializable>()
    .As<IDisposable>()
    .AsSelf();  // ← required if you Resolve<T> by concrete type

// MonoBehaviour in scene:
builder.RegisterComponentInHierarchy<ClassName>()
    .AsImplementedInterfaces()
    .AsSelf();

// MonoBehaviour from prefab reference:
builder.RegisterComponent(myReference);

// ScriptableObject / data:
builder.RegisterInstance(myAsset);

4.2 Messaging (GlobalMessagePipe)
// Messages are readonly structs in Game.Core.Messages
public readonly struct MyMessage
{
    public readonly float Value;
    public MyMessage(float value) => Value = value;
}

// Publisher (in Start or Initialize):
_pub = GlobalMessagePipe.GetPublisher<MyMessage>();
_pub.Publish(new MyMessage(42f));

// Subscriber (in Start or Initialize):
_sub = GlobalMessagePipe.GetSubscriber<MyMessage>();
_disposables.Add(_sub.Subscribe(OnMyMessage));

// Dispose:
foreach (var d in _disposables) d.Dispose();

Rule: All inter-system communication via messages. No direct class-to-class references between systems. UI→Controller via [SerializeField] is acceptable (view owns its controller).

4.3 Code Style (non-negotiable)
Rule	Example
Region blocks	#region Dependencies, #region State, #region Public API, #region Message Handlers, #region Internal, #region Cleanup
XML doc on every class	/// <summary>...</summary>
_isDisposed guard	Check at top of every public method
CancellationTokenSource for async	Cancelled in Dispose()
No singletons	Ever
No FindObjectOfType at runtime	Editor-only is fine
No DontDestroyOnLoad except Bootstrap root	Scope lifetime handles persistence
Fail-fast on [SerializeField]	throw new InvalidOperationException(...) not null-guard
#if UNITY_EDITOR || DEVELOPMENT_BUILD	For any debug/perf code
No null guards on DI-injected deps	If it's null, the wiring is broken — crash loudly

4.4 IInitializable Pattern
Plain C# classes (not MonoBehaviours) that need to subscribe to messages:

public class MySystem : IInitializable, IDisposable
{
    private ISubscriber<SomeMessage> _sub;
    private readonly List<IDisposable> _disposables = new();

    [Inject]
    public MySystem(Dependency dep) { ... }

    public void Initialize()
    {
        _sub = GlobalMessagePipe.GetSubscriber<SomeMessage>();
        _disposables.Add(_sub.Subscribe(OnMessage));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        for (int i = 0; i < _disposables.Count; i++)
            _disposables[i].Dispose();
        _disposables.Clear();
    }
}

Registered with:

builder.Register<MySystem>(Lifetime.Scoped)
    .As<IInitializable>()
    .As<IDisposable>()
    .AsSelf();

builder.RegisterBuildCallback(container =>
{
    container.Resolve<MySystem>().Initialize();
});

5. System Overviews
5.1 Camera
Architecture: Data-driven, per-scene config, zero coupling between classes.

ProjectLifetimeScope:
  MainCamera (Camera + CinemachineBrain + CameraPerfHook + Volume)
  CinemachineBlenderSettings (asset, assigned to Brain)

FacilitySceneLifetimeScope (per-scene):
  CameraConfig (MonoBehaviour, holds List<CameraRigEntry>)
  CameraSwitcher (plain C#, IInitializable — enables/disables vCams)
  CameraTargetManager (plain C#, IInitializable — applies follow/boundary)
  CameraEffectController (MonoBehaviour — shake + damping)
  CameraPanMover (MonoBehaviour — TargetOffset tween)
  SceneCameraSetup (MonoBehaviour — initial setup orchestration)
  CameraFollowObject (MonoBehaviour — mirrors player position)
  CinemachineImpulseSource (on CameraEffects GO)

Messages:

Message	Publisher	Subscriber
CameraSwitchRequest	Triggers, DebugWindow	CameraSwitcher
CameraModeActive	CameraSwitcher	External (analytics, cutscenes)
CameraTargetUpdate	SceneCameraSetup	CameraTargetManager
CameraBoundsResetRequest	DebugWindow, gameplay	CameraTargetManager
CameraShakeRequest	Triggers, gameplay	CameraEffectController
CameraDampingRequest	Gameplay (fall/land)	CameraEffectController
CameraPanRequest	Triggers, gameplay	CameraPanMover

Adding a new camera mode:

Add enum value to CameraMode
Create vCam GO in scene (CinemachineCamera + Composer + Confiner)
Add row to scene's CameraConfig.rigs
Add blend entries to CinemachineBlenderSettings
Publish CameraSwitchRequest(newMode)
Zero code changes.

Debug: Window → Game → Camera Debug

Perf: [Perf] Camera hook logs avg ms/frame, warns > 0.5ms.

5.2 Dialogue
Architecture: Message-driven, state machine, queue-based, accessible.

ProjectLifetimeScope:
  DialogueController (MonoBehaviour, persistent UI)
    ├── Subscribes: DialogueRequested
    ├── Publishes: DialogueStarted, DialogueEnded, TimeScalePause, TimeScaleResume
    └── Injects: InputManager, SettingsSavable

FacilitySceneLifetimeScope:
  DialogueSeenTracker (plain C#, IInitializable, ISaveable)
    └── Subscribes: DialogueStarted

NPCs (Gameplay.World):
  RoyalGuard, Merchant, etc.
    └── Interact() → publishes DialogueRequested

State machine:

Idle → Opening → Typing → Waiting → Closing → Idle
                        ↑         │
                        └─────────┘ (advance)

Key decisions:

NPCs publish DialogueRequested (no UI reference)
DialogueDataSO per conversation (create asset, drag into NPC)
Text Speed setting (SLOW/NORMAL/FAST/INSTANT) controls typewriter
Seen tracking enables first-visit / return-visit branching
CRT suppressed during dialogue (real-world interaction ≠ system display)
Plain text, no CRT on dialogue (diegetic boundary)
Adding a new NPC with dialogue:

Create DialogueDataSO asset (right-click → Create → Dialogue → Text)
Fill in speakerName + paragraphs + typeSpeed
Add to NPC's [SerializeField] field
NPC's Interact() publishes DialogueRequested
Zero code changes.

Debug: Window → Game → Dialogue Debug

5.3 Save System
Versioned JSON (SaveDataContainer with version field)
ISaveableRegistry (Lifetime.Singleton) — all ISaveable objects register/unregister
SaveManager.SaveAsync iterates registry, serializes each ISaveData to JSON
Migration chain: SaveMigration classes handle version bumps
Encryption: XOR + Base64 (obfuscation, not security)
Thread-pool file I/O via UniTask.RunOnThreadPool
Adding a new savable:

Implement ISaveable (return ISaveData struct with SaveId)
Register with _saveableRegistry.Register(this) in Initialize()
Unregister in Dispose()
Done — SaveAsync picks it up automatically.
5.4 Input
Unity Input System with two action maps: Player + Menu
InputManager (MonoBehaviour, persistent) exposes snapshot properties
IInputSwitcher.SwitchToMenu() / SwitchToPlayer() swaps active map
Dialogue uses ConfirmWasPressed (not raw Input.GetKeyDown)
Settings remaps 8 bindings via ApplyBindingOverride
Known issue: Diagnostic Debug.Log in Update() — wrap in #if UNITY_EDITOR before shipping.

5.5 Flow / State
GameState enum: MainMenu, Loading, Gameplay, Paused, GameOver, IntroVideo, QuitGame
GamePhase flags enum (long base): tracks broader narrative progression
GameStateMachine with transition table (15 unit tests)
GameFlowSystem with ExecuteTransition DRY pattern + last-known-good recovery
SceneTransitionOrchestrator: fade → load → unload → cleanup → fade in (re-entrancy guard)
SceneLoaderService with allowSceneActivation pattern
Debug: Window → Game → Flow Debug

6. Testing Strategy
Type	Location	Count	Covers
EditMode	Tests.EditMode	~55	State machines, save migration, camera config, dialogue data, null-safety, dispose guards
PlayMode	(none yet)	0	Camera switch, dialogue advance, scene load, player spawn

What EditMode tests prove:

Data structures work (rig lookup, filter, nullable)
Dispose is idempotent
Null inputs don't crash
Enum values are stable
What they can't prove (needs PlayMode):

Actual vCam enable/disable
Cinemachine blend application
Scene load/unload lifecycle
Message delivery timing
Input context switching
CI target: GitHub Actions → run EditMode tests on push → fail build on regression.

7. Editor Tooling
Window	Menu Path	Purpose
Flow Debug	Window → Game → Flow Debug	Current state, phase, transition history
Camera Debug	Window → Game → Camera Debug	Active mode, vCam, confiner bounds, damping, force switch
Dialogue Debug	Window → Game → Dialogue Debug	State, force advance/end

Perf hooks: [Perf] SceneLoad, [Perf] Transition, [Perf] SaveLoad, [Perf] Camera — all #if UNITY_EDITOR \|\| DEVELOPMENT_BUILD.

Parallax authoring: ParallaxOrchestrator.ToggleAuthoringMode(true) → SceneViewDriverDisabler disables runtime camera, drives local Camera from track object or Scene View.

8. Architecture Decision Records
ADR-001: Three Scope Hierarchy (not two)
Decision: Project → Facility → Level (three levels, not Project → Level).

Why: Camera, player, and checkpoints are per-scene (not per-level). A "facility" can contain multiple levels. The middle scope gives scene-level lifecycle without coupling to level progression.

Consequence: CameraConfig is a MonoBehaviour in the scene (not a ScriptableObject) because it holds scene references.

ADR-002: CameraConfig as MonoBehaviour (not ScriptableObject)
Decision: CameraConfig is a MonoBehaviour, not a ScriptableObject.

Why: ScriptableObjects are assets. Unity won't let you serialize scene object references into an asset (Type Mismatch error). Since the config is per-scene and holds references to that scene's vCams, it must be a scene object.

Consequence: Registered via RegisterComponent (not RegisterInstance). Lives in the scene, saves with it.

ADR-003: GlobalMessagePipe (not per-scope brokers)
Decision: Single GlobalMessagePipe static, initialized via RegisterBuildCallback in ProjectLifetimeScope.

Why: No per-type RegisterMessageBroker<T> calls. Subscribers/publishers obtained via GlobalMessagePipe.GetSubscriber<T>() / GetPublisher<T>() in Initialize()/Start(). Simpler, fewer registration lines, same performance.

Consequence: All messages are globally visible. No scope isolation for messages. Acceptable for a solo dev — the system count is small enough that accidental cross-system subscription is unlikely.

ADR-004: Dialogue as Messages (not direct references)
Decision: NPCs publish DialogueRequested. DialogueController subscribes. No [SerializeField] IDialogueController on NPCs.

Why: Decouples gameplay objects from UI. Moving/renaming the dialogue panel breaks zero NPCs. Adding a second dialogue type (radio, cutscene) = new subscriber, not new reference on every NPC.

Consequence: NPCs need using MessagePipe + one GlobalMessagePipe.GetPublisher<T>().Publish(...) call in Interact().

ADR-005: DialogueText → DialogueDataSO
Decision: Replaced [Serializable] class DialogueText with ScriptableObject DialogueDataSO.

Why: SOs are creatable in the Project panel (right-click → Create). Reusable across NPCs. No custom PropertyDrawer needed. Inspector handles string[] natively.

Consequence: Deleted DialogueText.cs + DialogueTextDrawer.cs (the drawer was causing ExtensionOfNativeClass editor error).

ADR-006: No CameraDirector
Decision: Did not create a separate CameraDirector class. CameraSwitcher is both decision + execution.

Why: With 3 cameras, a separate "director" that decides which camera and a "switcher" that executes the switch is over-engineering. The decision logic is a single FindRig(mode) call. Revisit at 6+ modes.

Consequence: CameraSwitcher is slightly larger than it would be with a director. Acceptable.

ADR-007: Per-Scene CinemachineBlenderSettings (not code)
Decision: Camera blend settings live in a CinemachineBlenderSettings asset assigned to the Brain. Not in code.

Why: Cinemachine 3.x removed per-vCam m_Transitions. Blends are Brain-level. Data-driven in the asset = add-a-mode = add-a-row, zero code.

Consequence: Names must match vCam GameObject names exactly (case-sensitive). Keep naming consistent across scenes.

ADR-008: CRT Suppressed During Dialogue
Decision: CRTDamageListener subscribes to DialogueStarted and zeroes failure/pulse.

Why: The CRT is the player's internal system (their eyes). NPC dialogue is a real-world interaction. Mixing them muddies the diegetic boundary.

Consequence: Next EntityHealthChanged message restores CRT values automatically. No explicit restore needed.

ADR-009: Text Speed Setting = Accessibility Toggle
Decision: No separate "skip typewriter" toggle. The existing Text Speed cycle (SLOW/NORMAL/FAST/INSTANT) handles it. Index 3 = INSTANT = skip typewriter.

Why: One less UI row. One less field in SettingsSaveData. The mapping is trivial (speedSetting == 3).

Consequence: DialogueController reads SettingsSavable.GetTextSpeed() on each OpenPanelAsync. Per-speaker typeSpeed on the SO acts as a base multiplier.

ADR-010: CameraSystem Deleted
Decision: Removed CameraSystem (the wiring hub that routed 2 messages to CameraTargetManager).

Why: Two messages don't justify an intermediary class. CameraTargetManager subscribes directly. One fewer injection point, one fewer Initialize() call, one fewer class to maintain.

Consequence: CameraTargetManager now implements IInitializable + IDisposable itself.

9. Known Gaps / Future Work
Item	Priority	Effort	Notes
PlayMode tests	High	M	Camera switch, dialogue advance, scene load
CI/CD (GitHub Actions)	High	S	Run EditMode tests on push
InputManager Debug.Log cleanup	Medium	XS	Wrap in #if UNITY_EDITOR
InputManager ReadValue double-call	Medium	XS	Cache in local var
Architecture doc (this file)	Done	—	You're reading it
Unity Profiler data capture	Low	M	Frame breakdowns, not just stopwatch
Localization pipeline	Low	M	Language setting exists but no string table yet
Audio: per-speaker type blip	Low	S	Field exists on DialogueDataSO, not yet wired to AudioManager

10. Quick Reference
Where Things Live
Thing	Location
Messages	Game.Core.Messages
Enums	Game.Core.Enums
Interfaces	Game.Core.Interfaces
Data (SOs)	Game.Core.Data
Camera system	Game.Gameplay.Camera
Player	Game.Gameplay.Player
Save	Game.Gameplay.Save
World/NPCs	Game.Gameplay.World
Audio	Game.Gameplay.Audio
UI	Game.UI
Flow/Bootstrap	Game.Bootstrap
Editor tools	Game.Editor
Tests	Game.Tests

Common Tasks
Task	Steps
Add a camera mode	Enum + vCam GO + SO row + BlenderSettings row
Add an NPC with dialogue	Create DialogueDataSO + assign to NPC field
Add a saveable	Implement ISaveable + register with registry
Add a message	Create struct in Game.Core.Messages + publish/subscribe
Add a debug window	New EditorWindow in Game.Editor + [MenuItem]
Add a perf hook	#if UNITY_EDITOR || DEVELOPMENT_BUILD MonoBehaviour with Stopwatch
Add a unit test	New [TestFixture] in Game.Tests + new GameObject + AddComponent pattern

The One Rule
If you're about to add a [SerializeField] reference from a gameplay object to a UI object, stop. Publish a message instead.