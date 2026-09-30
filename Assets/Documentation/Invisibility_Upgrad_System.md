# Invisibility Upgrade System

## Architecture

┌──────────────────────────────────────────────────────────────────┐
│ PROJECT SCOPE (Singleton, DnL) │
│ │
│ UpgradeStateManager — tracks unlocked upgrades, timed effects │
│ UpgradeOrchestrator — publishes UpgradeStateChanged/Expired │
└──────────────────────────────────────────────────────────────────┘
│
▼ UpgradeStateChanged / UpgradeExpired
┌──────────────────────────────────────────────────────────────────┐
│ SCENE SCOPE (Scoped, dies with scene) │
│ │
│ InvisibilityController (MonoBehaviour wrapper) │
│ └── InvisibilityModel (plain class, ITickable, IDisposable) │
│ - State: _isActive, _isTimedEffect, _callerActive, │
│ _callerTimer │
│ - Subscribes: InvisibilityToggleRequested, │
│ UpgradeExpired, UpgradeStateChanged │
│ - Publishes: InvisibilityStateChanged │
│ - Ticks: caller countdown │
│ │
│ InvisibilityInputHandler (MonoBehaviour) │
│ - Gates on UpgradeStateChanged (is upgrade unlocked?) │
│ - Reads IInputState.IsInvisibilityPressed │
│ - Publishes: InvisibilityToggleRequested │
│ │
│ InvisibilityVisualPresenter (MonoBehaviour) │
│ - Subscribes: InvisibilityStateChanged, PlayerSpawned │
│ - Captures player Renderer on PlayerSpawned │
│ - Animates _CloakFactor on target renderer │
│ - Toggles ShadowCaster2D │
│ │
│ InvisibilityPickup (MonoBehaviour, one-shot) │
│ - OnTriggerEnter2D → Publish(UpgradePickedUp) │
└──────────────────────────────────────────────────────────────────┘


## Message Flow

### Key Toggle (player presses C)

InputManager.IsInvisibilityPressed = true
→ InvisibilityInputHandler.Update()
→ gate: _isUpgradeUnlocked (set by UpgradeStateChanged)
→ Publish(InvisibilityToggleRequested.Default)
→ InvisibilityModel.OnToggleRequested
→ _isActive = !_isActive
→ Publish(InvisibilityStateChanged(factor, isTimedEffect))
→ InvisibilityVisualPresenter.OnStateChanged
→ guard: _targetRenderer != null
→ AnimateCloak(targetFactor)


### Player Spawned (renderer capture)

PlayerSpawnerService.InstantiatePlayer()
→ InjectGameObject(playerGO)
→ Publish(PlayerSpawned(playerTransform))
→ InvisibilityVisualPresenter.OnPlayerSpawned
→ _targetRenderer = message.Player.GetComponentInChildren()


> If the player respawns (destroyed + re-instantiated), `PlayerSpawned` fires again
> and `_targetRenderer` updates automatically. No stale references.

### Timed Upgrade Expiry

UpgradeStateManager.Tick() detects expiry
→ Publish(UpgradeExpired("invisibility"))
→ InvisibilityModel.OnUpgradeExpired
→ _callerActive = false, _callerTimer = 0
→ _isActive = false
→ Publish(InvisibilityStateChanged(0f, false))
→ InvisibilityVisualPresenter animates → 0f


### Caller-Activated (zone, scripted event)

Any scene component:
_controller.ActivateForDuration(5f)
→ InvisibilityModel.ActivateForDuration(5f)
→ _callerActive = true, _callerTimer = 5f
→ Publish(InvisibilityStateChanged(1f, isTimedEffect))
→ Visual presenter animates → 1f

InvisibilityModel.Tick() (every frame via EntryPointDispatcher):
→ _callerTimer -= deltaTime
→ when _callerTimer <= 0:
→ _callerActive = false
→ Publish(InvisibilityStateChanged(0f, false))
→ Visual presenter animates → 0f


### Upgrade First Unlocked

Player touches InvisibilityPickup
→ Publish(UpgradePickedUp("invisibility"))
→ UpgradeOrchestrator.UnlockUpgrade
→ UpgradeStateManager.AddUpgrade
→ Publish(UpgradeStateChanged("invisibility", true, duration))
→ InvisibilityInputHandler: _isUpgradeUnlocked = true
→ InvisibilityModel: _isTimedEffect = (duration > 0)


## Priority Rules

| Condition | Behavior |
|-----------|----------|
| Caller active | Key input ignored. Timer counts down. Auto-off at 0. |
| Upgrade expired (timed) | Caller cancelled. Auto-off. |
| Upgrade permanent | Key toggles freely. No auto-expire. |
| `Deactivate()` called | Immediate off, regardless of source. |
| `_targetRenderer == null` | `OnStateChanged` is a no-op (player not yet spawned). |

## Files

| File | Assembly | Lifetime | Registration |
|------|----------|----------|-------------|
| `InvisibilityModel.cs` | Gameplay | N/A (instantiated by controller) | — |
| `InvisibilityController.cs` | Gameplay | Scoped | `RegisterComponentInHierarchy` in Scene scope |
| `InvisibilityInputHandler.cs` | Gameplay | Scoped | `RegisterComponentInHierarchy` in Scene scope |
| `InvisibilityVisualPresenter.cs` | Gameplay | Scoped | `RegisterComponentInHierarchy` in Scene scope |
| `InvisibilityPickup.cs` | Gameplay | N/A (scene GO) | `RegisterComponent` in Scene scope |

## Tuning

| Parameter | Where | Default | Description |
|-----------|-------|---------|-------------|
| `_transitionDuration` | InvisibilityVisualPresenter | 0.5s | Fade in/out speed |
| `_CloakFactor` | Shader | 0–1 | 0 = visible, 1 = fully invisible |
| Upgrade duration | UpgradeListObject | varies | How long timed invisibility lasts |
| Caller duration | Caller code | varies | Set by zone/event |

## Testing

- Unit: `InvisibilityModel` — toggle on/off, caller activation, expiry cancellation, key ignored during caller
- Unit: `InvisibilityModel` — `ActivateForDuration(0)` is no-op
- Unit: `InvisibilityVisualPresenter` — `OnStateChanged` before `PlayerSpawned` is no-op
- Integration: Full scope resolves all 4 components

## Unity Setup

### In each gameplay scene (Facility1, Facility2):

**1. Create a GO named `InvisibilitySystem`** (empty, no tag)

Add these 3 components:

| Component | Serialized Fields to Set |
|-----------|--------------------------|
| `InvisibilityController` | *(none — all injected)* |
| `InvisibilityInputHandler` | *(none — all injected)* |
| `InvisibilityVisualPresenter` | `_shadowCaster2D` → drag the player prefab's ShadowCaster2D (or null if on a child). `_transitionDuration` → `0.5` |

> `_targetRenderer` is **not serialized**. It's captured at runtime via `PlayerSpawned` subscription.

**2. Place `InvisibilityPickup` GOs** wherever you want the upgrade:

| Component | Serialized Fields |
|-----------|-------------------|
| `InvisibilityPickup` | `_upgradeID` → `"invisibility"`. `_destroyOnPickup` → `true` |
| `BoxCollider2D` | `isTrigger = true` |

**3. `FacilitySceneScope.Configure`** must have:

```csharp
builder.RegisterComponentInHierarchy<InvisibilityController>()
    .AsSelf()
    .AsImplementedInterfaces();
builder.RegisterComponentInHierarchy<InvisibilityInputHandler>()
    .AsImplementedInterfaces();
builder.RegisterComponentInHierarchy<InvisibilityVisualPresenter>()
    .AsImplementedInterfaces();

4. InputManager — expose IsInvisibilityPressed via IInputState:

public bool IsInvisibilityPressed =>
    _invisibilityAction != null && _invisibilityAction.WasPressedThisFrame();

5. UpgradeListObject — add entry with UpgradeID = "invisibility", Duration = 10f.