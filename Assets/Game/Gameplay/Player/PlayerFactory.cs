using UnityEngine;
using VContainer;

namespace Game.Gameplay.Player
{
    public class PlayerFactory : IPlayerFactory
    {
        private readonly IObjectResolver _resolver;
        private readonly GameObject _prefab;

        public PlayerFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
            _prefab = resolver.Resolve<GameObject>("PlayerPrefab");

            if (_prefab == null)
                Debug.LogError("[PlayerFactory] 'PlayerPrefab' resolved to null. Check FacilitySceneLifetimeScope registration.");
        }

        public PlayerStateDriverShell CreatePlayer(Vector3 position, Quaternion rotation)
        {
            if (_prefab == null)
            {
                Debug.LogError("[PlayerFactory] Cannot create player: prefab is null.");
                return null;
            }

            var instance = Object.Instantiate(_prefab, position, rotation);
            _resolver.Inject(instance);

            var shell = instance.GetComponent<PlayerStateDriverShell>();
            if (shell == null)
            {
                Object.Destroy(instance);
                Debug.LogError($"[PlayerFactory] Prefab '{_prefab.name}' does not have a PlayerStateDriverShell component.");
                return null;
            }

            return shell;
        }
    }
}   