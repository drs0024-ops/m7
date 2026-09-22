# Audio Music & SFX Flow

## Architecture Overview

┌─────────────────────────────────────────────────────────────────────────────┐
│ MenuMaster / Gameplay (UI Layer) │
│ │
│ [Inject] IAudioManager _audio │
│ │
│ _audio.PlaySFX(SoundType.MenuClick) │
│ _audio.PlayMusic(MusicType.MainMenu) │
│ _audio.SetTrackVolume(0, 0.8f) │
│ _audio.SetSfxVolume(SoundType.MenuNavigate, 0.7f) │
└──────────────────────────────────┬──────────────────────────────────────────┘
│ Direct DI call
▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ AudioManager (Service Layer) │
│ IAudioManager, IStartable, IDisposable │
│ Pure C# — no MonoBehaviour │
│ │
│ [Inject] AudioConfigSO _config │
│ [Inject] AudioView _view │
│ │
│ State: │
│ Dictionary<SoundType, AudioClip> _clips │
│ Dictionary<SoundType, float> _sfxVolumes │
│ Dictionary<SoundType, bool> _sfxLoops │
│ Dictionary<MusicType, List> _musicTracks │
│ AudioSource[] _activeMusicSources │
│ MusicType _currentMusicType │
│ int _musicGeneration (stale-track guard) │
│ float _musicVolume │
│ │
│ MessagePipe Subscriptions (GlobalMessagePipe): │
│ TimeScalePause → duck music via mixer param │
│ TimeScaleResume → restore music volume │
│ StopMusic → _view.StopMusic() + generation++ │
│ PlayMusic → PlayMusic(clip, restart) │
│ SetVolume → SetMusicVolume() → mixer param │
└──────────────────────────────────┬──────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ AudioView (Unity Component) │
│ MonoBehaviour — holds AudioSources & Mixer │
│ │
│ SFX Pool (fixed, round-robin): │
│ AudioSource[8] _sfxPool │
│ int _sfxIndex │
│ │
│ Music Pool (auto-grow, idle-reuse): │
│ List _musicPool │
│ float _lastStopTime (trim timer) │
│ │
│ AudioMixer _audioMixer │
│ AudioMixerGroup _musicOutputGroup │
│ │
│ PlaySFX(clip, volume, loop) → round-robin grab, set, Play() │
│ PlayMusic(clip, loop, volume) → find idle or create new, Play() │
│ StopMusic() → stop all music sources, start trim timer │
│ StopAll() → stop all SFX + music sources │
│ SetSfxOutputGroup(group) → assign to all SFX pool sources │
│ SetMusicOutputGroup(group) → store + assign to all music sources │
└─────────────────────────────────────────────────────────────────────────────┘


---

## Interfaces

### IAudioManager

```csharp
public interface IAudioManager
{
    void PlaySFX(SoundType soundType);
    void PlaySFX(AudioClip clip, float volume = 1.0f);
    void PlayMusic(MusicType type);
    void PlayMusic(AudioClip clip, bool restart = false);
    void StopMusic();
    void StopAll();
    void SetMusicVolume(float volume);
    void SetTrackVolume(int trackIndex, float volume);
    void SetSfxVolume(SoundType soundType, float volume);
}

Enums
SoundType
public enum SoundType
{
    MenuHover,
    MenuClick,
    MenuNavigate,
    MenuOpen,
    MenuClose,
    CRTBoot
}

MusicType
public enum MusicType
{
    None = -1,
    MainMenu,
    Settings,
    Pause
}

Message Types (GlobalMessagePipe)
Message	Trigger	Handler in AudioManager
TimeScalePause	Gameplay starts / time scale → 0	Duck music via MusicVol mixer param
TimeScaleResume	Time scale → 1	Restore MusicVol to _musicVolume
StopMusic	Scene exit / phase change	_view.StopMusic() + generation++
PlayMusic	Cross-system music trigger (carries AudioClip)	_view.PlayMusic(clip, loop: true)
SetVolume	Settings UI	SetMusicVolume() → mixer param

Data Assets
AudioConfigSO (ScriptableObject)
Field	Type	Purpose
audioMixer	AudioMixer	Reference to the mixer asset
fallbackClip	AudioClip	Used when a clip slot is empty
musicDuckVolume	float [0..1]	Target volume multiplier when ducked
audioClips	List<AudioLibraryEntry>	SFX: SoundType → clip, volume, loop
musicClips	List<MusicEntry>	Music: MusicType → list of tracks

AudioLibraryEntry
[Serializable]
public class AudioLibraryEntry
{
    public SoundType soundType;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 1f;
    public bool loop = false;
}

MusicEntry
[Serializable]
public class MusicEntry
{
    public MusicType type;
    public List<MusicTrack> tracks = new();
}

MusicTrack
[Serializable]
public class MusicTrack
{
    public AudioClip clip;
    public bool loop = true;
    [Range(0f, 1f)]
    public float volume = 1f;
    public float delay = 0f;
}

VContainer Registration
// Root LifetimeScope (DontDestroyOnLoad)
builder.RegisterInstance(audioConfigSO).AsSelf();
builder.RegisterComponentInHierarchy<AudioView>(Lifetime.Singleton);
builder.Register<IAudioManager, AudioManager>(Lifetime.Singleton);

Component	Lifetime	Scope
IAudioManager / AudioManager	Singleton	Root
AudioConfigSO	Singleton	Root
AudioView	Singleton (component)	Root
AudioListenerManager	Singleton	Root
MenuMaster	Scoped	Per-scene child
BootTextController	Scoped	Per-scene child

SFX Pool (Fixed, Round-Robin)
Size: 8 (configurable via _sfxPoolSize in AudioView)
Strategy: Round-robin index, wraps around
Steal behavior: If the target source is still playing, it gets overwritten (intentional for short SFX)
Created in: AudioView.Awake() — child GameObjects under the view
SFX Trigger Map
Event	SoundType	Triggered In
Menu item click	MenuClick	MenuMaster button callbacks
Options panel open	MenuOpen	MenuMaster.OnOptionsClicked()
Options panel close	MenuClose	MenuMaster.OnOptionsBackClicked()
Selection move (up/down)	MenuNavigate	BootTextController.MoveSelection()
CRT power-on	CRTBoot	MenuMaster.ShowMainMenu()
Hover (if implemented)	MenuHover	BootTextController or raycast handler

Music Pool (Auto-Grow, Idle-Reuse)
Size: Starts empty, grows on demand
Strategy: Find an idle (non-playing) source, or create a new one
Steal behavior: Never overwrites a playing source
Trim: Idle sources destroyed 2 seconds after StopMusic() (prevents unbounded growth)
Created in: GetOrCreateMusicSource() — child GameObjects under the view
Generation Counter (Stale-Track Guard)
Prevents delayed tracks from starting after StopMusic() / StopAll() / a replacement PlayMusic() call:

PlayMusic(MainMenu) called
  → _musicGeneration++
  → StopMusic()
  → PlayMusicTracks(tracks, gen=1).Forget()
    → Track 0 (delay=0) → plays ✅
    → Track 1 (delay=0) → plays ✅
    → Track 2 (delay=3s) → awaiting...

StopMusic() called (or PlayMusic(Settings))
  → _musicGeneration++  (now 2)
  → _view.StopMusic()

t=+3s: Track 2's delay finishes
  → checks: gen(1) != _musicGeneration(2) → ABORTS ✅

Main Menu Track Example
Track Index	Clip	Loop	Volume	Delay
0	CRT_Hum	✅	0.8	0
1	CRT_Startup_MainMenu	❌	0.7	0
2	Crt_run_loop	✅	0.6	3.0
3	Pulse	✅	0.4	1.0

Volume Model
SFX loudness = _sfxVolumes[type] (per-clip, from AudioConfig) × mixer "SfxVol" (global)

Music track loudness = source.volume (per-track, from MusicTrack.volume or SetTrackVolume)
                      × mixer "MusicVol" (global, SetMusicVolume / ducking)

Ducking
OnTimeScalePause:  MusicVol = _musicVolume × musicDuckVolume  (e.g., 1.0 × 0.1 = 0.1 → -20dB)
OnTimeScaleResume: MusicVol = _musicVolume                      (e.g., 1.0 → 0dB)

Audio Mixer Hierarchy
Master
├── Music (exposed param: MusicVol)
└── SFX   (exposed param: SfxVol)

AudioView assigns SFX pool sources → SFX group
AudioView assigns music pool sources → Music group
Ducking: set MusicVol via SetFloat (no snapshots needed)
AudioListenerManager
Ensures exactly one AudioListener is active at all times.

Method	Purpose
Awake()	Ensures own AudioListener exists, hooks scene load
Start()	Calls EnforceSingleListener() (after all Awake calls complete)
EnforceSingleListener()	Disables all other active AudioListeners
OnSceneLoaded	Re-enforces on additive loads
SetActive(bool)	External mute/unmute

Must be on a persistent root object (DontDestroyOnLoad). If its GameObject deactivates, all audio goes silent.

Scene Lifetime
Scene (Root, DontDestroyOnLoad)
├── Root (LifetimeScope)
├── AudioListenerManager (AudioListener)
├── AudioView
│   ├── Music_0, Music_1, ... (runtime-created)
│   └── Sfx_0, Sfx_1, ... Sfx_7 (runtime-created)
└── (other persistent objects)

Menu Scene (child scope)
├── MenuCanvas
│   ├── MainMenuPanel
│   ├── OptionsPanel
│   └── CRTOverlay (RawImage, CRT shader)
├── MenuMaster (component)
├── BootTextController (component)
└── CRTBootAnimation (component)

File Structure
Assets/Game/
├── Core/
│   ├── Audio/
│   │   └── MusicTrack.cs
│   ├── Enums/
│   │   ├── SoundType.cs
│   │   └── MusicType.cs
│   ├── Interfaces/
│   │   └── IAudioManager.cs
│   └── Messages/
│       ├── TimeScalePause.cs
│       ├── TimeScaleResume.cs
│       ├── StopMusic.cs
│       ├── PlayMusic.cs
│       └── SetVolume.cs
├── Gameplay/
│   ├── Audio/
│   │   ├── AudioManager.cs
│   │   ├── AudioView.cs
│   │   ├── AudioConfigSO.cs
│   │   └── AudioListenerManager.cs
│   └── Player/
│       └── InputManager.cs
├── UI/
│   ├── Menus/
│   │   └── MainMenu/
│   │       ├── MenuMaster.cs
│   │       ├── BootTextController.cs
│   │       └── CRTBootAnimation.cs
│   └── Shader/
│       └── CRTBoot.shader
└── Editor/
    └── AudioEditorTools.cs

CRT Boot Animation
Shader: Game/CRT/Boot (UI-compatible, uses UNITY_UI_CLIP_RECT)
Target: RawImage (last child of MenuCanvas, renders on top)
Driver: CRTBootAnimation MonoBehaviour on MainMenu root
Properties: _BootProgress (0→1), _LineBrightness, _GlowWidth, _ScanlineIntensity, _FlashColor
Duration: configurable in Inspector
PostDelay: configurable pause after animation before text starts
Sound: SoundType.CRTBoot SFX fires at the same moment the shader starts
Flow
ShowMainMenu(fromGameplay)
  → _audio.PlaySFX(SoundType.CRTBoot)
  → await _crtBoot.Play()           (shader animates 0→1)
  → await UniTask.Delay(PostDelay)  (brief pause)
  → _bootText.StartSequence(fromGameplay)
  → _audio.PlayMusic(MusicType.MainMenu)

BootTextController
Mode	Title Text	Typing	Trigger
Fresh Boot	_freshBootTitle (Inspector string)	✅ Typewriter	First launch
Return Boot	_returnBootTitle (Inspector string)	❌ Instant	Returning from gameplay

Category lines are TMP objects dragged into Inspector (_lineTexts[])
Text is read from the TMP objects at runtime (no hardcoded strings)
Cursor blinks at end of selected line
Navigation sound (MenuNavigate) fires in MoveSelection()
StartSequence(bool fromGameplay) picks title + typing mode
CancellationTokenSource cancels in-flight typing on re-entry
Known Design Decisions
Decision	Rationale
GlobalMessagePipe (static) over VContainer-registered brokers	Simpler for a single-project game; no per-scope isolation needed
SFX pool is fixed (round-robin steal)	SFX are short; overwriting the oldest is acceptable
Music pool is auto-grow (no steal)	Music tracks must coexist; stealing kills the illusion
Generation counter over CancellationToken	Lighter weight; single int compare; no CTS lifecycle to manage
Volume in AudioConfigSO (not baked into WAV)	Rebalance without re-exporting; runtime overrides still work
Mixer param for global/ducking, per-source for relative balance	Two independent layers
MusicType.None = -1 sentinel	Prevents default enum value from matching a real track
AudioView on persistent root (not under UI)	Prevents accidental deactivation killing all audio
