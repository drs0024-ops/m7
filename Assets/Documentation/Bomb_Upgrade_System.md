# Bomb Upgrade System

## Overview

The bomb system is a timed upgrade that allows the player to place up to N bombs per unlock.
Each bomb has a fuse timer and explodes on expiry, dealing area damage.

## Architecture

┌─────────────────────────────────────────────────────────────────┐
│ PROJECT SCOPE (Singleton, DnL) │
│ │
│ BombUpgradeHandler (prefab, RegisterComponentInNewPrefab) │
│ - Tracks: _isUnlocked, _bombsRemaining, _cooldownTimer │
│ - Reacts to: UpgradeStateChanged (UpgradeIDs.Bomb) │
│ - Publishes: BombPlaced │
│ - Ticks: cooldown timer (ITickable) │
└─────────────────────────────────────────────────────────────────┘
│
▼ BombPlaced(position)
┌─────────────────────────────────────────────────────────────────┐
│ SCENE SCOPE (Scoped, dies with scene) │
│ │
│ BombSpawner (RegisterComponentInHierarchy) │
│ - Subscribes to: BombPlaced │
│ - Instantiates: Bomb.prefab at position │
│ │
│ Bomb (MonoBehaviour on Bomb.prefab) │
│ - Fuse countdown (Update) │
│ - Explodes: Physics2D.OverlapCircleAll → IDamagable.Damage │
│ - Self-destructs after explosion │
└─────────────────────────────────────────────────────────────────┘


## Message Flow

Player presses bomb key
→ BombUpgradeHandler.TryPlaceBomb(Vector2 position)
→ guard: CanPlaceBomb (unlocked + bombsRemaining > 0 + cooldown <= 0)
→ _bombsRemaining--
→ _cooldownTimer = _cooldown
→ Publish(BombPlaced(position))
→ BombSpawner.OnBombPlaced(msg)
→ Instantiate(Bomb.prefab, msg.Position)
→ Bomb.Update() counts down _fuseTime
→ Bomb.Explode()
→ Physics2D.OverlapCircleAll(position, _blastRadius)
→ For each: GetComponentInParent().Damage(_damage, dir)
→ Instantiate(_explosionVfx)
→ Destroy(gameObject)


## Files

| File | Assembly | Lifetime | Registration |
|------|----------|----------|-------------|
| `BombUpgradeHandler.cs` | Gameplay | Singleton | `RegisterComponentInNewPrefab(bombPrefab, Lifetime.Singleton)` in Project scope |
| `BombSpawner.cs` | Gameplay | Scoped | `RegisterComponentInHierarchy<BombSpawner>()` in Scene scope |
| `Bomb.cs` | Gameplay | N/A (prefab) | Instantiated by `BombSpawner`, injected via `InjectGameObject` |
| `Bomb.prefab` | — | — | Contains `Bomb` component + collider + VFX |

## Setup (Unity Editor)

### 1. Bomb Prefab

1. Create empty GO → `Bomb`
2. Add components:
   - `Bomb` (MonoBehaviour)
   - `BoxCollider2D` (isTrigger = true, size = blast radius)
   - `Rigidbody2D` (isKinematic = true)
   - Particle system / sprite for explosion VFX
3. Save as `Bomb.prefab` in `Assets/Prefabs/`

### 2. BombUpgradeHandler Prefab

1. Create empty GO → `BombHandler`
2. Add `BombUpgradeHandler` component
3. Set `_maxBombs = 3`, `_cooldown = 2f`
4. Save as `BombHandler.prefab` in `Assets/Prefabs/`

### 3. Assign in ProjectLifetimeScope

On the `ProjectLifetimeScope` GO in the Bootstrap scene:
- Drag `BombHandler.prefab` into the `bombPrefab` field

### 4. BombSpawner in Scene

1. In each gameplay scene (Facility1, Facility2), add a GO with `BombSpawner`
2. Drag `Bomb.prefab` into the `_bombPrefab` field
3. Ensure `FacilitySceneScope.Configure` has:
   ```csharp
   builder.RegisterComponentInHierarchy<BombSpawner>()
       .AsImplementedInterfaces();

5. Input Trigger
In your player input handler (wherever you handle the bomb key):

// Resolve from Project scope (injected or resolved)
if (_bombHandler.CanPlaceBomb)
    _bombHandler.TryPlaceBomb(playerTransform.position);

Upgrade Gate
The bomb is only usable after the player picks up the bomb upgrade:

UpgradePickedUp(UpgradeIDs.Bomb)
  → UpgradeOrchestrator.UnlockUpgrade("bomb")
    → UpgradeStateManager.AddUpgrade("bomb")
    → Publish(UpgradeStateChanged("bomb", true, duration))
      → BombUpgradeHandler.OnUpgradeStateChanged
        → _isUnlocked = true
        → _bombsRemaining = _maxBombs

When the timed effect expires (if Duration > 0):

UpgradeStateManager.Tick() detects expiry
  → Publish(UpgradeExpired("bomb"))
  → UpgradeOrchestrator (or StateManager) publishes UpgradeStateChanged("bomb", false, 0)
    → BombUpgradeHandler: _isUnlocked = false, _bombsRemaining = 0

Tuning Parameters
Parameter	Where	Default	Description
_maxBombs	BombUpgradeHandler prefab	3	Bombs per unlock
_cooldown	BombUpgradeHandler prefab	2s	Time between placements
_fuseTime	Bomb.prefab	2s	Time until explosion
_blastRadius	Bomb.prefab	1.5	Radius of explosion (world units)
_damage	Bomb.prefab	25	Damage dealt to each entity in radius

Testing
Unit: BombUpgradeHandler — verify CanPlaceBomb gates correctly, cooldown decrements in Tick()
Unit: Bomb.Explode() — verify OverlapCircleAll hits expected targets, IDamagable.Damage called with correct values
Integration: Place bomb → wait fuse → verify entity health decreased, bomb GO destroyed

Save as `Assets/Game/Gameplay/Upgrades/Bomb_Upgrade.md` (or wherever you keep design docs).