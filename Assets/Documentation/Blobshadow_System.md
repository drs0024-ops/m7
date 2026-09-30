# Blob Shadow System

## Overview

A soft ellipse shadow that tracks the player's ground contact in a 2D platformer.
Stays visible when the player is invisible (reduced opacity). Tilts to match ground angle.
Clips at walls. Shrinks and fades when the player jumps. Squashes on landing.

## Architecture

┌──────────────────────────────────────────────────────────────────┐
│ PLAYER PREFAB (instantiated by PlayerSpawnerService) │
│ │
│ Player (root GO, tag = "Player") │
│ ├── SpriteRenderer (body, animated, sortingOrder = 10) │
│ ├── PlayerHealth │
│ ├── PlayerController │
│ ├── ... │
│ └── BlobShadow (child GO) │
│ ├── SpriteRenderer │
│ │ ├── Sprite: BlobShadow_Sprite (128×64 soft ellipse) │
│ │ ├── Material: BlobShadowMat (shader = "Game/BlobShadow")│
│ │ └── Sorting Order: 5 (below body) │
│ └── BlobShadow (MonoBehaviour) │
│ - Subscribes: InvisibilityStateChanged, │
│ PlayerLanded, PlayerSpawned │
│ - Raycasts: Ground (down), Walls (left/right) │
│ - Drives: position, rotation, scale, opacity │
└──────────────────────────────────────────────────────────────────┘


## Message Dependencies

| Message | Direction | Purpose |
|---------|-----------|---------|
| `PlayerSpawned` | Subscribe | Capture player transform reference |
| `InvisibilityStateChanged` | Subscribe | Track cloak factor for opacity lerp |
| `PlayerLanded` | Subscribe | Trigger squash animation |

No messages are published by `BlobShadow`. It's a pure consumer.

## Behavior

### Ground Tracking

- Downward `Physics2D.Raycast` from player position, length 4.0, layer "Ground"
- Shadow positioned at hit point + 0.02 along hit normal (prevents z-fighting)
- If no ground hit: shadow hidden (`enabled = false`)

### Ground Angle Tilt

- Shadow rotation = `atan2(normal.x, normal.y)` in degrees
- Flat ground → 0°
- Angled slope → matches slope angle
- No angle (vertical wall hit) → shadow hidden (handled by no-hit case)

### Wall Clipping

- Left and right horizontal raycasts from shadow position, length 0.8, layer "Walls"
- If a wall is hit before the shadow's edge, that side's width is reduced
- Final X scale = `(leftDist + rightDist) / (2 × _shadowHalfWidth)`
- Result: shadow clips smoothly as player approaches a wall

### Jump (Height)

| Height Above Ground | Scale | Opacity |
|---------------------|-------|---------|
| 0 – 1.0 (fadeStart) | 1.0 | Full |
| 1.0 – 3.0 (maxHeight) | Lerp 1.0 → 0.3 | Lerp 1.0 → 0.0 |
| > 3.0 | Hidden | 0 |

### Landing Squash

- Trigger: `PlayerLanded` message
- Duration: 0.12s
- X scale: 1.4 → 1.0 (start wide, settle)
- Y scale: 0.6 → 1.0 (start flat, settle)
- Multiplied into the height scale each frame

### Invisibility

| Cloak Factor | Opacity |
|---|---|
| 0.0 (visible) | `_visibleOpacity` (default 0.4) |
| 1.0 (invisible) | `_invisibleOpacity` (default 0.15) |

Shadow **never fully disappears** when player is invisible. It provides a gameplay hint.

## Files

| File | Assembly | Location |
|------|----------|----------|
| `BlobShadow.cs` | Gameplay | `Assets/Game/Gameplay/Player/` |
| `BlobShadow.shader` | — | `Assets/Shaders/` |
| `BlobShadow_Sprite.png` | — | `Assets/Art/Shadows/` |
| `BlobShadowMat.mat` | — | `Assets/Materials/` |

## Shader: `Game/BlobShadow`

| Property | Type | Default | Driven By |
|----------|------|---------|-----------|
| `_MainTex` | 2D | white | Ellipse sprite |
| `_ShadowColor` | Color | (0,0,0,1) | Inspector (fixed black) |
| `_ShadowOpacity` | Range(0,1) | 0.4 | `BlobShadow.Update()` via `SetFloat` |

### Shader Properties

| Setting | Value | Reason |
|---------|-------|--------|
| `ZTest` | Always | Shadow always renders on top of ground |
| `ZWrite` | Off | Doesn't block other transparent objects |
| `Lighting` | Off | Unlit — unaffected by Light2D |
| `Queue` | Transparent-1 | Renders just before player body |
| `Cull` | Off | Double-sided (visible from all camera angles) |
| `Blend` | SrcAlpha OneMinusSrcAlpha | Standard alpha blend |

## Tuning Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `_visibleOpacity` | 0.4 | Shadow opacity when player is visible |
| `_invisibleOpacity` | 0.15 | Shadow opacity when player is invisible |
| `_fadeStartHeight` | 1.0 | Height where fade begins (world units) |
| `_maxHeight` | 3.0 | Height where shadow fully disappears |
| `_minScale` | 0.3 | Minimum shadow scale at max height |
| `_raycastLength` | 4.0 | Downward raycast distance |
| `_shadowHalfWidth` | 0.8 | Half-width of shadow for wall clipping |
| `_squashAmount` | 0.4 | Squash intensity on landing (0 = none, 1 = extreme) |

## Layers Required

| Layer Name | Used For |
|------------|----------|
| "Ground" | Downward raycast (ground detection) |
| "Walls" | Horizontal raycasts (wall clipping) |

**Assign in Unity:**
- All ground tiles / platforms → "Ground"
- All vertical wall colliders → "Walls"
- One-way platforms (if added later) → "Ground" (or a third layer)

## Unity Setup

### 1. Create the Sprite

1. Open any image editor
2. Draw a white filled ellipse on a transparent 128×64 canvas
3. Apply Gaussian blur (radius ~12px) to soften edges
4. Export as `BlobShadow_Sprite.png`
5. Import in Unity:
   - Texture Type: Sprite (2D and UI)
   - Filter Mode: Bilinear
   - Compression: None
   - Pixels Per Unit: 100

### 2. Create the Material

1. `Assets → Create → Material` → `BlobShadowMat`
2. Shader: `Game/BlobShadow`
3. `_MainTex`: drag `BlobShadow_Sprite`
4. `_ShadowColor`: (0, 0, 0, 1)
5. `_ShadowOpacity`: 0.4

### 3. Add to Player Prefab

1. In the Player prefab, create a child GO → `BlobShadow`
2. Add `SpriteRenderer` component:
   - Sprite: `BlobShadow_Sprite`
   - Material: `BlobShadowMat`
   - Sorting Order: 5
   - Color: White
3. Add `BlobShadow` (MonoBehaviour) component
4. Set fields:

| Field | Value |
|-------|-------|
| `_visibleOpacity` | 0.4 |
| `_invisibleOpacity` | 0.15 |
| `_fadeStartHeight` | 1.0 |
| `_maxHeight` | 3.0 |
| `_minScale` | 0.3 |
| `_raycastLength` | 4.0 |
| `_shadowHalfWidth` | 0.8 |
| `_groundLayer` | Check "Ground" |
| `_wallLayer` | Check "Walls" |
| `_squashAmount` | 0.4 |

5. Local Position: `(0, 0, 0)` — code sets position every frame
6. Local Scale: `(1, 1, 1)` — code overrides every frame

### 4. No Scene Registration Needed

`BlobShadow` is on the player prefab. It's injected via `InjectGameObject` when `PlayerSpawnerService` instantiates the player. No `RegisterComponent` or `RegisterComponentInHierarchy` line required in any scope.

### 5. Confirm `PlayerLanded` is Published

In your player controller / state machine, when transitioning from air → grounded:

```csharp
_playerLandedPublisher.Publish(PlayerLanded.Default);

Testing
Test	Type	What to Verify
Shadow appears at player feet	Integration	Raycast hits ground, shadow positioned correctly
Shadow tilts on slope	Integration	Rotation matches ground angle
Shadow clips at wall	Integration	X scale reduces as player approaches wall
Shadow shrinks on jump	Integration	Scale decreases with height
Shadow fades at max height	Integration	Opacity → 0 at 3.0u
Shadow hidden above max height	Integration	enabled = false
Squash on landing	Unit	Publish PlayerLanded, verify scale spike over 0.12s
Invisibility reduces opacity	Unit	Publish InvisibilityStateChanged(1f), verify opacity = _invisibleOpacity
No ground hit → hidden	Unit	Mock raycast miss, verify enabled = false
Player respawn updates reference	Integration	Publish PlayerSpawned, verify shadow tracks new player

Design Decisions
Decision	Rationale
Shadow stays visible when invisible	Gameplay: player position is hinted, not fully hidden
Dual layers (Ground + Walls)	Prevents ground tiles from registering as wall clips. Scalable for one-way platforms
PlayerLanded message for squash	Decoupled from physics. Testable. Consistent with message-driven architecture
Unlit shader	Shadows aren't lit by scene lights. Cheaper. No Light2D interaction
ZTest Always	Shadow always visible on ground. No z-fighting with terrain
Cloned material in Start()	Prevents modifying the shared asset. Each player instance gets its own opacity
100 PPU	Player is 2.56u tall. Raycast precision is good. Unity default.
