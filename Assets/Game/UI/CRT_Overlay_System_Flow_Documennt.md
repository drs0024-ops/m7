# CRT Overlay System — Flow Document

## Architecture Overview

┌─────────────────────────────────────────────────────────────────────┐
│ HUD Canvas (Screen Space - Overlay) │
│ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ HUD_Content (CanvasGroup ← _hudCanvasGroup) │ │
│ │ ├── Guidance Panel │ │
│ │ ├── Signal Bars Panel │ │
│ │ └── Orb Count Panel │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ CRT_Transition (Prefab) ← sibling, NOT under CanvasGroup │ │
│ │ ├── CRT_Overlay (RawImage → M_CRT_Overlay) │ │
│ │ ├── Roll_Bar (RawImage → M_CRT_RollBar) │ │
│ │ └── Components: │ │
│ │ ├── CRTTransitionController │ │
│ │ ├── CRTCollapseController │ │
│ │ └── CRTDamageListener │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Black_Fade (Image, alpha=0, sorting=200) │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ HUD_Controllers (empty GO) │ │
│ │ ├── HudManager │ │
│ │ ├── HudController │ │
│ │ ├── HUDPauseController │ │
│ │ └── FlinchHUDController │ │
│ └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘


**Key rule:** `CRT_Transition` is a **sibling** of `HUD_Content`, not a child. Pause/fade/flicker affect `HUD_Content` only. The CRT is always visible.

---

## Component Responsibilities

| Component | Assembly | Role |
|-----------|----------|------|
| `CRTTransitionController` | UI | State machine (Idle→Ramping→Swap→Settling), per-frame scroll animation, material uniform management |
| `CRTCollapseController` | UI | Drives `_Collapse` uniform via coroutine with easing curve |
| `CRTDamageListener` | UI | Subscribes to gameplay messages, drives damage-based CRT response |
| `FlinchHUDController` | UI | HUD flicker + freeze + CRT RGB split on flinch |
| `HudController` | UI | Phase-based panel show/hide (does NOT manage CRT) |
| `HudManager` | UI | Generic fade in/out for managed panels (does NOT manage CRT) |
| `HUDPauseController` | UI | Fades `HUD_Content` on pause, long-press-quit |
| `PlayerHealth` | Gameplay | Pure HP logic, publishes messages. Zero UI references. |
| `CRTShaderPreset` | UI | ScriptableObject holding all static shader values per state |

---

## Data Flow

### Message Dependencies

Gameplay (PlayerHealth)
│
├── Publishes: PlayerDamaged(damage, direction, inputX)
├── Publishes: EntityHealthChanged(transform, maxHP, currentHP)
└── Publishes: PlayerDied(position)

UI (CRTDamageListener)
│
├── Subscribes: PlayerDamaged → RGB split + camera shake
├── Subscribes: EntityHealthChanged → failure amount + conscience pulse
└── Subscribes: PlayerDied → collapse animation

UI (FlinchHUDController)
│
└── Subscribes: FlinchTriggeredMessage → HUD flicker + CRT RGB split

UI (CRTTransitionController)
│
├── Subscribes: ScreenTransitionRequested → TriggerTransition()
├── Subscribes: CRTSettingsChanged → Update presets
└── Subscribes: GamePhaseChangedMessage → _menuOnly visibility gate


**No `Gameplay → UI` assembly reference exists.** All cross-assembly communication is via MessagePipe.

---

## State Machine (CRTTransitionController)

     TriggerTransition()
┌──────────────────────────────┐
│                              ▼

┌─────────┐ ┌──────────┐
│ Idle │ │ Ramping │ (duration * 0.5)
│ │ │ │ Roll bar: Y 0.9→0.5, intensity 0→1
└─────────┘ └──────────┘
▲ │
│ │ elapsed >= rampDuration
│ ▼
│ ┌──────────┐
│ │ Swap │ (1 frame)
│ │ │ Publish ScreenSwapped
│ └──────────┘
│ │
│ ▼
│ ┌──────────┐
└───────────────────│Settling │ (duration * 0.5)
ApplyIdleMaterial() │ │ Roll bar: Y=0.1, intensity=OVERSHOOT
└──────────┘


### What changes per state

| Uniform | Idle | Ramping/Swap | Settling |
|---------|------|--------------|----------|
| `_FailureAmount` | `_idlePreset.failureAmount` | `_transitionPreset.failureAmount` | `_transitionPreset.failureAmount` |
| `_RetraceIntensity` | `_idlePreset.retraceIntensity` | `_transitionPreset.retraceIntensity` | `_transitionPreset.retraceIntensity` |
| `_TearHeight` | `_idlePreset.tearHeight` | `_transitionPreset.tearHeight` | `_transitionPreset.tearHeight` |
| `_TearOpacity` | `_idlePreset.tearOpacity` | `_transitionPreset.tearOpacity` | `_transitionPreset.tearOpacity` |
| `_TransitionOpacity` | 1.0 | `_transitionPreset.transitionOpacity` | `_transitionPreset.transitionOpacity` |
| Tear edges | `_idlePreset.*` | `_transitionPreset.*` | `_transitionPreset.*` |
| Motion params | `_idlePreset.*` | `_transitionPreset.*` | `_transitionPreset.*` |

---

## Per-Frame Animation (UpdateScrolling)

Runs every frame regardless of state:

| Element | Behavior |
|---------|----------|
| Retrace Line 1 | `_retraceY` scrolls at `_retraceSpeed` UV/s. Wraps: `[-height, 1+height]` |
| Retrace Line 2 | `_retraceY2` scrolls at `_retraceSpeed2` UV/s. Same wrap. Ghost (50% brightness) |
| Tear Bar | `_tearY` scrolls at `_tearSpeed` UV/s. Pauses `_tearPause` seconds at off-screen edge before re-entering |

---

## Shader Render Layers (execution order)

| # | Layer | Uniform(s) | Always active? |
|---|-------|-----------|----------------|
| 1 | Scanlines | `_FailureAmount`, `_PhosphorColor` | Yes (if failure > 0) |
| 2 | Sync Tear | `_TearY`, `_TearHeight`, `_TearDirection`, `_TearLeadingSize/Blur`, `_TearTrailingSize/Blur`, `_TearOpacity`, `_TearColor` | Yes (if tear in range) |
| 3 | Retrace Line | `_RetraceY`, `_RetraceIntensity`, `_RetraceWidth`, `_RetraceOpacity` | Yes (if Y in [0,1]) |
| 3b | Retrace Ghost | `_RetraceY2` (same intensity/width) | Yes |
| 4 | Phosphor Flicker | `_FailureAmount`, `_Time.y` | Yes (if failure > 0) |
| 5 | UV Jitter | `_FailureAmount`, `_Time.y` | Yes (if failure > 0) |
| 6 | Collapse | `_Collapse` | Only when > 0 |
| 7 | RGB Split | `_RGBSplit` | Only when > 0 (event-driven) |
| 8 | Conscience Pulse | `_ConsciencePulse`, `_Time.y` | Only when > 0 (HP-driven) |

**Early-out:** If all of `_FailureAmount`, `_RetraceIntensity`, `_TearOpacity`, `_Collapse`, `_ConsciencePulse`, `_RGBSplit` are ~0, returns `half4(0,0,0,0)` immediately.

---

## Damage Response Flow

PlayerHealth.Damage(amount, direction)
│
├── Publish: EntityHealthChanged(transform, maxHP, currentHP)
│ │
│ └── CRTDamageListener.OnHealthChanged()
│ ├── SetFailureAmount(lerp(idle, max, 1-hpRatio))
│ └── SetConsciencePulse(smoothstep(0.3, 0.05, hpRatio))
│
└── Publish: PlayerDamaged(amount, direction, inputX)
│
└── CRTDamageListener.OnPlayerDamaged()
├── Publish: CameraShakeRequest(severity * scale, profile)
└── CRTTransitionController.PlayRGBSplit(amount, duration)


### Visual result by HP threshold

| HP % | Failure | Pulse | Visual |
|------|---------|-------|--------|
| 100 | 0.15 | 0 | Faint scanlines, subtle tear |
| 75 | 0.53 | 0 | Noticeable degradation |
| 50 | 0.83 | 0 | Heavy tear, flicker, jitter |
| 30 | 1.13 | 0.0 → 1.0 | Heartbeat fades in |
| 10 | 1.43 | 1.0 | Full pulse, near collapse |
| 0 | — | — | Collapse triggers |

---

## Death Flow

PlayerHealth.Damage() → CurrentHealth <= 0
│
└── Publish: PlayerDied(position)
│
└── CRTDamageListener.OnPlayerDied()
├── Set CRTCollapseController.OnComplete callback
└── CRTCollapseController.PlayCollapse()
│
└── Coroutine: _Collapse 0→1 over 1.2s
Curve: (0,0) → (0.15,0) → (0.4,1) → (1,1)
│
├── 0–0.18s: Hold (frozen)
├── 0.18–0.48s: Snap (line forms)
└── 0.48–1.2s: Settle (dot phase)
│
└── OnComplete.Invoke() → death menu


### Respawn

Respawn flow:
├── CRTDamageListener.ResetAll()
│ ├── SetFailureAmount(idle)
│ ├── SetConsciencePulse(0)
│ └── CRTCollapseController.ResetCollapse() // _Collapse = 0
└── (optional) Publish CRTSettingsChanged to re-apply phase presets


---

## Flinch Flow

FlinchTriggeredMessage published
│
└── FlinchHUDController.OnFlinch()
├── _hudState.IsFrozen = true
├── CRTTransitionController.PlayRGBSplit(0.005, 0.2) ← heavier than damage
├── LeanTween: HUD_Content alpha 1→0→1 (0.1s total)
└── UniTask.Delay(1.5s) → IsFrozen = false → Publish HudUnfrozenMessage


---

## Phase Change Flow

GamePhaseChangedMessage published
│
├── HudController.OnPhaseChanged()
│ ├── If Gameplay/Climax: ShowAllPanels()
│ └── Else: HideAllPanels()
│
├── CRTTransitionController.OnPhaseChanged()
│ └── If _menuOnly: toggle _overlayRawImage.enabled
│
└── (External phase manager)
└── Publish CRTSettingsChanged(phase-specific preset)
│
└── CRTTransitionController.OnSettingsChanged()
├── Update _idlePreset fields
├── Update _transitionPreset fields
└── If Idle: ApplyIdleMaterialState()


---

## Pause Flow

GamePaused published
│
└── HUDPauseController.Pause()
└── LeanTween: HUD_Content CanvasGroup alpha → 0 (0.3s, ignoreTimeScale)
│
└── CRT_Transition UNAFFECTED (sibling, not child)
└── Roll bar continues scrolling
└── Retrace lines continue scrolling
└── Tear continues scrolling

GameResumed published
│
└── HUDPauseController.Resume()
└── Delay 0.3s → LeanTween: HUD_Content alpha → 1


---

## Asset Dependencies

| Asset | Type | Used By |
|-------|------|---------|
| `CRT_Preset_Gameplay` | `CRTShaderPreset` | `CRTTransitionController._idlePreset` (gameplay scene) |
| `CRT_Preset_Transition` | `CRTShaderPreset` | `CRTTransitionController._transitionPreset` |
| `CRT_Preset_Menu` | `CRTShaderPreset` | `CRTTransitionController._idlePreset` (menu scene) |
| `M_CRT_Overlay` | Material (instance) | `CRT_Overlay` RawImage + `CRTCollapseController` |
| `M_CRT_RollBar` | Material (instance) | `Roll_Bar` RawImage |
| `Impact_Damage` | `ScreenShakeProfile` | `CRTDamageListener._damageShakeProfile` |

---

## Assembly References

Core (messages, interfaces, enums)
↑
Gameplay (PlayerHealth, CameraShakeProfile, InputManager)
↑
UI (all CRT components, HudManager, HudController, etc.)


**One direction only.** `Gameplay` has zero references to `UI`.

---

## Shader Uniform Reference

| Uniform | Type | Range | Driven By |
|---------|------|-------|-----------|
| `_PhosphorColor` | float4 | — | Preset / `SetScreenTint()` |
| `_FailureAmount` | float | 0–2 | Preset / `SetFailureAmount()` / damage |
| `_RetraceY` | float | -1–2 | `UpdateScrolling()` per-frame |
| `_RetraceY2` | float | -1–2 | `UpdateScrolling()` per-frame |
| `_RetraceIntensity` | float | 0–2 | Preset (state-dependent) |
| `_RetraceOpacity` | float | 0–1 | Preset |
| `_RetraceWidth` | float | 0.001–0.1 | Preset |
| `_Collapse` | float | 0–1 | `CRTCollapseController` coroutine |
| `_TearY` | float | -1–2 | `UpdateScrolling()` per-frame |
| `_TearHeight` | float | 0.005–0.2 | Preset / `TearHeight` property override |
| `_TearOpacity` | float | 0–1 | Preset (state-dependent) |
| `_TearColor` | float4 | — | Preset / `SetTearColor()` |
| `_TearDirection` | float | -1 or 1 | Preset |
| `_TearLeadingSize` | float | 0.005–0.2 | Preset |
| `_TearLeadingBlur` | float | 0–1 | Preset |
| `_TearTrailingSize` | float | 0.005–0.5 | Preset |
| `_TearTrailingBlur` | float | 0–1 | Preset |
| `_RGBSplit` | float | 0–0.05 | Event-driven (damage, flinch) |
| `_ConsciencePulse` | float | 0–1 | HP-driven (smoothstep) |
| `_TransitionOpacity` | float | 0–1 | Preset |

---

## File Map

Assets/
├── Scripts/
│ ├── Core/ (asmdef: Core)
│ │ ├── Enums/
│ │ │ └── CRTScrollDirection.cs
│ │ ├── Messages/
│ │ │ ├── PlayerDamaged.cs
│ │ │ ├── PlayerDied.cs
│ │ │ ├── EntityHealthChanged.cs
│ │ │ ├── ScreenTransitionRequested.cs
│ │ │ ├── ScreenSwapped.cs
│ │ │ ├── CRTSettingsChanged.cs
│ │ │ ├── GamePhaseChangedMessage.cs
│ │ │ ├── FlinchTriggeredMessage.cs
│ │ │ ├── HudUnfrozenMessage.cs
│ │ │ ├── GamePaused.cs
│ │ │ └── GameResumed.cs
│ │ └── Interfaces/
│ │ ├── IDamagable.cs
│ │ ├── ISaveable.cs
│ │ └── IStartable.cs
│ │
│ ├── Gameplay/ (asmdef: Gameplay)
│ │ ├── Player/
│ │ │ └── PlayerHealth.cs
│ │ └── Camera/
│ │ ├── CameraShakeRequest.cs
│ │ └── ScreenShakeProfile.cs
│ │
│ └── UI/ (asmdef: UI)
│ ├── CRT/
│ │ ├── CRTTransitionController.cs
│ │ ├── CRTCollapseController.cs
│ │ ├── CRTDamageListener.cs
│ │ ├── CRTShaderPreset.cs
│ │ └── CRTSettings.cs
│ ├── HudManager.cs
│ ├── HudController.cs
│ ├── HUDPauseController.cs
│ ├── FlinchHUDController.cs
│ └── (other HUD controllers)
│
├── Shaders/
│ └── CRT_Failure.shader
│
├── Materials/
│ ├── M_CRT_Overlay.mat
│ └── M_CRT_RollBar.mat
│
├── Prefabs/
│ └── UI/
│ ├── CRT_Transition.prefab
│ └── Presets/
│ ├── CRT_Preset_Gameplay.asset
│ ├── CRT_Preset_Transition.asset
│ ├── CRT_Preset_Menu.asset
│ └── Impact_Damage.asset

