# Player Upgrade System — Architecture & Flow

## Overview

A fully decoupled, event-driven player upgrade system built on **VContainer** (DI) and **MessagePipe** (pub/sub messaging). No class holds direct references to specific upgrade implementations. Adding a new upgrade requires **zero modifications** to existing core classes.

---

## Core Principles

| Principle | Implementation |
| :--- | :--- |
| **Decoupling** | Manager fires events; handlers listen. No `GetComponent<T>()` in core logic. |
| **Open/Closed** | New upgrades = new ScriptableObject + new Handler. No switch statements, no enum edits. |
| **Zero Allocation** | All signals are `struct`. MessagePipe guarantees zero GC per publish.  |
| **Single Source of Truth** | `PlayerUpgradesManager` owns unlock state. Handlers react, never store state. |
| **DI-First** | All dependencies injected via VContainer.  No static singletons, no `FindObjectOfType`. |

---

## Class Responsibilities

### `PlayerUpgradesManager` (Core Orchestrator)
- **Owns**: `_unlockedUpgrades` list, `_upgradeDict` index, `_activeTimedUpgrades` dictionary
- **Subscribes to**: `PlayerSpawnedSignal`, `UpgradePickedUpSignal`
- **Publishes**: `UpgradeAppliedSignal`, `UpgradeExpiredSignal`, `UpgradeStateChangedSignal`, `UpgradeUnlockedSignal`, `NotificationSignal`, `SaveRequestSignal`
- **Implements**: `IUpgradeManager`, `IUpgradeStateManager`, `ISaveable`, `IStartable`, `IDisposable`
- **Does NOT know about**: `InvisibilityInputHandler`, `PlayerBombUpgrade`, or any specific component

### `PlayerUpgrade` (ScriptableObject — Data Only)
- **Owns**: Static config (Name, ID, Duration, Icon, Description)
- **Provides**: `GetNotificationData()` (polymorphic, no switch statements)
- **Does NOT contain**: `ApplyTo()`, `GetComponent()`, or any execution logic

### `InvisibilityUpgradeHandler` (Effect Handler)
- **Subscribes to**: `UpgradeAppliedSignal`, `UpgradeExpiredSignal`
- **Publishes**: `InvisibilityStateChanged`
- **Action**: Toggles `InvisibilityInputHandler.enabled` on the player
- **Filter**: `if (message.UpgradeId != "Invisibility") return;`

### `InvisibilityInputHandler` (Input Boundary)
- **Subscribes to**: `UpgradeStateChangedSignal`
- **Publishes**: `InvisibilityToggleRequested`
- **Action**: Detects raw keypress, gates on unlock state, fires semantic intent
- **Does NOT know about**: Invisibility logic, durations, or visuals

### `PlayerView` (Visual Presenter)
- **Subscribes to**: `UpgradeStateChangedSignal`, `InvisibilityStateChanged`
- **Publishes**: `InvisibilityVisualSignal`
- **Action**: Drives shader/material changes based on state
- **Does NOT know about**: Upgrade rules, input, or component toggling

### `PlayerStateDriverShell` (Composition Root)
- **Owns**: `Rigidbody2D`, `PlayerController` (HSM), physics lifecycle
- **Injects**: `InputManager`, `ISignalBus` (or MessagePipe), `PlayerMovementStats`
- **Action**: Ticks state machine, applies velocity, fires `PlayerFacingChangedSignal`

---

## Signal Flow: Unlocking an Upgrade

┌─────────────────────────────────────────────────────────────────────────┐ │ 1. PLAYER PICKS UP UPGRADE │ │ [PickupTrigger] → Publish(UpgradePickedUpSignal { UpgradeID }) │ └──────────────────────────────┬──────────────────────────────────────────┘ ▼ ┌─────────────────────────────────────────────────────────────────────────┐ │ 2. PLAYERUPGRADES MANAGER │ │ OnUpgradePickedUp() → UnlockUpgrade("Invisibility") │ │ • Add to _unlockedUpgrades │ │ • Publish(UpgradeAppliedSignal { UpgradeId, Player, Duration }) │ │ • Publish(UpgradeStateChangedSignal { UpgradeID, IsActive=true }) │ │ • Publish(UpgradeUnlockedSignal { Data }) │ │ • Publish(NotificationSignal { Data }) │ │ • Publish(SaveRequestSignal { Source }) │ │ • If Duration > 0: ScheduleExpiration() via UniTask.Delay │ └──────────────────────────────┬──────────────────────────────────────────┘ ▼ ┌──────────────────────┼──────────────────────┐ ▼ ▼ ▼ ┌──────────────┐ ┌──────────────────┐ ┌──────────────┐ │ 3a. INVISIBILITY│ │ 3b. INVISIBILITY │ │ 3c. SAVE │ │ UPGRADE │ │ INPUT HANDLER │ │ SYSTEM │ │ HANDLER │ │ │ │ │ │ │ │ │ │ │ │ OnUpgradeApplied│ │ OnUpgradeStateChanged│ │ OnSaveRequest│ │ • GetComponent │ │ • Cache: _isUpgrade │ │ • Persist │ │ <Invisibility│ │ Unlocked = true │ │ to disk │ │ InputHandler>│ │ │ │ │ │ • .enabled = true│ │ │ │ │ │ • Publish( │ │ │ │ │ │ Invisibility │ │ │ │ │ │ StateChanged)│ │ │ │ │ └──────┬───────┘ └──────────────────┘ └──────────────┘ ▼ ┌─────────────────────────────────────────────────────────────────────────┐ │ 4. PLAYER VIEW │ │ OnInvisibilityStateChanged() │ │ • Publish(InvisibilityVisualSignal { TargetFactor = 1.0f }) │ └──────────────────────────────┬──────────────────────────────────────────┘ ▼ ┌─────────────────────────────────────────────────────────────────────────┐ │ 5. SHADER / VFX SYSTEM │ │ OnInvisibilityVisualSignal() │ │ • Fade material alpha to 0.0f │ └─────────────────────────────────────────────────────────────────────────┘


---

## Signal Flow: Timed Upgrade Expiration

┌─────────────────────────────────────────────────────────────────────────┐ │ 1. TIMER FIRES (UniTask.Delay completes) │ │ DeactivateAfterDelay() → DeactivateUpgrade("Invisibility") │ └──────────────────────────────┬──────────────────────────────────────────┘ ▼ ┌─────────────────────────────────────────────────────────────────────────┐ │ 2. PLAYERUPGRADES MANAGER │ │ • Remove from _activeTimedUpgrades │ │ • Publish(UpgradeExpiredSignal { UpgradeId, Player }) │ │ • Publish(UpgradeStateChangedSignal { UpgradeID, IsActive=false }) │ └──────────────────────────────┬──────────────────────────────────────────┘ ▼ ┌──────────────────────┼──────────────────────┐ ▼ ▼ ▼ ┌──────────────┐ ┌──────────────────┐ ┌──────────────┐ │ 3a. INVISIBILITY│ │ 3b. INVISIBILITY │ │ 3c. PLAYER │ │ UPGRADE │ │ INPUT HANDLER │ │ VIEW │ │ HANDLER │ │ │ │ │ │ │ │ │ │ │ │ OnUpgradeExpired│ │ OnUpgradeStateChanged│ │ OnUpgrade │ │ • .enabled = false│ │ • Cache: _isUpgrade │ │ StateChanged│ │ • Publish( │ │ Unlocked = false │ │ • Remove │ │ Invisibility │ │ │ │ visual │ │ StateChanged)│ │ │ │ │ └──────────────┘ └──────────────────┘ └──────────────┘


---

## Signal Flow: Player Spawn (Re-apply)

┌─────────────────────────────────────────────────────────────────────────┐ │ 1. PLAYER SPAWNS │ │ [Spawner] → Publish(PlayerSpawnedSignal { Player }) │ └──────────────────────────────┬──────────────────────────────────────────┘ ▼ ┌─────────────────────────────────────────────────────────────────────────┐ │ 2. PLAYERUPGRADES MANAGER │ │ OnPlayerSpawned() │ │ • Cache _currentPlayer │ │ • For each permanent upgrade (Duration == 0): │ │ Publish(UpgradeAppliedSignal { UpgradeId, Player, Duration }) │ └──────────────────────────────┬──────────────────────────────────────────┘ ▼ (Same flow as Section 3a/3b/3c above)


---

## Adding a New Upgrade (e.g., "Speed Boost")

**Step 1**: Create `SpeedBoostUpgrade.asset` (ScriptableObject)

UpgradeID: "SpeedBoost" UpgradeName: "Speed Boost" Duration: 0 (permanent) or 30f (timed) NotificationTitleOverride: "Speed Boost Acquired"


**Step 2**: Create `SpeedBoostUpgradeHandler.cs`
```csharp
public class SpeedBoostUpgradeHandler : IDisposable
{
    [Inject] private ISubscriber<UpgradeAppliedSignal> _appliedSub;
    [Inject] private ISubscriber<UpgradeExpiredSignal> _expiredSub;

    [Inject]
    private void Initialize()
    {
        _appliedSub.Subscribe(OnApplied);
        _expiredSub.Subscribe(OnExpired);
    }

    private void OnApplied(UpgradeAppliedSignal msg)
    {
        if (msg.UpgradeId != "SpeedBoost") return;
        msg.Player.GetComponent<PlayerMovementStats>().MoveSpeed += 2f;
    }

    private void OnExpired(UpgradeExpiredSignal msg)
    {
        if (msg.UpgradeId != "SpeedBoost") return;
        msg.Player.GetComponent<PlayerMovementStats>().MoveSpeed -= 2f;
    }

    public void Dispose() { /* dispose subscriptions */ }
}

Step 3: Register in PlayerLifetimeScope

builder.Register<SpeedBoostUpgradeHandler>(Lifetime.Scoped);

Step 4: Done. No changes to PlayerUpgradesManager, PlayerUpgrade, PlayerView, or any existing handler. 

VContainer Registration
public class PlayerLifetimeScope : LifetimeScope
{
    [SerializeField] private UpgradeListObject upgradeList;

    protected override void Configure(IContainerBuilder builder)
    {
        // MessagePipe
        var options = builder.RegisterMessagePipe();
        builder.RegisterMessageBroker(options);
        builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));

        // Upgrade Manager
        builder.RegisterInstance(upgradeList);
        builder.Register<PlayerUpgradesManager>(Lifetime.Scoped)
               .As<IUpgradeManager>()
               .As<IUpgradeStateManager>()
               .As<IStartable>()
               .As<IDisposable>();

        // Handlers
        builder.Register<InvisibilityUpgradeHandler>(Lifetime.Scoped);
        builder.Register<BombUpgradeHandler>(Lifetime.Scoped);
        builder.Register<InvisibilityInputHandler>(Lifetime.Scoped);
        builder.Register<PlayerView>(Lifetime.Scoped);

        // Player
        builder.RegisterComponentInHierarchy<PlayerStateDriverShell>();
    }
}

Signal Catalog
Signal	Type	Publisher	Subscribers
PlayerSpawnedSignal	struct	Spawner	PlayerUpgradesManager
UpgradePickedUpSignal	struct	PickupTrigger	PlayerUpgradesManager
UpgradeAppliedSignal	struct	PlayerUpgradesManager	All *UpgradeHandler
UpgradeExpiredSignal	struct	PlayerUpgradesManager	All *UpgradeHandler
UpgradeStateChangedSignal	struct	PlayerUpgradesManager	InvisibilityInputHandler, PlayerView
UpgradeUnlockedSignal	struct	PlayerUpgradesManager	UI, Achievements
NotificationSignal	struct	PlayerUpgradesManager	Notification UI
SaveRequestSignal	struct	PlayerUpgradesManager	Save System
InvisibilityToggleRequested	struct	InvisibilityInputHandler	InvisibilityUpgradeHandler
InvisibilityStateChanged	struct	InvisibilityUpgradeHandler	PlayerView
InvisibilityVisualSignal	struct	PlayerView	Shader/VFX System
PlayerFacingChangedSignal	struct	PlayerStateDriverShell	Audio, Animation

Package Dependencies
Package	Version	Source
com.cysharp.messagepipe	1.8.1	OpenUPM
com.cysharp.messagepipe.vcontainer	1.8.1	OpenUPM
com.cysharp.unitask	2.5.10	OpenUPM
jp.hadashikick.vcontainer	1.19.0	GitHub

Key Design Decisions
IStartable over IInitializable: Guarantees PlayerUpgradesManager.Start() runs before PlayerView.Start(), ensuring save data is loaded before visual state is queried. 
struct signals: Zero GC allocation per publish. Critical for high-frequency events (e.g., PlayerFacingChangedSignal). 
IDisposable subscriptions: MessagePipe's Subscribe() returns an IDisposable. Disposing it unsubscribes. Combined with VContainer's scoped lifetime, subscriptions are automatically cleaned up when the scope disposes.
No CancellationToken on Subscribe(): The MessagePipe extension method signature is Subscribe(Action<T>, params MessageHandlerFilter<T>[]). Cancellation is handled via Dispose() on the returned IDisposable.
UniTask.Delay for timers: Replaces Coroutines. Runs on Unity's PlayerLoop (main thread), cancellation via CancellationTokenSource, fire-and-forget via .Forget().
Input Boundary pattern: InvisibilityInputHandler translates raw keypress → semantic intent (InvisibilityToggleRequested). Game logic never reads Input.GetKeyDown() directly.