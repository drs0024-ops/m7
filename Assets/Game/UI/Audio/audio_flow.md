# Audio Flow

## Architecture Overview

┌─────────────────────────────────────────────────────────────────────┐
│ MenuMaster (UI Layer) │
│ │
│ [Inject] IAudioManager _audio │
│ │
│ OnNewGameClicked() → _audio.PlaySFX(SoundType.MenuClick) │
│ OnOptionsClicked() → _audio.PlaySFX(SoundType.MenuOpen) │
│ OnOptionsBackClicked() → _audio.PlaySFX(SoundType.MenuClose) │
│ ShowMainMenu() → _audio.PlayMusic(MusicType.MainMenu) │
└──────────────────────────────┬──────────────────────────────────────┘
│ Direct DI call
▼
┌─────────────────────────────────────────────────────────────────────┐
│ AudioManager (Service Layer) │
│ IAudioManager, IStartable, IDisposable │
│ │
│ [Inject] AudioConfigSO _config │
│ [Inject] AudioView _view │
│ │
│ Dictionary<SoundType, AudioClip> _clips │
│ Dictionary<MusicType, AudioClip> _musicClips │
│ float _musicVolume │
│ │
│ MessagePipe Subscriptions (GlobalMessagePipe): │
│ TimeScalePause → duck/pause music │
│ TimeScaleResume → restore music │
│ StopMusic → _view.StopMusic() │
│ PlayMusic → _view.PlayMusic(clip, restart) │
│ SetVolume → update _musicVolume, apply to mixer │
└──────────────────────────────┬──────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────────────┐
│ AudioView (Unity Component) │
│ MonoBehaviour │
│ │
│ AudioSource[] _sfxPool (round-robin, size 8) │
│ AudioSource _musicSource │
│ AudioMixer _audioMixer │
│ │
│ PlaySFX(clip, volume) → round-robin grab, set, Play() │
│ PlayMusic(clip, restart) → set clip, Play() │
│ StopMusic() → _musicSource.Stop() │
│ StopAll() → stop all pool + music │
│ SetSfxOutputGroup(g) → assign to all pool sources │
└─────────────────────────────────────────────────────────────────────┘


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
}

Enums
SoundType
public enum SoundType
{
    MenuHover,
    MenuClick,
    MenuNavigate,
    MenuOpen,
    MenuClose
}

MusicType
public enum MusicType
{
    MainMenu,
    Settings,
    Pause
}

Message Types (GlobalMessagePipe)
Message	Trigger	Handler in AudioManager
TimeScalePause	Gameplay starts / time scale → 0	Duck or pause music
TimeScaleResume	Time scale → 1	Restore music
StopMusic	Scene exit / phase change	_view.StopMusic()
PlayMusic	Cross-system music trigger	_view.PlayMusic(clip, restart)
SetVolume	Settings UI	Update _musicVolume, apply to mixer

Data Assets
AudioConfigSO (ScriptableObject)
Field	Type	Purpose
audioMixer	AudioMixer	Reference to the mixer asset
fallbackClip	AudioClip	Used when a clip slot is empty
audioClips	List<AudioLibraryEntry>	SFX: SoundType → AudioClip
musicClips	List<MusicEntry>	Music: MusicType → AudioClip
musicDuckVolume	float [0..1]	Target volume when ducked

AudioLibraryEntry
[Serializable]
public class AudioLibraryEntry
{
    public SoundType soundType;
    public AudioClip clip;
}

MusicEntry
[Serializable]
public class MusicEntry
{
    public MusicType type;
    public AudioClip clip;
}

VContainer Registration
// Root LifetimeScope (DontDestroyOnLoad)
builder.Register<AudioConfigSO>(Lifetime.Singleton);
builder.Register<IAudioManager, AudioManager>(Lifetime.Singleton);
builder.RegisterComponentInHierarchy<AudioView>();

Component	Lifetime	Scope
IAudioManager / AudioManager	Singleton	Root
AudioConfigSO	Singleton	Root
AudioView	Singleton (component)	Root
MenuMaster	Scoped	Per-scene child
BootTextController	Scoped	Per-scene child

SFX Pool (Overlap)
Size: 8 (configurable via _sfxPoolSize in AudioView)
Strategy: Round-robin index, wraps around
Steal behavior: If the target source is still playing, it gets overwritten
Created in: AudioView.Awake() — child GameObjects under the view
SFX Trigger Map
Event	SoundType	Triggered In
Menu item click (New Game, Load, Exit)	MenuClick	MenuMaster button callbacks
Options panel open	MenuOpen	MenuMaster.OnOptionsClicked()
Options panel close	MenuClose	MenuMaster.OnOptionsBackClicked()
Selection move (up/down)	MenuNavigate	BootTextController.MoveSelection()
Hover (if implemented)	MenuHover	BootTextController or raycast handler

Music Flow
MenuMaster.ShowMainMenu()
  → _audio.PlayMusic(MusicType.MainMenu)
    → AudioManager resolves clip from _musicClips dictionary
      → _view.PlayMusic(clip, restart: false)
        → _musicSource.clip = clip; _musicSource.Play();

Gameplay starts
  → GlobalMessagePipe.GetPublisher<TimeScalePause>().Publish(...)
    → AudioManager.OnTimeScalePause()
      → _view.MusicSource.volume = _config.musicDuckVolume

Audio Mixer Hierarchy
Master
├── Music (exposed: _MusicVolume)
└── SFX   (exposed: _SfxVolume)

AudioView assigns pool sources → SFX group
AudioView assigns _musicSource → Music group
Ducking: set _MusicVolume to musicDuckVolume value via mixer parameter
File Structure
Game/
├── Core/
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
│   │   └── AudioConfigSO.cs
│   └── Player/
│       └── InputManager.cs
└── UI/
    ├── MenuMaster.cs
    ├── BootTextController.cs
    └── OptionsMenuController.cs


Save this as `audio_flow.md` in your project root (or `Docs/` folder). It captures the full data flow from UI input through DI, MessagePipe, and down to the `AudioSource` pool.