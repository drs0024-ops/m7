using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Interfaces;
using Game.Gameplay.Camera;
using Game.Gameplay.Enemies;
using Game.Gameplay.Enemies.Runtime;
using Game.Gameplay.Player;
using Game.Gameplay.Upgrades;
using Game.Gameplay.World;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap.Scopes
{
    [DefaultExecutionOrder(-100)]
    public class FacilitySceneLifetimeScope : LifetimeScope
    {
        #region Serialized Fields

        [Header("Player")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Transform _startSpawnPoint;
        [SerializeField] private Vector3 _defaultStartPosition;

        [Header("Checkpoints")]
        [SerializeField] private CheckpointManager _checkpointManager;

        [Header("Camera")]
        [SerializeField] private SceneCameraSetup _sceneCameraSetup;
        [SerializeField] private CameraFollowObject _cameraFollowObject;
        [SerializeField] private CameraConfig _cameraConfig;
        [SerializeField] private CinemachineImpulseSource _impulseSource;
        [SerializeField] private CameraTargetManager _cameraTargetManager;

        [Header("Invisibility")]
        [SerializeField] private InvisibilityVisualPresenter _invisibilityVisualPresenter;
        [SerializeField] private InvisibilityPickup _invisibilityPickup;
        [SerializeField] private InvisibilityCharger[] _invisibilityChargers = Array.Empty<InvisibilityCharger>();
        [SerializeField] private Enemy[] _invisibilityAwareEnemies = Array.Empty<Enemy>();

        [Header("Bombs")]
        [SerializeField] private GameObject _bombPrefab;

        #endregion

        #region Configure

        protected override void Configure(IContainerBuilder builder)
        {
            ConfigureCheckpoints(builder);
            ConfigureSpawnPoint(builder);
            ConfigureTriggers(builder);
            ConfigureCamera(builder);
            ConfigureInvisibility(builder);
            ConfigureEnemyRuntime(builder);
            ConfigurePlayer(builder);
            ConfigureBomb(builder);
            ConfigureSceneConfig(builder);
            ConfigureEntryPoints(builder);
            ConfigureBuildCallbacks(builder);
            ConfigureOrbCounter(builder);
        }

        private void ConfigureOrbCounter(IContainerBuilder builder)
        {
            builder.Register<OrbCounter>(Lifetime.Scoped);
        }

        private void ConfigureEnemyRuntime(IContainerBuilder builder)
        {
            var currentScene = gameObject.scene;
            var allEntities = FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            var entities = new List<EnemyEntity>(allEntities.Length);
            for (int i = 0; i < allEntities.Length; i++)
                if (allEntities[i].gameObject.scene == currentScene)
                    entities.Add(allEntities[i]);

            var allSensors = FindObjectsByType<EnemyPerceptionSensor>(FindObjectsSortMode.None);
            var sensors = new List<EnemyPerceptionSensor>(allSensors.Length);
            for (int i = 0; i < allSensors.Length; i++)
                if (allSensors[i].gameObject.scene == currentScene)
                    sensors.Add(allSensors[i]);

            builder.RegisterInstance(entities).As<IReadOnlyList<EnemyEntity>>();
            builder.RegisterInstance(sensors).As<IReadOnlyList<EnemyPerceptionSensor>>();
            builder.Register<DamagableRegistry>(Lifetime.Scoped).As<IDamagableRegistry>().AsSelf();
            builder.RegisterEntryPoint<EnemyRuntimeSystem>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<EnemyExecutionSystem>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<EnemyExecutionAdapterSystem>(Lifetime.Scoped).AsSelf();

            builder.RegisterBuildCallback(resolver =>
            {
                for (int i = 0; i < entities.Count; i++)
                    resolver.InjectGameObject(entities[i].gameObject);
                for (int i = 0; i < sensors.Count; i++)
                    resolver.InjectGameObject(sensors[i].gameObject);
            });
        }

        private void ConfigureCheckpoints(IContainerBuilder builder)
        {
            var currentScene = gameObject.scene;
            var all = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);

            var checkpoints = new List<Checkpoint>(all.Length);
            for (int i = 0; i < all.Length; i++)
                if (all[i].gameObject.scene == currentScene)
                    checkpoints.Add(all[i]);

            if (checkpoints.Count == 0)
                Debug.LogWarning($"[FacilitySceneScope] No Checkpoints found in '{currentScene.name}'.");

            builder.RegisterInstance(checkpoints).As<IReadOnlyList<Checkpoint>>();

            builder.RegisterBuildCallback(resolver =>
            {
                for (int i = 0; i < checkpoints.Count; i++)
                    resolver.Inject(checkpoints[i]);
            });

            if (_checkpointManager != null)
                builder.RegisterComponent(_checkpointManager).As<ICheckpointProvider>().AsSelf();
            else
                builder.RegisterComponentInHierarchy<CheckpointManager>().As<ICheckpointProvider>().AsSelf();
        }

        private void ConfigureSpawnPoint(IContainerBuilder builder)
        {
            if (_startSpawnPoint == null)
            {
                var fallback = new GameObject("DefaultStartSpawn");
                fallback.transform.SetParent(transform, false);
                fallback.transform.position = _defaultStartPosition;
                _startSpawnPoint = fallback.transform;
            }

            builder.RegisterInstance(_startSpawnPoint).Keyed(SpawnKeys.StartSpawn);
        }

        private void ConfigureTriggers(IContainerBuilder builder)
        {
            var currentScene = gameObject.scene;
            var allTriggers = FindObjectsByType<TriggerInteractionBase>(FindObjectsSortMode.None);
            var allCameraZoneTriggers = FindObjectsByType<CameraZoneTrigger>(FindObjectsSortMode.None);
            var allCameraPanTriggers = FindObjectsByType<CameraPanTrigger>(FindObjectsSortMode.None);

            var triggers = new List<TriggerInteractionBase>(allTriggers.Length);
            for (int i = 0; i < allTriggers.Length; i++)
            {
                if (allTriggers[i].gameObject.scene == currentScene)
                    triggers.Add(allTriggers[i]);
            }

            var cameraZoneTriggers = new List<CameraZoneTrigger>(allCameraZoneTriggers.Length);
            for (int i = 0; i < allCameraZoneTriggers.Length; i++)
            {
                if (allCameraZoneTriggers[i].gameObject.scene == currentScene)
                    cameraZoneTriggers.Add(allCameraZoneTriggers[i]);
            }

            var cameraPanTriggers = new List<CameraPanTrigger>(allCameraPanTriggers.Length);
            for (int i = 0; i < allCameraPanTriggers.Length; i++)
            {
                if (allCameraPanTriggers[i].gameObject.scene == currentScene)
                    cameraPanTriggers.Add(allCameraPanTriggers[i]);
            }

            builder.RegisterInstance(triggers).As<IReadOnlyList<TriggerInteractionBase>>();

            builder.RegisterBuildCallback(resolver =>
            {
                for (int i = 0; i < triggers.Count; i++)
                    resolver.Inject(triggers[i]);

                for (int i = 0; i < cameraZoneTriggers.Count; i++)
                    resolver.InjectGameObject(cameraZoneTriggers[i].gameObject);

                for (int i = 0; i < cameraPanTriggers.Count; i++)
                    resolver.InjectGameObject(cameraPanTriggers[i].gameObject);
            });
        }

        private void ConfigureCamera(IContainerBuilder builder)
        {
            if (_sceneCameraSetup == null)
                _sceneCameraSetup = GetComponentInChildren<SceneCameraSetup>(true);
            if (_cameraFollowObject == null)
                _cameraFollowObject = GetComponentInChildren<CameraFollowObject>(true);
            if (_cameraConfig == null)
                _cameraConfig = GetComponentInChildren<CameraConfig>(true);
            if (_impulseSource == null)
                _impulseSource = GetComponentInChildren<CinemachineImpulseSource>(true);
            if (_cameraTargetManager == null)
                _cameraTargetManager = GetComponentInChildren<CameraTargetManager>(true);

            if (_sceneCameraSetup == null || _cameraFollowObject == null)
                throw new InvalidOperationException(
                    $"[FacilitySceneScope] Camera setup or follow object not found under '{gameObject.name}'.");

            builder.RegisterComponent(_sceneCameraSetup);
            builder.RegisterComponent(_cameraFollowObject);

            if (_cameraConfig == null)
                throw new InvalidOperationException(
                    $"[FacilitySceneScope] CameraConfig not assigned on '{gameObject.name}'.");

            builder.RegisterComponent(_cameraConfig);
            builder.RegisterComponent(_impulseSource);

            // FIX: Register as scene component (was Register<T>, which created a detached instance)
            if (_cameraTargetManager != null)
                builder.RegisterComponent(_cameraTargetManager);
            else
                throw new InvalidOperationException(
                    $"[FacilitySceneScope] CameraTargetManager not assigned on '{gameObject.name}'.");

            builder.Register<CameraSwitcher>(Lifetime.Scoped);
            builder.RegisterComponentInHierarchy<CameraEffectController>();
            builder.RegisterComponentInHierarchy<CameraPanMover>();
        }

        private void ConfigureInvisibility(IContainerBuilder builder)
        {
            InvisibilityInstaller.InstallSceneScoped(builder);

            if (_invisibilityPickup != null)
                builder.RegisterComponent(_invisibilityPickup);
            else
                Debug.LogWarning($"[FacilitySceneScope] InvisibilityPickup not assigned on '{gameObject.name}'.");

            for (int i = 0; i < _invisibilityChargers.Length; i++)
            {
                if (_invisibilityChargers[i] != null)
                    builder.RegisterComponent(_invisibilityChargers[i]);
            }

            for (int i = 0; i < _invisibilityAwareEnemies.Length; i++)
            {
                if (_invisibilityAwareEnemies[i] != null)
                    builder.RegisterComponent(_invisibilityAwareEnemies[i]);
            }

            builder.RegisterBuildCallback(resolver =>
            {
                for (int i = 0; i < _invisibilityChargers.Length; i++)
                {
                    InvisibilityCharger charger = _invisibilityChargers[i];
                    if (charger == null) continue;
                    resolver.InjectGameObject(charger.gameObject);
                    charger.Initialize();
                }

                for (int i = 0; i < _invisibilityAwareEnemies.Length; i++)
                {
                    Enemy enemy = _invisibilityAwareEnemies[i];
                    if (enemy == null) continue;
                    resolver.InjectGameObject(enemy.gameObject);
                    enemy.InitializeInvisibilityInvestigation();
                    enemy.GetComponent<InvisibilityEnemyInvestigator>()?.Initialize();
                }

                if (_invisibilityVisualPresenter != null)
                    resolver.InjectGameObject(_invisibilityVisualPresenter.gameObject);
            });
        }

        private void ConfigurePlayer(IContainerBuilder builder)
        {
            if (_playerPrefab == null)
                throw new InvalidOperationException(
                    $"[FacilitySceneScope] Player prefab not assigned on '{gameObject.name}'.");

            builder.RegisterInstance(gameObject.scene);
            builder.RegisterInstance(_playerPrefab).Keyed("PlayerPrefab");
            builder.Register<IPlayerFactory, PlayerFactory>(Lifetime.Scoped);

            builder.Register<PlayerDependencies>(Lifetime.Scoped);
            builder.Register<PlayerContext>(Lifetime.Scoped);
            builder.Register<PlayerController>(Lifetime.Scoped);
            builder.Register<PlayerHSMBuilder>(Lifetime.Scoped);
            builder.Register<PlayerStateManager>(Lifetime.Scoped);
        }

        private void ConfigureBomb(IContainerBuilder builder)
        {
            if (_bombPrefab == null)
                Debug.LogWarning($"[FacilitySceneScope] Bomb prefab not assigned on '{gameObject.name}'. BombSpawner will fail to resolve.");

            builder.RegisterInstance(_bombPrefab).Keyed("BombPrefab");
            builder.Register<BombSpawner>(Lifetime.Scoped);
        }

        private void ConfigureSceneConfig(IContainerBuilder builder)
        {
            var config = new CharacterSceneConfig
            {
                SpawnPlayerOnStart = true,
                DefaultStartPosition = _startSpawnPoint.position,
                DoorSpawnOffset = 3f
            };

            builder.RegisterInstance(config).As<ICharacterSceneConfig>().AsSelf();
        }

        private void ConfigureEntryPoints(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<PlayerSpawnerService>().AsSelf();
        }

        private void ConfigureBuildCallbacks(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                TryInitialize<CameraSwitcher>(container, nameof(CameraSwitcher), camSwitcher =>
                    camSwitcher.Initialize());

                TryInitialize<CameraFollowObject>(container, nameof(CameraFollowObject), cameraFollowObject =>
                {
                    container.InjectGameObject(cameraFollowObject.gameObject);
                    cameraFollowObject.Initialize();
                });

                if (_cameraFollowObject != null)
                    container.InjectGameObject(_cameraFollowObject.gameObject);

                TryInitialize<CameraTargetManager>(container, nameof(CameraTargetManager), camTarget =>
                {
                    container.InjectGameObject(camTarget.gameObject);
                    camTarget.Initialize();
                });

                TryInitialize<SceneCameraSetup>(container, nameof(SceneCameraSetup), sceneCameraSetup =>
                {
                    container.InjectGameObject(sceneCameraSetup.gameObject);
                    sceneCameraSetup.Initialize();
                });

                TryInitialize<InvisibilityVisualPresenter>(container, nameof(InvisibilityVisualPresenter), presenter =>
                {
                    container.InjectGameObject(presenter.gameObject);
                    presenter.Initialize();
                });

                TryInitialize<CameraEffectController>(container, nameof(CameraEffectController), camEffect =>
                    camEffect.Initialize());

                TryInitialize<CameraPanMover>(container, nameof(CameraPanMover), camPan =>
                    camPan.Initialize());
            });
        }   

        private static void TryInitialize<T>(IObjectResolver container, string typeName, System.Action<T> init)
            where T : class
        {
            try
            {
                var instance = container.Resolve<T>();
                if (instance != null)
                    init(instance);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FacilitySceneScope] Build callback failed for {typeName}. " +
                               $"Entry points may not start correctly.\n{e}");
            }
        }

        #endregion
    }
}   