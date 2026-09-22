using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Gameplay.Camera;
using Game.Gameplay.Player;
using Game.Gameplay.Upgrades;
using Game.Gameplay.World;
using Game.Core.Enums;
using Game.UI;

namespace Game.Bootstrap.Scopes
{
    public class FacilitySceneLifetimeScope : LifetimeScope
    {
        [SerializeField] private Transform _startSpawnPoint;
        [SerializeField] private Vector3 _defaultStartPosition;
        [SerializeField] private UpgradePickup _upgradePickup1;
        [SerializeField] private CheckpointManager _checkpointManager;
        [SerializeField] private SceneCameraSetup _sceneCameraSetup;
        [SerializeField] private CameraFollowObject _cameraFollowObject;
        [SerializeField] private CameraConfigSO _cameraConfig;

        [Header("Pause Menu")]
        [SerializeField] private PauseManager pauseManager;
        [SerializeField] private PauseMenuTabs pauseMenuTabs;
        //[SerializeField] private ObjectivesManager objectivesManager;
        [SerializeField] private PauseButton pauseButton;

        protected override void Configure(IContainerBuilder builder)
        {
            // --- Checkpoints ---
            var checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).ToList();

            if (checkpoints.Count == 0)
                Debug.LogWarning("[FacilitySceneScope] No Checkpoints found in scene.");
            else
            {
                builder.RegisterBuildCallback(container =>
                {
                    for (int i = 0; i < checkpoints.Count; i++)
                        container.Inject(checkpoints[i]);
                });
            }

            builder.RegisterInstance(checkpoints).As<IReadOnlyList<Checkpoint>>();

            if (_checkpointManager != null)
            {
                builder.RegisterComponent(_checkpointManager)
                    .AsSelf()
                    .AsImplementedInterfaces();
            }
            else
            {
                builder.RegisterComponentInHierarchy<CheckpointManager>()
                    .AsSelf()
                    .AsImplementedInterfaces();
            }

            // --- Spawn Point ---
            if (_startSpawnPoint == null)
            {
                var fallback = new GameObject("DefaultStartSpawn");
                fallback.transform.position = _defaultStartPosition;
                _startSpawnPoint = fallback.transform;
            }

            builder.RegisterInstance(_startSpawnPoint).Keyed(SpawnKeys.StartSpawn);

            // --- Triggers / Interactables ---
            var triggers = FindObjectsByType<TriggerInteractionBase>(FindObjectsSortMode.None).ToList();

            if (triggers.Count > 0)
            {
                builder.RegisterBuildCallback(container =>
                {
                    for (int i = 0; i < triggers.Count; i++)
                    {
                        if (triggers[i] != null)
                            container.Inject(triggers[i]);
                    }
                });
            }

            builder.RegisterInstance(triggers).As<IReadOnlyList<TriggerInteractionBase>>();

            // --- Upgrade Pickups ---
            if (_upgradePickup1 != null)
                builder.RegisterComponent(_upgradePickup1);

            // --- Camera System ---
            if (_sceneCameraSetup != null)
                builder.RegisterComponent(_sceneCameraSetup);

            if (_cameraFollowObject != null)
                builder.RegisterComponent(_cameraFollowObject);

            if (_cameraConfig == null)
            {
                Debug.LogError("[FacilitySceneScope] CameraConfigSO not assigned!");
                return;
            }

            builder.RegisterInstance(_cameraConfig);
            builder.Register<CameraTargetManager>(Lifetime.Scoped);
            builder.Register<CameraSwitcher>(Lifetime.Scoped);

            // =====================================================
            // MONOBEHAVIOUR COMPONENTS
            // =====================================================
            builder.RegisterComponentInHierarchy<CameraEffectController>();
            builder.RegisterComponentInHierarchy<CameraPanMover>();

            // -- Pause Menu Components --
            if (pauseManager != null)      builder.RegisterComponent(pauseManager);
            if (pauseMenuTabs != null)     builder.RegisterComponent(pauseMenuTabs);
            //if (objectivesManager != null) builder.RegisterComponent(objectivesManager);
            if (pauseButton != null)       builder.RegisterComponent(pauseButton);

            builder.Register<CameraSystem>(Lifetime.Scoped)
                .As<IInitializable>()
                .As<IDisposable>();

            // --- Scene Config ---
            builder.RegisterInstance(new CharacterSceneConfig
            {
                SpawnPlayerOnStart = true,
                DefaultStartPosition = _startSpawnPoint.position
            });

            // --- Player ---
            builder.Register<PlayerDependencies>(Lifetime.Scoped);
            builder.Register<PlayerContext>(Lifetime.Scoped);
            builder.Register<PlayerController>(Lifetime.Scoped);
            builder.Register<PlayerHSMBuilder>(Lifetime.Scoped);
            builder.Register<PlayerStateManager>(Lifetime.Scoped);

            builder.RegisterEntryPoint<PlayerSpawnerService>()
                .As<IDisposable>();

            #if UNITY_EDITOR
            builder.RegisterComponentInHierarchy<SceneViewDriverDisabler>();
            #endif
        }
    }
}   