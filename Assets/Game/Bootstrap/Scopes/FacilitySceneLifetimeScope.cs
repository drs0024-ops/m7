using System;
using System.Collections.Generic;
using Game.Gameplay.Player;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using Game.Gameplay.Camera;
using Game.Gameplay.Upgrades;
using Game.Gameplay.World;
using Unity.Cinemachine;
using Game.Core.Enums;

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

        [Header("Invisibility")]
        [SerializeField] private InvisibilityVisualPresenter _invisibilityVisualPresenter;
        [SerializeField] private InvisibilityPickup _invisibilityPickup;

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
            ConfigurePlayer(builder);
            ConfigureBomb(builder);
            ConfigureSceneConfig(builder);
            ConfigureEntryPoints(builder);
            ConfigureBuildCallbacks(builder);
            ConfigureOrbCounter(builder);

#if UNITY_EDITOR
            builder.RegisterComponentInHierarchy<SceneViewDriverDisabler>();
#endif
        }

        private void ConfigureOrbCounter(IContainerBuilder builder)
        {
            builder.Register<OrbCounter>(Lifetime.Scoped);
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
                builder.RegisterComponent(_checkpointManager);
            else
                builder.RegisterComponentInHierarchy<CheckpointManager>();
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

            var triggers = new List<TriggerInteractionBase>(allTriggers.Length);
            for (int i = 0; i < allTriggers.Length; i++)
            {
                if (allTriggers[i].gameObject.scene == currentScene)
                    triggers.Add(allTriggers[i]);
            }

            builder.RegisterInstance(triggers).As<IReadOnlyList<TriggerInteractionBase>>();

            builder.RegisterBuildCallback(resolver =>
            {
                for (int i = 0; i < triggers.Count; i++)
                    resolver.Inject(triggers[i]);
            });
        }

        private void ConfigureCamera(IContainerBuilder builder)
        {
            builder.RegisterComponent(_sceneCameraSetup);
            builder.RegisterComponent(_cameraFollowObject);

            if (_cameraConfig == null)
                throw new InvalidOperationException(
                    $"[FacilitySceneScope] CameraConfig not assigned on '{gameObject.name}'.");

            builder.RegisterComponent(_cameraConfig);
            builder.RegisterComponent(_impulseSource);

            builder.Register<CameraTargetManager>(Lifetime.Scoped);
            builder.Register<CameraSwitcher>(Lifetime.Scoped);
            builder.RegisterComponentInHierarchy<CameraEffectController>();
            builder.RegisterComponentInHierarchy<CameraPanMover>();
        }

        private void ConfigureInvisibility(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<InvisibilityController>();
            builder.RegisterComponentInHierarchy<InvisibilityInputHandler>();

            // FIX #31: Use the serialized scene component only.
            // Previously this class registered BOTH the serialized field AND a new
            // RegisterComponentInHierarchy, creating a duplicate instance.
            if (_invisibilityVisualPresenter != null)
                builder.RegisterComponent(_invisibilityVisualPresenter);
            else
            {
                Debug.LogWarning($"[FacilitySceneScope] InvisibilityVisualPresenter not assigned. Registering in hierarchy as fallback.");
                builder.RegisterComponentInHierarchy<InvisibilityVisualPresenter>();
            }

            if (_invisibilityPickup != null)
                builder.RegisterComponent(_invisibilityPickup);
            else
                Debug.LogWarning($"[FacilitySceneScope] InvisibilityPickup not assigned on '{gameObject.name}'.");
        }

        private void ConfigurePlayer(IContainerBuilder builder)
        {
            if (_playerPrefab == null)
                throw new InvalidOperationException(
                    $"[FacilitySceneScope] Player prefab not assigned on '{gameObject.name}'.");

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
            builder.RegisterInstance(new CharacterSceneConfig
            {
                SpawnPlayerOnStart = true,
                DefaultStartPosition = _startSpawnPoint.position,
                DoorSpawnOffset = 3f
            });
        }

        private void ConfigureEntryPoints(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<PlayerSpawnerService>().AsSelf();
        }

        private void ConfigureBuildCallbacks(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                container.Resolve<CameraSwitcher>().Initialize();
                container.Resolve<CameraTargetManager>().Initialize();
                container.Resolve<CameraEffectController>().Initialize();
                container.Resolve<CameraPanMover>().Initialize();
            });
        }

        #endregion
    }
}   