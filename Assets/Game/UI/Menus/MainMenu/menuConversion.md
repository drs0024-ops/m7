Step-by-Step: Merge OptionsMenu into MainMenu
Step 1: Move the Options UI into the MainMenu scene

In your MainMenu scene, add a child to the Canvas:

MainMenuScene
└── HUD (Canvas)
    ├── MainMenuPanel  (active)
    │   ├── NewGameButton
    │   ├── LoadGameButton
    │   ├── OptionsButton
    │   └── ExitButton
    │
    └── OptionsPanel  (INACTIVE)
        ├── CanvasGroup (alpha 0)
        ├── VolumeSlider
        ├── MusicSlider
        ├── SFXSlider
        ├── ControllerRemapUI
        └── BackButton

Drag your existing Options UI elements from the Options scene into this new OptionsPanel GameObject. Uncheck Active on OptionsPanel.

Step 2: Update MainMenuView

Replace OnOptionsClicked and add OnOptionsBackClicked:

[SerializeField] private GameObject _mainMenuPanel;
[SerializeField] private GameObject _optionsPanel;
[SerializeField] private CanvasGroup _optionsCanvasGroup;

public void OnOptionsClicked()
{
    _mainMenuPanel.SetActive(false);
    _optionsPanel.SetActive(true);
    _optionsCanvasGroup.alpha = 0f;
    LeanTween.alpha(_optionsCanvasGroup.gameObject, 1f, 0.3f)
        .setEase(LeanTweenType.easeInOutQuad);
}

public void OnOptionsBackClicked()
{
    LeanTween.alpha(_optionsCanvasGroup.gameObject, 0f, 0.3f)
        .setEase(LeanTweenType.easeInOutQuad)
        .setOnComplete(() =>
        {
            _optionsPanel.SetActive(false);
            _mainMenuPanel.SetActive(true);
        });
}

Wire the OptionsButton.onClick → OnOptionsClicked, BackButton.onClick → OnOptionsBackClicked in the inspector.

Step 3: Remove GameState.OptionsMenu routing from GameStateSceneRouter

In OnStateChanged, delete:

// DELETE:
case GameState.OptionsMenu:
    HandleOptionsMenu();
    break;

Delete the HandleOptionsMenu() method entirely.

Step 4: Remove CloseOptionsMenuRequested handling

In GameStateSceneRouter, delete:

// DELETE:
_closeOptionsSub = GlobalMessagePipe.GetSubscriber<CloseOptionsMenuRequested>();
_subscriptions.Add(_closeOptionsSub.Subscribe(_ => CloseOptionsMenu()));

Delete the CloseOptionsMenu() method and SetStateBeforeOptions() method.

Delete the _stateBeforeOptions field.

Step 5: Update GameStateInputHandler

Currently:

case GameState.OptionsMenu:
    _closeOptionsMenuPublisher.Publish(default);
    _stateMachine.SetState(GameState.Paused);
    break;

Since options is no longer a game state, this case disappears. If the player presses Escape while the OptionsPanel is open (in MainMenu), it should just close the panel. Handle it in MainMenuView:

private void Update()
{
    if (_optionsPanel.activeSelf && _inputManager.EscapeWasPressed)
        OnOptionsBackClicked();
}

(Inject InputManager into MainMenuView if not already present.)

Then in GameStateInputHandler, delete the OptionsMenu case and the _closeOptionsMenuPublisher field.

Step 6: Remove GameState.OptionsMenu from the enum

public enum GameState
{
    Boot,
    IntroVideo,
    MainMenu,
    Loading,
    Gameplay,
    Paused,
    GameOver,
    QuitGame
}

Grep for GameState.OptionsMenu — should be zero hits after Steps 3–5.

Step 7: Delete the Options scene

Remove the Options scene from Edit → Build Settings → Scenes In Build
Delete the scene file (or move it to an archive folder)
Step 8: Clean up SceneRegistry

Remove:

// DELETE:
public string OptionsMenuScene => "Options";

Grep for OptionsMenuScene — should be zero hits.

Step 9: Delete dead messages

Assets/Game/Core/Messages/CloseOptionsMenuRequested.cs  → DELETE
Assets/Game/Core/Messages/OptionsClosed.cs              → DELETE

Grep for both — should be zero hits.

Step 10: Remove PopMenuAsync usage (optional)

SceneTransitionOrchestrator.PopMenuAsync() is now only called for the Options flow. If nothing else calls it, delete the method. If you might need it for a future sub-menu, keep it.

Step 11: Verify

Check	Expected
Grep OptionsMenu	Zero hits (or only in comments)
Grep OptionsClosed	Zero hits
Grep CloseOptionsMenuRequested	Zero hits
Grep PopMenuAsync	Zero hits (if deleted)
Build Settings	No Options scene listed
Play → MainMenu → Options	Panel fades in. Back fades out. No scene load.
Play → Escape during Options	Panel closes.

Total files touched:

File	Action
MainMenuView.cs	Add panel toggle + Escape handling
GameStateSceneRouter.cs	Delete HandleOptionsMenu, CloseOptionsMenu, _stateBeforeOptions, OptionsMenu case
GameStateInputHandler.cs	Delete OptionsMenu case, _closeOptionsMenuPublisher
GameState.cs	Remove OptionsMenu
SceneRegistry.cs	Remove OptionsMenuScene
SceneTransitionOrchestrator.cs	Delete PopMenuAsync (optional)
CloseOptionsMenuRequested.cs	Delete file
OptionsClosed.cs	Delete file
Options scene	Delete / remove from build


NEW M7 MENU
Wiring (Inspector)
Field	Drag in
_mainMenuPanel	MainMenuPanel GameObject
_optionsPanel	OptionsPanel GameObject
_optionsCanvasGroup	CanvasGroup on OptionsPanel
_panelFadeDuration	0.3

Button wiring (in inspector, on each Button):

Button	On Click →
NewGameButton	MenuMaster.OnNewGameClicked()
LoadGameButton	MenuMaster.OnLoadGameClicked()
OptionsButton	MenuMaster.OnOptionsClicked()
ExitButton	MenuMaster.OnExitClicked()
BackButton (in OptionsPanel)	MenuMaster.OnOptionsBackClicked()


C# MenuMaster tutorial

View all
What to delete
MainMenuView.cs → DELETE
MainMenuFader.cs → DELETE (fade logic is now in MenuMaster)
MenuNavigationHighlighter.cs → DELETE (no highlight system; the menu is text, not a traditional selectable list)
Flow
Scene loads → MenuMaster starts hidden
    → GamePhaseController.SetPhase(Letter) → IntroVideo plays (menu stays hidden)
    → VideoFinished → SetPhase(Gameplay) → menu stays hidden (gameplay flag)
    → Player quits → SetState(MainMenu) → phase clears Gameplay
        → MenuMaster.OnPhaseChanged() → ShowMainMenu()
            → MainMenuPanel active, OptionsPanel inactive

Player clicks Options:
    → MainMenuPanel off, OptionsPanel on, fade in 0.3s

Player presses Escape (or clicks Back):
    → OptionsPanel fades out 0.3s → MainMenuPanel on



    ****************** LAST UPDATE
    MainMenuScene
│
├── Main Camera
│   └── Camera
│
├── Background  (EMPTY GameObject)
│   └── Renderer  (child, NOT a UI element)
│       └── MeshRenderer (corridor model or sprite)
│           Material: dim corridor, warm amber, slightly out of focus
│
├── FilmGrain  (EMPTY GameObject)
│   └── Renderer
│       └── MeshRenderer (full-screen quad)
│           Material: film grain / CRT scanline, opacity 5%
│
├── MainMenu  (Canvas)
│   │
│   │  Components:
│   │    • Canvas (Render Mode: Screen Space - Overlay)
│   │    • Canvas Scaler (Reference: 1920×1080, Match: 0.5)
│   │    • Graphic Raycaster
│   │    • MenuMaster
│   │
│   ├── MainMenuPanel  (EMPTY GameObject)
│   │   │
│   │   │  Components:
│   │   │    • CanvasGroup
│   │   │    • BootTextController
│   │   │        _lines: ["M7 OPTICS v2.4.1", "SIGNAL: ACTIVE", "SUBJECT: PRESENT", "", "> NEW SESSION", "> RESUME SESSION", "> CONFIGURATION", "> TERMINATE"]
│   │   │        _charInterval: 0.03
│   │   │        _linePause: 0.8
│   │   │        _blinkInterval: 0.5
│   │   │
│   │   └── BootText  (UI Element)
│   │       │
│   │       │  Components:
│   │       │    • RectTransform (anchored: Bottom-Left, offset 80,80)
│   │       │    • TextMeshProUGUI
│   │       │        Font: IBM Plex Mono
│   │       │        Font Size: 18
│   │       │        Color: #E0F0FF, Alpha: 0.8
│   │       │        Alignment: Top Left
│   │       │        Line Spacing: 140%
│   │       │        Text: "" (filled by BootTextController)
│   │       │        Rich Text: ✅ (for <alpha> tags)
│   │
│   └── OptionsPanel  (EMPTY GameObject, INACTIVE)
│       │
│       │  Components:
│       │    • CanvasGroup (alpha 0)
│       │
│       ├── OptionsBackground  (UI Element)
│       │   │
│       │   │  Components:
│       │   │    • RectTransform (full-screen, Stretch)
│       │   │    • Image (Color: #000000, Alpha: 0.7)
│       │   │    • Raycast Target: ON
│       │
│       ├── OptionsText  (UI Element)
│       │   │
│       │   │  Components:
│       │   │    • RectTransform (anchored: Top-Left, offset 80, 60)
│       │   │    • TextMeshProUGUI
│       │   │        Font: IBM Plex Mono
│       │   │        Font Size: 18
│       │   │        Color: #E0F0FF, Alpha: 0.8
│       │   │        Text: "CONFIGURATION"
│       │
│       ├── VolumeSlider  (UI Element)
│       │   │
│       │   │  Components:
│       │   │    • RectTransform (anchored: Left, offset 80, 180)
│       │   │    • Slider
│       │   │    • TextMeshProUGUI (label: "MASTER VOL", Font Size: 14)
│       │
│       ├── MusicSlider  (UI Element)
│       │   │
│       │   │  Components:
│       │   │    • RectTransform (anchored: Left, offset 80, 140)
│       │   │    • Slider
│       │   │    • TextMeshProUGUI (label: "MUSIC", Font Size: 14)
│       │
│       ├── SFXSlider  (UI Element)
│       │   │
│       │   │  Components:
│       │   │    • RectTransform (anchored: Left, offset 80, 100)
│       │   │    • Slider
│       │   │    • TextMeshProUGUI (label: "SFX", Font Size: 14)
│       │
│       ├── ControllerRemap  (UI Element)
│       │   │
│       │   │  Components:
│       │   │    • RectTransform (anchored: Left, offset 80, 40)
│       │   │    • TextMeshProUGUI (label: "CONTROLLER", Font Size: 14)
│       │   │    • Button (small, text: "REMAP")
│       │
│       └── BackButton  (UI Element)
│           │
│           │  Components:
│           │    • RectTransform (anchored: Bottom-Right, offset -80, -60)
│           │    • Button
│           │    • TextMeshProUGUI
│           │        Text: "< BACK"
│           │        Font: IBM Plex Mono, Size 14
│           │        Color: #E0F0FF, Alpha: 0.8
│           │
│           └── (Button onClick → MenuMaster.OnOptionsBackClicked)
│
└── EventSystem  (auto-created by Unity)
    └── EventSystem
    └── StandaloneInputModule   