# CRT Overlay Flow

## Architecture

ProjectLifetimeScope (Root, never destroyed)
├── InputManager (Singleton)
├── AudioMixerMaster (Singleton)
├── SettingsSavable (Singleton)
├── CRTTransitionController (Singleton, scene instance in bootstrap)
│ └── CRTCanvas (Screen Space - Overlay, Sort Order 100)
│ ├── CRTOverlayQuad (RawImage, M_CRT Failure, always visible)
│ └── RollBarQuad (RawImage, M_CRT Roll Bar, disabled when idle)
│
├── MainMenuSceneLifetimeScope (child of Root)
│ ├── MenuMaster
│ ├── BootTextController
│ ├── OptionsMenuController
│ ├── CrtWorldOverlay
│ ├── AudioSettingsUI
│ ├── VideoSettingsUI
│ ├── ControlsSettingsUI
│ └── SystemSettingsUI
│
└── LevelLifetimeScope (child of Root, per-level)
└── ...


## Render Stack (bottom → top)

| Layer | Sort Order | Content |
|-------|:---:|---------|
| World | — | Sprites, tiles, player |
| HUD Canvas (Bootstrap) | 0 | Gameplay HUD panels |
| MainMenu Canvas (MainMenu scene) | 50 | Boot text, options, world overlay |
| CRT Canvas (Bootstrap, prefab) | 100 | Scanlines + roll bar |

## Idle State (Always-On CRT Look)

- `_FailureAmount = 0.15` → subtle scanlines, faint flicker, occasional sync tear
- `_RetraceY = -1` → retrace line off
- `_Collapse = 0` → no contraction
- Roll bar `RawImage.enabled = false` → zero GPU cost
- Shader early-out: `if (failure < 0.001) return half4(0,0,0,0)`

**Menu-Only mode:** When `_menuOnly = true` and phase is Gameplay/Climax/Ending, `_FailureAmount` drops to `0` (shader early-outs, zero cost).

## Transition (4 Frames)

### Trigger

[Menu / Gameplay System]
→ Publishes ScreenTransitionRequested { TargetMenu }
→ CRTTransitionController.TriggerTransition()


### State Machine

| Frame | State | Roll Bar Y | Roll Bar Intensity | Overlay `_FailureAmount` |
|:---:|-------|:---:|:---:|:---:|
| 1 | Ramping | 0.9 → 0.7 | 0 → 0.33 (ease-in) | 1.0 |
| 2 | Ramping | 0.7 → 0.5 | 0.33 → 1.0 (ease-in) | 1.0 |
| 3 | **Swap** | 0.5 | 1.0 | 1.0 |
| 4 | **Settling** | 0.1 | **1.15** (overshoot) | 1.0 |
| 5 | Idle | — | — | **0.15** (or 0 if menu-only + gameplay) |

### Swap Frame (Frame 3)

1. Roll bar holds at center (`_Y = 0.5`, `_Intensity = 1`)
2. `ScreenSwapped` published
3. `MenuMaster.OnScreenSwapped()` executes the pending `Action` (panel swap)
4. Hard cut — no fade, no crossfade

### Cancellation

If `TriggerTransition()` is called mid-transition, restart Ramping from current `_Y` toward 0.5. No reset.

### Input Gate

While `IsTransitioning == true`, `MenuMaster.Update()` returns immediately. No input processed.

## Message Flow

[Menu System / GameFlowSystem]
│
├──▶ ScreenTransitionRequested { TargetMenu }
│ → CRTTransitionController starts 4-frame roll bar
│
├──▶ CRTIntensityChanged { Intensity }
│ → Sets _FailureAmount on overlay (idle state only)
│
├──▶ CRTColorChanged { ScreenTint, BarTint }
│ → Sets _PhosphorColor + _Tint
│
├──▶ CRTModeChanged { MenuOnly }
│ → Toggles menu-only behavior
│
└──▶ MenuNavDirectionMessage { Direction }
→ CrtWorldOverlay pans UV rect
(published by BootTextController while _isSelecting)

[CRTTransitionController]
│
└──▶ ScreenSwapped
→ MenuMaster executes panel swap (hard cut)


## CrtWorldOverlay (Menu Background Pan)

### Hierarchy

MenuOverlay (child of MainMenu Canvas)
├── [CrtWorldOverlay]
├── SolidBackground (Image, black, stretch-to-fill)
└── WorldImage (RawImage, PNG texture, stretch-to-fill, raycast OFF)


### Behavior

- Receives `MenuNavDirectionMessage` via `ISubscriber` (constructor injection)
- Lerps `uvRect` offset toward target (smooth, no snapping)
- Clamped to `[0, 1-Zoom]` in both axes (no blank space)
- Starts centered on enable
- Only receives messages while `BootTextController._isSelecting == true`

### Serialized Parameters

| Field | Default | Purpose |
|-------|:---:|---------|
| `_opacity` | 1.0 | RawImage alpha |
| `_zoom` | 0.5 | Fraction of texture visible |
| `_panSpeed` | 0.5 | UV units/sec at full deflection |
| `_lerpSpeed` | 8.0 | Smoothing rate |

## Settings Integration

### Video Settings Row 3: CRT Effect (Slider)

Left/Right → _sliderValue (0-1, step 0.05)
→ Publishes CRTIntensityChanged { Intensity = _sliderValue }
→ _settings.SetVideo(...)


### System Settings Row 7: CRT Effect (Cycle)

| Index | Label | Action |
|:---:|---|---|
| 0 | Always | `CRTModeChanged { MenuOnly = false }` + `CRTIntensityChanged { Intensity = 1 }` |
| 1 | Menu Only | `CRTModeChanged { MenuOnly = true }` |
| 2 | Off | `CRTIntensityChanged { Intensity = 0 }` |

### Persistence

All CRT settings saved via `SettingsSavable` → `SettingsSaveData` struct → `SaveManager` registry. Re-applied on load via `LoadFromData()`.

## Shaders

### `Game/CRT Failure` (existing, unmodified)

| Uniform | Type | Purpose |
|---------|------|---------|
| `_FailureAmount` | float (0-1) | Master intensity (early-out < 0.001) |
| `_PhosphorColor` | color | Tint |
| `_RetraceY` | float (-1 to 1) | Retrace line position (-1 = off) |
| `_Collapse` | float (0-1) | Screen contraction |

Effects: scanlines, sync tear, retrace line, phosphor flicker, UV jitter, collapse.

### `Game/CRT Roll Bar` (new)

| Uniform | Type | Purpose |
|---------|------|---------|
| `_Tint` | color | Band color |
| `_Y` | float (0-1) | Center position |
| `_Width` | float (0.01-0.5) | Band height |
| `_Intensity` | float (0-2) | Brightness (early-out < 0.001) |

Single smoothstep band. No scanlines, no flicker. Clean brightness mask.

## Constraints

- Total transition: **4 frames max** (never exceed 5)
- No fade/dissolve/cross-fade on menu content
- No coroutines or tweens in the state machine
- Roll bar is a **brightness** band, not a dark band
- Screen never goes fully black
- HUD is a **passive display** — no state, no timing, no decisions
- `CRTTransitionController` owns ALL transition logic
- No new messaging or DI patterns introduced
- Roll bar quad disabled when idle (zero GPU cost)

Save as Assets/Game/UI/CRT/crtOverlay_flow.md (or wherever your docs live). It's a single reference document covering the full pipeline from trigger to render.