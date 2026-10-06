using System;
using Game.Core.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Player
{
    public class PlayerFactory : IPlayerFactory
    {
        private readonly IObjectResolver _resolver;
        private readonly GameObject _prefab;
        private readonly Scene _targetScene;
        private readonly IDamagableRegistry _damagableRegistry;

        public PlayerFactory(IObjectResolver resolver, Scene targetScene, IDamagableRegistry damagableRegistry)
        {
            _resolver = resolver;
            _targetScene = targetScene;
            _damagableRegistry = damagableRegistry;
            _prefab = resolver.Resolve<GameObject>("PlayerPrefab");

            if (_prefab == null)
                throw new InvalidOperationException(
                    "[PlayerFactory] 'PlayerPrefab' resolved to null. Check FacilitySceneLifetimeScope serialization.");
        }

        public PlayerStateDriverShell CreatePlayer(Vector3 position, Quaternion rotation)
        {
            if (_prefab == null) return null;

            var instance = UnityEngine.Object.Instantiate(_prefab, position, rotation);
            if (instance.scene != _targetScene)
                SceneManager.MoveGameObjectToScene(instance, _targetScene);

            var shell = instance.GetComponentInChildren<PlayerStateDriverShell>(true);
            if (shell == null)
            {
                UnityEngine.Object.Destroy(instance);
                Debug.LogError($"[PlayerFactory] Prefab '{_prefab.name}' missing PlayerStateDriverShell.");
                return null;
            }

            shell.AlignFeetToGroundPosition(position);
            _resolver.InjectGameObject(instance);

            var playerHealth = instance.GetComponentInChildren<PlayerHealth>(true);
            if (playerHealth != null)
                _damagableRegistry.Register(instance.GetInstanceID(), playerHealth);
            else
                Debug.LogError($"[PlayerFactory] Prefab '{_prefab.name}' has no PlayerHealth IDamagable.", instance);

            // Explicit init for components where VContainer 1.19 silently skips [Inject]:
            var attack = instance.GetComponentInChildren<PlayerAttack>(true);
            if (attack != null)
            {
                var input = _resolver.Resolve<InputManager>();
                attack.Initialize(input);
            }

            var blobShadow = instance.GetComponentInChildren<BlobShadow>(true);
            if (blobShadow != null)
                blobShadow.Initialize();

            return shell;
        }
    }
}   