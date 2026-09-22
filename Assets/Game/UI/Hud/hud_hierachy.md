9/11/2026
HUD Hierarchy — Full Setup
HUDCanvas (Canvas, Screen Space - Overlay)
│   Canvas: Render Mode = Screen Space - Overlay, Sort Order = 100
│   CanvasGroup: Alpha = 1
│
├── VignetteImage (Image)
├── NoiseImage (Image)
├── BlackFadeImage (Image)
├── GuidancePanel (Panel)
│   ├── GuidanceText (TextMeshProUGUI)
│   └── CodeFragmentText (TextMeshProUGUI)
├── SignalBarsPanel (Panel)
│   ├── Bar1 (Image)
│   ├── Bar2 (Image)
│   ├── Bar3 (Image)
│   └── Bar4 (Image)
├── OrbCountPanel (Panel)
│   └── CountText (TextMeshProUGUI)
└── (no visual for pause — it operates on HUDCanvas CanvasGroup)


Unity HUD setup tutorial

View all
1. HUDCanvas (root)
Property	Value
GameObject name	HUDCanvas
Canvas	Render Mode: Screen Space - Overlay, Sort Order: 100
CanvasGroup	Alpha: 1
RectTransform	Anchors: Stretch-Stretch, Offset: 0 all sides
Scripts	HudController, HudManager

HudController Inspector:

Field	Drag In
_hudManager	Self (same object)
_guidance	GuidancePanel
_signalBars	SignalBarsPanel
_orbCount	OrbCountPanel
_damageVignette	HUDCanvas (it lives on the root)
_guidancePanelIndex	0
_signalBarsPanelIndex	1
_orbCountPanelIndex	2
_vignettePanelIndex	3

HudManager Inspector: (configure your panel array to match the indices above)

Index	Panel GameObject
0	GuidancePanel
1	SignalBarsPanel
2	OrbCountPanel
3	VignetteImage (or a wrapper Panel around it)

2. VignetteImage
Property	Value
RectTransform	Stretch-Stretch, Offset 0
Image	Radial gradient texture (transparent center, red edges)
Color	R:1 G:0 B:0 A:0
Raycast Target	false
Script	DamageVignetteController

DamageVignetteController Inspector:

Field	Drag In
_vignetteImage	Self (Image component)
_noiseImage	NoiseImage


Unity vignette UI tutorial

View all
3. NoiseImage
Property	Value
RectTransform	Stretch-Stretch, Offset 0
Image	Static noise texture (tileable)
Color	R:1 G:1 B:1 A:0
Raycast Target	false
Sibling index	Above VignetteImage

4. BlackFadeImage
Property	Value
RectTransform	Stretch-Stretch, Offset 0
Image	White solid (tinted black via color)
Color	R:0 G:0 B:0 A:0
Raycast Target	false
Sibling index	Top-most (above everything)

Used by HUDPauseController for the quit fade.

5. GuidancePanel
Property	Value
RectTransform	Anchors: Bottom-Center, Pos Y: 80, Width: 800, Height: 120
Image	None (or transparent)
Raycast Target	false
Script	GuidanceTextController

Children:

GuidanceText:

Property	Value
RectTransform	Anchors: Top-Left, Pos (20, -10), Width: 760, Height: 60
TMP Font	IBM Plex Mono
Font Size	24
Color	#E0F0FF
Alignment	Left-Middle
Text	(empty — filled at runtime)
Raycast Target	false

CodeFragmentText:

Property	Value
RectTransform	Anchors: Top-Left, Pos (20, -10), Width: 760, Height: 60
TMP Font	IBM Plex Mono
Font Size	24
Color	#E0F0FF
Alignment	Left-Middle
Text	(empty — filled at runtime)
Raycast Target	false
Active	Unchecked (inactive by default)

GuidanceTextController Inspector:

Field	Drag In
_guidanceText	GuidanceText
_codeFragmentText	CodeFragmentText

6. SignalBarsPanel
Property	Value
RectTransform	Anchors: Top-Right, Pos (-20, -20), Width: 120, Height: 20
Image	None
Raycast Target	false
Script	SignalBarsController

Children (4 Images, left to right):

Child	RectTransform	Image	Color
Bar1	Pos (0, 0), Size (20, 20)	Solid white	#E0F0FF, A:0
Bar2	Pos (28, 0), Size (20, 20)	Solid white	#E0F0FF, A:0
Bar3	Pos (56, 0), Size (20, 20)	Solid white	#E0F0FF, A:0
Bar4	Pos (84, 0), Size (20, 20)	Solid white	#E0F0FF, A:0

All: Raycast Target = false

SignalBarsController Inspector:

Field	Value
_bars[0]	Bar1
_bars[1]	Bar2
_bars[2]	Bar3
_bars[3]	Bar4

7. OrbCountPanel
Property	Value
RectTransform	Anchors: Top-Left, Pos (20, -20), Width: 100, Height: 30
Image	None
Raycast Target	false
Script	OrbCountController

Child — CountText:

Property	Value
RectTransform	Anchors: Stretch, Offset 0
TMP Font	IBM Plex Mono
Font Size	24
Color	#E0F0FF
Alignment	Left-Middle
Text	0
Raycast Target	false

OrbCountController Inspector:

Field	Drag In
_countText	CountText

8. Scripts on HUDCanvas (root)
Add these components directly to the HUDCanvas GameObject:

Script	Key Inspector Fields
FlinchHUDController	_hudManager → self, _hudCanvasGroup → self (CanvasGroup)
HUDPauseController	_hudCanvasGroup → self, _blackFadeImage → BlackFadeImage, 3 AudioSources (see below)

Audio Sources for HUDPauseController:

Create 3 child AudioSources on HUDCanvas:

HUDCanvas
├── HoldChargeSource (AudioSource)
├── HoldCompleteSource (AudioSource)
└── HoldReleaseSource (AudioSource)

AudioSource	Settings
HoldChargeSource	Play On Awake: false, Loop: true, Clip: (assign later)
HoldCompleteSource	Play On Awake: false, Loop: false, Clip: (assign later)
HoldReleaseSource	Play On Awake: false, Loop: false, Clip: (assign later)

Drag them into HUDPauseController's _holdChargeSource, _holdCompleteSource, _holdReleaseSource.

9. Sibling order (bottom to top in Hierarchy)
VignetteImage          ← z-order 0 (behind everything)
NoiseImage             ← z-order 1
GuidancePanel          ← z-order 2
SignalBarsPanel        ← z-order 3
OrbCountPanel          ← z-order 4
BlackFadeImage         ← z-order 5 (top-most, covers all on quit)

10. VContainer / DI — no Inspector work needed
These are all injected at runtime:

Component	Injected
HudController	HudState
FlinchHUDController	HudState
HUDPauseController	InputManager, HudState
DamageVignetteController	HudState

11. Verify
Play mode → main menu → start game
HUD should be hidden (panels inactive via HudManager)
After intro video finishes → SetPhase(Gameplay) → all 4 panels show
ESC → GamePaused → CanvasGroup fades to 0
ESC again → GameResumed → CanvasGroup fades to 1
Hold ESC 1.2s → black fade → quit
*************** prior 
HUD  (Canvas)
│
│  Components:
│    • Canvas (Render Mode: Screen Space - Overlay)
│    • Canvas Scaler (Reference: 1920×1080, Match: 0.5)
│    • Graphic Raycaster
│    • CanvasGroup
│    • HudController
│    • HudManager
│    • FlinchHUDController
│    • HUDPauseController
│    • ObjectivePulseController
│
├── GuidanceText  (bottom-center, anchored)
│   │
│   │  Components:
│   │    • CanvasGroup
│   │    • TextMeshProUGUI
│   │        Font: IBM Plex Mono / JetBrains Mono
│   │        Color: #E0F0FF, Alpha: 0.8
│   │        Alignment: Bottom Center
│   │        Font Size: 14
│   │    • GuidanceTextController
│   │
│   └── CodeFragment  (same anchor, initially INACTIVE)
│       │
│       │  Components:
│       │    • TextMeshProUGUI
│       │        Font: IBM Plex Mono (monospace)
│       │        Color: #E0F0FF, Alpha: 0.8
│       │        Alignment: Bottom Center
│       │        Font Size: 12 (slightly smaller than guidance)
│
├── SignalBars  (top-right corner, anchored)
│   │
│   │  Components:
│   │    • CanvasGroup
│   │    • SignalBarsController
│   │    • Horizontal Layout Group
│   │        Spacing: 2px
│   │        Padding: 0
│   │
│   ├── Bar1  (8×12px)
│   │   └── Image (Color: #E0F0FF, Alpha: 0)
│   ├── Bar2  (8×12px)
│   │   └── Image (Color: #E0F0FF, Alpha: 0)
│   ├── Bar3  (8×12px)
│   │   └── Image (Color: #E0F0FF, Alpha: 0)
│   └── Bar4  (8×12px)
│       └── Image (Color: #E0F0FF, Alpha: 0)
│
├── OrbCount  (bottom-right corner, anchored)
│   │
│   │  Components:
│   │    • CanvasGroup
│   │    • TextMeshProUGUI
│   │        Font: IBM Plex Mono
│   │        Color: #E0F0FF, Alpha: 0.8
│   │        Alignment: Bottom Right
│   │        Font Size: 14
│   │        Text: "0"
│   │    • OrbCountController
│   │        _humanoidFlash: #FFD080
│   │        _nonHumanoidFlash: #80FFD0
│   │        _flashDuration: 0.5
│
└── VignetteOverlay  (full-screen, Stretch)
    │
    │  Components:
    │    • CanvasGroup
    │    • DamageVignetteController
    │
    ├── Vignette  (full-screen, Stretch)
    │   │
    │   │  Components:
    │   │    • Image
    │   │        Texture: radial gradient (transparent center → #FF0000 edges)
    │   │        Color: #FF0000, Alpha: 0
    │   │        Raycast Target: OFF
    │
    └── Noise  (full-screen, Stretch)
        │
        │  Components:
        │    • Image
        │        Texture: static/noise (grainy B&W)
        │        Color: #FFFFFF, Alpha: 0
        │        Raycast Target: OFF

Inspector Wiring (component by component)
HudController (on HUD root)
Field	Drag in
_hudManager	HudManager (same object)
_guidance	GuidanceTextController (on GuidanceText)
_signalBars	SignalBarsController (on SignalBars)
_orbCount	OrbCountController (on OrbCount)
_damageVignette	DamageVignetteController (on VignetteOverlay)
_flinch	FlinchHUDController (on HUD root)
_pause	HUDPauseController (on HUD root)
_guidancePanelIndex	0
_signalBarsPanelIndex	1
_orbCountPanelIndex	2
_vignettePanelIndex	3

HudManager (on HUD root)
Field	Value
_hudPanels[0]	panel: GuidanceText, isConstant: false, displayDuration: 5.0
_hudPanels[1]	panel: SignalBars, isConstant: true
_hudPanels[2]	panel: OrbCount, isConstant: true
_hudPanels[3]	panel: VignetteOverlay, isConstant: false, displayDuration: 0.0
_fadeDuration	0.3
_fadeEaseType	easeInOutQuad

FlinchHUDController (on HUD root)
Field	Drag in
_hudManager	HudManager (same object)
_hudCanvasGroup	CanvasGroup (on HUD root)
_flickerDuration	0.1
_freezeDuration	1.5

HUDPauseController (on HUD root)
Field	Drag in
_hudCanvasGroup	CanvasGroup (on HUD root)
_inputManager	InputManager (on player or bootstrap)
_fadeDuration	0.3
_handshakeDelay	0.3

ObjectivePulseController (on HUD root)
Field	Value
_fragmentIndex	0

(No drag-in fields — all message-driven.)

GuidanceTextController (on GuidanceText)
Field	Drag in
_guidanceText	TextMeshProUGUI (same object)
_codeFragmentText	TextMeshProUGUI (on CodeFragment child)
_charInterval	0.03
_fadeOutDuration	0.5
_codeFragmentDuration	2.0

*************


HUD  (Canvas)
│
│  Components:
│    • Canvas (Render Mode: Screen Space - Overlay)
│    • Canvas Scaler (Reference: 1920×1080, Match: 0.5)
│    • Graphic Raycaster
│    • CanvasGroup
│    • HudController
│    • HudManager
│    • FlinchHUDController
│    • HUDPauseController
│
├── GuidanceText  (bottom-center)
│   │
│   │  Components:
│   │    • CanvasGroup
│   │    • TextMeshProUGUI (sans-serif, #E0F0FF @ 80% alpha)
│   │    • GuidanceTextController
│   │
│   └── CodeFragment  (same position, initially inactive)
│       │
│       │  Components:
│       │    • TextMeshProUGUI (monospace, #E0F0FF @ 80%, slightly smaller)
│
├── SignalBars  (top-right corner)
│   │
│   │  Components:
│   │    • CanvasGroup
│   │    • SignalBarsController
│   │    • Horizontal Layout Group (2px spacing)
│   │
│   ├── Bar1  →  Image (8×12px, #E0F0FF, alpha 0)
│   ├── Bar2  →  Image (8×12px, #E0F0FF, alpha 0)
│   ├── Bar3  →  Image (8×12px, #E0F0FF, alpha 0)
│   └── Bar4  →  Image (8×12px, #E0F0FF, alpha 0)
│
├── OrbCount  (bottom-right corner)
│   │
│   │  Components:
│   │    • CanvasGroup
│   │    • TextMeshProUGUI (sans-serif, #E0F0FF @ 80%)
│   │    • OrbCountController
│
└── VignetteOverlay  (full-screen, stretch)
    │
    │  Components:
    │    • CanvasGroup
    │    • DamageVignetteController
    │
    ├── Vignette  (full-screen)
    │   │
    │   │  Components:
    │   │    • Image (radial gradient: transparent center → red #FF0000 edges, alpha 0)
    │
    └── Noise  (full-screen)
        │
        │  Components:
        │    • Image (static/noise texture, alpha 0)   





