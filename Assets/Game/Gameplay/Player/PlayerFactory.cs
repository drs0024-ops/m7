using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Player
{
    public class PlayerFactory : IPlayerFactory
    {
        private readonly IObjectResolver _resolver;
        private readonly PlayerPrefabReference _prefabRef;

        public PlayerFactory(IObjectResolver resolver, PlayerPrefabReference prefabRef)
        {
            _resolver = resolver;
            _prefabRef = prefabRef;
        }

        public PlayerStateDriverShell CreatePlayer(Vector3 position, Quaternion rotation)
        {
            var gameObject = _resolver.Instantiate(_prefabRef.Value, position, rotation);
            return gameObject.GetComponent<PlayerStateDriverShell>();
        }

    }
}   