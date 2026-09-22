a separate component that goes on whatever GameObject holds your Slider UI elements (the settings panel in your menu). It does not go on the CRTTransitionController object.

CRTTransitionOverlay (prefab)
 └─ CRTTransitionController   ← receives messages, drives the shader

SettingsPanel (in your menu UI)
 ├─ Slider (Idle Intensity)
 ├─ Slider (Transition Intensity)
 ├─ Slider (Idle Tear Height)
 ├─ ...
 └─ CRTSettingsPanel          ← publishes messages when sliders move

 They communicate purely through MessagePipe — no direct reference between them.

If you don't have a dedicated settings panel yet, create an empty GameObject under your menu Canvas, add the CRTSettingsPanel component, then create your Sliders as children and assign them in the Inspector.

How CRTSettingsPanel works in your menu
It's a dumb bridge between Unity UI events and MessagePipe. It has zero logic, zero state, zero knowledge of the CRT system.

┌─────────────────────────────────────────────────┐
│  Settings Panel (GameObject in your menu UI)    │
│                                                 │
│  ┌─────────────────────────────────────────┐    │
│  │  CRTSettingsPanel (component)           │    │
│  │                                         │    │
│  │  Awake():                               │    │
│  │    slider.onValueChanged.AddListener(   │    │
│  │      v => publisher.Publish(msg)        │    │
│  │    )                                    │    │
│  └─────────────────────────────────────────┘    │
│                                                 │
│  Children:                                      │
│    ├─ Slider "Idle Intensity"        (0–2)     │
│    ├─ Slider "Transition Intensity"  (0–2)     │
│    ├─ Slider "Idle Tear Height"      (0.005–0.2)│
│    ├─ Slider "Transition Tear Height"(0.005–0.2)│
│    ├─ Slider "Idle Collapse"         (0–1)     │
│    ├─ Slider "Transition Collapse"   (0–1)     │
│    ├─ Slider "Retrace Idle"          (0–2)     │
│    ├─ Slider "Retrace Transition"    (0–2)     │
│    ├─ Slider "Roll Bar Idle"         (0–2)     │
│    └─ Slider "Roll Bar Transition"   (0–2)     │
└─────────────────────────────────────────────────┘
         │
         │  MessagePipe (no direct reference)
         ▼
┌─────────────────────────────────────────────────┐
│  CRTTransitionController                        │
│  (separate GameObject, e.g. on a HUD prefab)    │
│                                                 │
│  Receives messages → writes shader uniforms     │
└─────────────────────────────────────────────────┘

Why separate them:

Concern	Where it lives
"What does the slider look like?" (position, label, range)	CRTSettingsPanel + Slider UI
"What does the value do?" (write to shader, state machine)	CRTTransitionController
"How do they talk?"	MessagePipe (decoupled)

This means you can swap the settings UI for a touch screen, a console prompt, or a saved config file without touching CRTTransitionController at all — as long as something publishes the same messages.

In practice: You create the Sliders in your menu scene (drag them into the Hierarchy under a "CRT Settings" panel), attach CRTSettingsPanel to the panel GameObject, assign each Slider in the Inspector, and you're done. The panel only exists while the settings menu is open — when it's destroyed, the subscriptions are gone, but the controller keeps running with whatever values were last set.