using VContainer;
using VContainer.Unity;
using UnityEngine;
using MessagePipe;
using Game.Gameplay.Audio;
using UnityEngine.EventSystems;
using Game.Gameplay.Player;
using Game.Gameplay.Save;
using Game.Gameplay.Achievements;
using Game.Gameplay.Upgrades;
using Game.Core;
using UnityEngine.InputSystem.UI;
using Game.UI;
using System;
using Game.Gameplay.World;
using Game.Core.Data;
using Game.Core.Interfaces;

namespace Game.Bootstrap.Scopes
{ 
    [DefaultExecutionOrder(-200)]
    public class ProjectLifetimeScope : LifetimeScope
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private PlayerMovementStats _defaultStats;
        [SerializeField] private LevelLoader levelLoaderPrefab;  // ← assign the prefab
        
        [Header("Managers Monobehavior")]
        [SerializeField] private InputManager inputManagerPrefab;
        [SerializeField] private string mainMenu = "MainMenu";
        [SerializeField] private AudioListenerManager audioListenerPrefab;
        [SerializeField] private SceneFadeManager sceneFadeManagerPrefab;

        [Header("Audio Mono & System Config")]
        [SerializeField] private AudioView audioView;
        [SerializeField] private AudioConfigSO audioConfig;
        [SerializeField] private AudioMixerMaster audioMixerPrefab; // ← drag the prefab asset
        
        [Header("Config")]
        [SerializeField] private SaveConfigSO saveConfig;
        [SerializeField] private AchievementConfigSO achievementConfig;
        [SerializeField] private UpgradeListObject myUpgradeListAsset;
        [SerializeField] private GameFlowConfig gameFlowConfig;
        [SerializeField] private CutscenePoolSO _cutscenePool;

        [Header("UI")]
        [SerializeField] private CRTTransitionController cRTTransitionController;
        [SerializeField] private OptionsMenuController optionsMenuController;
        [SerializeField] private AudioSettingsUI audioSettingsUI;
        [SerializeField] private VideoSettingsUI videoSettingsUI;
        [SerializeField] private SystemSettingsUI systemSettingsUI;
        [SerializeField] private ControlsSettingsUI controlsSettingsUI;
        [SerializeField] private PauseManager pauseManager;
        [SerializeField] private LoadingPanelController loadingPanelController;

    
        protected override void Configure(IContainerBuilder builder)
        {

            // ── MESSAGING (MessagePipe) ───────────────────────────────────────────────

            builder.RegisterMessagePipe(o => o.EnableCaptureStackTrace = true);
            builder.RegisterBuildCallback(resolver =>
            {
                GlobalMessagePipe.SetProvider(resolver.AsServiceProvider());
            });

            // ── Instances ───────────────────────────────────────────────
            builder.RegisterInstance(saveConfig);
            builder.RegisterInstance(audioConfig);
            builder.RegisterInstance(achievementConfig);
            builder.RegisterInstance(myUpgradeListAsset);
            builder.RegisterInstance(_defaultStats);
            builder.RegisterInstance(gameFlowConfig);
            builder.RegisterInstance(new PlayerPrefabReference(_playerPrefab));   
            builder.RegisterInstance(new SceneRegistry());
            
            // --- Cutscenes ---
            builder.RegisterInstance(_cutscenePool);
   

            // ── Singletons ──────────────────────────────────────────────
            builder.Register<ISaveableRegistry, SaveableRegistry>(Lifetime.Singleton);
            builder.Register<OverlayManager>(Lifetime.Singleton);
            builder.Register<HudState>(Lifetime.Singleton);
            builder.Register<SettingsSavable>(Lifetime.Singleton)
                .AsSelf();
            builder.Register<CutsceneSelector>(Lifetime.Scoped)
                .AsSelf();

            // --- Currency ---
            builder.Register<CurrencyManager>(Lifetime.Singleton)
                .AsSelf()
                .AsImplementedInterfaces();;

            // Mono Prefabs, NOT in scene 
            builder.RegisterComponentInNewPrefab(audioMixerPrefab, Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
            builder.RegisterComponentInNewPrefab(inputManagerPrefab, Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
            
            
            // ── Mono in scene (prefab instances) ────────────────────────
            // --- Bootstrap UI & Managers ---
            builder.RegisterComponent(loadingPanelController);
            builder.RegisterComponent(pauseManager);
            builder.RegisterComponent(audioListenerPrefab);
            builder.RegisterComponent(sceneFadeManagerPrefab);
            builder.RegisterComponent(levelLoaderPrefab);
            builder.RegisterComponent(audioView);
            builder.RegisterComponent(cRTTransitionController);

            // --- Options / Settings UI ---
            builder.RegisterComponent(audioSettingsUI);
            builder.RegisterComponent(optionsMenuController);
            builder.RegisterComponent(systemSettingsUI);
            builder.RegisterComponent(videoSettingsUI);
            builder.RegisterComponent(controlsSettingsUI);

            builder.RegisterComponentInHierarchy<CutscenePlayer>()
                .AsSelf();

            // ── Transient ───────────────────────────────────────────────
            builder.Register<SceneCleanupService>(Lifetime.Transient);
            builder.Register<SceneTransitionDirector>(Lifetime.Transient);

            // ── Entry points (LAST) ─────────────────────────────────────
            builder.UseEntryPoints(entryPoints =>
            {
                entryPoints.Add<GamePhaseController>().AsSelf();       // ← injected by concrete type
                entryPoints.Add<GameStateMachine>().AsSelf();           // ← injected by concrete type
                entryPoints.Add<GameStateSceneRouter>();                // ← not injected by anyone
                entryPoints.Add<AudioManager>();                        // ← injected via IAudioManager
                entryPoints.Add<SaveManager>().AsSelf();                // ← injected by concrete type
                entryPoints.Add<SceneLoaderService>().AsSelf();         // ← injected by concrete type
                entryPoints.Add<SceneTransitionOrchestrator>().AsSelf(); // ← injected by concrete type
                entryPoints.Add<ObjectiveTracker>();                    // ← not injected by anyone
                entryPoints.Add<GameFlowSystem>();                      // ← not injected by anyone
                entryPoints.Add<GameStateInputHandler>();               // ← not injected by anyone
                entryPoints.Add<AchievementManager>();                  // ← not injected by anyone
            });

            #if UNITY_EDITOR
            builder.Register<GameFlowDebugCollector>(Lifetime.Scoped).As<IStartable>().As<IDisposable>();
            #endif

            // --- Analytics ---
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.Register<ConsoleAnalyticsReporter>(Lifetime.Scoped)
                .As<IAnalyticsReporter>()
                .AsSelf();
            #else
            builder.Register<NoOpAnalyticsReporter>(Lifetime.Scoped)
                .As<IAnalyticsReporter>()
                .AsSelf();
            #endif

            builder.Register<AnalyticsReporter>(Lifetime.Scoped)
                .As<IStartable>()
                .As<IDisposable>();
        
            // ── EVENT SYSTEM ────────────────────────────────────────────
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var allSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
                for (int i = 1; i < allSystems.Length; i++)
                    Destroy(allSystems[i].gameObject);
            }
            //DontDestroyOnLoad(eventSystem.gameObject);
            builder.RegisterInstance(eventSystem);
        }   

       protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }
    }
}