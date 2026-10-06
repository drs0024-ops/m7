# Invisibility Charge System

## Architecture

- `InvisibilityPickup` publishes `UpgradePickedUp`; `UpgradeOrchestrator` unlocks and saves the permanent Invisibility upgrade.
- `InvisibilityCharger` publishes `InvisibilityChargingChanged` while an unlocked player overlaps a charger trigger.
- Scoped `InvisibilityModel` consumes charge, interruption, and expiry messages; it publishes buffered charge and broadcast snapshots plus tier changes.
- `InvisibilityVisualPresenter` applies the buffered 0-100 charge to the player's `_CloakFactor` using a cached `MaterialPropertyBlock`.
- `BlobShadow` consumes the same charge snapshot and retains its separate visible/invisible opacity behavior.
- `InvisibilityEnemyInvestigator` consumes the buffered broadcast snapshot and publishes targeted investigation messages; enemies handle those messages in their grounded FSM.

## Tuning

Global tuning is serialized on `ProjectLifetimeScope`:

| Setting | Default | Meaning |
|---|---:|---|
| Charge rate | 10 percent/second | Baseline rate; each station can override it |
| Decay rate | 5 percent/second | Charge loss after leaving a station |
| Crouch multiplier | 0.6 | Charge rate multiplier while Down is held |
| Tier thresholds | 30 / 60 / 80 percent | Visible / Translucent / CloakedSlowed / Cloaked boundaries |
| Interruption drain | 10 percent | Charge drained by each accepted positive player damage event |

Each `InvisibilityCharger` exposes its own charge rate, maximum total charge, maximum dwell time, broadcast radius, and crouch broadcast-radius multiplier. A zero dwell-time limit means unlimited time at that station. A station maximum is a ceiling on total charge, so stations can provide progressive caps, for example 30, 65, then 100 percent.

## Message Flow

```text
Pickup -> UpgradePickedUp -> UpgradeOrchestrator -> saved unlock
Charger enter -> InvisibilityChargingChanged -> InvisibilityModel
Model tick -> buffered InvisibilityChargeChanged -> SpriteRenderer and BlobShadow
Model tick -> InvisibilityTierChanged -> future speed/detection consumers
Model charging -> buffered broadcast snapshot -> enemy investigator -> enemy investigation request
Accepted player damage -> InvisibilityInterrupted -> model drains charge
Charger exit/expiry -> model stops broadcast; charge decays or resets on expiry
```

## Unity Setup

1. Keep the `InvisibilityPickup` assigned in the Facility scope and ensure its upgrade ID is `Invisibility`.
2. Place `InvisibilityCharger` on each desired platform. Its required `BoxCollider2D` is forced to trigger mode at runtime.
3. Assign every charger to the Facility scope's `_invisibilityChargers` array.
4. Add `InvisibilityEnemyInvestigator` to each participating enemy root and assign those enemy roots to `_invisibilityAwareEnemies` on the Facility scope. Runtime-spawned enemies must be instantiated/injected through their scene resolver.
5. Keep the `InvisibilitySystem` object containing `InvisibilityController` and `InvisibilityVisualPresenter` in the scene.
6. The player root must retain the `Player` tag. The player sprite material must expose `_CloakFactor`.

## Tests

`InvisibilityModelTests` covers station-rate charging, crouch scaling, station cap, dwell-time limit, decay, interruption drain, tier changes, and expiry reset. `PlayerHealthTests` verifies accepted damage publishes one configured interruption. Unity Play Mode should additionally verify charger overlap, pickup gating, enemy attraction, visual charge, and scene-scope registration.
