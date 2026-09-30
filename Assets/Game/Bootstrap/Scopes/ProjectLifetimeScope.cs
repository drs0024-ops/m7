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
using Game.Gameplay.World;
using Game.Core.Data;
using Game.Core.Interfaces;
using Unity.Cinemachine;
using Game.Gameplay.Level;

namespace Game.Bootstrap.Scopes
{
    [DefaultExecutionOrder(-200)]
    public class ProjectLifetimeScope : LifetimeScope
    {
        #region Serialized Fields

        [Header("Prefabs")]
        [SerializeField] private PlayerMovementStats _defaultStats;
        [SerializeField] private BombUpgradeHandler bombPrefab;
        [SerializeField] private HudManager hudPrefab;

        [Header("Managers MonoBehaviour")]
        [SerializeField] private InputManager inputManagerPrefab;
        [SerializeField] private AudioListenerManager audioListenerPrefab;
        [SerializeField] private SceneFadeManager sceneFadeManagerPrefab;

        [Header("Audio Mono & System Config")]
        [SerializeField] private AudioView audioView;
        [SerializeField] private AudioConfigSO audioConfig;
        [SerializeField] private AudioMixerMaster audioMixerPrefab;

        [Header("Config")]
        [SerializeField] private SaveConfigSO saveConfig;
        [SerializeField] private AchievementConfigSO achievementConfig;
        [SerializeField] private UpgradeListObject myUpgradeListAsset;
        [SerializeField] private GameFlowConfig gameFlowConfig;
        [SerializeField] private CutscenePoolSO _cutscenePool;
        [SerializeField] private LevelConfigSO _levelConfig;

        [Header("UI")]
        [SerializeField] private CRTTransitionController cRTTransitionController;
        [SerializeField] private OptionsMenuController optionsMenuController;
        [SerializeField] private AudioSettingsUI audioSettingsUI;
        [SerializeField] private VideoSettingsUI videoSettingsUI;
        [SerializeField] private SystemSettingsUI systemSettingsUI;
        [SerializeField] private ControlsSettingsUI controlsSettingsUI;
        [SerializeField] private PauseManager pauseManager;
        [SerializeField] private LoadingPanelController loadingPanelController;
        [SerializeField] private DialogueController dialogueController;

        [Header("Camera")]
        [SerializeField] private Camera _physicalCamera;
        [SerializeField] private CinemachineBrain _brain;

        #endregion

        #region Configure

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateSceneReferences();

            ConfigureMessaging(builder);
            ConfigureInstances(builder);
            ConfigureSingletons(builder);
            ConfigureMonoPrefabs(builder);
            ConfigureSceneComponents(builder);
            ConfigureTransient(builder);
            ConfigureCamera(builder);
            ConfigureAnalytics(builder);
            ConfigureEntryPoints(builder);
            ConfigureEventSystem(builder);
        }

        /// <summary>
        /// FIX #10: Fail-fast validation of all scene-placed references.
        /// If any component is missing, log a clear error identifying the field.
        /// </summary>
        private void ValidateSceneReferences()
        {
            var errors = new System.Text.StringBuilder();

            CheckRef(errors, "inputManagerPrefab", inputManagerPrefab);
            CheckRef(errors, "audioListenerPrefab", audioListenerPrefab);
            CheckRef(errors, "sceneFadeManagerPrefab", sceneFadeManagerPrefab);
            CheckRef(errors, "audioView", audioView);
            CheckRef(errors, "cRTTransitionController", cRTTransitionController);
            CheckRef(errors, "dialogueController", dialogueController);
            CheckRef(errors, "audioSettingsUI", audioSettingsUI);
            CheckRef(errors, "optionsMenuController", optionsMenuController);
            CheckRef(errors, "systemSettingsUI", systemSettingsUI);
            CheckRef(errors, "videoSettingsUI", videoSettingsUI);
            CheckRef(errors, "controlsSettingsUI", controlsSettingsUI);
            CheckRef(errors, "pauseManager", pauseManager);
            CheckRef(errors, "loadingPanelController", loadingPanelController);
            CheckRef(errors, "_physicalCamera", _physicalCamera);
            CheckRef(errors, "_brain", _brain);
            CheckRef(errors, "bombPrefab", bombPrefab);
            CheckRef(errors, "hudPrefab", hudPrefab);
            CheckRef(errors, "audioMixerPrefab", audioMixerPrefab);

            if (errors.Length > 0)
            {
                Debug.LogError($"[ProjectLifetimeScope] Missing scene references in '{gameObject.name}':\n{errors}");
            }
        }

        private static void CheckRef(System.Text.StringBuilder errors, string fieldName, UnityEngine.Object obj)
        {
            if (obj == null)
                errors.AppendLine($"  • {fieldName}");
        }

        private void ConfigureMessaging(IContainerBuilder builder)
        {
            // FIX #5: Only capture stack traces in editor/dev builds
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterMessagePipe(o => o.EnableCaptureStackTrace = true);
#else
            builder.RegisterMessagePipe();
#endif
            builder.RegisterBuildCallback(resolver =>
            {
                GlobalMessagePipe.SetProvider(resolver.AsServiceProvider());
            });
        }

        private void ConfigureInstances(IContainerBuilder builder)
        {
            builder.RegisterInstance(saveConfig);
            builder.RegisterInstance(audioConfig);
            builder.RegisterInstance(achievementConfig);
            builder.RegisterInstance(myUpgradeListAsset);
            builder.RegisterInstance(_defaultStats);
            builder.RegisterInstance(gameFlowConfig);
            builder.RegisterInstance(_cutscenePool);
            builder.RegisterInstance(_levelConfig);
            builder.RegisterInstance(new SceneRegistry());
        }

        private void ConfigureSingletons(IContainerBuilder builder)
        {
            builder.Register<SaveableRegistry>(Lifetime.Singleton)
                .As<ISaveableRegistry>()
                .AsSelf();

            builder.Register<HudState>(Lifetime.Singleton);

            builder.Register<CutsceneSelector>(Lifetime.Singleton)
                .AsSelf();

            builder.Register<SceneCleanupService>(Lifetime.Singleton);
        }

        private void ConfigureMonoPrefabs(IContainerBuilder builder)
        {
            builder.RegisterComponentInNewPrefab(bombPrefab, Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.RegisterComponentInNewPrefab(audioMixerPrefab, Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.RegisterComponentInNewPrefab(inputManagerPrefab, Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.RegisterComponentInNewPrefab(hudPrefab, Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }

        private void ConfigureSceneComponents(IContainerBuilder builder)
        {
            // All references validated in ValidateSceneReferences() above.
            // If any were null, an error was already logged.
            builder.RegisterComponent(loadingPanelController);
            builder.RegisterComponent(pauseManager);
            builder.RegisterComponent(audioListenerPrefab);
            builder.RegisterComponent(sceneFadeManagerPrefab);
            builder.RegisterComponent(audioView);
            builder.RegisterComponent(cRTTransitionController);
            builder.RegisterComponent(dialogueController);
            builder.RegisterComponent(audioSettingsUI);
            builder.RegisterComponent(optionsMenuController);
            builder.RegisterComponent(systemSettingsUI);
            builder.RegisterComponent(videoSettingsUI);
            builder.RegisterComponent(controlsSettingsUI);

            builder.RegisterComponentInHierarchy<CutscenePlayer>()
                .AsSelf();
        }

        private void ConfigureTransient(IContainerBuilder builder)
        {
            builder.Register<SceneTransitionDirector>(Lifetime.Transient)
                .As<ISceneTransitionDirector>()
                .AsSelf();
        }

        private void ConfigureCamera(IContainerBuilder builder)
        {
            builder.RegisterComponent(_physicalCamera);
            builder.RegisterComponent(_brain);
        }

        private void ConfigureAnalytics(IContainerBuilder builder)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.Register<ConsoleAnalyticsReporter>(Lifetime.Singleton)
                .As<IAnalyticsReporter>()
                .AsSelf();
            builder.Register<MessagePipeTracer>(Lifetime.Singleton)
                .AsSelf();
#else
            builder.Register<NoOpAnalyticsReporter>(Lifetime.Singleton)
                .As<IAnalyticsReporter>()
                .AsSelf();
#endif
        }

        private void ConfigureEntryPoints(IContainerBuilder builder)
        {
            // Game Flow
            builder.RegisterEntryPoint<GamePhaseController>().AsSelf();
            builder.RegisterEntryPoint<GameStateMachine>().AsSelf();
            builder.RegisterEntryPoint<GameStateSceneRouter>().AsSelf();
            builder.RegisterEntryPoint<GameFlowSystem>().AsSelf();
            builder.RegisterEntryPoint<GameStateInputHandler>().AsSelf();
            builder.RegisterEntryPoint<LevelProgressionManager>().AsSelf();

            // Scene / Transition
            builder.RegisterEntryPoint<SceneLoaderService>().AsSelf();
            builder.RegisterEntryPoint<SceneTransitionOrchestrator>().AsSelf();

            // Audio
            builder.RegisterEntryPoint<AudioManager>().AsSelf();

            // Save
            builder.RegisterEntryPoint<SaveManager>().AsSelf();
            builder.RegisterEntryPoint<SettingsSavable>().AsSelf();
            builder.RegisterEntryPoint<DialogueSeenTracker>().AsSelf();

            // Currency & Upgrades
            builder.RegisterEntryPoint<CurrencyManager>().AsSelf();
            builder.RegisterEntryPoint<UpgradeStateManager>().AsSelf();
            builder.RegisterEntryPoint<UpgradeOrchestrator>().AsSelf();

            // Achievements & Objectives
            builder.RegisterEntryPoint<ObjectiveTracker>().AsSelf();
            builder.RegisterEntryPoint<AchievementManager>().AsSelf();

            // Observability
            builder.RegisterEntryPoint<ScopeLifecycleLogger>().AsSelf();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterEntryPoint<AnalyticsReporter>().AsSelf();
#endif
#if UNITY_EDITOR
            builder.RegisterEntryPoint<GameFlowDebugCollector>().AsSelf();
#endif
        }

        private void ConfigureEventSystem(IContainerBuilder builder)
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                go.transform.SetParent(transform, false);
                eventSystem = go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != eventSystem)
                    {
                        Debug.LogWarning($"[ProjectLifetimeScope] Destroyed duplicate EventSystem: '{all[i].gameObject.name}'");
                        DestroyImmediate(all[i].gameObject);
                    }
                }
            }

            builder.RegisterInstance(eventSystem);
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        #endregion
    }
}   