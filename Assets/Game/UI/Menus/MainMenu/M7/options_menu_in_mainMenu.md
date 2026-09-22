┌─ AUDIO CONFIG ─────────────────────────────┐
│                                            │
│  MASTER    [████████████████░░░░]  78      │
│  SFX       [████████░░░░░░░░░░░░]  42      │
│  MUSIC     [████████████░░░░░░░░]  60      │
│  VOICE     [████████████████░░░░]  78      │
│  AMBIENT   [████░░░░░░░░░░░░░░░░]  15      │
│                                            │
│  ── ADJUST: ← →   APPLY: [ENTER] ──        │
└────────────────────────────────────────────┘


Canvas (Screen Space - Overlay)
│
├── CRT Overlay (RawImage, full-screen, top-most sorting)
│
└── MenuPanel (RectTransform, centered, fixed size ~640×400)
    │
    ├── PanelBorder (Image, 1px green border, no fill)
    │
    ├── Header (Horizontal Layout Group)
    │   ├── Title (Text: "AUDIO CONFIG")
    │   └── Spacer (empty RectTransform, stretch)
    │
    ├── SliderList (Vertical Layout Group)
    │   │   spacing: 4px, childAlignment: UpperLeft
    │   │   childForceExpandWidth: false
    │   │   childForceExpandHeight: false
    │   │
    │   ├── Row_Master (Horizontal Layout Group)
    │   │   │   spacing: 8px, childAlignment: MiddleLeft
    │   │   │
    │   │   ├── Cursor (Text: ">", width 12px)
    │   │   ├── Label (Text: "MASTER", width 80px)
    │   │   ├── Bar (Text: "████████████████░░░░", width 200px)
    │   │   └── Value (Text: "78", width 30px, right-aligned)
    │   │
    │   ├── Row_SFX (Horizontal Layout Group)
    │   │   ├── Cursor (Text: "  ", width 12px)
    │   │   ├── Label (Text: "SFX", width 80px)
    │   │   ├── Bar (Text: "████████░░░░░░░░░░░░", width 200px)
    │   │   └── Value (Text: "42", width 30px)
    │   │
    │   ├── Row_Music (Horizontal Layout Group)
    │   ├── Row_Voice (Horizontal Layout Group)
    │   └── Row_Ambient (Horizontal Layout Group)
    │
    └── Footer (Horizontal Layout Group)
        ├── HintLeft (Text: "← → ADJUST")
        ├── Spacer (stretch)
        └── HintRight (Text: "[ENTER] APPLY")


Unity gameobject vertical horizontal layout tutorial

View all
Component Settings
MenuPanel
Component	Setting
RectTransform	Anchored: center-center, size 640×400
(no layout group here)	It's the fixed container

SliderList — Vertical Layout Group
Property	Value
Spacing	4
Padding	L:16 T:8 R:16 B:8
Child Alignment	Upper Left
Child Force Expand Width	false
Child Force Expand Height	false
Control Child Width	false
Control Child Height	false

Each Row_* — Horizontal Layout Group
Property	Value
Spacing	8
Child Alignment	Middle Left
Child Force Expand Width	false
Child Force Expand Height	false

Individual Text children
Child	Width	Alignment
Cursor	12px fixed	Left
Label	80px fixed	Left
Bar	200px fixed	Left
Value	30px fixed	Right